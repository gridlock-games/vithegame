using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Vi.ProceduralAnimations
{
    public class HintPositionSolver : MonoBehaviour
    {
        [SerializeField] private Transform root;
        [SerializeField] private Axis rootAxis;
        [SerializeField] private float rootMultiplier = 1;
        [SerializeField] private Transform tip;
        [SerializeField] private Axis tipAxis;
        [SerializeField] private float tipMultiplier = 1;
        [SerializeField] private Vector3 offset;

        public enum Axis
        {
            X,
            Y,
            Z
        }

        private void LateUpdate()
        {
            transform.position = ((root.position + EvaluateAxisOffset(root, rootAxis, rootMultiplier)) + (tip.position + EvaluateAxisOffset(tip, tipAxis, tipMultiplier))) / 2;
            transform.localPosition += offset;
        }

        private Vector3 EvaluateAxisOffset(Transform t, Axis axis, float multiplier)
        {
            switch (axis)
            {
                case Axis.X:
                    return t.right * multiplier;
                case Axis.Y:
                    return t.up * multiplier;
                case Axis.Z:
                    return t.forward * multiplier;
            }
            return Vector3.zero;
        }

        private void OnDrawGizmosSelected()
        {
            if (!root | !tip) { return; }

            Vector3 rootPos = root.position + EvaluateAxisOffset(root, rootAxis, rootMultiplier);
            Vector3 tipPos = tip.position + EvaluateAxisOffset(tip, tipAxis, tipMultiplier);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(tip.position, tipPos);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(root.position, rootPos);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) { return; }

            TwoBoneIKConstraint twoBoneIKConstraint = GetComponentInParent<TwoBoneIKConstraint>();

            if (twoBoneIKConstraint)
            {
                if (!tip) { tip = twoBoneIKConstraint.data.tip; }
                if (!root) { root = twoBoneIKConstraint.data.root; }
            }
        }
#endif
    }
}