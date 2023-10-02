using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations
{
    public class HintPositionSolver : MonoBehaviour
    {
        public Transform root;
        public Axis rootAxis;
        public float rootMultiplier = 1;
        public Transform tip;
        public Axis tipAxis;
        public float tipMultiplier = 1;
        public Vector3 offset;
        public bool debugLines;

        public enum Axis
        {
            X,
            Y,
            Z
        }

        Vector3 rootPosition;
        Vector3 tipPosition;
        private void Update()
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