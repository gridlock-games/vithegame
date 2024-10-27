using UnityEngine;
using MagicaCloth2;

namespace Vi.ProceduralAnimations
{
    [RequireComponent(typeof(ColliderComponent))]
    public class ChildRespondentMagicaClothCollider : MonoBehaviour
    {
        private int originalChildrenCount;
        private ColliderComponent clothCollider;
        private void Awake()
        {
            clothCollider = GetComponent<ColliderComponent>();
            originalChildrenCount = transform.childCount;
        }

        private void OnEnable()
        {
            clothCollider.enabled = transform.childCount > originalChildrenCount;
        }

        private void OnTransformChildrenChanged()
        {
            clothCollider.enabled = transform.childCount > originalChildrenCount;
        }
    }
}