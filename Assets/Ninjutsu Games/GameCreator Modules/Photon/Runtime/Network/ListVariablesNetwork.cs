using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using GameCreator.Core;
using GameCreator.Variables;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using UnityEngine;

namespace NJG.PUN
{
    [AddComponentMenu(""), RequireComponent(typeof(PhotonView)), DisallowMultipleComponent]
    public class ListVariablesNetwork : MonoBehaviourPun, IMatchmakingCallbacks, IInRoomCallbacks
    {
        [System.Serializable]
        public class ListRPC
        {
            public bool usePlayerList;
            public bool useNetworkInstantiation;
            public bool syncVariables;
            public bool syncIterator;
            public bool onlyMasterCanSync;
            public ListVariables variables;
        }
        
        public List<ListRPC> listVars = new List<ListRPC>();

        private Dictionary<string, MBVariable> VARS = new Dictionary<string, MBVariable>();
        private bool initialized;
        private List<ListVariables> updatePlayers = new List<ListVariables>();

        // CONSTRUCTORS: --------------------------------------------------------------------------

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);

#if UNITY_EDITOR
            // HideStuff();
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

        private void Start()
        {
            // if (PhotonNetwork.InRoom)
            {
                if (!initialized) Initialize();
            }
        }

        private void Initialize()
        {
            if (initialized) return;

            initialized = true;

            //if (photonView.IsMine || photonView.IsSceneView)
            {
                for (int i = 0, imax = listVars.Count; i < imax; i++)
                {
                    int index = i;
                    ListRPC rpc = listVars[i];
                    if (rpc == null || !rpc.variables) continue;
                    if (rpc.syncIterator)
                    {
                        // updateIterator
                    }
                    if (rpc.usePlayerList)
                    {
                        CharacterNetwork.OnPlayerInstantiated.AddListener(OnPlayerInstantiated);
                        UpdatePlayerList(rpc.variables);
                        updatePlayers.Add(rpc.variables);
                    }
                    else if (rpc.useNetworkInstantiation)
                    {
                        for (int e = 0, emax = rpc.variables.references.Length; e < emax; e++)
                        {
                            var variable = rpc.variables.references[e];
                            NJGPhotonPool pool = PhotonNetwork.PrefabPool as NJGPhotonPool;

                            GameObject prefab = variable.variable.Get<GameObject>();
                            if (!prefab) continue;
                            if (pool != null && !pool.ResourceCache.ContainsKey(prefab.name))
                                pool.ResourceCache.Add(prefab.name, prefab);

                            if (rpc.syncVariables)
                            {
                                /*Variable.DataType varType = (Variable.DataType)variable.variable.type;
    
                                if (varType == Variable.DataType.Null ||
                                    varType == Variable.DataType.GameObject ||
                                    varType == Variable.DataType.Sprite ||
                                    varType == Variable.DataType.Texture2D) continue;
    
                                if (!VARS.ContainsKey(variable.variable.name))
                                {
                                    VARS.Add(variable.variable.name, variable);
    
                                    VariablesManager.events.SetOnChangeLocal(
                                        this.OnVariableChange,
                                        variable.gameObject,
                                        variable.variable.name
                                    );
    
                                    VariablesManager.events.StartListenListAny(
                                        this.OnListChange,
                                        variable.gameObject
                                    );
                                }*/
                            }
                        }
                    }
                }

                //Debug.LogWarningFormat("[LocalVarsNetwork] Initialized vars: {0}", VARS.Count);
            }
        }

        private void OnPlayerInstantiated(CharacterNetwork character, PhotonMessageInfo info)
        {
            UpdateAllPlayerLists();
        }

        private void OnListChange(int index, object prevElem, object newElem)
        {
        }

        private void UpdateAllPlayerLists()
        {
            // Debug.LogWarningFormat("Player lists: {0}", updatePlayers.Count);
            for (int i = 0; i < updatePlayers.Count; i++)
            {
                UpdatePlayerList(updatePlayers[i]);
            }
        }

        private void UpdatePlayerList(ListVariables list)
        {
            for (int i = list.variables.Count - 1; i >= 0; --i)
            {
                list.Remove(i);
            }
            for (int i = 0; i < NetworkManager.Instance.Players.Length; i++)
            {
                var player = NetworkManager.Instance.Players[i];
                if(player.TagObject == null) continue;
                // Debug.LogWarningFormat("Adding player: {0} number: {1}", player, player.GetPlayerNumber());
                list.Push(player.TagObject as GameObject);
            }
            // Debug.LogWarningFormat("Players: {0} Entries: {1}", NetworkManager.Instance.Players.Length, list.variables.Count);
        }

        private void OnVariableChange(string variableID)
        {
            MBVariable var = null;
            //Debug.LogWarningFormat("[LocalVarsNetwork] OnVariableChange variableID: {0}", variableID);
            if (VARS.TryGetValue(variableID, out var))
            {
                if (photonView.IsRoomView && gameObject.name.StartsWith("==>"))
                {
                    gameObject.name = string.Format("{0}", gameObject.name.Replace("==>", string.Empty));
                    return;
                }

                //Debug.LogWarningFormat("[LocalVarsNetwork] OnVariableChange Send RPC variableID: {0} value: {1}", variableID, var.variable.Get());
                photonView.RPC(nameof(ListVPRPC), RpcTarget.Others, variableID, var.variable.Get());
            }
        }

        [PunRPC]
        public virtual void ListVPRPC(string variableID, object value, PhotonMessageInfo info)
        {
            MBVariable var = null;
            //Debug.LogWarningFormat("[LocalVarsNetwork] RPC variableID: {0} value: {1}", variableID, value);
            if (VARS.TryGetValue(variableID, out var))
            {
                if (info.photonView.IsRoomView)
                {
                    info.photonView.gameObject.name = string.Format("==>{0}", info.photonView.gameObject.name);
                }

                var.variable.Update(value);
                VariablesManager.events.OnChangeLocal(var.gameObject, var.variable.name);
                //Debug.LogWarningFormat("[LocalVarsNetwork] 2 RPC updatedValue: {0} networkValue: {1}", var.variable.Get(), value);
            }
            else
            {
                Debug.LogWarningFormat("Could not find variable {0} on {1}.", variableID, gameObject, gameObject);
            }
        }

        [PunRPC]
        public virtual void UpdateList(Hashtable data)
        {
            foreach(var d in data)
            {
                MBVariable var = VARS[d.Key.ToString()];
                var.variable.Update(d.Value);
                VariablesManager.events.OnChangeLocal(var.gameObject, var.variable.name);
            }
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
            // UpdateAllPlayerLists();
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

        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            /*Hashtable data = new Hashtable(VARS.Count);
            foreach (var v in VARS)
            {
                data.Add(v.Key, v.Value.variable.Get());
            }
            photonView.RPC(nameof(UpdateList), newPlayer, data);*/
            // UpdateAllPlayerLists();
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            UpdateAllPlayerLists();
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
    }
}
