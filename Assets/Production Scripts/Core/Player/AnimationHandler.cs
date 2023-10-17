using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Player
{
    [RequireComponent(typeof(Animator))]
    public class AnimationHandler : MonoBehaviour
    {
        Animator animator;
        CharacterController characterController;
        private void Start()
        {
            characterController = GetComponentInParent<CharacterController>();
            animator = GetComponent<Animator>();
        }

        private void OnAnimatorMove()
        {
            //if (animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty"))
            //{

            //}

            characterController.Move(animator.deltaPosition);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            
        }
    }
}