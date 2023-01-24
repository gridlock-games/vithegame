using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using GameCreator.Variables;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace NJG.PUN
{
    [AddComponentMenu(""), RequireComponent(typeof(PhotonView)), DisallowMultipleComponent]
    public class LocalVariablesNetwork : MonoBehaviourPun, IMatchmakingCallbacks, IInRoomCallbacks
    {
        [Serializable]
        public class VarRPC
        {
            public LocalVariables variables;
        }

        public List<VarRPC> localVars = new List<VarRPC>();

        private Dictionary<string, int> VARS = new Dictionary<string, int>();
        private bool initialized;

        // CONSTRUCTORS: --------------------------------------------------------------------------

        private void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);

#if UNITY_EDITOR
            HideStuff();
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
        private void HideStuff()
        {
            // hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        }

        private void OnValidate()
        {
            HideStuff();
        }
#endif

        private void Awake()
        {
            if (PhotonNetwork.InRoom)
            {
                if (!initialized) Initialize();
            }
        }

        private void Initialize()
        {
            if (initialized) return;

            initialized = true;

            //if (photonView.IsMine || photonView.IsRoomView)
            {
                for (int i = 0, imax = localVars.Count; i < imax; i++)
                {
                    int index = i;
                    VarRPC rpc = localVars[i];
                    if (rpc == null || !rpc.variables) continue;

                    for (int e = 0, emax = rpc.variables.references.Length; e < emax; e++)
                    {
                        var variable = rpc.variables.references[e];
                        Variable.DataType varType = (Variable.DataType)variable.variable.type;

                        if (varType == Variable.DataType.Null ||
                            varType == Variable.DataType.GameObject ||
                            varType == Variable.DataType.Sprite ||
                            varType == Variable.DataType.Texture2D) continue;

                        // Debug.LogWarningFormat("[LocalVarsNetwork] Init variableID: {0} localVar: {1}", variable.variable.name, index);
                        if (!VARS.ContainsKey(variable.variable.name))
                        {
                            VARS.Add(variable.variable.name, index);

                            VariablesManager.events.SetOnChangeLocal(OnVariableChange, rpc.variables.gameObject, variable.variable.name);
                        }
                    }
                }

                //Debug.LogWarningFormat("[LocalVarsNetwork] Initialized vars: {0}", VARS.Count);
            }
        }

        private void OnDestroy()
        {
            foreach(var v in VARS)
            {
                VariablesManager.events.RemoveChangeLocal(OnVariableChange, localVars[v.Value].variables.gameObject, v.Key);
            }
        }

        private void OnVariableChange(string variableID)
        {
            int index = -1;
            LocalVariables var = null;
            // Debug.LogWarningFormat("[LocalVarsNetwork] OnVariableChange variableID: {0}", variableID);
            if (VARS.TryGetValue(variableID, out index))
            {
                var = localVars[index].variables;
                if (photonView.IsRoomView && gameObject.name.StartsWith("==>"))
                {
                    gameObject.name = $"{gameObject.name.Replace("==>", string.Empty)}";
                    return;
                }

                //Debug.LogWarningFormat("[LocalVarsNetwork] OnVariableChange Send RPC variableID: {0} value: {1} var: {2}", variableID, var.Get(variableID).Get(), LocalVariablesUtilities.Get(var.gameObject, variableID, false).Get());
                var value = var.Get(variableID).Get();
                if(value is string) photonView.RPC(nameof(LocalString), RpcTarget.Others, variableID, value);
                else if(value is bool) photonView.RPC(nameof(LocalBool), RpcTarget.Others, variableID, value);
                else if(value is float) photonView.RPC(nameof(LocalNumber), RpcTarget.Others, variableID, value);
                else if(value is Vector2) photonView.RPC(nameof(LocalVector2), RpcTarget.Others, variableID, value);
                else if(value is Vector3) photonView.RPC(nameof(LocalVector3), RpcTarget.Others, variableID, value);
                else if(value is Color) photonView.RPC(nameof(LocalColor), RpcTarget.Others, variableID, value);
            }
        }

        [PunRPC]
        public virtual void LocalString(string variableID, string value, PhotonMessageInfo info)
        {
            ListVarRPC(variableID, value, info);
        }
        
        [PunRPC]
        public virtual void LocalBool(string variableID, bool value, PhotonMessageInfo info)
        {
            ListVarRPC(variableID, value, info);
        }
        
        [PunRPC]
        public virtual void LocalNumber(string variableID, float value, PhotonMessageInfo info)
        {
            ListVarRPC(variableID, value, info);
        }
        
        [PunRPC]
        public virtual void LocalVector2(string variableID, Vector2 value, PhotonMessageInfo info)
        {
            ListVarRPC(variableID, value, info);
        }
        
        [PunRPC]
        public virtual void LocalVector3(string variableID, Vector3 value, PhotonMessageInfo info)
        {
            ListVarRPC(variableID, value, info);
        }
        
        [PunRPC]
        public virtual void LocalColor(string variableID, Color value, PhotonMessageInfo info)
        {
            ListVarRPC(variableID, value, info);
        }

        //[PunRPC]
        private void ListVarRPC(string variableID, object value, PhotonMessageInfo info)
        {
            int index = -1;
            LocalVariables lVar = null;
            //Debug.LogWarningFormat("[LocalVarsNetwork] RPC variableID: {0} value: {1}", variableID, value);
            if (VARS.TryGetValue(variableID, out index))
            {
                lVar = localVars[index].variables;
                if (info.photonView.IsRoomView)
                {
                    var o = info.photonView.gameObject;
                    o.name = $"==>{o.name}";
                }

                Variable var = lVar.Get(variableID);
                var.Update(value);
                VariablesManager.events.OnChangeLocal(lVar.gameObject, var.name);
                //Debug.LogWarningFormat("[LocalVarsNetwork] 2 RPC updatedValue: {0} networkValue: {1}", var.Get(), value);
            }
            else
            {
                Debug.LogWarningFormat(gameObject, "Could not find variable {0} on {1}.", variableID, gameObject);
            }
        }

        [PunRPC]
        public virtual void UpdateLocalVariables(int[] indexes, Hashtable data)
        {
            int index = 0;
            foreach(var d in data)
            {
                int dataIndex = indexes[index];
                var lvar = localVars[dataIndex].variables;
                var variable = lvar.Get(d.Key.ToString());
                variable.Update(d.Value);
                //MBVariable var = VARS[d.Key.ToString()];
                //var.variable.Update(d.Value);
                VariablesManager.events.OnChangeLocal(lvar.gameObject, variable.name);
                index++;
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
            if(!photonView.IsMine) return;
            
            int[] indexes = new int[VARS.Count];
            Hashtable data = new Hashtable(VARS.Count);
            int index = 0;
            foreach (var v in VARS)
            {
                data.Add(v.Key, localVars[v.Value].variables.Get(v.Key).Get());
                indexes[index] = v.Value;
                index++;
            }
            photonView.RPC(nameof(UpdateLocalVariables), newPlayer, indexes, data);
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
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
