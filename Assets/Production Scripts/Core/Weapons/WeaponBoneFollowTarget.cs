using UnityEngine;
using Vi.ScriptableObjects;
using Vi.ProceduralAnimations;
using Vi.Utility;

namespace Vi.Core.Weapons
{
    public class WeaponBoneFollowTarget : MonoBehaviour
    {
        [SerializeField] private bool followWhileAiming = true;
        [SerializeField] private Weapon.WeaponBone weaponBoneToFollow;
        [SerializeField] private ChildWeaponBone[] childWeaponBones;
        [SerializeField] private Vector3 attackingLocalPosition;
        [SerializeField] private OffsetData[] offsetData;

        [System.Serializable]
        private struct OffsetData
        {
            public CharacterReference.RaceAndGender raceAndGender;
            public Vector3 offset;
        }

        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;
        private ShooterWeapon shooterWeapon;

        private void Awake()
        {
            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;
            shooterWeapon = GetComponentInParent<ShooterWeapon>();
        }

        private void OnDisable()
        {
            combatAgent = null;
            target = null;
        }

        private CombatAgent combatAgent;
        private Transform target;
        public void Initialize(CombatAgent combatAgent)
        {
            this.combatAgent = combatAgent;
            target = combatAgent.AnimationHandler.LimbReferences.GetBoneTransform(weaponBoneToFollow);
        }

        private Vector3 TargetPosition { get { return target ? target.position + target.rotation * System.Array.Find(offsetData, item => item.raceAndGender == combatAgent.GetRaceAndGender()).offset : Vector3.zero; } }

        [SerializeField] private float lerpSpeed = 8;
        [SerializeField] private float moveTowardsSpeed = 6;

        private Vector3 lastShooterWeaponPosition;

        private const float maxRigWeight = 1;
        private const float minRigWeight = 0.7f;

        private void LateUpdate()
        {
            if (!combatAgent) { return; }
            if (!target) { return; }
            if (!combatAgent.AnimationHandler.LimbReferences) { return; }

            bool evaluated = false;
            if (followWhileAiming)
            {
                if (!shooterWeapon) { Debug.LogError("No shooter weapon found!"); return; }

                if (shooterWeapon.IsStowed) { return; }

                Vector3 weaponPositionDelta = shooterWeapon.transform.position - lastShooterWeaponPosition;
                lastShooterWeaponPosition = shooterWeapon.transform.position;

                if (combatAgent.WeaponHandler.IsAttacking)
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, attackingLocalPosition, Time.deltaTime * lerpSpeed);
                    foreach (ChildWeaponBone childWeaponBone in childWeaponBones)
                    {
                        childWeaponBone.LerpProgressive(ChildWeaponBone.TargetRotationMode.Attacking, Time.deltaTime * lerpSpeed);
                    }
                }
                else if (combatAgent.WeaponHandler.IsInRecovery)
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, Time.deltaTime * lerpSpeed);
                    foreach (ChildWeaponBone childWeaponBone in childWeaponBones)
                    {
                        childWeaponBone.LerpProgressive(ChildWeaponBone.TargetRotationMode.None, Time.deltaTime * lerpSpeed);
                    }
                }
                else // Animations at rest
                {
                    RigWeightTarget rigWeightTarget = combatAgent.AnimationHandler.LimbReferences.GetAimRigByHand(shooterWeapon.GetAimHand());
                    float weight = rigWeightTarget ? rigWeightTarget.GetRig().weight : 0;

                    bool isAiming = false;
                    if (rigWeightTarget)
                    {
                        if (Mathf.Approximately(rigWeightTarget.weight, 1))
                        {
                            if (weight >= maxRigWeight)
                            {
                                transform.position = Vector3.MoveTowards(transform.position, TargetPosition, Time.deltaTime * moveTowardsSpeed + weaponPositionDelta.magnitude);
                                foreach (ChildWeaponBone childWeaponBone in childWeaponBones)
                                {
                                    childWeaponBone.MoveTowards(ChildWeaponBone.TargetRotationMode.Target, Time.deltaTime * moveTowardsSpeed);
                                }
                            }
                            else if (weight > minRigWeight)
                            {
                                float t = StringUtility.NormalizeValue(weight, minRigWeight, maxRigWeight);
                                transform.position = Vector3.Lerp(transform.position, TargetPosition, t);
                                foreach (ChildWeaponBone childWeaponBone in childWeaponBones)
                                {
                                    childWeaponBone.Lerp(ChildWeaponBone.TargetRotationMode.Target, t);
                                }
                            }
                            else
                            {
                                transform.localPosition = originalLocalPosition;
                                foreach (ChildWeaponBone childWeaponBone in childWeaponBones)
                                {
                                    childWeaponBone.Lerp(ChildWeaponBone.TargetRotationMode.None, 1);
                                }
                            }
                            isAiming = true;
                        }
                    }

                    if (!isAiming)
                    {
                        if (weight >= maxRigWeight)
                        {
                            transform.position = Vector3.MoveTowards(transform.position, TargetPosition, Time.deltaTime * moveTowardsSpeed + weaponPositionDelta.magnitude);
                            foreach (ChildWeaponBone childWeaponBone in childWeaponBones)
                            {
                                childWeaponBone.MoveTowards(ChildWeaponBone.TargetRotationMode.Target, Time.deltaTime * moveTowardsSpeed);
                            }
                        }
                        else if (weight > minRigWeight)
                        {
                            float t = 1 - StringUtility.NormalizeValue(weight, minRigWeight, maxRigWeight);
                            transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, t);
                            foreach (ChildWeaponBone childWeaponBone in childWeaponBones)
                            {
                                childWeaponBone.Lerp(ChildWeaponBone.TargetRotationMode.None, t);
                            }
                        }
                        else
                        {
                            transform.localPosition = originalLocalPosition;
                            foreach (ChildWeaponBone childWeaponBone in childWeaponBones)
                            {
                                childWeaponBone.Lerp(ChildWeaponBone.TargetRotationMode.None, 1);
                            }
                        }
                    }
                }
                evaluated = true;
            }
            else
            {
                foreach (ChildWeaponBone childWeaponBone in childWeaponBones)
                {
                    childWeaponBone.Lerp(ChildWeaponBone.TargetRotationMode.Target, 1);
                }
                transform.position = TargetPosition;
                evaluated = true;
            }
            
            if (!evaluated) { transform.position = TargetPosition; }
        }
    }
}
