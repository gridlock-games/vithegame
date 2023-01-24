using NJG.PUN;

namespace GameCreator.Core
{
    using Photon.Pun;
    using Photon.Realtime;
    using System.Collections.Generic;
    using UnityEngine;

    [AddComponentMenu("")]
    public class IgniterOnPhotonMatchmaking : Igniter, IMatchmakingCallbacks
    {
#if UNITY_EDITOR
        public new static string NAME = "Photon/On Photon Matchmaking";
        public new static bool REQUIRES_COLLIDER = false;
        public new static string ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
#endif
        public enum PhotonEvent
        {
            OnCreatedRoom,
            OnCreateRoomFailed,
            OnJoinedRoom,
            OnJoinRoomFailed,
            OnJoinRandomFailed,
            OnLeftRoom,
            OnFriendListUpdate,
        }

        public PhotonEvent eventType = PhotonEvent.OnJoinedRoom;

        new private void OnEnable()
        {
            if (PhotonNetwork.NetworkingClient != null) PhotonNetwork.AddCallbackTarget(this);
        }

        private void OnDisable()
        {
            if (PhotonNetwork.NetworkingClient != null) PhotonNetwork.RemoveCallbackTarget(this);
        }

        public void OnFriendListUpdate(List<FriendInfo> friendList)
        {
            if (eventType == PhotonEvent.OnFriendListUpdate) ExecuteTrigger(gameObject);
        }

        public void OnCreatedRoom()
        {
            if (eventType == PhotonEvent.OnCreatedRoom) ExecuteTrigger(gameObject);
        }

        public void OnCreateRoomFailed(short returnCode, string message)
        {
            if (eventType == PhotonEvent.OnCreateRoomFailed) ExecuteTrigger(gameObject);
        }

        public void OnJoinedRoom()
        {
            NetworkManager.Instance.LastJoinedPlayer = PhotonNetwork.LocalPlayer;
            if (eventType == PhotonEvent.OnJoinedRoom) ExecuteTrigger(gameObject);
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
            if (eventType == PhotonEvent.OnJoinRoomFailed) ExecuteTrigger(gameObject);
        }

        public void OnJoinRandomFailed(short returnCode, string message)
        {
            if (eventType == PhotonEvent.OnJoinRandomFailed) ExecuteTrigger(gameObject);
        }

        public void OnLeftRoom()
        {
            if(!Application.isPlaying) return;
            if (eventType == PhotonEvent.OnLeftRoom) ExecuteTrigger(gameObject);
        }
    }
}