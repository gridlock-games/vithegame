using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using ExitGames.Client.Photon;
using NJG.PUN.BulkSync;
using Photon.Pun.UtilityScripts;

namespace NJG.PUN
{
    using System.Collections.Generic;
    using UnityEngine;
    using GameCreator.Core;
    using System;
    using Photon.Pun;
    using Photon.Realtime;

    [AddComponentMenu("")]
    public class NetworkManager : Singleton<NetworkManager>, IMatchmakingCallbacks, IInRoomCallbacks
    {
        private const string RESOURCE_PATH = "Photon/NetworkItemSyncer";
        private const string MSG_ACTION_NOT_FOUND = "ActionsNetwork with id: {0} not found.";

        public const byte EVENT_ACTION = 100;
        

        //private static double startTime = -1;
        /// <summary>
        /// Used in an edge-case when we wanted to set a start time but don't know it yet.
        /// </summary>
        private static bool startRoundWhenTimeIsSynced;
        private float lastTimeCheck;
        //private static PlayerNumbering PN;

        // PROPERTIES: ----------------------------------------------------------------------------

        public static List<System.Action> UpdateCalls = new List<Action>();
        public static List<System.Action> PhotonCalls = new List<Action>();
        private static double clientStartTime;
        private float lastLagCheck;
        private bool skipLagCheck;
        private int errors;
        private float lastPingCheck;
        private float timeAsMaster;

        private const float LAG_CHECK_DELAY = 5;

        /// <summary>
        /// Returns the time this client has been in the room. In Seconds.
        /// </summary>
        public static double CurrentClientTime { get { return PhotonNetwork.Time - clientStartTime; } }

        public DatabasePhoton DB { get; private set; }

        /// <summary>
        /// Cached list of PhotonNetwork.PlayerList.
        /// </summary>
        public Player[] Players 
        {   
            get
            {
                if (players.Length == 0)
                {
                    players = PhotonNetwork.PlayerList;
                }
                return players;
            }
        }

        private Player[] players = new Player[0];

        private bool CanSwitch { get { return DB.switchMasterClient && PhotonNetwork.IsMasterClient && Time.timeSinceLevelLoad > 60 && Time.time > lastLagCheck && !skipLagCheck && timeAsMaster >= 60; } }
        public Player LastJoinedPlayer { get; set; }

        private static readonly List<string> IGNORE_ERRORS = new List<string>(2) { "AnimationEvent", "missing a component" };


        // INITIALIZERS: --------------------------------------------------------------------------

        private void Start()
        {
            if (Application.isPlaying)
            {
                if (PhotonNetwork.NetworkingClient != null) PhotonNetwork.AddCallbackTarget(this);
                Application.logMessageReceived += LogReceived;

                DB = DatabasePhoton.Load();

                lastLagCheck = Time.time + DB.lagCheck * LAG_CHECK_DELAY;
                gameObject.AddComponent<PlayerNumbering>();
            }
        }

        private void LogReceived(string condition, string stackTrace, LogType type)
        {
            if(PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient && DB && IsValidError(condition, stackTrace))
            {
                if (type == LogType.Error || type == LogType.Assert)
                {
                    errors++;
                }

                if (type == LogType.Exception || errors >= DB.switchMasterErrors)
                {
                    TrySwitchMasterClient();
                }
            }
        }

        private bool IsValidError(string condition, string stackTrace)
        {
            for(int i = 0, imax = IGNORE_ERRORS.Count; i<imax; i++)
            {
                if (condition.Contains(IGNORE_ERRORS[i]) || stackTrace.Contains(IGNORE_ERRORS[i])) return false;
            }
            return true;
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                PhotonNetwork.RemoveCallbackTarget(this);
                Application.logMessageReceived -= LogReceived;
            }
        }

        private void Update()
        {
            if (PhotonNetwork.InRoom)
            {
                if (PhotonNetwork.IsMasterClient) timeAsMaster += Time.deltaTime;

                if (startRoundWhenTimeIsSynced)
                {
                    SetStartTime();   // the "time is known" check is done inside the method.
                }
                
                if (Time.time > lastTimeCheck)
                {
                    lastTimeCheck = Time.time + 1f;
                    for(int i = 0, imax = UpdateCalls.Count; i<imax; i++)
                    {
                        UpdateCalls[i]();
                    }
                }

                if (DB)
                {
                    if (DB.updatePing && Time.time > lastPingCheck)
                    {
                        lastPingCheck = Time.time + DB.updatePingEvery;
                        PhotonNetwork.LocalPlayer.SetPing();
                    }

                    if (CanSwitch)
                    {
                        lastLagCheck = Time.time + DB.lagCheck;

                        int p = PhotonNetwork.GetPing();
                        if (p > DB.lagThreshold)
                        {
                            TrySwitchMasterClient();
                        }
                    }
                }
            }
        }

        private void TrySwitchMasterClient()
        {
            if (!DB || Players == null || Players.Length == 1) return;

            int p = PhotonNetwork.GetPing();
            if (Players.Length > 1 && (p > DB.lagThreshold || errors >= DB.switchMasterErrors))
            {
                Player n = GetNextMaster();
                if (Equals(n, PhotonNetwork.LocalPlayer))
                {
                    lastLagCheck = Time.time + DB.lagCheck * LAG_CHECK_DELAY;
                    //continue;
                }
                else
                {
                    if(!Equals(n, PhotonNetwork.MasterClient)) PhotonNetwork.SetMasterClient(n);
                    skipLagCheck = true;
                }
            }
        }

        /// <summary>
        /// Returns the next best Master based on the Photon Player ping.
        /// </summary>
        /// <returns></returns>
        private Player GetNextMaster()
        {
            Array.Sort<Player>(Players, (x, y) => x.GetPing().CompareTo(y.GetPing()));
            //List<Player> players = PhotonNetwork.PlayerList.ToList();
            //players.Sort((x, y) => x.GetPing().CompareTo(y.GetPing()));

            /*for (int i = 0, imax = players.Count; i < imax; i++)
            {
                Debug.Log("players " + players[i] + " / " + players[i].GetInt(pingProperty));
            }*/

            return Players[0];
        }

        protected override void OnCreate()
        {
            //DontDestroyOnLoad(gameObject);
            //PhotonNetwork.OnEventCall += OnPhotonEvent;
            /*if (PN == null)
            {
                PN = gameObject.GetComponent<PlayerNumbering>();
                if (PN == null) PN = gameObject.AddComponent<PlayerNumbering>();
            }*/
        }

        private void OnPhotonEvent(byte eventCode, object content, int senderId)
        {
            Debug.Log("NetworkManager OnPhotonEvent eventCode: "+ eventCode+" / senderId: "+senderId+" / content: "+content);
            /*switch (eventCode)
            {
                case EVENT_ACTION:
                    string uuid = content.ToString();
                    if (ActionsNetwork.REGISTER.ContainsKey(uuid))
                    {
                        ActionsNetwork.REGISTER[uuid].Execute(senderId);
                    }
                    else
                    {
                        Debug.LogWarningFormat(MSG_ACTION_NOT_FOUND, uuid);
                    }
                    break;
            }*/
        }

        // PUBLIC METHODS: ------------------------------------------------------------------------

        public static void SetStartTime()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            // in some cases, when you enter a room, the server time is not available immediately.
            // time should be 0.0f but to make sure we detect it correctly, check for a very low value.
            if (PhotonNetwork.Time < 0.0001f)
            {
                // we can only start the round when the time is available. let's check that in Update()
                startRoundWhenTimeIsSynced = true;
                return;
            }
            startRoundWhenTimeIsSynced = false;
            
            PhotonNetwork.CurrentRoom.SetStartTime(PhotonNetwork.Time);
        }

        public void Wakeup()
        {
            base.WakeUp();
        }

        private void UpdatePhotonCalls()
        {
            for (int i = 0, imax = PhotonCalls.Count; i < imax; i++)
            {
                PhotonCalls[i]();
            }
        }
        
        public void OnFriendListUpdate(List<FriendInfo> friendList)
        {
            UpdatePhotonCalls();
        }

        public void OnCreatedRoom()
        {
            lastLagCheck = Time.time + DB.lagCheck * LAG_CHECK_DELAY;
            UpdatePhotonCalls();
        }

        public void OnCreateRoomFailed(short returnCode, string message)
        {
            UpdatePhotonCalls();
        }

        public void OnJoinedRoom()
        {
            LastJoinedPlayer = PhotonNetwork.LocalPlayer;

            players = PhotonNetwork.PlayerList;
            clientStartTime = PhotonNetwork.Time;
            lastLagCheck = Time.time + DB.lagCheck * LAG_CHECK_DELAY;
            UpdatePhotonCalls();
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
            UpdatePhotonCalls();
        }

        public void OnJoinRandomFailed(short returnCode, string message)
        {
            UpdatePhotonCalls();
        }

        public void OnLeftRoom()
        {
            startRoundWhenTimeIsSynced = false;
            UpdatePhotonCalls();
        }

        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            LastJoinedPlayer = newPlayer;
            players = PhotonNetwork.PlayerList;
            UpdatePhotonCalls();
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            players = PhotonNetwork.PlayerList;
            UpdatePhotonCalls();
        }

        public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            if (propertiesThatChanged.ContainsKey(PlayerProperties.PING)) return;

            UpdatePhotonCalls();
        }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (changedProps.ContainsKey(PlayerProperties.PING)) return;

            UpdatePhotonCalls();
        }

        public void OnMasterClientSwitched(Player newMasterClient)
        {
            lastLagCheck = Time.time + DB.lagCheck * LAG_CHECK_DELAY;
            timeAsMaster = 0;
            //Debug.LogWarning("OnMasterClientSwitched: " + newMasterClient);
            skipLagCheck = false;
            UpdatePhotonCalls();
        }
    }
}
