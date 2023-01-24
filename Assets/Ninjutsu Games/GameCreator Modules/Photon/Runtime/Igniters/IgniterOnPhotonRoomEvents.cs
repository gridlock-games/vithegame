namespace GameCreator.Core
{
    using ExitGames.Client.Photon;
    using Photon.Pun;
    using Photon.Realtime;
    using System.Collections.Generic;
    using UnityEngine;

    [AddComponentMenu("")]
    public class IgniterOnPhotonRoomEvents : Igniter, IInRoomCallbacks
    {
#if UNITY_EDITOR
        public new static string NAME = "Photon/On Room Events";
        public new static bool REQUIRES_COLLIDER = false;
        public new static string ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
#endif
        public enum PhotonEvent
        {
            OnPlayerEnteredRoom,
            OnPlayerLeftRoom,
            OnMasterClientSwitched,
        }

        public PhotonEvent eventType = PhotonEvent.OnPlayerEnteredRoom;

        new private void OnEnable()
        {
#if UNITY_EDITOR
            this.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
#endif
            if (PhotonNetwork.NetworkingClient != null) PhotonNetwork.AddCallbackTarget(this);
        }

        private void OnDisable()
        {
            if (PhotonNetwork.NetworkingClient != null) PhotonNetwork.RemoveCallbackTarget(this);
        }

        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (eventType == PhotonEvent.OnPlayerEnteredRoom)
            {
                var t = gameObject;
                if (newPlayer.TagObject != null)
                {
                    t = (GameObject) newPlayer.TagObject;
                }
                ExecuteTrigger(t);
            }
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (eventType == PhotonEvent.OnPlayerLeftRoom)
            {
                var t = gameObject;
                if (otherPlayer.TagObject != null)
                {
                    t = (GameObject) otherPlayer.TagObject;
                }
                ExecuteTrigger(t);
            }
        }

        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) { }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) { }

        public void OnMasterClientSwitched(Player newMasterClient)
        {
            if (eventType == PhotonEvent.OnMasterClientSwitched)
            {
                var t = gameObject;
                if (newMasterClient.TagObject != null)
                {
                    t = (GameObject) newMasterClient.TagObject;
                }
                ExecuteTrigger(t);
            }
        }
    }
}