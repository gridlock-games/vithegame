using System;
using System.Collections.Generic;
using GameCreator.Core;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace NJG.PUN
{
    [AddComponentMenu("")]
    public class RPCNetwork : MonoBehaviour
    {
        public IgniterPhotonRPC[] igniters;
        private Dictionary<string, IgniterPhotonRPC> cache = new Dictionary<string, IgniterPhotonRPC>();

        private void Awake()
        {
            igniters = GetComponentsInChildren<IgniterPhotonRPC>();
        }

        [PunRPC]
        public void RPCHandler(string rpcName, object[] data, PhotonMessageInfo info)
        {
            IgniterPhotonRPC target = null;
            if (cache.TryGetValue(rpcName, out target))
            {
                target.RPCHandler(data, info);
            }
            else
            {
                foreach (IgniterPhotonRPC igniter in igniters)
                {
                    if (igniter.rpcName.Equals(rpcName))
                    {
                        if(!cache.ContainsKey(rpcName)) cache.Add(rpcName, igniter);
                        igniter.RPCHandler(data, info);
                        break;
                    }
                }
            }
        }
    }
}