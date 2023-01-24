using GameCreator.Core.Hooks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace NJG.PUN
{
    public class LookAtCamera : MonoBehaviour
    {
        private void Update()
        {
            if (HookCamera.Instance && enabled) Setup();
        }

        private void Setup()
        {
            Camera camera = HookCamera.Instance ? HookCamera.Instance.Get<Camera>() : null;
            if (!camera) camera = GameObject.FindObjectOfType<Camera>();
            
            LookAtConstraint constraint = GetComponent<LookAtConstraint>();
            if (!constraint) constraint = gameObject.AddComponent<LookAtConstraint>();
            constraint.rotationOffset = new Vector3(0, 180, 0);
            constraint.SetSources(new List<ConstraintSource>()
                {
                    new ConstraintSource()
                    {
                        sourceTransform = camera.transform,
                        weight = 1.0f
                    }
                });
            
            Canvas canvas = GetComponent<Canvas>();
            if (canvas) canvas.worldCamera = camera;

            constraint.constraintActive = true;

            enabled = false;
            Destroy(this);
        }
    }
}
