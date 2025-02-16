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
        public void AimHand(Hand hand, Vector3 handAimIKOffset, bool isAiming, bool shouldAimBody, Vector3 bodyAimIKOffset, BodyAimType bodyAimType, Vector3 offHandIKOffset)
        {
            float weight = isAiming ? 1 : 0;
            shouldAimBody = shouldAimBody & isAiming;
            if (hand == Hand.RightHand)
            {
                if (!rightHandAimRig.GetRig()) { return; }
                rightHandAimRig.weight = weight;
                rightHandAimConstraint.data.offset = handAimIKOffset;

                rightAimOffHandRotationConstraint.data.offset = offHandIKOffset;

                switch (bodyAimType)
                {
                    case BodyAimType.Normal:
                        if (rightHandAimBodyConstraint)
                        {
                            rightHandAimBodyConstraintTarget.weight = shouldAimBody ? 1 : 0;
                            rightHandAimBodyConstraint.data.offset = bodyAimIKOffset;
                        }
                        break;
                    case BodyAimType.Inverted:
                        if (rightHandAimBodyInvertedConstraint)
                        {
                            rightHandAimBodyInvertedConstraintTarget.weight = shouldAimBody ? 1 : 0;
                            rightHandAimBodyInvertedConstraint.data.offset = bodyAimIKOffset;
                        }
                        break;
                    case BodyAimType.None:
                        if (rightHandAimBodyConstraint)
                        {
                            rightHandAimBodyConstraintTarget.weight = 0;
                            rightHandAimBodyConstraint.data.offset = Vector3.zero;
                        }

                        if (rightHandAimBodyInvertedConstraint)
                        {
                            rightHandAimBodyInvertedConstraintTarget.weight = 0;
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

                leftAimOffHandRotationConstraint.data.offset = offHandIKOffset;

                switch (bodyAimType)
                {
                    case BodyAimType.Normal:
                        if (leftHandAimBodyConstraint)
                        {
                            leftHandAimBodyConstraintTarget.weight = shouldAimBody ? 1 : 0;
                            leftHandAimBodyConstraint.data.offset = bodyAimIKOffset;
                        }
                        break;
                    case BodyAimType.Inverted:
                        if (leftHandAimBodyInvertedConstraint)
                        {
                            leftHandAimBodyInvertedConstraintTarget.weight = shouldAimBody ? 1 : 0;
                            leftHandAimBodyInvertedConstraint.data.offset = bodyAimIKOffset;
                        }
                        break;
                    case BodyAimType.None:
                        if (leftHandAimBodyConstraint)
                        {
                            leftHandAimBodyConstraintTarget.weight = 0;
                            leftHandAimBodyConstraint.data.offset = Vector3.zero;
                        }

                        if (leftHandAimBodyInvertedConstraint)
                        {
                            leftHandAimBodyInvertedConstraintTarget.weight = 0;
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

        private ConstraintWeightTarget rightHandAimBodyConstraintTarget;
        private ConstraintWeightTarget rightHandAimBodyInvertedConstraintTarget;
        private ConstraintWeightTarget leftHandAimBodyConstraintTarget;
        private ConstraintWeightTarget leftHandAimBodyInvertedConstraintTarget;

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

            if (rightHandAimBodyConstraint) { rightHandAimBodyConstraintTarget = rightHandAimBodyConstraint.GetComponent<ConstraintWeightTarget>(); }
            if (rightHandAimBodyInvertedConstraint) { rightHandAimBodyInvertedConstraintTarget = rightHandAimBodyInvertedConstraint.GetComponent<ConstraintWeightTarget>(); }
            if (leftHandAimBodyConstraint) { leftHandAimBodyConstraintTarget = leftHandAimBodyConstraint.GetComponent<ConstraintWeightTarget>(); }
            if (leftHandAimBodyInvertedConstraint) { leftHandAimBodyInvertedConstraintTarget = leftHandAimBodyInvertedConstraint.GetComponent<ConstraintWeightTarget>(); }
        }

        private void OnEnable()
        {
            foreach (Rigidbody rigidbody in GetComponentsInChildren<Rigidbody>())
            {
                NetworkPhysicsSimulation.AddRigidbody(rigidbody);
            }

            if (rightHandAimRig) { rightHandAimRig.weight = 0; rightHandAimRig.GetRig().weight = 0; }
            if (rightHandAimBodyConstraint) { rightHandAimBodyConstraintTarget.weight = 0; rightHandAimBodyConstraint.weight = 0; }
            if (rightHandAimBodyInvertedConstraint) { rightHandAimBodyInvertedConstraintTarget.weight = 0; rightHandAimBodyInvertedConstraint.weight = 0; }
            if (leftHandAimRig) { leftHandAimRig.weight = 0; leftHandAimRig.GetRig().weight = 0; }
            if (leftHandAimBodyConstraint) { leftHandAimBodyConstraintTarget.weight = 0; leftHandAimBodyConstraint.weight = 0; }
            if (leftHandAimBodyInvertedConstraint) { leftHandAimBodyInvertedConstraintTarget.weight = 0; leftHandAimBodyInvertedConstraint.weight = 0; }
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
            if (rightHandAimBodyConstraint) { rightHandAimBodyConstraintTarget.weight = 0; rightHandAimBodyConstraint.weight = 0; }
            if (rightHandAimBodyInvertedConstraint) { rightHandAimBodyInvertedConstraintTarget.weight = 0; rightHandAimBodyInvertedConstraint.weight = 0; }
            if (leftHandAimRig) { leftHandAimRig.weight = 0; leftHandAimRig.GetRig().weight = 0; }
            if (leftHandAimBodyConstraint) { leftHandAimBodyConstraintTarget.weight = 0; leftHandAimBodyConstraint.weight = 0; }
            if (leftHandAimBodyInvertedConstraint) { leftHandAimBodyInvertedConstraintTarget.weight = 0; leftHandAimBodyInvertedConstraint.weight = 0; }
            if (rightHandReachRig) { rightHandReachRig.weight = 0; rightHandReachRig.GetRig().weight = 0; }
            if (leftHandReachRig) { leftHandReachRig.weight = 0; leftHandReachRig.GetRig().weight = 0; }
            if (meleeVerticalAimRig) { meleeVerticalAimRig.weight = 0; meleeVerticalAimRig.GetRig().weight = 0; }

            aimingDictionary[Hand.RightHand] = false;
            aimingDictionary[Hand.LeftHand] = false;
        }

        private Dictionary<Weapon.WeaponBone, Transform> weaponBoneMapping = new Dictionary<Weapon.WeaponBone, Transform>();

        public RigWeightTarget GetAimRigByHand(LimbReferences.Hand hand)
        {
            switch (hand)
            {
                case Hand.LeftHand:
                    return leftHandAimRig;
                case Hand.RightHand:
                    return rightHandAimRig;
                default:
                    Debug.LogError("Unsure how to handle hand for GetAimRigWeight()" + hand);
                    break;
            }
            return null;
        }

        [Header("IK Settings")]
        public AimTargetIKSolver aimTargetIKSolver;
        [SerializeField] private RigWeightTarget rightHandAimRig;
        [SerializeField] private MultiAimConstraint rightHandAimBodyConstraint;
        [SerializeField] private MultiAimConstraint rightHandAimBodyInvertedConstraint;
        [SerializeField] private MultiAimConstraint rightHandAimConstraint;
        [SerializeField] private MultiRotationConstraint rightAimOffHandRotationConstraint;
        [SerializeField] private RigWeightTarget leftHandAimRig;
        [SerializeField] private MultiAimConstraint leftHandAimBodyConstraint;
        [SerializeField] private MultiAimConstraint leftHandAimBodyInvertedConstraint;
        [SerializeField] private MultiAimConstraint leftHandAimConstraint;
        [SerializeField] private MultiRotationConstraint leftAimOffHandRotationConstraint;
        [SerializeField] private RigWeightTarget rightHandReachRig;
        [SerializeField] private RigWeightTarget leftHandReachRig;
        [SerializeField] private RigWeightTarget meleeVerticalAimRig;
        [SerializeField] private MultiRotationConstraint meleeVerticalAimConstraint;
        [SerializeField] private RigWeightTarget headIKRig;
        [Header("Animation Rotation Offset Settings")]
        [SerializeField] private MultiRotationConstraint rotationOffsetConstraint;
        [SerializeField] private Axis rotationOffsetAxis = Axis.Z;
        [Header("Rest Of Assignments")]
        [SerializeField] private Transform middleSpine;

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

        public void SetHeadIKWeight(float newWeightTarget)
        {
            if (headIKRig)
            {
                headIKRig.weight = newWeightTarget;
            }
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

        public Transform Hips { get { return hips; } }
        [SerializeField] private Transform hips;

        [Header("For Weapon Parenting")]
        [SerializeField] private Weapon.WeaponBone[] keys = new Weapon.WeaponBone[0];
        [SerializeField] private Transform[] values = new Transform[0];

        public Transform GetBoneTransform(Weapon.WeaponBone weaponBone)
        {
            if (weaponBoneMapping.ContainsKey(weaponBone))
            {
                return weaponBoneMapping[weaponBone];
            }
            else
            {
                if (animator.avatar)
                {
                    if (animator.avatar.isHuman)
                    {
                        return animator.GetBoneTransform((HumanBodyBones)weaponBone);
                    }
                }
                Debug.LogError("Weapon bone not present in mapping! " + weaponBone + " " + name);
            }
            return null;
        }

        [System.Serializable]
        private struct StowedWeaponParent
        {
            public Weapon.StowedWeaponParentType stowedWeaponParentType;
            public Transform bone;
        }

        [SerializeField] private StowedWeaponParent[] stowedWeaponParents;
        public Transform GetStowedWeaponParent(Weapon.StowedWeaponParentType stowedWeaponParentType)
        {
            if (System.Array.Exists(stowedWeaponParents, item => item.stowedWeaponParentType == stowedWeaponParentType))
            {
                StowedWeaponParent stowedWeaponParent = System.Array.Find(stowedWeaponParents, item => item.stowedWeaponParentType == stowedWeaponParentType);
                return stowedWeaponParent.bone;
            }
            return middleSpine;
        }

#if UNITY_EDITOR
        [ContextMenu("Call On Validate")]
        private void CallOnValidate() { OnValidate(); }

        private void OnValidate()
        {
            if (Application.isPlaying) { return; }

            if (keys.Length != values.Length) { Debug.LogError("Keys and Values must be the same length!"); }

            if (!hips)
            {
                Transform root = transform.Find("root");
                if (!root) { root = transform.Find("Root"); }
                if (root)
                {
                    hips = root.Find("pelvis");
                    if (!hips) { hips = root.Find("Pelvis"); }
                    if (hips) { UnityEditor.EditorUtility.SetDirty(this); }
                }
            }

            if (!aimTargetIKSolver) { aimTargetIKSolver = transform.Find("AimTarget")?.GetComponent<AimTargetIKSolver>(); }

            if (!rightHandAimRig) { rightHandAimRig = transform.Find("RightAimRig")?.GetComponent<RigWeightTarget>(); }
            if (!rightHandAimBodyConstraint) { rightHandAimBodyConstraint = transform.Find("RightAimRig")?.Find("BodyAim")?.GetComponent<MultiAimConstraint>(); }
            if (!rightHandAimBodyInvertedConstraint) { rightHandAimBodyInvertedConstraint = transform.Find("RightAimRig")?.Find("BodyAimInverted")?.GetComponent<MultiAimConstraint>(); }
            if (!rightHandAimConstraint) { rightHandAimConstraint = transform.Find("RightAimRig")?.Find("RightHandAimConstraint")?.GetComponent<MultiAimConstraint>(); }
            if (!rightAimOffHandRotationConstraint) { rightAimOffHandRotationConstraint = transform.Find("RightAimRig")?.Find("OffHandRotationOffset")?.GetComponent<MultiRotationConstraint>(); }

            if (!leftHandAimRig) { leftHandAimRig = transform.Find("LeftAimRig")?.GetComponent<RigWeightTarget>(); }
            if (!leftHandAimBodyConstraint) { leftHandAimBodyConstraint = transform.Find("LeftAimRig")?.Find("BodyAim")?.GetComponent<MultiAimConstraint>(); }
            if (!leftHandAimBodyInvertedConstraint) { leftHandAimBodyInvertedConstraint = transform.Find("LeftAimRig")?.Find("BodyAimInverted")?.GetComponent<MultiAimConstraint>(); }
            if (!leftHandAimConstraint) { leftHandAimConstraint = transform.Find("LeftAimRig")?.Find("LeftHandAimConstraint")?.GetComponent<MultiAimConstraint>(); }
            if (!leftAimOffHandRotationConstraint) { leftAimOffHandRotationConstraint = transform.Find("LeftAimRig")?.Find("OffHandRotationOffset")?.GetComponent<MultiRotationConstraint>(); }

            if (!meleeVerticalAimRig) { meleeVerticalAimRig = transform.Find("MeleeVerticalAimRig")?.GetComponent<RigWeightTarget>(); }
            if (!meleeVerticalAimConstraint) { meleeVerticalAimConstraint = transform.Find("MeleeVerticalAimRig")?.Find("VerticalAimRotationConstraint")?.GetComponent<MultiRotationConstraint>(); }

            if (!rightHandReachRig) { rightHandReachRig = transform.Find("RightReachRig")?.GetComponent<RigWeightTarget>(); }
            if (!leftHandReachRig) { leftHandReachRig = transform.Find("LeftReachRig")?.GetComponent<RigWeightTarget>(); }

            if (!rotationOffsetConstraint) { rotationOffsetConstraint = transform.Find("RotationOffsetRig")?.Find("RotationOffsetConstraint")?.GetComponent<MultiRotationConstraint>(); }

            if (TryGetComponent(out Animator animator))
            {
                if (animator.avatar)
                {
                    if (animator.avatar.isHuman)
                    {
                        middleSpine = animator.GetBoneTransform(HumanBodyBones.Spine).GetChild(0);

                        if (rightHandReachRig)
                        {
                            rightHandReachRig.GetComponentInChildren<TwoBoneIKConstraint>().data.tip = animator.GetBoneTransform(HumanBodyBones.RightHand);
                        }

                        if (leftHandReachRig)
                        {
                            leftHandReachRig.GetComponentInChildren<TwoBoneIKConstraint>().data.tip = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                        }

                        if (rotationOffsetConstraint) { rotationOffsetConstraint.data.constrainedObject = transform.Find("Root") ? transform.Find("Root") : transform.Find("root"); }

                        if (meleeVerticalAimConstraint) { meleeVerticalAimConstraint.data.constrainedObject = middleSpine; }

                        if (rightHandAimBodyConstraint) { rightHandAimBodyConstraint.data.constrainedObject = animator.GetBoneTransform(HumanBodyBones.Spine); }
                        if (rightHandAimBodyInvertedConstraint) { rightHandAimBodyInvertedConstraint.data.constrainedObject = animator.GetBoneTransform(HumanBodyBones.Spine); }
                        if (rightHandAimConstraint) { rightHandAimConstraint.data.constrainedObject = animator.GetBoneTransform(HumanBodyBones.RightHand); }
                        if (rightAimOffHandRotationConstraint) { rightAimOffHandRotationConstraint.data.constrainedObject = animator.GetBoneTransform(HumanBodyBones.LeftHand); }

                        if (leftHandAimBodyConstraint) { leftHandAimBodyConstraint.data.constrainedObject = animator.GetBoneTransform(HumanBodyBones.Spine); }
                        if (leftHandAimBodyInvertedConstraint) { leftHandAimBodyInvertedConstraint.data.constrainedObject = animator.GetBoneTransform(HumanBodyBones.Spine); }
                        if (leftHandAimConstraint) { leftHandAimConstraint.data.constrainedObject = animator.GetBoneTransform(HumanBodyBones.LeftHand); }
                        if (leftAimOffHandRotationConstraint) { leftAimOffHandRotationConstraint.data.constrainedObject = animator.GetBoneTransform(HumanBodyBones.RightHand); }
                    }
                }
            }
        }
#endif
    }
}