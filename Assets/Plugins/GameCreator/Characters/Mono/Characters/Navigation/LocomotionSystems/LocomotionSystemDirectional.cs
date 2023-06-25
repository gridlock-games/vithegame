namespace GameCreator.Characters
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using GameCreator.Core;
    using GameCreator.Core.Hooks;

    public class LocomotionSystemDirectional : ILocomotionSystem
    {
        // PROPERTIES: ----------------------------------------------------------------------------

        protected Vector3 desiredDirection = Vector3.zero;

        // OVERRIDE METHODS: ----------------------------------------------------------------------

        public override CharacterLocomotion.LOCOMOTION_SYSTEM Update()
        {
            if (!this.characterLocomotion.characterController.enabled)
            {
                return CharacterLocomotion.LOCOMOTION_SYSTEM.CharacterController;
            }

            base.Update();

            if (this.characterLocomotion.navmeshAgent != null)
            {
                this.characterLocomotion.navmeshAgent.updatePosition = false;
                this.characterLocomotion.navmeshAgent.updateUpAxis = false;
            }

            Vector3 targetDirection = this.desiredDirection;

            float speed = this.CalculateSpeed(targetDirection, this.characterLocomotion.characterController.isGrounded);
            Quaternion targetRotation = this.UpdateRotation(targetDirection);

            if (characterLocomotion.character.TryGetComponent(out PlayerCharacterNetworkTransform networkTransform))
            {
                if (!networkTransform.IsOwner)
                    targetRotation = networkTransform.currentRotation;
                
                // If distance is greater than teleport threshold, teleport player object to the network position
                if (Vector3.Distance(networkTransform.currentPosition, characterLocomotion.character.transform.position) > networkTransform.playerObjectTeleportThreshold)
                {
                    characterLocomotion.characterController.enabled = false;
                    characterLocomotion.characterController.transform.position = networkTransform.currentPosition;
                    characterLocomotion.characterController.enabled = true;
                }
                else // Teleport threshold not reached
                {
                    // Calculate target direction towards network position
                    targetDirection = networkTransform.currentPosition - characterLocomotion.character.transform.position;
                    
                    // If our magnitude is less than the magnitude threshold, do not move the player
                    if (targetDirection.magnitude < networkTransform.playerObjectDirectionalMagnitudeThreshold)
                        targetDirection = Vector3.zero;
                    else
                        targetDirection = targetDirection.normalized;
                }
            }

            this.UpdateAnimationConstraints(ref targetDirection, ref targetRotation);
            this.UpdateSliding();

            targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1.0f);
            targetDirection *= speed;

            if (this.isSliding) targetDirection = this.slideDirection;
            targetDirection += Vector3.up * this.characterLocomotion.verticalSpeed;

            // If there is no player character network transform component on this object
            if (this.isRootMoving)
            {
                this.UpdateRootMovement(Vector3.up * this.characterLocomotion.verticalSpeed);
                this.characterLocomotion.characterController.transform.rotation = targetRotation;
            }
            else
            {
                this.characterLocomotion.characterController.Move(targetDirection * Time.deltaTime);
                this.characterLocomotion.characterController.transform.rotation = targetRotation;
            }

            if (this.characterLocomotion.navmeshAgent != null &&
                this.characterLocomotion.navmeshAgent.isActiveAndEnabled)
            {
                this.characterLocomotion.navmeshAgent.enabled = false;
            }

            return CharacterLocomotion.LOCOMOTION_SYSTEM.CharacterController;
        }

        public override void OnDestroy()
        {
            return;
        }

        // PUBLIC METHODS: ------------------------------------------------------------------------

        public void SetDirection(Vector3 direction, TargetRotation rotation = null)
        {
            this.desiredDirection = direction;
        }
    }
}