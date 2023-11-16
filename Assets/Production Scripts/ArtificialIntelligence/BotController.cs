using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;

namespace Vi.ArtificialIntelligence
{
    public class BotController : MovementHandler
    {
        [SerializeField] private bool lightAttack;
        [SerializeField] private bool isBlocking;
        [SerializeField] private bool attackPlayer;

        private CharacterController characterController;
        private AnimationHandler animationHandler;
        private WeaponHandler weaponHandler;
        private Attributes attributes;

        public override void SetOrientation(Vector3 newPosition, Quaternion newRotation)
        {
            currentPosition.Value = newPosition;
            currentRotation.Value = newRotation;
            base.SetOrientation(newPosition, newRotation);
        }

        private new void Start()
        {
            characterController = GetComponent<CharacterController>();
            animationHandler = GetComponent<AnimationHandler>();
            weaponHandler = GetComponent<WeaponHandler>();
            attributes = GetComponent<Attributes>();
        }

        private NetworkVariable<Vector3> currentPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> currentRotation = new NetworkVariable<Quaternion>();
        private void Update()
        {
            if (!IsSpawned) { return; }
            if (!characterController.enabled) { return; }

            if (IsServer)
            {
                Vector3 animDir = Vector3.zero;
                if (!NetworkManager.LocalClient.PlayerObject) { return; }

                if (attackPlayer & attributes.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death)
                {
                    Vector3 dir = (NetworkManager.LocalClient.PlayerObject.transform.position - transform.position).normalized;
                    dir.Scale(new Vector3(1, 0, 1));
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 540);

                    if (Vector3.Distance(NetworkManager.LocalClient.PlayerObject.transform.position, transform.position) < 1.5f
                        & NetworkManager.LocalClient.PlayerObject.GetComponent<Attributes>().GetAilment() != ScriptableObjects.ActionClip.Ailment.Death)
                    {
                        SendMessage("OnLightAttack");
                    }
                    else
                    {
                        if (!animationHandler.ShouldApplyRootMotion())
                        {
                            characterController.Move(5 * Time.deltaTime * dir);
                        }
                        animDir = transform.InverseTransformDirection(Vector3.ClampMagnitude(dir, 1));
                    }
                }
                
                if (animationHandler.ShouldApplyRootMotion())
                {
                    characterController.Move(animationHandler.ApplyLocalRootMotion());
                }

                characterController.Move(Physics.gravity);

                if (attributes.ShouldApplyAilmentRotation())
                {
                    transform.rotation = attributes.GetAilmentRotation();
                }

                currentPosition.Value = transform.position;
                currentRotation.Value = transform.rotation;

                animationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveForward"), animDir.z > 0.9f ? Mathf.RoundToInt(animDir.z) : animDir.z, Time.deltaTime * 5));
                animationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveSides"), animDir.x > 0.9f ? Mathf.RoundToInt(animDir.x) : animDir.x, Time.deltaTime * 5));
            }
            else
            {
                Vector3 dir = currentPosition.Value - transform.position;
                Vector3 animDir = transform.InverseTransformDirection(Vector3.ClampMagnitude(dir, 1));
                characterController.Move(dir);
                transform.rotation = currentRotation.Value;

                animationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveForward"), animDir.z > 0.9f ? Mathf.RoundToInt(animDir.z) : animDir.z, Time.deltaTime * 5));
                animationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveSides"), animDir.x > 0.9f ? Mathf.RoundToInt(animDir.x) : animDir.x, Time.deltaTime * 5));
            }

            /*
            weaponHandler.SetIsBlocking(isBlocking);

            if (!animationHandler.ShouldApplyRootMotion())
            {
                if (!NetworkManager.LocalClient.PlayerObject) { return; }

                if (attackPlayer)
                {
                    Vector3 dir = (NetworkManager.LocalClient.PlayerObject.transform.position - transform.position).normalized;
                    dir.Scale(new Vector3(1, 0, 1));
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 540);

                    if (Vector3.Distance(NetworkManager.LocalClient.PlayerObject.transform.position, transform.position) < 1.5f)
                    {
                        SendMessage("OnLightAttack");
                    }
                    else
                    {
                        characterController.Move(5 * Time.deltaTime * dir);

                        Vector3 animDir = transform.InverseTransformDirection(Vector3.ClampMagnitude(dir, 1));
                        animationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveForward"), animDir.z > 0.9f ? Mathf.RoundToInt(animDir.z) : animDir.z, Time.deltaTime * 5));
                        animationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveSides"), animDir.x > 0.9f ? Mathf.RoundToInt(animDir.x) : animDir.x, Time.deltaTime * 5));
                    }
                }
                characterController.Move(Physics.gravity);
            }
            else
            {
                if (attributes.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death) { characterController.Move(animationHandler.ApplyLocalRootMotion()); }
            }

            if (attributes.ShouldApplyAilmentRotation())
            {
                transform.rotation = attributes.GetAilmentRotation();
            }*/
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying) { return; }

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
    }
}