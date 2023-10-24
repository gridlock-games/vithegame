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
        private Animator animator;
        private WeaponHandler weaponHandler;

        private void Start()
        {
            characterController = GetComponent<CharacterController>();
            animationHandler = GetComponentInChildren<AnimationHandler>();
            animator = GetComponentInChildren<Animator>();
            weaponHandler = GetComponent<WeaponHandler>();
        }

        private void Update()
        {
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
                        animator.SetFloat("MoveForward", Mathf.MoveTowards(animator.GetFloat("MoveForward"), animDir.z > 0.9f ? Mathf.RoundToInt(animDir.z) : animDir.z, Time.deltaTime * 5));
                        animator.SetFloat("MoveSides", Mathf.MoveTowards(animator.GetFloat("MoveSides"), animDir.x > 0.9f ? Mathf.RoundToInt(animDir.x) : animDir.x, Time.deltaTime * 5));
                    }
                }
            }
            else
            {
                characterController.Move(animationHandler.ApplyLocalRootMotion());
            }
        }
    }
}