using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        [SerializeField] private bool debugLines;

        public enum Axis
        {
            X,
            Y,
            Z
        }

        Vector3 rootPosition;
        Vector3 tipPosition;
        private void LateUpdate()
        {
            rootPosition = root.position;
            switch (rootAxis)
            {
                case Axis.X:
                    rootPosition += root.right * rootMultiplier;
                    break;
                case Axis.Y:
                    rootPosition += root.up * rootMultiplier;
                    break;
                case Axis.Z:
                    rootPosition += root.forward * rootMultiplier;
                    break;
            }

            tipPosition = tip.position;
            switch (tipAxis)
            {
                case Axis.X:
                    tipPosition += tip.right * tipMultiplier;
                    break;
                case Axis.Y:
                    tipPosition += tip.up * tipMultiplier;
                    break;
                case Axis.Z:
                    tipPosition += tip.forward * tipMultiplier;
                    break;
            }

            if (debugLines)
            {
                Debug.DrawLine(tip.position, tipPosition, Color.red, Time.deltaTime);
                Debug.DrawLine(root.position, rootPosition, Color.green, Time.deltaTime);
            }

            transform.position = (rootPosition + tipPosition) / 2;
            transform.localPosition += offset;
        }
    }
}