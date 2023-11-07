using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;

namespace Vi.ArtificialIntelligence
{
    public class BotController : NetworkBehaviour
    {
        [SerializeField] private bool lightAttack;
        [SerializeField] private bool isBlocking;
        [SerializeField] private bool attackPlayer;

        private CharacterController characterController;
        private AnimationHandler animationHandler;
        private WeaponHandler weaponHandler;
        private Attributes attributes;

        private void Start()
        {
            characterController = GetComponent<CharacterController>();
            animationHandler = GetComponent<AnimationHandler>();
            weaponHandler = GetComponent<WeaponHandler>();
            attributes = GetComponent<Attributes>();
        }

        private void Update()
        {
            if (!IsSpawned) { return; }
            if (!characterController.enabled) { return; }

            if (animationHandler.ShouldApplyRootMotion())
            {
                characterController.Move(animationHandler.ApplyLocalRootMotion());
            }
            else
            {
                characterController.Move(Physics.gravity);
            }

            if (attributes.ShouldApplyAilmentRotation())
            {
                transform.rotation = attributes.GetAilmentRotation();
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