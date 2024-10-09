using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Unity.Netcode.Examples.PingTool
{
    public class PingTool : NetworkBehaviour
    {
        #region PUBLIC PROPERTIES
        /// Note: Sending anything beyond 100 pings per second (100HZ) is only going to congest the 
        /// reliable pipeline. If the client is locked at a fixed frame rate (i.e.60HZ) then it will
        /// send 1 ping per frame and will wait for 100 frames to reconcile the values and populate
        /// each client's <see cref="ClientRtt"/> entry.
        [Tooltip("The number of pings that will be sent per second.")]
        [Range(1, 100)]
        public int PingRate = 30;

        public bool EnableConsoleLogging = true;

        public UnityEvent<ulong, ClientRtt> LogMessage;

        /// <summary>
        /// Used to hold the averaged values of:
        /// Ping: This is the average time it takes to receive a PingRpc from the client
        /// RTT: This is the average UnityTransport round trip time value
        /// </summary>
        public struct ClientRtt
        {
            public float Ping;
            public float RTT;
        }
        #endregion

        #region PRIVATE PROPERTIES
        private struct ClientPingEntry
        {
            public int ReceivedPongs;
            public float PingTime;
            public ulong UTP_RTT;

            public void Reset()
            {
                ReceivedPongs = 0;
                PingTime = 0.0f;
                UTP_RTT = 0;
            }
        }
        private NetworkVariable<int> m_PingRate = new NetworkVariable<int>();
        private UnityTransport m_UnityTransport;
        private Dictionary<ulong, ClientPingEntry> m_ClientPingQueue = new Dictionary<ulong, ClientPingEntry>();
        private Dictionary<ulong, ClientRtt> m_ClientRtt = new Dictionary<ulong, ClientRtt>();
        private Dictionary<ulong, float> m_NextClientPingTime = new Dictionary<ulong, float>();
        private Dictionary<ulong, Coroutine> m_ClientUpdateRoutines = new Dictionary<ulong, Coroutine>();
        private Slider m_Slider;
        private Text m_PingRateValue;
        private Canvas m_PingUICanvas;
        private bool m_PingRateUpdated;
        private float m_LastUpdatedTime;
        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// Reutrns the current reconciled client's ping and RTT values.
        /// </summary>
        public ClientRtt GetClientRtt(ulong clientID)
        {
            if (m_ClientRtt.ContainsKey(clientID))
            {
                return m_ClientRtt[clientID];
            }
            return new ClientRtt()
            {
                Ping = -1.0f,
                RTT = -1.0f,
            };
        }
        #endregion

        #region NETWORKBEHAVIOUR AND NETCODE EVENTS
        private void Awake()
        {
            m_Slider = GetComponentInChildren<Slider>();
            m_PingRateValue = GetComponentInChildren<Text>();
            m_PingUICanvas = GetComponentInChildren<Canvas>();
            m_PingUICanvas.gameObject.SetActive(false);
        }

        protected override void OnNetworkPreSpawn(ref NetworkManager networkManager)
        {
            m_UnityTransport = NetworkManager.NetworkConfig.NetworkTransport as UnityTransport;
            base.OnNetworkPreSpawn(ref networkManager);
        }

        private void InitializeAllClients()
        {
            // Register to get the UTP RTT to the server or service and to get frame times
            // Server does not need to update its RTT to itself
            if (!NetworkManager.IsServer)
            {
                InitializePingClientTime(NetworkManager.LocalClientId);
            }

            foreach (var clientId in NetworkManager.ConnectedClientsIds)
            {
                // Clients or the server ignores their entry
                if (clientId == NetworkManager.LocalClientId)
                {
                    continue;
                }
                InitializePingClientTime(clientId);
            }
        }

        public override void OnNetworkSpawn()
        {
            m_PingUICanvas.gameObject.SetActive(true);
            m_Slider.gameObject.SetActive(IsOwner);

            if (IsOwner)
            {
                m_PingUICanvas.gameObject.SetActive(true);
                m_Slider.onValueChanged.AddListener(delegate { OnPingRateChanged(); });
                m_PingRate.Value = PingRate;
                m_Slider.value = PingRate;

                InitializeAllClients();
                NetworkManager.OnConnectionEvent += OnConnectionEvent;
                // Start the PingRate change monitor that avoids sending a bunch
                // of updates until the user is finished selecting their value
                StartCoroutine(OwnerMonitorPingRateChange());
            }
            else
            {
                m_PingRate.OnValueChanged += OnPingRateChanged;
                InitializeAllClients();
                NetworkManager.OnConnectionEvent += OnConnectionEvent;
            }

            m_PingRateValue.text = $"{m_PingRate.Value}";
            base.OnNetworkSpawn();
        }

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            if (current == NetworkManager.LocalClientId)
            {
                m_Slider.gameObject.SetActive(true);
                m_Slider.onValueChanged.AddListener(delegate { OnPingRateChanged(); });
                m_Slider.value = m_PingRate.Value;
                StartCoroutine(OwnerMonitorPingRateChange());
            }
            else if (previous == NetworkManager.LocalClientId)
            {
                m_Slider.gameObject.SetActive(false);
                m_Slider.onValueChanged.RemoveAllListeners();
            }

            base.OnOwnershipChanged(previous, current);
        }

        private void OnPingRateChanged(int previous, int current)
        {
            InitializePingClientTime(OwnerClientId);
            m_PingRateValue.text = $"{current}";
        }

        private bool HasAuthority { get { return NetworkManager.Singleton.IsServer; } }

        public override void OnNetworkDespawn()
        {
            m_PingRate.OnValueChanged -= OnPingRateChanged;
            NetworkManager.OnConnectionEvent -= OnConnectionEvent;
            m_PingUICanvas.gameObject.SetActive(false);
            if (HasAuthority)
            {
                m_Slider.onValueChanged.RemoveAllListeners();
            }

            foreach (var clientEntry in m_ClientRtt)
            {
                if (m_ClientUpdateRoutines.ContainsKey(clientEntry.Key))
                {
                    StopCoroutine(m_ClientUpdateRoutines[clientEntry.Key]);
                }
            }

            m_ClientUpdateRoutines.Clear();
            m_ClientPingQueue.Clear();
            m_ClientRtt.Clear();
            m_NextClientPingTime.Clear();
            base.OnNetworkDespawn();
        }

        private void OnConnectionEvent(NetworkManager networkManager, ConnectionEventData eventData)
        {
            switch (eventData.EventType)
            {
                case ConnectionEvent.ClientDisconnected:
                    {
                        if (m_ClientUpdateRoutines.ContainsKey(eventData.ClientId))
                        {
                            StopCoroutine(m_ClientUpdateRoutines[eventData.ClientId]);
                            m_ClientUpdateRoutines.Remove(eventData.ClientId);
                        }
                        m_ClientPingQueue.Remove(eventData.ClientId);
                        m_ClientRtt.Remove(eventData.ClientId);
                        m_NextClientPingTime.Remove(eventData.ClientId);
                        break;
                    }
                case ConnectionEvent.ClientConnected:
                    {
                        if (eventData.ClientId == NetworkManager.LocalClientId || m_ClientPingQueue.ContainsKey(eventData.ClientId))
                        {
                            break;
                        }
                        else
                        {
                            InitializePingClientTime(eventData.ClientId);
                        }
                        break;
                    }
                case ConnectionEvent.PeerConnected:
                    {
                        if (eventData.ClientId == NetworkManager.LocalClientId || m_ClientPingQueue.ContainsKey(eventData.ClientId))
                        {
                            break;
                        }
                        else
                        {
                            InitializePingClientTime(eventData.ClientId);
                        }

                        break;
                    }
                case ConnectionEvent.PeerDisconnected:
                    {
                        if (m_ClientUpdateRoutines.ContainsKey(eventData.ClientId))
                        {
                            StopCoroutine(m_ClientUpdateRoutines[eventData.ClientId]);
                            m_ClientUpdateRoutines.Remove(eventData.ClientId);
                        }
                        m_ClientPingQueue.Remove(eventData.ClientId);
                        m_ClientRtt.Remove(eventData.ClientId);
                        m_NextClientPingTime.Remove(eventData.ClientId);
                        break;
                    }
            }
        }

        private void OnPingRateChanged()
        {
            if (!IsSpawned || !HasAuthority)
            {
                return;
            }

            if (m_Slider.value != PingRate)
            {
                PingRate = (int)m_Slider.value;
                m_PingRateValue.text = $"{PingRate}";
                // Flag the last time this value updated for the OwnerMonitorPingRateChange coroutine
                m_LastUpdatedTime = Time.realtimeSinceStartup;
                m_PingRateUpdated = true;
            }
        }

        /// <summary>
        /// Helper coroutine to prevent from sending every micro-adjustment to 
        /// the ping rate value. Once the user has stopped changing the slider
        /// then the clients will be notified of the change and the local authority
        /// will reset its inbound client queues.
        /// </summary>
        private IEnumerator OwnerMonitorPingRateChange()
        {
            var waitForOneSecond = new WaitForSeconds(1.0f);
            var changeWaitPeriod = new WaitForSeconds(0.1f);
            var continueToMonitor = true;
            var networkManager = NetworkManager;

            while (continueToMonitor)
            {
                if (!m_PingRateUpdated)
                {
                    yield return waitForOneSecond;
                }
                else
                {
                    yield return changeWaitPeriod;
                }


                // Terminate if shutting down, the local client is disconnected, or the targeted client is no longer connected
                continueToMonitor = !networkManager.ShutdownInProgress && networkManager.IsConnectedClient && IsSpawned && IsOwner;
                if (!continueToMonitor)
                {
                    continue;
                }

                if (!m_PingRateUpdated)
                {
                    continue;
                }

                // Just continue to wait for any slider changes to stop before
                // triggering an update to non-authority instances and resetting
                // the inboudn client queue
                if ((m_LastUpdatedTime + 0.25f) > Time.realtimeSinceStartup)
                {
                    continue;
                }

                // Unset this flag
                m_PingRateUpdated = false;

                // If the user slide the slider around but landed back to the same
                // value we were already on, then just ignore this change update
                if (m_PingRate.Value == PingRate)
                {
                    continue;
                }

                // Update remote clients
                m_PingRate.Value = PingRate;

                // Reset all of the local inbound client queues
                foreach (var clientId in NetworkManager.ConnectedClientsIds)
                {
                    if (clientId == OwnerClientId)
                    {
                        continue;
                    }
                    InitializePingClientTime(clientId);
                }
            }
            yield break;
        }
        #endregion

        #region INBOUND CLIENT INITIALIZATION
        /// <summary>
        /// Initializes or resets a client's ping and RTT data
        /// </summary>
        private void InitializePingClientTime(ulong clientId)
        {
            if (m_ClientUpdateRoutines.ContainsKey(clientId))
            {
                StopCoroutine(m_ClientUpdateRoutines[clientId]);

                m_ClientUpdateRoutines.Remove(clientId);
            }
            if (!m_NextClientPingTime.ContainsKey(clientId))
            {
                m_NextClientPingTime.Add(clientId, 0);
            }
            m_NextClientPingTime[clientId] = Time.realtimeSinceStartup;

            m_ClientUpdateRoutines.Add(clientId, StartCoroutine(UpdateClientRoutine(clientId)));
        }

        private void InitializeClient(ulong clientId, bool hasEntry)
        {
            if (!hasEntry)
            {
                m_ClientPingQueue.Add(clientId, new ClientPingEntry());
            }
            else
            {
                m_ClientPingQueue[clientId].Reset();
            }
        }
        #endregion

        #region INBOUND PING PROCESSING
        [Rpc(SendTo.SpecifiedInParams)]
        private void PingRpc(float timeSent, RpcParams rpcParams)
        {
            PongRpc(Mathf.Abs(NetworkManager.ServerTime.TimeAsFloat - timeSent), RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void PongRpc(float timeDelta, RpcParams rpcParams)
        {
            UpdateClientRTT(rpcParams.Receive.SenderClientId, timeDelta);
        }

        private void UpdateClientRTT(ulong clientId, float timeDelta)
        {
            var clientHasEntry = m_ClientPingQueue.ContainsKey(clientId);
            // Handle creating a new entry or reconfiguring the client's queue size (in the event PingRate has changed)
            if (!clientHasEntry)
            {
                InitializeClient(clientId, clientHasEntry);
            }
            var currentRTT = m_UnityTransport.GetCurrentRtt(!NetworkManager.IsServer ? NetworkManager.ServerClientId : clientId);
            var latencyToTimeServer = 0.0f;

            if (!NetworkManager.IsServer)
            {
                latencyToTimeServer = m_UnityTransport.GetCurrentRtt(NetworkManager.ServerClientId);
            }

            var pingEntry = m_ClientPingQueue[clientId];
            var ping = timeDelta - (latencyToTimeServer * 0.001f);
            pingEntry.PingTime += ping;
            pingEntry.UTP_RTT += currentRTT;
            pingEntry.ReceivedPongs++;
            m_ClientPingQueue[clientId] = pingEntry;
        }
        #endregion

        #region CLIENT COROUTINE AND RTT PROCESSING
        /// <summary>
        /// Reconciles a clients inbound ping queue
        /// </summary>
        private void ReconcileClient(ulong clientId, float pingFrequency)
        {
            if (!m_ClientPingQueue.ContainsKey(clientId))
            {
                return;
            }
            if (!m_ClientRtt.ContainsKey(clientId))
            {
                m_ClientRtt.Add(clientId, new ClientRtt());
            }
            var pingEntry = m_ClientPingQueue[clientId];
            var clientRTT = m_ClientRtt[clientId];
            clientRTT.Ping = pingEntry.PingTime / pingEntry.ReceivedPongs;
            clientRTT.RTT = (float)pingEntry.UTP_RTT / pingEntry.ReceivedPongs;
            m_ClientRtt[clientId] = clientRTT;
            pingEntry.Reset();
            m_ClientPingQueue[clientId] = pingEntry;
        }

        private IEnumerator UpdateClientRoutine(ulong clientId)
        {
            var pingFrequency = 1.0f / m_PingRate.Value;
            var pingFrequencyWait = new WaitForSeconds(pingFrequency);
            var clientToReconcile = clientId;
            var continueToPingAndReconcile = true;
            var networkManager = NetworkManager;
            var nextReconcileTime = Time.realtimeSinceStartup + 1.0f;
            // the queueSize is always 1 larger than the ping rate to account for there always being a tail in the queue.
            var queueSize = PingRate + 1;
            var clientHasEntry = m_ClientPingQueue.ContainsKey(clientId);
            // Handle creating a new entry or reconfiguring the client's queue size (in the event PingRate has changed)
            if (!clientHasEntry)
            {
                InitializeClient(clientId, clientHasEntry);
            }

            while (continueToPingAndReconcile)
            {
                // Clients only need to get the RTT to the server or service
                if (!networkManager.IsServer && clientId == networkManager.LocalClientId)
                {
                    UpdateClientRTT(clientToReconcile, 0.0f);
                }
                else
                {
                    // Send a ping to this client so it can reconcile on its end
                    PingRpc(networkManager.ServerTime.TimeAsFloat, RpcTarget.Single(clientId, RpcTargetUse.Temp));
                }
                yield return pingFrequencyWait;

                // Only reconcile every second
                if (nextReconcileTime > Time.realtimeSinceStartup)
                {
                    continue;
                }

                // Terminate if shutting down, the local client is disconnected, or the targeted client is no longer connected
                continueToPingAndReconcile = !networkManager.ShutdownInProgress && networkManager.IsConnectedClient && networkManager.ConnectedClientsIds.Contains(clientToReconcile);
                if (!continueToPingAndReconcile)
                {
                    continue;
                }

                nextReconcileTime = Time.realtimeSinceStartup + 1.0f;

                ReconcileClient(clientToReconcile, pingFrequency);
                if (EnableConsoleLogging)
                {
                    var clientRtt = GetClientRtt(clientToReconcile);
                    LogMessage?.Invoke(clientToReconcile, GetClientRtt(clientToReconcile));
                }
            }
            yield break;
        }
        #endregion
    }
}