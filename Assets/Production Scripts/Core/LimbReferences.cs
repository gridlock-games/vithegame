using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Vi.ProceduralAnimations;

namespace Vi.Core
{
    public class LimbReferences : MonoBehaviour
    {
        public enum Hand
        {
            RightHand,
            LeftHand
        }

        public void AimHand(Hand hand, bool isAiming)
        {
            float weight = isAiming ? 1 : 0;
            if (hand == Hand.RightHand)
            {
                rightHandAimRig.weight = weight;
            }
            else if (hand == Hand.LeftHand)
            {
                leftHandAimRig.weight = weight;
            }
            animator.SetBool("Aiming", isAiming);
            animator.SetLayerWeight(animator.GetLayerIndex("Aiming"), weight);
        }

        public void ReachHand(Hand hand, Transform reachTarget, bool isReaching)
        {
            float weight = isReaching ? 1 : 0;
            if (hand == Hand.RightHand)
            {
                rightHandReachRig.weight = weight;
                rightHandReachRig.GetComponentInChildren<FollowTarget>().target = reachTarget;
            }
            else if (hand == Hand.LeftHand)
            {
                leftHandReachRig.weight = weight;
                leftHandReachRig.GetComponentInChildren<FollowTarget>().target = reachTarget;
            }
        }

        private Animator animator;
        private void Start()
        {
            animator = GetComponent<Animator>();
        }

        [SerializeField] private GameObject rightHand;
        [SerializeField] private GameObject leftHand;

        [Header("IK Settings")]
        [SerializeField] private RigWeightTarget rightHandAimRig;
        [SerializeField] private RigWeightTarget leftHandAimRig;
        [SerializeField] private RigWeightTarget rightHandReachRig;
        [SerializeField] private RigWeightTarget leftHandReachRig;

        [Header("Hand IK Settings")]
        public Vector3 rightHandAimForwardDir = new Vector3(0, 0, 1);
        public Vector3 rightHandAimUpDir = new Vector3(0, 1, 0);

        public Vector3 rightHandAimIKOffset;

        public Vector3 leftHandPosOffset;
        public Vector3 leftHandRotOffset;
    }
}