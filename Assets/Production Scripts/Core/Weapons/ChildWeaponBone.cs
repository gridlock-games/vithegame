using UnityEngine;

namespace Vi.ProceduralAnimations
{
    public class ChildWeaponBone : MonoBehaviour
    {
        [SerializeField] private Vector3 targetLocalRotation;
        private Quaternion originalLocalRotation;

        private void Awake()
        {
            originalLocalRotation = transform.localRotation;
        }

        public void Lerp(bool isActive, float t)
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, isActive ? Quaternion.Euler(targetLocalRotation) : originalLocalRotation, t);
        }
    }
}