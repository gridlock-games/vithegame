using GameCreator.Core;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NJG.PUN
{
    [RequireComponent(typeof(PhotonView))]
    public class ObjectNetwork : MonoBehaviourPunCallbacks
    {
        private Actions executeActions;
        private Actions currentActions;
        private RpcTarget targets = RpcTarget.Others;
        private const string ARPC = "ActionRPC";

        public void ExecuteActions(Actions actions, Actions currentActions, RpcTarget targets)
        {
            this.executeActions = actions;
            this.currentActions = currentActions;
            this.targets = targets;
            photonView.RPC(ARPC, targets);
            Debug.Log("ExecuteActions " + actions + " / " + targets);
        }

        [PunRPC]
        public virtual void ActionRPC()
        {
            Debug.LogWarning("ActionRPC " + executeActions+" / "+ targets);
            if (this.executeActions != null && this.executeActions != currentActions)
            {
                CoroutinesManager.Instance.StartCoroutine(
                    this.executeActions.actionsList.ExecuteCoroutine(gameObject, null)
                );

                //if (this.waitToFinish) yield return coroutine;
            }
        }
    }
}
