using UnityEngine;
using Vi.ScriptableObjects;
using Vi.ProceduralAnimations;

namespace Vi.Core
{
    public class WeaponBoneFollowTarget : MonoBehaviour
    {
        [SerializeField] private bool followWhileAiming = true;
        [SerializeField] private Weapon.WeaponBone weaponBoneToFollow;

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

        private const float maxRigWeight = 1;
        private const float minRigWeight = 0.7f;
        float NormalizeValue(float value)
        {
            float minValue = minRigWeight;
            float maxValue = maxRigWeight;
            return (value - minValue) / (maxValue - minValue);
        }

        private void LateUpdate()
        {
            if (!combatAgent) { return; }
            if (!target) { return; }
            if (!combatAgent.AnimationHandler.LimbReferences) { return; }

            bool evaluated = false;
            if (followWhileAiming)
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
                            transform.position = target.position;
                        }
                        else if (weight > minRigWeight)
                        {
                            transform.position = Vector3.Lerp(transform.position, target.position, NormalizeValue(weight));
                        }
                        else
                        {
                            transform.localPosition = originalLocalPosition;
                        }
                        isAiming = true;
                    }
                }

                if (!isAiming)
                {
                    if (weight >= maxRigWeight)
                    {
                        transform.position = target.position;
                    }
                    else if (weight > minRigWeight)
                    {
                        transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, 1 - NormalizeValue(weight));
                    }
                    else
                    {
                        transform.localPosition = originalLocalPosition;
                    }
                }
                evaluated = true;
            }
            else
            {
                transform.position = target.position;
                evaluated = true;
            }
            
            if (!evaluated) { transform.position = target.position; }
        }
    }
}
