using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Core;
using Unity.Collections;

namespace Vi.UI
{
    public class TextChat : NetworkBehaviour
    {
        public struct TextChatElement : INetworkSerializable
        {
            public FixedString32Bytes username;
            public PlayerDataManager.Team userTeam;
            public FixedString32Bytes content;

            private const string saidColor = "7D7D7D";

            public TextChatElement(string username, PlayerDataManager.Team userTeam, string content)
            {
                this.username = username ?? "";
                this.userTeam = userTeam;
                this.content = content ?? "";
            }

            public string GetMessageUIValue()
            {
                return "<b><color=#" + ColorUtility.ToHtmlStringRGBA(PlayerDataManager.GetTeamTextChatColor(userTeam)) + ">" + username + "</color></b> " + "<color=#" + saidColor + ">said:</color> " + content;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref username);
                serializer.SerializeValue(ref userTeam);
                serializer.SerializeValue(ref content);
            }
        }

        public void SendTextChat(string username, PlayerDataManager.Team userTeam, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) { return; }
            TextChatElement textChatElement = new TextChatElement(username, userTeam, content);
            SendTextChatRpc(textChatElement);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SendTextChatRpc(TextChatElement textChatElement)
        {
            if (!playerUI) { playerUI = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<PlayerUI>(); }

            if (playerUI) { playerUI.DisplayNextTextElement(textChatElement); }
        }

        private PlayerUI playerUI;
        public override void OnNetworkSpawn()
        {
            if (IsLocalPlayer)
            {
                playerUI = GetComponentInChildren<PlayerUI>();
            }
        }
    }
}