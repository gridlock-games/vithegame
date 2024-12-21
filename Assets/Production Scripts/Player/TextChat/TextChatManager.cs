using UnityEngine;
using Vi.Core;
using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;

namespace Vi.Player.TextChat
{
    public class TextChatManager : NetworkBehaviour
    {
        public static bool DoesExist() { return _singleton; }

        public static TextChatManager Singleton
        {
            get
            {
                if (!_singleton) { Debug.LogError("Text Chat Manager is null"); }
                return _singleton;
            }
        }

        private static TextChatManager _singleton;

        private void Awake()
        {
            _singleton = this;
        }

        public struct TextChatElement : INetworkSerializable
        {
            public ulong senderClientId;
            public FixedString64Bytes username;
            public PlayerDataManager.Team userTeam;
            public FixedString64Bytes content;

            private const string saidColor = "7D7D7D";

            public TextChatElement(ulong senderClientId, string username, PlayerDataManager.Team userTeam, string content)
            {
                this.senderClientId = senderClientId;
                this.username = username ?? "";
                this.userTeam = userTeam;
                this.content = content ?? "";
            }

            public string GetMessageUIValue()
            {
                return "<b><color=#" + ColorUtility.ToHtmlStringRGBA(NetworkManager.Singleton.LocalClientId == senderClientId ? PlayerDataManager.Singleton.LocalPlayerColor : PlayerDataManager.Singleton.GetRelativeTeamColor(userTeam))
                    + ">" + username + "</color></b> " + "<color=#" + saidColor + ">said:</color> " + content;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref senderClientId);
                serializer.SerializeValue(ref username);
                serializer.SerializeValue(ref userTeam);
                serializer.SerializeValue(ref content);
            }
        }

        public void SendTextChat(string username, PlayerDataManager.Team userTeam, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) { return; }
            TextChatElement textChatElement = new TextChatElement(NetworkManager.LocalClientId, username, userTeam, content);
            SendTextChatRpc(textChatElement);
        }

        [Rpc(SendTo.Everyone)]
        private void SendTextChatRpc(TextChatElement textChatElement)
        {
            foreach (TextChat textChatInstance in textChatInstances)
            {
                textChatInstance.DisplayNextTextElement(textChatElement);
            }
        }

        private const string connectionMessageColor = "008000";
        private const string disconnectionMessageColor = "902A13";

        private List<TextChat> textChatInstances = new List<TextChat>();
        public void RegisterTextChatInstance(TextChat textChat)
        {
            textChatInstances.Add(textChat);
        }

        public void UnregisterTextChatInstance(TextChat textChat)
        {
            textChatInstances.Remove(textChat);
        }

        public override void OnNetworkSpawn()
        {
            string connectionMessage = "<color=#" + connectionMessageColor + ">" + PlayerDataManager.Singleton.GetPlayerData((int)OwnerClientId).character.name.ToString() + " has connected." + "</color>";
            foreach (TextChat textChatInstance in textChatInstances)
            {
                textChatInstance.DisplayConnectionMessage(connectionMessage);
            }
        }

        public override void OnNetworkDespawn()
        {
            string connectionMessage = "<color=#" + disconnectionMessageColor + ">" + PlayerDataManager.Singleton.GetPlayerData((int)OwnerClientId).character.name.ToString() + " has disconnected." + "</color>";
            foreach (TextChat textChatInstance in textChatInstances)
            {
                textChatInstance.DisplayConnectionMessage(connectionMessage);
            }
        }
    }
}