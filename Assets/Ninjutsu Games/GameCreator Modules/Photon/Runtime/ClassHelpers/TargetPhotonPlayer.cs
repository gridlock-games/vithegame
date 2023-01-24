namespace NJG.PUN
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using GameCreator.Core.Hooks;
    using GameCreator.Characters;
    using GameCreator.Variables;
    using Photon.Realtime;
    using Photon.Pun;

    [System.Serializable]
    public class TargetPhotonPlayer
    {
        public enum Target
        {
            Player,
            Invoker,
            GameObject,
            Id, 
            MasterClient,
            LastJoinedPlayer,
            // PlayerNumber,
            //GlobalVariable,
            //ListVariable
        }

        // PROPERTIES: ----------------------------------------------------------------------------

        public Target target = Target.Id;
        public GameObjectProperty character;
        public int playerId;
        public HelperLocalVariable local = new HelperLocalVariable();
        public HelperGlobalVariable global = new HelperGlobalVariable();
        public HelperGetListVariable list = new HelperGetListVariable();

        private int cacheInstanceID;
        private Player cachePlayer;

        // PUBLIC METHODS: ------------------------------------------------------------------------

        public Player GetPhotonPlayer(GameObject invoker)
        {
            PhotonView view;

            switch (target)
            {
                case Target.Player:
                    if (PhotonNetwork.LocalPlayer != null)
                    {
                        cachePlayer = PhotonNetwork.LocalPlayer;
                    }
                    else if (HookPlayer.Instance && HookPlayer.Instance.GetInstanceID() != cacheInstanceID)
                    {
                        view = HookPlayer.Instance.Get<PhotonView>();
                        if (view) cachePlayer = view.Owner;
                        CacheInstanceID(HookPlayer.Instance.gameObject);
                    }
                    break;

                case Target.Invoker:
                    if (!invoker)
                    {
                        cachePlayer = null;
                        break;
                    }

                    if (cachePlayer == null || invoker.GetInstanceID() != cacheInstanceID)
                    {
                        view = invoker.GetComponentInChildren<PhotonView>();
                        if (view)
                        {
                            cachePlayer = view.Owner;
                            CacheInstanceID(invoker);
                        }
                    }

                    break;

                case Target.GameObject:
                    if (character != null)
                    {
                        GameObject go = character.GetValue(invoker);
                        if (go && go.GetInstanceID() != cacheInstanceID)
                        {
                            view = go.GetComponentInChildren<PhotonView>();
                            if(view) cachePlayer = view.Owner;
                            CacheInstanceID(go);
                        }
                    }
                    break;
                case Target.Id:
                    cachePlayer = PhotonNetwork.CurrentRoom.GetPlayer(playerId);
                    break;
                case Target.MasterClient:
                    cachePlayer = PhotonNetwork.MasterClient;
                    break;
                case Target.LastJoinedPlayer:
                    cachePlayer = NetworkManager.Instance.LastJoinedPlayer;
                    break;
                // case Target.PlayerNumber:
                //     cachePlayer = PhotonNetwork.CurrentRoom.GetPlayer(playerId);
                //     break;
            }

            return cachePlayer;
        }

        private void CacheInstanceID(GameObject go)
        {
            cacheInstanceID = go.GetInstanceID();
        }

        // UTILITIES: -----------------------------------------------------------------------------

        public override string ToString()
        {
            string result = "(unknown)";
            switch (target)
            {
                case Target.Player: result = "Player"; break;
                case Target.Invoker: result = "Invoker"; break;
                case Target.MasterClient: result = "MasterClient"; break;
                case Target.LastJoinedPlayer: result = "LastJoinedPlayer"; break;
                case Target.GameObject:
                    result = (character == null ? "(none)" : character.ToString());
                    break;
                case Target.Id:
                    result = "Photon Player Id: "+playerId;
                    break;
                //case Target.LocalVariable: result = this.local.ToString(); break;
                //case Target.GlobalVariable: result = this.global.ToString(); break;
                //case Target.ListVariable: result = this.list.ToString(); break;
            }

            return result;
        }
    }
}
