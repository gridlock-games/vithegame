using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

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
            rightHandAimRig.weight = weight;
            animator.SetBool("Aiming", isAiming);
            animator.SetLayerWeight(animator.GetLayerIndex("Aiming"), weight);
        }

        private Animator animator;
        private void Start()
        {
            animator = GetComponent<Animator>();
        }

        [SerializeField] private GameObject rightHand;
        [SerializeField] private GameObject leftHand;

        [Header("IK Settings")]
        public Rig rightHandAimRig;

        [Header("Hand IK Settings")]
        public Vector3 rightHandAimForwardDir = new Vector3(0, 0, 1);
        public Vector3 rightHandAimUpDir = new Vector3(0, 1, 0);

        public Vector3 rightHandAimIKOffset;

        public Vector3 leftHandPosOffset;
        public Vector3 leftHandRotOffset;
    }
}