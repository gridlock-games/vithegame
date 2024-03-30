using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ProceduralAnimations;
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

        public bool IsAiming(Hand hand) { return aimingDictionary[hand]; }

        private Dictionary<Hand, bool> aimingDictionary = new Dictionary<Hand, bool>();
        public void AimHand(Hand hand, Vector3 handAimIKOffset, bool isAiming, bool instantAim, bool shouldAimBody, Vector3 bodyAimIKOffset, BodyAimType bodyAimType)
        {
            float weight = isAiming ? 1 : 0;
            if (hand == Hand.RightHand)
            {
                if (!rightHandAimRig.GetRig()) { return; }
                rightHandAimRig.weight = weight;
                rightHandAimConstraint.data.offset = handAimIKOffset;

                switch (bodyAimType)
                {
                    case BodyAimType.Normal:
                        if (rightHandAimBodyConstraint)
                        {
                            rightHandAimBodyConstraint.weight = shouldAimBody ? 1 : 0;
                            rightHandAimBodyConstraint.data.offset = bodyAimIKOffset;
                        }
                        break;
                    case BodyAimType.Inverted:
                        if (rightHandAimBodyInvertedConstraint)
                        {
                            rightHandAimBodyInvertedConstraint.weight = shouldAimBody ? 1 : 0;
                            rightHandAimBodyInvertedConstraint.data.offset = bodyAimIKOffset;
                        }
                        break;
                    default:
                        Debug.LogError("Not sure how to handle body type " + bodyAimType);
                        break;
                }

                if (instantAim) { rightHandAimRig.GetRig().weight = weight; }
            }
            else if (hand == Hand.LeftHand)
            {
                if (!leftHandAimRig.GetRig()) { return; }
                leftHandAimRig.weight = weight;
                leftHandAimConstraint.data.offset = handAimIKOffset;

                switch (bodyAimType)
                {
                    case BodyAimType.Normal:
                        if (leftHandAimBodyConstraint)
                        {
                            leftHandAimBodyConstraint.weight = shouldAimBody ? 1 : 0;
                            leftHandAimBodyConstraint.data.offset = bodyAimIKOffset;
                        }
                        break;
                    case BodyAimType.Inverted:
                        if (leftHandAimBodyInvertedConstraint)
                        {
                            leftHandAimBodyInvertedConstraint.weight = shouldAimBody ? 1 : 0;
                            leftHandAimBodyInvertedConstraint.data.offset = bodyAimIKOffset;
                        }
                        break;
                    default:
                        Debug.LogError("Not sure how to handle body type " + bodyAimType);
                        break;
                }

                if (instantAim) { leftHandAimRig.GetRig().weight = weight; }
            }
            animator.SetBool("Aiming", isAiming);
            animator.SetLayerWeight(animator.GetLayerIndex("Aiming"), weight);
            StartCoroutine(SetHandAimingBool(hand, isAiming));
        }

        private IEnumerator SetHandAimingBool(Hand hand, bool isAiming)
        {
            yield return null;
            aimingDictionary[hand] = isAiming;
        }

        public void ReachHand(Hand hand, Transform reachTarget, bool isReaching, bool instantReach)
        {
            float weight = isReaching & reachTarget ? 1 : 0;
            if (hand == Hand.RightHand)
            {
                if (!rightHandReachRig.GetRig()) { return; }
                rightHandReachRig.weight = weight;
                RightHandFollowTarget.target = reachTarget;
                if (instantReach) { rightHandReachRig.GetRig().weight = weight; }
            }
            else if (hand == Hand.LeftHand)
            {
                if (!leftHandReachRig.GetRig()) { return; }
                leftHandReachRig.weight = weight;
                LeftHandFollowTarget.target = reachTarget;
                if (instantReach) { leftHandReachRig.GetRig().weight = weight; }
            }
        }

        private Animator animator;
        public FollowTarget RightHandFollowTarget { get; private set; }
        public FollowTarget LeftHandFollowTarget { get; private set; }

        private void Awake()
        {
            animator = GetComponent<Animator>();

            if (rightHandReachRig) { RightHandFollowTarget = rightHandReachRig.GetComponentInChildren<FollowTarget>(); }
            if (leftHandReachRig) { LeftHandFollowTarget = leftHandReachRig.GetComponentInChildren<FollowTarget>(); }

            aimingDictionary.Add(Hand.RightHand, false);
            aimingDictionary.Add(Hand.LeftHand, false);

            rightHandAimRig.weight = 0;
            rightHandAimBodyConstraint.weight = 0;
            rightHandAimBodyInvertedConstraint.weight = 0;
            leftHandAimRig.weight = 0;
            leftHandAimBodyConstraint.weight = 0;
            leftHandAimBodyInvertedConstraint.weight = 0;
            rightHandReachRig.weight = 0;
            leftHandReachRig.weight = 0;
            meleeVerticalAimRig.weight = 0;
        }

        public RigWeightTarget GetRightHandReachRig() { return rightHandReachRig; }
        public RigWeightTarget GetLeftHandReachRig() { return leftHandReachRig; }

        [Header("IK Settings")]
        public AimTargetIKSolver aimTargetIKSolver;
        [SerializeField] private RigWeightTarget rightHandAimRig;
        [SerializeField] private MultiAimConstraint rightHandAimBodyConstraint;
        [SerializeField] private MultiAimConstraint rightHandAimBodyInvertedConstraint;
        [SerializeField] private MultiAimConstraint rightHandAimConstraint;
        [SerializeField] private RigWeightTarget leftHandAimRig;
        [SerializeField] private MultiAimConstraint leftHandAimBodyConstraint;
        [SerializeField] private MultiAimConstraint leftHandAimBodyInvertedConstraint;
        [SerializeField] private MultiAimConstraint leftHandAimConstraint;
        [SerializeField] private RigWeightTarget rightHandReachRig;
        [SerializeField] private RigWeightTarget leftHandReachRig;
        [SerializeField] private RigWeightTarget meleeVerticalAimRig;
        [SerializeField] private MultiRotationConstraint meleeVerticalAimConstraint;
        [SerializeField] private MultiRotationConstraint rotationOffsetConstraint;

        private const float rotationConstraintOffsetSpeed = 12;
        public void SetMeleeVerticalAimConstraintOffset(float zAngle)
        {
            zAngle = Mathf.Clamp(zAngle, -35, 35);
            meleeVerticalAimConstraint.data.offset = Vector3.Lerp(meleeVerticalAimConstraint.data.offset, new Vector3(0, 0, zAngle), Time.deltaTime * rotationConstraintOffsetSpeed);
        }

        public void SetMeleeVerticalAimEnabled(bool isEnabled)
        {
            meleeVerticalAimRig.weight = isEnabled ? 1 : 0;
        }

        public void SetRotationOffset(float zAngle)
        {
            rotationOffsetConstraint.data.offset = Vector3.Lerp(rotationOffsetConstraint.data.offset, new Vector3(0, 0, zAngle), Time.deltaTime * rotationConstraintOffsetSpeed);
        }

        public enum BodyAimType
        {
            Normal,
            Inverted
        }
    }
}