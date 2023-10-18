using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using Vi.ScriptableObjects;

namespace Vi.Player
{
    [RequireComponent(typeof(Animator))]
    public class AnimationHandler : NetworkBehaviour
    {
        public void PlayAction(ActionClip actionClip)
        {
            PlayActionServerRpc(actionClip.name, actionClip.GetClipType());
        }

        private ActionClip.ClipType lastClipType;
        [ServerRpc]
        private void PlayActionServerRpc(string actionStateName, ActionClip.ClipType clipType)
        {
            if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty"))
            {
                if (clipType == ActionClip.ClipType.Dodge & lastClipType == ActionClip.ClipType.Dodge) { return; }
            }

            //// If we are not at or transitioning to the empty state, do not perform an action
            //if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty") & !animator.GetNextAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty")) { return; }
            
            if (!IsClient)
            {
                //animator.Play(actionStateName, animator.GetLayerIndex("Actions"));
                animator.CrossFade(actionStateName, 0.15f, animator.GetLayerIndex("Actions"));
            }
            PlayActionClientRpc(actionStateName);
            lastClipType = clipType;
        }

        [ClientRpc]
        private void PlayActionClientRpc(string actionStateName)
        {
            //animator.Play(actionStateName, animator.GetLayerIndex("Actions"));
            animator.CrossFade(actionStateName, 0.15f, animator.GetLayerIndex("Actions"));
        }

        Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private Vector3 networkRootMotion;
        public Vector3 ApplyNetworkRootMotion()
        {
            Vector3 _ = networkRootMotion;
            networkRootMotion = Vector3.zero;
            return _;
        }

        private Vector3 localRootMotion;
        public Vector3 ApplyLocalRootMotion()
        {
            Vector3 _ = localRootMotion;
            localRootMotion = Vector3.zero;
            return _;
        }

        private void OnAnimatorMove()
        {
            if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty"))
            {
                networkRootMotion += animator.deltaPosition;
                localRootMotion += animator.deltaPosition;
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            
        }
    }
}