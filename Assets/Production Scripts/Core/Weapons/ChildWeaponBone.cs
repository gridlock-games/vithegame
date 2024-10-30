using UnityEngine;

namespace Vi.Core.Weapons
{
    public class ChildWeaponBone : MonoBehaviour
    {
        [SerializeField] private Vector3 targetLocalRotation;
        [SerializeField] private Vector3 attackingLocalRotation;
        private Quaternion originalLocalRotation;

        private void Awake()
        {
            originalLocalRotation = transform.localRotation;
        }

        public enum TargetRotationMode
        {
            None,
            Attacking,
            Target
        }

        private Quaternion GetTargetRotation(TargetRotationMode targetRotationMode)
        {
            switch (targetRotationMode)
            {
                case TargetRotationMode.None:
                    return originalLocalRotation;
                case TargetRotationMode.Attacking:
                    return Quaternion.Euler(attackingLocalRotation);
                case TargetRotationMode.Target:
                    return Quaternion.Euler(targetLocalRotation);
                default:
                    Debug.LogError("Unsure how to handle target rotation mode " + targetRotationMode);
                    break;
            }
            return Quaternion.identity;
        }

        public void Lerp(TargetRotationMode targetRotationMode, float t)
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, GetTargetRotation(targetRotationMode), t);
        }

        public void LerpProgressive(TargetRotationMode targetRotationMode, float deltaTime)
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, GetTargetRotation(targetRotationMode), deltaTime);
        }

        public void MoveTowards(TargetRotationMode targetRotationMode, float deltaTime)
        {
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, GetTargetRotation(targetRotationMode), deltaTime * Mathf.Rad2Deg);
        }
    }
}