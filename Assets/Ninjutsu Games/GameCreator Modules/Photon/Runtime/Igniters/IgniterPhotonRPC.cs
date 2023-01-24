using System.Collections;
using System.Collections.Generic;
using NJG.PUN;
using Photon.Realtime;
using UnityEngine;

namespace GameCreator.Core
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using GameCreator.Core.Hooks;
    using ExitGames.Client.Photon;
    using Photon.Pun;

    [AddComponentMenu(""), RequireComponent(typeof(PhotonView), typeof(RPCNetwork))]
    public class IgniterPhotonRPC : Igniter
    {
#if UNITY_EDITOR
        public new static string NAME = "Photon/On Photon RPC";
        public new static bool REQUIRES_COLLIDER = false;
        public new static string ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
#endif
        public string rpcName;
        public PhotonReceiveData receivedData;

        public const string RPC_NAME = "RPCHandler";

        protected override void Awake()
        {
            base.Awake();
            if (!GetComponent<RPCNetwork>()) gameObject.AddComponent<RPCNetwork>();
        }

        private new void OnEnable()
        {
#if UNITY_EDITOR
            this.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
#endif
        }

        public void RPCHandler(object[] data, PhotonMessageInfo info)
        {
            GameObject owner = info.Sender.TagObject as GameObject;
            if (owner) Execute(data, info);
            else StartCoroutine(TagCheck(data, info));
        }

        IEnumerator TagCheck(object[] data, PhotonMessageInfo info)
        {
            float time = Time.time;
            yield return new WaitWhile(() => info.Sender.TagObject == null);
            //Debug.LogWarningFormat("IgniterRPC SenderTag Ready: {0} time: {1}", info.Sender.TagObject, Time.time - time);
            Execute(data, info);
        }

        private void Execute(object[] data, PhotonMessageInfo info)
        {
            GameObject owner = info.Sender.TagObject as GameObject;
            GameObject invoker = owner ? owner : info.photonView.gameObject;
            // Debug.LogWarningFormat("IgniterRPC Invoker: {0} root: {1} rpcName: {2} sender: {4} senderTag: {5} data: {3}", 
            //     invoker, transform.root, rpcName, data.ToStringFull(), info.Sender, info.Sender.TagObject);
            
            receivedData.FromObject(invoker, data);
            ExecuteTrigger(invoker);
        }
    }
}