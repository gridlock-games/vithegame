using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCreator.Characters;
using Unity.Netcode;

namespace LightPat.Core
{
    public class ChangeAilmentOnCollision : MonoBehaviour
    {
        public CharacterLocomotion.CHARACTER_AILMENTS ailment;

        private CharacterController charController;
        private Character character;

        private void Start()
        {
            charController = GetComponent<CharacterController>();
            character = GetComponent<Character>();
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                if (hit.gameObject.TryGetComponent(out ChangeAilmentOnCollision changeAilment) & !hit.gameObject.GetComponent<CharacterController>())
                {
                    Debug.Log(changeAilment.ailment);
                    character.UpdateAilment(changeAilment.ailment, null);
                }
            }
        }
    }
}
