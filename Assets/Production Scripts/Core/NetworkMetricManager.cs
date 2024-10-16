using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using Unity.Collections;

namespace Vi.Core
{
    public class NetworkMetricManager : NetworkBehaviour
    {
        private const string MessageName = "MyCustomNamedMessage";

        /// <summary>
        /// For most cases, you want to register once your NetworkBehaviour's
        /// NetworkObject (typically in-scene placed) is spawned.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            // Both the server-host and client(s) register the custom named message.
            NetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(MessageName, ReceiveMessage);

            if (IsServer)
            {
                // Server broadcasts to all clients when a new client connects (just for example purposes)
                NetworkManager.NetworkTickSystem.Tick += Tick;
            }
        }

        private void Tick()
        {
            SendMessage(NetworkManager.NetworkTickSystem.LocalTime.Tick % 128);
        }

        public override void OnNetworkDespawn()
        {
            // De-register when the associated NetworkObject is despawned.
            NetworkManager.CustomMessagingManager.UnregisterNamedMessageHandler(MessageName);

            if (IsServer)
            {
                NetworkManager.NetworkTickSystem.Tick -= Tick;
            }
        }

        /// <summary>
        /// Invoked when a custom message of type <see cref="MessageName"/>
        /// </summary>
        private void ReceiveMessage(ulong senderId, FastBufferReader messagePayload)
        {
            var receivedMessageContent = new ForceNetworkSerializeByMemcpy<int>();
            messagePayload.ReadValueSafe(out receivedMessageContent);
            if (IsServer)
            {
                Debug.Log($"Sever received message ({receivedMessageContent.Value}) from client ({senderId})");
            }
            else
            {
                Debug.Log($"Client received message ({receivedMessageContent.Value}) from the server.");
            }
        }

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
                    customMessagingManager.SendNamedMessageToAll(MessageName, writer, NetworkDelivery.Unreliable);
                }
                else
                {
                    // This is a client or server method that sends a named message to one target destination
                    // (client to server or server to client)
                    customMessagingManager.SendNamedMessage(MessageName, NetworkManager.ServerClientId, writer, NetworkDelivery.Unreliable);
                }
            }
        }
    }
}