using GameCreator.Characters;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.ProceduralAnimations
{
    public class HandIKSolver : MonoBehaviour
    {
        [SerializeField] private Transform handIKTarget;
        [SerializeField] private AvatarIKGoal referenceGoal;

        private Animator animator;

        private void Start()
        {
            animator = GetComponentInParent<Animator>();
        }

        private void Update()
        {
            handIKTarget.position = animator.GetIKPosition(referenceGoal);
            handIKTarget.rotation = animator.GetIKRotation(referenceGoal);
        }
    }
}