using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Vi.ProceduralAnimations
{
    [RequireComponent(typeof(IRigConstraint))]
    public class ConstraintWeightTarget : MonoBehaviour
    {
        public float weight;
        public bool instantWeight;

        private const float weightSpeed = 3;

        private IRigConstraint constraint;
        private Animator animator;

        private void Awake()
        {
            constraint = GetComponent<IRigConstraint>();
            animator = GetComponentInParent<Animator>();
        }

        private void Update()
        {
            if (Mathf.Approximately(constraint.weight, weight)) { return; }
            if (instantWeight) { constraint.weight = weight; return; }

            constraint.weight = Mathf.MoveTowards(constraint.weight, weight, Time.deltaTime * weightSpeed * animator.speed);
        }
    }
}