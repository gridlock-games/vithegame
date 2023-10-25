using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;

// Define the namespace for the script
namespace Vi.Core
{
    // This script requires an Animator component and extends the NetworkBehaviour class
    [RequireComponent(typeof(Animator))]
    public class AnimationHandler : NetworkBehaviour
    {
        // This method plays an action based on the provided ActionClip parameter
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

        // Stores the type of the last action clip played
        private ActionClip.ClipType lastClipType;

        // This method plays the action on the server
        private void PlayActionOnServer(string actionStateName)
        {
            // Retrieve the appropriate ActionClip based on the provided actionStateName
            ActionClip actionClip = weaponHandler.GetWeapon().GetActionClipByName(actionStateName);

            // If we are not at rest and the last clip was a dodge, don't play this clip
            if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty"))
            {
                if (lastClipType == ActionClip.ClipType.Dodge | (actionClip.GetClipType() != ActionClip.ClipType.HitReaction & lastClipType == ActionClip.ClipType.HitReaction)) { return; }
            }

            // Checks if the action is not a hit reaction and prevents the animation from getting stuck
            if (actionClip.GetClipType() != ActionClip.ClipType.HitReaction)
            {
                if (animator.GetNextAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName(actionStateName)) { return; }
            }

            // Check stamina and rage requirements and apply statuses for specific actions
            if (actionClip.GetClipType() == ActionClip.ClipType.Dodge)
            {
                if (actionClip.agentStaminaCost > attributes.GetStamina()) { return; }
                attributes.AddStamina(-actionClip.agentStaminaCost);
                StartCoroutine(SetInvincibleStatusOnDodge(actionStateName));
            }
            else if (actionClip.GetClipType() == ActionClip.ClipType.HeavyAttack)
            {
                if (actionClip.agentStaminaCost > attributes.GetStamina()) { return; }
                attributes.AddStamina(-actionClip.agentStaminaCost);
            }
            else if (actionClip.GetClipType() == ActionClip.ClipType.Ability)
            {
                if (actionClip.agentStaminaCost > attributes.GetStamina()) { return; }
                if (actionClip.agentDefenseCost > attributes.GetDefense()) { return; }
                if (actionClip.agentRageCost > attributes.GetRage()) { return; }
                attributes.AddStamina(-actionClip.agentStaminaCost);
                attributes.AddStamina(-actionClip.agentDefenseCost);
                attributes.AddStamina(-actionClip.agentRageCost);
            }

            // Set the current action clip for the weapon handler
            weaponHandler.SetActionClip(actionClip);

            // Play the action clip based on its type
            if (actionClip.GetClipType() == ActionClip.ClipType.HitReaction)
            {
                animator.CrossFade(actionStateName, 0.15f, animator.GetLayerIndex("Actions"), 0);
            }
            else
            {
                if (animator.GetNextAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName(actionStateName)) { return; }
                animator.CrossFade(actionStateName, 0.15f, animator.GetLayerIndex("Actions"));
            }

            // Invoke the PlayActionClientRpc method on the client side
            PlayActionClientRpc(actionStateName);
            // Update the lastClipType to the current action clip type
            lastClipType = actionClip.GetClipType();
        }

        // Remote Procedure Call method for playing the action on the server
        [ServerRpc(RequireOwnership = false)] 
        private void PlayActionServerRpc(string actionStateName) { PlayActionOnServer(actionStateName); }

        // Remote Procedure Call method for playing the action on the client
        [ClientRpc]
        private void PlayActionClientRpc(string actionStateName)
        {
            if (IsServer) { return; }

            // Retrieve the ActionClip based on the actionStateName
            ActionClip actionClip = weaponHandler.GetWeapon().GetActionClipByName(actionStateName);

            // Play the action clip on the client side based on its type
            if (actionClip.GetClipType() == ActionClip.ClipType.HitReaction)
            {
                animator.CrossFade(actionStateName, 0.15f, animator.GetLayerIndex("Actions"), 0);
            }
            else
            {
                animator.CrossFade(actionStateName, 0.15f, animator.GetLayerIndex("Actions"));
            }

            // Set the current action clip for the weapon handler
            weaponHandler.SetActionClip(actionClip);

            // If the action clip is a dodge, start the SetInvincibleStatusOnDodge coroutine
            if (actionClip.GetClipType() == ActionClip.ClipType.Dodge) { StartCoroutine(SetInvincibleStatusOnDodge(actionStateName)); }
        }

        // Coroutine for setting invincibility status during a dodge
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

        // Declare variables for animator, attributes, and weapon handler
        Animator animator;
        Attributes attributes;
        WeaponHandler weaponHandler;

        // Initialization method to set the references to animator, attributes, and weapon handler
        private void Awake()
        {
            animator = GetComponent<Animator>();
            attributes = GetComponentInParent<Attributes>();
            weaponHandler = GetComponentInParent<WeaponHandler>();
        }

        // Variable to store network root motion
        private Vector3 networkRootMotion;
        // Method to apply network root motion
        public Vector3 ApplyNetworkRootMotion()
        {
            Vector3 _ = networkRootMotion;
            networkRootMotion = Vector3.zero;
            return _;
        }

        // Variable to store local root motion
        private Vector3 localRootMotion;
        // Method to apply local root motion
        public Vector3 ApplyLocalRootMotion()
        {
            Vector3 _ = localRootMotion;
            localRootMotion = Vector3.zero;
            return _;
        }

        public bool ShouldApplyRootMotion() { return !animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty"); }

        // Event handler for animator's movement
        private void OnAnimatorMove()
        {
            // Check if the current animator state is not "Empty" and update networkRootMotion and localRootMotion accordingly
            if (ShouldApplyRootMotion())
            {
                networkRootMotion += animator.deltaPosition * weaponHandler.CurrentActionClip.rootMotionMulitplier;
                localRootMotion += animator.deltaPosition * weaponHandler.CurrentActionClip.rootMotionMulitplier;
            }
        }

        // Event handler for animator's inverse kinematics
        private void OnAnimatorIK(int layerIndex)
        {

        }
    }
}