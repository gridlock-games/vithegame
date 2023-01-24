namespace GameCreator.Core
{
    using ExitGames.Client.Photon;
    using GameCreator.Variables;
    using Photon.Pun;
    using Photon.Realtime;
    using UnityEngine;
    using Hashtable = ExitGames.Client.Photon.Hashtable;

    [AddComponentMenu("")]
    public class IgniterOnPhotonRoomProperty : Igniter, IInRoomCallbacks
    {
#if UNITY_EDITOR
        public new static string NAME = "Photon/On Room Property";
        public new static string COMMENT = "Leave property empty to trigger when any Room Property changes";
        public new static string ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
#endif
        public StringProperty property = new StringProperty();

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

        public void OnPhotonCustomRoomPropertiesChanged(Hashtable propertiesThatChanged)
        {
            string prop = property.GetValue(null);

            if ((!string.IsNullOrEmpty(prop) && propertiesThatChanged.ContainsKey(prop)) || string.IsNullOrEmpty(prop))
            {
                this.ExecuteTrigger(gameObject);
            }
        }

        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            string prop = property.GetValue(null);

            if ((!string.IsNullOrEmpty(prop) && propertiesThatChanged.ContainsKey(prop)) || string.IsNullOrEmpty(prop))
            {
                this.ExecuteTrigger(gameObject);
            }
        }

        public void OnPlayerEnteredRoom(Player newPlayer) { }

        public void OnPlayerLeftRoom(Player otherPlayer) { }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) { }

        public void OnMasterClientSwitched(Player newMasterClient) { }
    }
}