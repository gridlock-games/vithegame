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
            if (IsServer)
            {
                PlayActionOnServer(actionClip.name);
            }
            else
            {
                PlayActionServerRpc(actionClip.name);
            }
        }

        private ActionClip.ClipType lastClipType;

        private void PlayActionOnServer(string actionStateName)
        {
            ActionClip actionClip = weaponHandler.GetWeapon().GetActionClipByName(actionStateName);

            if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty"))
            {
                if (actionClip.GetClipType() == ActionClip.ClipType.Dodge & lastClipType == ActionClip.ClipType.Dodge) { return; }
            }

            // Check stamina and rage requirements and apply statuses
            if (actionClip.GetClipType() == ActionClip.ClipType.Dodge)
            {
                if (actionClip.agentStaminaDamage > attributes.GetStamina()) { return; }
                StartCoroutine(SetInvincibleStatusOnDodge(actionStateName));
            }
            else if (actionClip.GetClipType() == ActionClip.ClipType.HeavyAttack)
            {
                if (actionClip.agentStaminaDamage > attributes.GetStamina()) { return; }
            }

            attributes.AddStamina(-actionClip.agentStaminaDamage);
            weaponHandler.SetActionClip(actionClip);

            if (actionClip.GetClipType() == ActionClip.ClipType.HitReaction)
            {
                animator.CrossFade(actionStateName, 0.15f, animator.GetLayerIndex("Actions"), 0);
            }
            else
            {
                if (animator.GetNextAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName(actionStateName)) { return; }
                animator.CrossFade(actionStateName, 0.15f, animator.GetLayerIndex("Actions"));
            }

            PlayActionClientRpc(actionStateName);
            lastClipType = actionClip.GetClipType();
        }

        [ServerRpc(RequireOwnership = false)] private void PlayActionServerRpc(string actionStateName) { PlayActionOnServer(actionStateName); }

        [ClientRpc]
        private void PlayActionClientRpc(string actionStateName)
        {
            if (IsServer) { return; }

            ActionClip actionClip = weaponHandler.GetWeapon().GetActionClipByName(actionStateName);
            if (actionClip.GetClipType() == ActionClip.ClipType.HitReaction)
            {
                animator.CrossFade(actionStateName, 0.15f, animator.GetLayerIndex("Actions"), 0);
            }
            else
            {
                animator.CrossFade(actionStateName, 0.15f, animator.GetLayerIndex("Actions"));
            }

            weaponHandler.SetActionClip(actionClip);
            if (actionClip.GetClipType() == ActionClip.ClipType.Dodge) { StartCoroutine(SetInvincibleStatusOnDodge(actionStateName)); }
        }

        private IEnumerator SetInvincibleStatusOnDodge(string actionStateName)
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