using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Vi.ScriptableObjects;
using Vi.Utility;

namespace Vi.ProceduralAnimations
{
    [DisallowMultipleComponent]
    public class LimbReferences : MonoBehaviour
    {
        public enum Hand
        {
            RightHand,
            LeftHand
        }

        public bool IsAiming(Hand hand) { return aimingDictionary[hand]; }

        private Dictionary<Hand, bool> aimingDictionary = new Dictionary<Hand, bool>();
        public void AimHand(Hand hand, Vector3 handAimIKOffset, bool isAiming, bool shouldAimBody, Vector3 bodyAimIKOffset, BodyAimType bodyAimType)
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
                    case BodyAimType.None:
                        if (rightHandAimBodyConstraint)
                        {
                            rightHandAimBodyConstraint.weight = 0;
                            rightHandAimBodyConstraint.data.offset = Vector3.zero;
                        }

                        if (rightHandAimBodyInvertedConstraint)
                        {
                            rightHandAimBodyInvertedConstraint.weight = 0;
                            rightHandAimBodyInvertedConstraint.data.offset = Vector3.zero;
                        }
                        break;
                    default:
                        Debug.LogError("Not sure how to handle body type " + bodyAimType);
                        break;
                }
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
                    case BodyAimType.None:
                        if (leftHandAimBodyConstraint)
                        {
                            leftHandAimBodyConstraint.weight = 0;
                            leftHandAimBodyConstraint.data.offset = Vector3.zero;
                        }

                        if (leftHandAimBodyInvertedConstraint)
                        {
                            leftHandAimBodyInvertedConstraint.weight = 0;
                            leftHandAimBodyInvertedConstraint.data.offset = Vector3.zero;
                        }
                        break;
                    default:
                        Debug.LogError("Not sure how to handle body type " + bodyAimType);
                        break;
                }
            }
            animator.SetLayerWeight(animator.GetLayerIndex("Aiming"), weight);
            StartCoroutine(SetHandAimingBool(hand, isAiming));
        }

        private IEnumerator SetHandAimingBool(Hand hand, bool isAiming)
        {
            yield return null;
            aimingDictionary[hand] = isAiming;
        }

        public void ReachHand(Hand hand, Transform reachTarget, bool isReaching)
        {
            float weight = isReaching & reachTarget ? 1 : 0;
            if (hand == Hand.RightHand)
            {
                if (!rightHandReachRig.GetRig()) { return; }
                rightHandReachRig.weight = weight;
                RightHandFollowTarget.target = reachTarget;
            }
            else if (hand == Hand.LeftHand)
            {
                if (!leftHandReachRig.GetRig()) { return; }
                leftHandReachRig.weight = weight;
                LeftHandFollowTarget.target = reachTarget;
            }
        }

        public void OnCannotAim()
        {
            animator.SetLayerWeight(animator.GetLayerIndex("Aiming"), 0);
            leftHandAimRig.weight = 0;
            rightHandAimRig.weight = 0;
            leftHandReachRig.weight = 0;
            rightHandReachRig.weight = 0;
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

            for (int i = 0; i < keys.Length; i++)
            {
                if (weaponBoneMapping.ContainsKey(keys[i]))
                {
                    Debug.LogError("Duplciate key found " + keys[i] + " " + name);
                    continue;
                }
                weaponBoneMapping.Add(keys[i], values[i]);
            }
        }

        private void OnEnable()
        {
            foreach (Rigidbody rigidbody in GetComponentsInChildren<Rigidbody>())
            {
                NetworkPhysicsSimulation.AddRigidbody(rigidbody);
            }

            if (rightHandAimRig) { rightHandAimRig.weight = 0; rightHandAimRig.GetRig().weight = 0; }
            if (rightHandAimBodyConstraint) { rightHandAimBodyConstraint.weight = 0; }
            if (rightHandAimBodyInvertedConstraint) { rightHandAimBodyInvertedConstraint.weight = 0; }
            if (leftHandAimRig) { leftHandAimRig.weight = 0; leftHandAimRig.GetRig().weight = 0; }
            if (leftHandAimBodyConstraint) { leftHandAimBodyConstraint.weight = 0; }
            if (leftHandAimBodyInvertedConstraint) { leftHandAimBodyInvertedConstraint.weight = 0; }
            if (rightHandReachRig) { rightHandReachRig.weight = 0; rightHandReachRig.GetRig().weight = 0; }
            if (leftHandReachRig) { leftHandReachRig.weight = 0; leftHandReachRig.GetRig().weight = 0; }
            if (meleeVerticalAimRig) { meleeVerticalAimRig.weight = 0; meleeVerticalAimRig.GetRig().weight = 0; }

            aimingDictionary[Hand.RightHand] = false;
            aimingDictionary[Hand.LeftHand] = false;
        }

        private void OnDisable()
        {
            foreach (Rigidbody rigidbody in GetComponentsInChildren<Rigidbody>())
            {
                NetworkPhysicsSimulation.RemoveRigidbody(rigidbody);
            }

            if (rightHandAimRig) { rightHandAimRig.weight = 0; rightHandAimRig.GetRig().weight = 0; }
            if (rightHandAimBodyConstraint) { rightHandAimBodyConstraint.weight = 0; }
            if (rightHandAimBodyInvertedConstraint) { rightHandAimBodyInvertedConstraint.weight = 0; }
            if (leftHandAimRig) { leftHandAimRig.weight = 0; leftHandAimRig.GetRig().weight = 0; }
            if (leftHandAimBodyConstraint) { leftHandAimBodyConstraint.weight = 0; }
            if (leftHandAimBodyInvertedConstraint) { leftHandAimBodyInvertedConstraint.weight = 0; }
            if (rightHandReachRig) { rightHandReachRig.weight = 0; rightHandReachRig.GetRig().weight = 0; }
            if (leftHandReachRig) { leftHandReachRig.weight = 0; leftHandReachRig.GetRig().weight = 0; }
            if (meleeVerticalAimRig) { meleeVerticalAimRig.weight = 0; meleeVerticalAimRig.GetRig().weight = 0; }

            aimingDictionary[Hand.RightHand] = false;
            aimingDictionary[Hand.LeftHand] = false;
        }

        private Dictionary<Weapon.WeaponBone, Transform> weaponBoneMapping = new Dictionary<Weapon.WeaponBone, Transform>();

        public RigWeightTarget GetRightHandReachRig() { return rightHandReachRig; }
        public RigWeightTarget GetLeftHandReachRig() { return leftHandReachRig; }

        public Transform GetStowedWeaponParent() { return stowedWeaponParent; }

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
        [Header("Animation Rotation Offset Settings")]
        [SerializeField] private MultiRotationConstraint rotationOffsetConstraint;
        [SerializeField] private Axis rotationOffsetAxis = Axis.Z;
        [Header("Weapon Swapping")]
        [SerializeField] private Transform stowedWeaponParent;

        public const float rotationConstraintOffsetSpeed = 12;
        public void SetMeleeVerticalAimConstraintOffset(float zAngle)
        {
            zAngle = Mathf.Clamp(zAngle, -35, 35);
            meleeVerticalAimConstraint.data.offset = Vector3.Lerp(meleeVerticalAimConstraint.data.offset, new Vector3(0, 0, zAngle), Time.deltaTime * rotationConstraintOffsetSpeed);
        }

        public void SetMeleeVerticalAimEnabled(bool isEnabled)
        {
            if (!meleeVerticalAimRig) { return; }
            meleeVerticalAimRig.weight = isEnabled ? 1 : 0;
        }

        public void SetRotationOffset(float pitchAngle, float yawAngle, float rollAngle)
        {
            if (!rotationOffsetConstraint) { return; }
            Vector3 targetOffset = Vector3.zero;
            switch (rotationOffsetAxis)
            {
                case Axis.X:
                    targetOffset = new Vector3(yawAngle, rollAngle, pitchAngle);
                    break;
                case Axis.Y:
                    targetOffset = new Vector3(pitchAngle, yawAngle, rollAngle);
                    break;
                case Axis.Z:
                    targetOffset = new Vector3(rollAngle, pitchAngle, yawAngle);
                    break;
                default:
                    Debug.LogError("Unsure how to handle rotation offset axis " + rotationOffsetAxis);
                    break;
            }
            rotationOffsetConstraint.data.offset = Vector3.Lerp(rotationOffsetConstraint.data.offset, targetOffset, Time.deltaTime * rotationConstraintOffsetSpeed);
        }

        public enum BodyAimType
        {
            Normal,
            Inverted,
            None
        }

        private enum Axis
        {
            X,
            Y,
            Z
        }

        [Header("For Generic Rigs")]
        [SerializeField] private Weapon.WeaponBone[] keys = new Weapon.WeaponBone[0];
        [SerializeField] private Transform[] values = new Transform[0];

        private void OnValidate()
        {
            if (keys.Length != values.Length) { Debug.LogError("Keys and Values must be the same length!"); }
        }

        public Transform GetBoneTransform(Weapon.WeaponBone weaponBone)
        {
            if (weaponBoneMapping.ContainsKey(weaponBone))
            {
                return weaponBoneMapping[weaponBone];
            }
            else
            {
                Debug.LogError("Weapon bone not present in mapping! " + weaponBone + " " + name);
            }
            return null;
        }

        public Transform Hips { get { return hips; } }
        [SerializeField] private Transform hips;
    }
}