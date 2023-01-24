namespace GameCreator.Core
{
    using Photon.Pun;
    using Photon.Realtime;
    using System.Collections.Generic;
    using UnityEngine;

    [AddComponentMenu("")]
    public class IgniterOnPhotonEvent : Igniter, IConnectionCallbacks
    {
#if UNITY_EDITOR
        public new static string NAME = "Photon/On Connection Events";
        public new static bool REQUIRES_COLLIDER = false;
        public new static string ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
#endif
        public enum PhotonEvent
        {
            OnConnected,
            OnConnectedToMaster,
            OnDisconnected,
        }

        public PhotonEvent eventType = PhotonEvent.OnConnectedToMaster;

        new private void OnEnable()
        {
#if UNITY_EDITOR
            this.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
#endif
            if(PhotonNetwork.NetworkingClient != null) PhotonNetwork.AddCallbackTarget(this);
        }

        void OnDisable()
        {
            if (PhotonNetwork.NetworkingClient != null) PhotonNetwork.RemoveCallbackTarget(this);
        }

        public void OnConnected()
        {
            if (eventType == PhotonEvent.OnConnected) ExecuteTrigger(gameObject);
        }

        public void OnConnectedToMaster()
        {
            if (eventType == PhotonEvent.OnConnectedToMaster) ExecuteTrigger(gameObject);
        }

        public void OnDisconnected(DisconnectCause cause)
        {
            if (!isExitingApplication && eventType == PhotonEvent.OnDisconnected) ExecuteTrigger(gameObject);
        }

        public void OnRegionListReceived(RegionHandler regionHandler) { }

        public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }

        public void OnCustomAuthenticationFailed(string debugMessage) { }
    }
}