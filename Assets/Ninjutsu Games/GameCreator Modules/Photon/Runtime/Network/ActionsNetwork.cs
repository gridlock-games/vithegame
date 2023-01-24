using System;
using System.Collections.Generic;
using GameCreator.Core;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace NJG.PUN
{
    [AddComponentMenu(""), RequireComponent(typeof(PhotonView))]
    public class ActionsNetwork : MonoBehaviourPun, IMatchmakingCallbacks
    {
        //public static Dictionary<string, ActionsNetwork> REGISTER = new Dictionary<string, ActionsNetwork>();

        [System.Serializable]
        public class ActionRPC
        {
            public enum TargetType
            {
                RpcTarget,
                PhotonPlayer
            }
            /*public enum RpcTarget
            {
                Others,
                OthersBuffered,
                MasterClient,
                SpecificPlayer
            }*/
            public Actions actions;
            public TargetType targetType = TargetType.RpcTarget;
            public RpcTarget targets = RpcTarget.Others;
            public TargetPhotonPlayer targetPlayer;
            //internal GameObject lastInvoker;
            //internal Player lastSender;
        }

        public List<ActionRPC> actions = new List<ActionRPC>();
        //public int group = 0;
        private bool initialized;

        // CONSTRUCTORS: --------------------------------------------------------------------------

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);

#if UNITY_EDITOR
            // HideStuff();
            Cleanup();
#endif
            if (PhotonNetwork.UseRpcMonoBehaviourCache)
            {
                photonView.RefreshRpcMonoBehaviourCache();
            }
        }

        private void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

#if UNITY_EDITOR
        /*private void HideStuff()
        {
            this.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        }

        private void OnValidate()
        {
            HideStuff();
        }*/
#endif

        private void Awake()
        {
            if (PhotonNetwork.InRoom)
            {
                if(!initialized) Initialize();
            }
        }

        private void Initialize()
        {
            if (initialized) return;

            initialized = true;
            
            if (photonView.IsMine || photonView.IsRoomView)
            {
                for (int i = 0, imax = actions.Count; i < imax; i++)
                {
                    int index = i;
                    ActionRPC rpc = actions[i];
                    if (rpc == null || !rpc.actions) continue;
                    rpc.actions.onExecute.AddListener((go) => 
                    {
                        //Debug.LogWarningFormat("[ActionsNetwork] OnExecute index: {0} name: {1} invoker: {2}",
                        //    index, rpc.actions.gameObject.name, go);

                        //rpc.lastInvoker = go;
                        if(go && photonView.IsRoomView && go.name.StartsWith("==>"))
                        {
                            go.name = $"{go.name.Replace("==>", string.Empty)}";
                            return;
                        }
                            
                        OnActionsExecute(index); 
                    });
                }
            }
        }

        private void OnActionsExecute(int index)
        {
            ActionRPC rpc = actions[index];
            if (rpc == null)
            {
                Debug.LogWarningFormat("Could not Sync Action reference is null.");
                return;
            }

            //Debug.LogWarningFormat("[ActionsNetwork] OnActionsExecute index: {0} name: {4} IsRoomView: {1} isMine: {2}, isExecuting: {3}"
            //    , index, photonView.IsRoomView, photonView.IsMine, rpc.actions.actionsList.isExecuting, rpc.actions.gameObject.name);

            /*if (photonView.IsRoomView)
            {
                Debug.LogWarningFormat("Sender: {0}", rpc.lastSender);
                if(rpc.lastSender != null && !rpc.lastSender.IsLocal)
                {
                    Debug.LogWarningFormat("Stop Resending");
                    return;
                }
                rpc.lastSender = PhotonNetwork.LocalPlayer;
                //photonView.RPC(RPC, RpcTarget.MasterClient, index);
                //return;
            }*/

            if (rpc.targetType == ActionRPC.TargetType.PhotonPlayer)
            {
                photonView.RPC(nameof(APRPC), rpc.targetPlayer.GetPhotonPlayer(gameObject), index);
            }
            else
            {
                /*RpcTarget targets = RpcTarget.Others;
                if (rpc.targets == ActionRPC.RpcTarget.Others) targets = RpcTarget.Others;
                if (rpc.targets == ActionRPC.RpcTarget.OthersBuffered) targets = RpcTarget.OthersBuffered;
                if (rpc.targets == ActionRPC.RpcTarget.MasterClient) targets = RpcTarget.MasterClient;*/
                photonView.RPC(nameof(APRPC), rpc.targets, index);
            }
        }

        [PunRPC]
        public virtual void APRPC(int index, PhotonMessageInfo info)
        {
            if(index >= actions.Count)
            {
#if UNITY_EDITOR
                Debug.LogWarning("The action you are trying to execute is out of range. Could be that a new action was created on the original client." + gameObject, gameObject);
#endif
                return;
            }

            /*if (info.Sender.IsLocal)
            {
                Debug.LogWarning("Prevent executing this since local player started it.");
                return;
            }*/       

            ActionRPC rpc = actions[index];

            //Debug.LogWarningFormat("[ActionsNetwork] RPC index: {0} name: {1} isExecuting: {2} IsRoomView: {3} isMine: {4} sender: {5} lastSender: {6}", 
            //    index, rpc.actions.gameObject.name, rpc.actions.actionsList.isExecuting, photonView.IsRoomView, info.photonView.IsMine, info.Sender, rpc.lastSender);

            //rpc.lastSender = info.Sender;
            if (info.photonView.IsRoomView)
            {
                var o = info.photonView.gameObject;
                o.name = $"==>{o.name}";
            }
            
            rpc.actions.ExecuteWithTarget(info.photonView.gameObject);
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

        public void OnJoinedRoom()
        {
            Initialize();
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
        }

        public void OnJoinRandomFailed(short returnCode, string message)
        {
        }

        public void OnLeftRoom()
        {
        }

#if UNITY_EDITOR

        /*public bool RemoveAction(ActionRPC actionsRPC)
        {
            bool canRemove = false;
            if (actions.Contains(actionsRPC))
            {
                actions.Remove(actionsRPC);
            }

            if(actions.Count == 0 && photonView != null)
            {
                int count = 0;
                if (photonView.ObservedComponents != null)
                {
                    foreach (var p in photonView.ObservedComponents)
                    {
                        if (p != null) count++;
                    }
                }
                if (GetComponent<CharacterNetwork>() != null) count++;
                canRemove = count == 0;
            }

            Cleanup();

            return canRemove;
        }

        public void AddAction(ActionRPC actionsRPC)
        {
            if (!actions.Contains(actionsRPC))
            {
                actions.Add(actionsRPC);
            }

            Cleanup();
        }*/

        public void Cleanup()
        {
            /*for (int i = 0; i < actions.Count; i++)
            {
                ActionRPC rpc = actions[i];
                if (rpc == null || rpc.actions == null)
                {
                    //Debug.LogWarning("Cleanup Action " + i+" on "+ gameObject, gameObject);
                    actions.RemoveAt(i);
                }
            }*/
        }


#endif
    }
}
