using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

namespace Vi.Player
{
    public class PlayerController : NetworkBehaviour
    {
        Animator animator;

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            animator.SetFloat("MoveForward", moveInput.y);
            animator.SetFloat("MoveSides", moveInput.x);
        }

        private Vector2 lookInput;
        void OnLook(InputValue value)
        {
            lookInput = value.Get<Vector2>();
        }

        private Vector2 moveInput;
        void OnMove(InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }

        void OnInteract()
        {
            Debug.Log("Interact at " + Time.time);
        }

        void OnDodge()
        {
            Debug.Log("Dodge at " + Time.time);
        }

        void OnBlock(InputValue value)
        {
            Debug.Log("Blocking " + value.isPressed);
        }

        void OnLightAttack()
        {
            Debug.Log("LightAttack at " + Time.time);
        }

        void OnHeavyAttack()
        {
            Debug.Log("Heavy attack at " + Time.time);
        }

        void OnAbility1()
        {
            Debug.Log("Ability 1 at " + Time.time);
        }

        void OnAbility2()
        {
            Debug.Log("Ability 1 at " + Time.time);
        }

        void OnAbility3()
        {
            Debug.Log("Ability 1 at " + Time.time);
        }

        void OnAbility4()
        {
            Debug.Log("Ability 1 at " + Time.time);
        }

        void OnReload()
        {
            Debug.Log("Reload at " + Time.time);
        }
    }
}