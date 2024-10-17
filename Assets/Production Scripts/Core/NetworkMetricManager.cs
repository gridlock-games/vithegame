using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

namespace Vi.Core
{
    public class NetworkMetricManager : NetworkBehaviour
    {
        private const string messageName = "PacketLoss";

        public static NetworkMetricManager Singleton { get { return _singleton; } }

        private static NetworkMetricManager _singleton;

        /// <summary>
        /// For most cases, you want to register once your NetworkBehaviour's
        /// NetworkObject (typically in-scene placed) is spawned.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            _singleton = this;

            // Both the server-host and client(s) register the custom named message.
            NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(messageName, ReceiveMessage);

            if (IsServer)
            {
                // Server broadcasts to all clients when a new client connects (just for example purposes)
                StartCoroutine(SendPacket());
            }
        }

        private int localPacketID;
        private const int numPacketsToSend = 32;
        private IEnumerator SendPacket()
        {
            while (true)
            {
                SendMessage(localPacketID);
                yield return new WaitForSeconds(0.25f);
                localPacketID++;
                if (localPacketID == numPacketsToSend) { localPacketID = 0; }
            }
        }

        public override void OnNetworkDespawn()
        {
            _singleton = null;

            // De-register when the associated NetworkObject is despawned.
            NetworkManager.CustomMessagingManager.UnregisterNamedMessageHandler(messageName);

            PacketLoss = 0;
        }

        public float PacketLoss { get; private set; }

        private bool firstPacketRecieved = true;
        /// <summary>
        /// Invoked when a custom message of type <see cref="messageName"/>
        /// </summary>
        private void ReceiveMessage(ulong senderId, FastBufferReader messagePayload)
        {
            var receivedMessageContent = new ForceNetworkSerializeByMemcpy<int>();
            messagePayload.ReadValueSafe(out receivedMessageContent);
            if (IsServer)
            {
                //Debug.Log($"Sever received message ({receivedMessageContent.Value}) from client ({senderId})");
            }
            else
            {
                //Debug.Log($"Client received message ({receivedMessageContent.Value}) from the server.");

                if (firstPacketRecieved)
                {
                    for (int i = 0; i < receivedMessageContent.Value; i++)
                    {
                        packetIDsRecieved.Add(i);
                    }
                    firstPacketRecieved = false;
                }

                if (receivedMessageContent.Value < localPacketID)
                {
                    localPacketID = 0;
                    packetIDsRecieved.Clear();
                }
                packetIDsRecieved.Add(receivedMessageContent.Value);
                localPacketID = receivedMessageContent.Value;

                if (receivedMessageContent.Value > numPacketsToSend / 2) { PacketLoss = 1 - (receivedMessageContent.Value == 0 ? 1 : ((packetIDsRecieved.Count - 1) / (float)receivedMessageContent.Value)); }
                //Debug.Log(packetIDsRecieved.Count - 1 + " " + receivedMessageContent.Value + " " + PacketLoss);
            }
        }

        private List<int> packetIDsRecieved = new List<int>();

        /// <summary>
        /// Invoke this with a Guid by a client or server-host to send a
        /// custom named message.
        /// </summary>
        public void SendMessage(int sequenceNumber)
        {
            var messageContent = new ForceNetworkSerializeByMemcpy<int>(sequenceNumber);
            var writer = new FastBufferWriter(4, Allocator.Temp);
            var customMessagingManager = NetworkManager.CustomMessagingManager;
            using (writer)
            {
                writer.WriteValueSafe(messageContent);
                if (IsServer)
                {
                    // This is a server-only method that will broadcast the named message.
                    // Caution: Invoking this method on a client will throw an exception!
                    customMessagingManager.SendNamedMessageToAll(messageName, writer, NetworkDelivery.UnreliableSequenced);
                }
                else
                {
                    // This is a client or server method that sends a named message to one target destination
                    // (client to server or server to client)
                    customMessagingManager.SendNamedMessage(messageName, NetworkManager.ServerClientId, writer, NetworkDelivery.UnreliableSequenced);
                }
            }
        }
    }
}