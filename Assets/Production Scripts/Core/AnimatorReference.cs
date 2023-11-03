using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Core
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(LimbReferences))]
    [RequireComponent(typeof(GlowRenderer))]
    public class AnimatorReference : MonoBehaviour
    {
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

        Animator animator;
        WeaponHandler weaponHandler;
        LimbReferences limbReferences;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            weaponHandler = GetComponentInParent<WeaponHandler>();
            limbReferences = GetComponent<LimbReferences>();
        }

        public bool ShouldApplyRootMotion() { return !animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty"); }

        private void OnAnimatorMove()
        {
            // Check if the current animator state is not "Empty" and update networkRootMotion and localRootMotion accordingly
            if (ShouldApplyRootMotion())
            {
                //networkRootMotion += animator.deltaPosition * weaponHandler.CurrentActionClip.rootMotionMulitplier;
                //localRootMotion += animator.deltaPosition * weaponHandler.CurrentActionClip.rootMotionMulitplier;
                float normalizedTime = 0;
                if (animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName(weaponHandler.CurrentActionClip.name))
                {
                    normalizedTime = animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).normalizedTime;
                }
                else if (animator.GetNextAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName(weaponHandler.CurrentActionClip.name))
                {
                    normalizedTime = animator.GetNextAnimatorStateInfo(animator.GetLayerIndex("Actions")).normalizedTime;
                }

                Vector3 worldSpaceRootMotion = transform.TransformDirection(animator.deltaPosition);
                worldSpaceRootMotion.x *= weaponHandler.CurrentActionClip.rootMotionSidesMultiplier.Evaluate(normalizedTime);
                worldSpaceRootMotion.y *= weaponHandler.CurrentActionClip.rootMotionVerticalMultiplier.Evaluate(normalizedTime);
                worldSpaceRootMotion.z *= weaponHandler.CurrentActionClip.rootMotionForwardMultiplier.Evaluate(normalizedTime);
                Vector3 curveAdjustedLocalRootMotion = transform.InverseTransformDirection(worldSpaceRootMotion);
                
                networkRootMotion += curveAdjustedLocalRootMotion;
                localRootMotion += curveAdjustedLocalRootMotion;
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (limbReferences.RightHandFollowTarget)
            {
                if (limbReferences.RightHandFollowTarget.target)
                {
                    animator.SetIKPosition(AvatarIKGoal.RightHand, limbReferences.RightHandFollowTarget.target.position);
                    animator.SetIKPositionWeight(AvatarIKGoal.RightHand, limbReferences.GetRightHandReachRig().weight);
                }
            }

            if (limbReferences.LeftHandFollowTarget)
            {
                if (limbReferences.LeftHandFollowTarget.target)
                {
                    animator.SetIKPosition(AvatarIKGoal.LeftHand, limbReferences.LeftHandFollowTarget.target.position);
                    animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, limbReferences.GetLeftHandReachRig().weight);
                }
            }
        }
    }
}