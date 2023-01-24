using ExitGames.Client.Photon;
using GameCreator.Variables;
using NJG.PUN;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace GameCreator.Core
{
    [AddComponentMenu("")]
    public class IgniterOnPhotonPlayerProperty : Igniter, IInRoomCallbacks
    {
#if UNITY_EDITOR
        public new static string NAME = "Photon/On Player Property";
        public new static string COMMENT = "Leave property empty to trigger when any Player Property changes.\nSet targetPlayer as invoker to trigger when any Player Property changes.";
        public new static string ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
#endif
        public TargetPhotonPlayer targetPlayer = new TargetPhotonPlayer { target = TargetPhotonPlayer.Target.Player };
        public StringProperty property = new StringProperty();

        new private void OnEnable()
        {
#if UNITY_EDITOR
            hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
#endif
            if (PhotonNetwork.NetworkingClient != null) PhotonNetwork.AddCallbackTarget(this);
        }

        private void OnDisable()
        {
            if (PhotonNetwork.NetworkingClient != null) PhotonNetwork.RemoveCallbackTarget(this);
        }

        public void OnPlayerPropertiesUpdate(Player player, Hashtable props)
        {
            if (props.ContainsKey(PlayerProperties.PING)) return;

            bool canExecute = true;

            Player tplayer = targetPlayer.target == TargetPhotonPlayer.Target.Invoker ? player : targetPlayer.GetPhotonPlayer(gameObject);
            GameObject invoker = gameObject;

            if (tplayer.TagObject != null) invoker = tplayer.TagObject as GameObject;
            
            if (tplayer != null && tplayer.ActorNumber != player.ActorNumber) canExecute = false;
            if (property != null && !string.IsNullOrEmpty(property.GetValue(invoker)) && !props.ContainsKey(property.GetValue(invoker))) canExecute = false;
            
            // Debug.LogWarningFormat("OnPlayerProp player: {0} invoker: {1} target: {2} contains: {3} canExecute: {4} tagObject: {5}", 
            //     player, invoker, property.GetValue(invoker), props.ContainsKey(property.GetValue(invoker)), canExecute, tplayer.TagObject);

            if (canExecute)
            {
                ExecuteTrigger(invoker);
            }
        }

        public void OnPlayerEnteredRoom(Player newPlayer) { }

        public void OnPlayerLeftRoom(Player otherPlayer) { }

        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) { }

        public void OnMasterClientSwitched(Player newMasterClient) { }
    }
}