using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;

namespace Vi.Core
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
            
            if (animator.GetNextAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName(actionStateName)) { return; }

            animator.CrossFade(actionStateName, 0.15f, animator.GetLayerIndex("Actions"));
            ActionClip actionClip = weaponHandler.GetWeapon().GetActionClipByName(actionStateName);

            if (Application.isEditor)
                animator.speed = actionClip.animationSpeed;
            
            weaponHandler.SetActionClip(actionClip);

            if (clipType == ActionClip.ClipType.Dodge) { StartCoroutine(SetStatusOnDodge(actionStateName)); }

            PlayActionClientRpc(actionStateName);
            lastClipType = clipType;
        }

        [ClientRpc]
        private void PlayActionClientRpc(string actionStateName)
        {
            if (IsServer) { return; }

            animator.CrossFade(actionStateName, 0.15f, animator.GetLayerIndex("Actions"));
            ActionClip actionClip = weaponHandler.GetWeapon().GetActionClipByName(actionStateName);

            weaponHandler.SetActionClip(actionClip);
        }

        private IEnumerator SetStatusOnDodge(string actionStateName)
        {
            attributes.SetInviniciblity(5);
            yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName(actionStateName));
            AnimatorClipInfo[] dodgeClips = animator.GetCurrentAnimatorClipInfo(animator.GetLayerIndex("Actions"));
            if (dodgeClips.Length > 0)
                attributes.SetInviniciblity(dodgeClips[0].clip.length * 0.35f);
            else
                attributes.SetInviniciblity(0);
        }

        Animator animator;
        Attributes attributes;
        WeaponHandler weaponHandler;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            attributes = GetComponentInParent<Attributes>();
            weaponHandler = GetComponentInParent<WeaponHandler>();
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