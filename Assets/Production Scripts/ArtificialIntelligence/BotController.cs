using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;
using Vi.ScriptableObjects;

namespace Vi.ArtificialIntelligence
{
    public class BotController : MovementHandler
    {
        [SerializeField] private bool moveToPlayer;
        [SerializeField] private bool canLightAttack;

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
            animationHandler = GetComponent<AnimationHandler>();
            weaponHandler = GetComponent<WeaponHandler>();
            attributes = GetComponent<Attributes>();
            movementPredictionRigidbody.transform.SetParent(null, true);
            targetMovementPredictionRigidbodyPosition = transform.position;
        }

        public override void ReceiveOnCollisionEnterMessage(Collision collision)
        {
            targetMovementPredictionRigidbodyPosition = movementPredictionRigidbody.position;
        }

        public override void ReceiveOnCollisionStayMessage(Collision collision)
        {
            targetMovementPredictionRigidbodyPosition = movementPredictionRigidbody.position;
        }

        [SerializeField] private Rigidbody movementPredictionRigidbody;
        private Vector3 targetMovementPredictionRigidbodyPosition;
        private float positionStrength = 1;
        private float runSpeed = 5;
        private bool isGrounded = true;
        private float runAnimationTransitionSpeed = 5;
        private void FixedUpdate()
        {
            if (attributes.GetAilment() == ActionClip.Ailment.Death) { movementPredictionRigidbody.velocity = Vector3.zero; return; }
            if (!CanMove()) { return; }

            Vector3 movement = Vector3.zero;
            if (moveToPlayer)
            {
                if (Vector3.Distance(NetworkManager.LocalClient.PlayerObject.transform.position, transform.position) > 1.5f)
                {
                    Vector3 target = new Vector3(NetworkManager.LocalClient.PlayerObject.transform.position.x, transform.position.y, NetworkManager.LocalClient.PlayerObject.transform.position.z);
                    Vector3 dir = Vector3.ClampMagnitude(target - transform.position, 1);
                    movement = isGrounded ? runSpeed * Time.fixedDeltaTime * dir : Vector3.zero;
                    Vector3 animDir = transform.rotation * new Vector3(-dir.x, dir.y, dir.z);

                    if (dir == Vector3.zero)
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.identity, Time.deltaTime * 540);
                    else
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 540);

                    animationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveForward"), animDir.z, Time.deltaTime * runAnimationTransitionSpeed));
                    animationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveSides"), animDir.x, Time.deltaTime * runAnimationTransitionSpeed));
                }
                else
                {
                    animationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveForward"), 0, Time.deltaTime * runAnimationTransitionSpeed));
                    animationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveSides"), 0, Time.deltaTime * runAnimationTransitionSpeed));
                }
            }

            if (animationHandler.ShouldApplyRootMotion())
            {
                movement = animationHandler.ApplyLocalRootMotion();
            }

            if (canLightAttack)
            {
                if (Vector3.Distance(NetworkManager.LocalClient.PlayerObject.transform.position, transform.position) < 1.5f)
                {
                    SendMessage("OnLightAttack");
                }
            }

            // Handle gravity
            RaycastHit[] allHits = Physics.SphereCastAll(targetMovementPredictionRigidbodyPosition + transform.rotation * animationHandler.LimbReferences.bottomPointOfCapsuleOffset,
                                            animationHandler.LimbReferences.characterRadius, Physics.gravity, Physics.gravity.magnitude, ~LayerMask.GetMask(new string[] { "NetworkPrediction" }), QueryTriggerInteraction.Ignore);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
            Vector3 gravity = Vector3.zero;
            bool bHit = false;
            foreach (RaycastHit hit in allHits)
            {
                if (hit.transform.root == transform) { continue; }
                gravity += Time.fixedDeltaTime * Mathf.Clamp01(hit.distance) * Physics.gravity;
                bHit = true;
                break;
            }
            if (!bHit) { gravity += Physics.gravity * Time.fixedDeltaTime; }
            isGrounded = bHit;
            targetMovementPredictionRigidbodyPosition += gravity;
            targetMovementPredictionRigidbodyPosition += movement;
            Vector3 deltaPos = targetMovementPredictionRigidbodyPosition - movementPredictionRigidbody.position;
            movementPredictionRigidbody.velocity = 1f / Time.fixedDeltaTime * deltaPos * Mathf.Pow(positionStrength, 90f * Time.fixedDeltaTime);
        }

        private NetworkVariable<Vector3> currentPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> currentRotation = new NetworkVariable<Quaternion>();
        private void Update()
        {
            UpdateLocomotion();
            /*
            if (!IsSpawned) { return; }

            if (IsServer)
            {
                Vector3 movement = Vector3.zero;
                Vector3 animDir = Vector3.zero;
                if (!NetworkManager.LocalClient.PlayerObject) { return; }

                if (moveToPlayer & attributes.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death)
                {
                    Vector3 dir = (NetworkManager.LocalClient.PlayerObject.transform.position - transform.position).normalized;
                    dir.Scale(new Vector3(1, 0, 1));
                    if (dir == Vector3.zero)
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.identity, Time.deltaTime * 540);
                    else
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 540);

                    if (Vector3.Distance(NetworkManager.LocalClient.PlayerObject.transform.position, transform.position) < 1.5f
                        & NetworkManager.LocalClient.PlayerObject.GetComponent<Attributes>().GetAilment() != ScriptableObjects.ActionClip.Ailment.Death
                        & canLightAttack)
                    {
                        SendMessage("OnLightAttack");
                    }
                    else
                    {
                        if (!animationHandler.ShouldApplyRootMotion())
                        {
                            movement = 5 * Time.deltaTime * dir;
                        }
                        animDir = transform.InverseTransformDirection(Vector3.ClampMagnitude(dir, 1));
                    }
                }
                
                if (animationHandler.ShouldApplyRootMotion())
                {
                    movement = animationHandler.ApplyLocalRootMotion();
                }

                if (attributes.ShouldApplyAilmentRotation())
                {
                    transform.rotation = attributes.GetAilmentRotation();
                }

                TryMove(movement);
                currentPosition.Value = transform.position;
                currentRotation.Value = transform.rotation;

                animationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveForward"), animDir.z > 0.9f ? Mathf.RoundToInt(animDir.z) : animDir.z, Time.deltaTime * 5));
                animationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveSides"), animDir.x > 0.9f ? Mathf.RoundToInt(animDir.x) : animDir.x, Time.deltaTime * 5));
            }
            else
            {
                Vector3 dir = currentPosition.Value - transform.position;
                Vector3 animDir = transform.InverseTransformDirection(Vector3.ClampMagnitude(dir, 1));
                TryMove(dir);
                transform.rotation = currentRotation.Value;

                animationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveForward"), animDir.z > 0.9f ? Mathf.RoundToInt(animDir.z) : animDir.z, Time.deltaTime * 5));
                animationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveSides"), animDir.x > 0.9f ? Mathf.RoundToInt(animDir.x) : animDir.x, Time.deltaTime * 5));
            }*/

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

        private void UpdateLocomotion()
        {
            float runSpeed = 5;

            Vector3 movement = Time.deltaTime * (NetworkManager.NetworkTickSystem.TickRate / 2) * (targetMovementPredictionRigidbodyPosition - transform.position);
            transform.position += movement;

            animationHandler.Animator.speed = (Mathf.Max(0, 5 - attributes.GetMovementSpeedDecreaseAmount()) + attributes.GetMovementSpeedIncreaseAmount()) / runSpeed;

            //if (attributes.ShouldApplyAilmentRotation())
            //    transform.rotation = attributes.GetAilmentRotation();
            //else if (weaponHandler.IsAiming())
            //    transform.rotation = Quaternion.Slerp(transform.rotation, currentRotation.Value, Time.deltaTime * NetworkManager.NetworkTickSystem.TickRate);
            //else
            //    transform.rotation = Quaternion.Slerp(transform.rotation, currentRotation.Value, Time.deltaTime * NetworkManager.NetworkTickSystem.TickRate);
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(currentPosition.Value, 0.25f);
            }
            else
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position, 0.25f);
            }
        }
    }
}