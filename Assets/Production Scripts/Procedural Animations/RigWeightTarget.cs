using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Vi.ProceduralAnimations
{
    [RequireComponent(typeof(Rig))]
    public class RigWeightTarget : MonoBehaviour
    {
        public float weight;
        public bool instantWeight;

        private const float weightSpeed = 3;

        private Rig rig;
        private Animator animator;

        public Rig GetRig()
        {
            if (!rig) { rig = GetComponent<Rig>(); }
            return rig;
        }

        private void Awake()
        {
            rig = GetComponent<Rig>();
            animator = GetComponentInParent<Animator>();
        }

        private void Update()
        {
            if (Mathf.Approximately(rig.weight, weight)) { return; }
            if (instantWeight) { rig.weight = weight; return; }

            rig.weight = Mathf.MoveTowards(rig.weight, weight, Time.deltaTime * weightSpeed * animator.speed);
        }
    }
}