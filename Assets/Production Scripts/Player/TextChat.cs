using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Core;
using Unity.Collections;

namespace Vi.Player
{
    public class TextChat : NetworkBehaviour
    {
        [HideInInspector] public List<TextChatElement> textChatElements = new List<TextChatElement>();

        public struct TextChatElement : INetworkSerializable
        {
            public FixedString32Bytes username;
            public PlayerDataManager.Team userTeam;
            public FixedString32Bytes content;

            public TextChatElement(string username, PlayerDataManager.Team userTeam, string content)
            {
                this.username = username;
                this.userTeam = userTeam;
                this.content = content;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref username);
                serializer.SerializeValue(ref userTeam);
                serializer.SerializeValue(ref content);
            }
        }

        [Rpc(SendTo.Server)]
        private void SendTextChatServerRpc(TextChatElement textChatElement)
        {
            textChatElements.Add(textChatElement);

            SendTextChatClientRpc(textChatElement);
        }

        [Rpc(SendTo.NotServer)]
        private void SendTextChatClientRpc(TextChatElement textChatElement)
        {
            textChatElements.Add(textChatElement);
        }
    }
}