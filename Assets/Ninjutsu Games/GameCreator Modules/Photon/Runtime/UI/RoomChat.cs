using System.Collections.Generic;
using ExitGames.Client.Photon;
using GameCreator.Core;
using GameCreator.Localization;
using GameCreator.Messages;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using UnityEngine;

namespace NJG.PUN.UI
{

    /// <summary>
    /// Networked chat logic. Takes care of sending and receiving of chat messages.
    /// </summary>

    public class RoomChat : Chat, IOnEventCallback, IInRoomCallbacks, IMatchmakingCallbacks
    {
        public static RoomChat Instance => mInst;
        private static RoomChat mInst;

        public Color playerColor = new Color(0.6f, 1.0f, 0f);
        // public Color otherColor = new Color(0.6f, 1.0f, 0f);
        public Gradient otherColor = new Gradient();
        public Color serverColor = new Color(0.6f, 1.0f, 0f);
        /*public string factionAName = "Blue Team";
        public string factionBName = "Red Team";
        public string factionCName = "Red Team";
        public string factionColorProperty = "characterColor";

        private ActorFaction factionA;
        private ActorFaction factionB;
        private ActorFaction factionC;
        private Color factionAColor;
        private Color factionBColor;
        private Color factionCColor;*/

        public int chatEventCode;
        /*{
            get
            {
                if (evCode == -1) evCode = PhotonRaiseEventAsset.Instance.GetDefinition("ChatEvent") == null ? 0 : PhotonRaiseEventAsset.Instance.GetDefinition("ChatEvent").eventCode;
                return evCode;
            }
        }*/

        /// <summary>
        /// If you want the chat window to only be shown in multiplayer games, set this to 'true'.
        /// </summary>
        public bool destroyIfOffline = false;
        
        [Header("Floating Message")] public bool enableFloatingMessage = true;
        public Vector3 floatingMessageOffset = new Vector3(0, 2, 0);
        public float floatingMessageTime = 2;
        public Color floatingMessageColor = Color.white;

        [Header("Notification Messages")] 
        public bool notifyWhenPlayersConnect = true;
        [LocStringNoPostProcess]
        public LocString youJoinedMessage = new LocString("Joined just joined room {0}.");
        [LocStringNoPostProcess]
        public LocString playerJoinedMessage = new LocString("{0} Joined!");
        [LocStringNoPostProcess]
        public LocString playerLeftMessage = new LocString("{0} Left!");

        //private int evCode = -1;
        private const string S_SPLIT = "|s|";
        private Color[] colors;


        protected override void Awake()
        {
            base.Awake();
            
            colors = new Color[100];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = otherColor.Evaluate(Random.Range(0f, 1f));
            }

            mInst = this;
        }

        /// <summary>
        /// We want to listen to input field's events.
        /// </summary>

        private void Start()
        {
            if (destroyIfOffline && !PhotonNetwork.InRoom || PhotonNetwork.OfflineMode)
            {
                Destroy(gameObject);
                return;
            }
            if (PhotonNetwork.NetworkingClient != null) PhotonNetwork.AddCallbackTarget(this);
            
            

            /*factionA = ActorFactionManager.Instance.GetRuntimeByIdent(factionAName, plyGameObjectIdentifyingType.screenName);
            factionB = ActorFactionManager.Instance.GetRuntimeByIdent(factionBName, plyGameObjectIdentifyingType.screenName);
            factionC = ActorFactionManager.Instance.GetRuntimeByIdent(factionCName, plyGameObjectIdentifyingType.screenName);

            if (factionA != null && factionA.varListDef.Exists(f => f.name == factionColorProperty))
            {
                factionAColor = (Color) factionA.GetFactionVarValue(factionColorProperty, factionA);
            }

            if (factionB != null && factionB.varListDef.Exists(f => f.name == factionColorProperty))
            {
                factionBColor = (Color) factionB.GetFactionVarValue(factionColorProperty, factionB);
            }

            if (factionC != null && factionC.varListDef.Exists(f => f.name == factionColorProperty))
            {
                factionCColor = (Color)factionC.GetFactionVarValue(factionColorProperty, factionC);
            }*/

            //input.gameObject.SetActive(PhotonNetwork.inRoom);
        }

        public void OnFriendListUpdate(List<FriendInfo> friendList)
        {
        }

        public void OnCreatedRoom()
        {
        }

        public void OnCreateRoomFailed(short returnCode, string message)
        {
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
        }

        public void OnJoinRandomFailed(short returnCode, string message)
        {
        }

        public void OnJoinedRoom()
        {
            if(notifyWhenPlayersConnect) Add(string.Format(youJoinedMessage.content, PhotonNetwork.CurrentRoom.Name), serverColor, false, null);

            input.gameObject.SetActive(true);
            
        }

        public void OnLeftRoom()
        {
            input.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (PhotonNetwork.NetworkingClient != null) PhotonNetwork.RemoveCallbackTarget(this);
        }

        /// <summary>
        /// Send the chat message to everyone else.
        /// </summary>

        protected override void OnSubmit(string text)
        {
            //OnRaiseEvent((byte)chatEventCode, text, PhotonNetwork.LocalPlayer.ActorNumber);

            Send(text);
        }

        /// <summary>
        /// True when input field is focused.
        /// </summary>
        public static bool IsOpen()
        {
            return mInst && mInst.selected; //mInst.input.isFocused;
        }

        public static void Send(string text)
        {
            //mInst.OnRaiseEvent((byte)mInst.chatEventCode, text, PhotonNetwork.LocalPlayer.ActorNumber);

            RaiseEventOptions options = RaiseEventOptions.Default;
            options.Receivers = ReceiverGroup.All;
            PhotonNetwork.RaiseEvent((byte)mInst.chatEventCode, text, options, SendOptions.SendReliable);
        }

        /// <summary>
        /// Add a new chat entry.
        /// </summary>
        /// <param name="text"></param>
        public static void Add(string text)
        {
            if(mInst == null)
            {
                Debug.LogWarning("Can't add chat messages there is no RoomChat instance found.");
                return;
            }
            Add(text, mInst.serverColor);
        }

        /// <summary>
        /// Add a new chat entry.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        public static void Add(string text, Color color)
        {
            if (mInst) mInst.Add(text, mInst.serverColor, false, null);
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code == chatEventCode)
            {
                Player player = PhotonNetwork.CurrentRoom.GetPlayer(photonEvent.Sender);
                if (player == null) return;
                Color color = playerColor;
                string message = (string)photonEvent.CustomData;
                string originalMessage = message;

                if (message.Contains(S_SPLIT))
                {
                    message = message.Replace(S_SPLIT, string.Empty);
                    color = serverColor;
                }
                else
                {
                    // If the message was not sent by the player, color it differently and play a sound
                    if (!Equals(player, PhotonNetwork.LocalPlayer))
                    {
                        /*if (player.HasProperty("Faction"))
                        {
                            if ((player.GetString("Faction") as string) == factionAName) color = factionAColor;
                            else if ((player.GetString("Faction") as string) == factionBName) color = factionBColor;
                            else if ((player.GetString("Faction") as string) == factionCName) color = factionCColor;
                        }
                        else
                        {*/
                            color = colors[player.GetPlayerNumber()];
                        //}
                    }

                    // Embed the player's name into the message
                    message = $"[{player.NickName}]: {message}";
                }
                
                if(enableFloatingMessage) FloatingMessage(originalMessage, floatingMessageColor, player);
                Add(message, color, false, player);

                //if (notificationSound != null)
                //    NGUITools.PlaySound(notificationSound);
            }
        }
        
        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            if(notifyWhenPlayersConnect) Add(string.Format(playerJoinedMessage.content, newPlayer.NickName), serverColor, false, null);
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (notifyWhenPlayersConnect) Add(string.Format(playerLeftMessage.content, otherPlayer.NickName), serverColor, false, null);
        }

        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {           
        }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
        }

        public void OnMasterClientSwitched(Player newMasterClient)
        {
        }

        private void FloatingMessage(string message, Color color, Player player)
        {
            if(player?.TagObject != null)
            {
                // if(player.IsLocal) return;

                GameObject go = player.TagObject as GameObject;

                FloatingMessageManager.Show(
                    message, color,
                    go.transform, floatingMessageOffset, floatingMessageTime
                );
            }
        }
    }
}
