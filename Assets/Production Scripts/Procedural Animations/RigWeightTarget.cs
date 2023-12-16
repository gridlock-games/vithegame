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

        public Rig GetRig() { return rig; }

        private void Start()
        {
            rig = GetComponent<Rig>();
            animator = GetComponentInParent<Animator>();
        }

        private void Update()
        {
            if (rig.weight == weight) { return; }
            if (instantWeight) { rig.weight = weight; return; }

            rig.weight = Mathf.MoveTowards(rig.weight, weight, Time.deltaTime * weightSpeed * animator.speed);

            //if (Mathf.Abs(weight - rig.weight) > 0.1)
            //{
            //    rig.weight = Mathf.Lerp(rig.weight, weight, Time.deltaTime * weightSpeed * animator.speed);
            //}
            //else
            //{
            //    rig.weight = Mathf.MoveTowards(rig.weight, weight, Time.deltaTime * animator.speed);
            //}
        }
    }
}