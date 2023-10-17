using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Vi.Player
{
    public class InputHandler : MonoBehaviour
    {
        void OnLook(InputValue value)
        {
            Debug.Log(value.Get<Vector2>());
        }

        void OnMove(InputValue value)
        {
            Debug.Log(value.Get<Vector2>());
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