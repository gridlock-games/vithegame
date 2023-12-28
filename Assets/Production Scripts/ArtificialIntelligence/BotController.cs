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
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                currentPosition.Value = transform.position;
                NetworkManager.NetworkTickSystem.Tick += ProcessMovement;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) { NetworkManager.NetworkTickSystem.Tick -= ProcessMovement; }
        }

        [SerializeField] private float angularSpeed = 540;
        [SerializeField] private Vector3 gravitySphereCastPositionOffset = new Vector3(0, 0.75f, 0);
        [SerializeField] private float gravitySphereCastRadius = 0.75f;
        private void ProcessMovement()
        {
            if (!IsServer) { return; }

            if (!CanMove() | attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                moveForwardTarget.Value = 0;
                moveSidesTarget.Value = 0;
                return;
            }

            Attributes targetAttributes = null;
            Collider[] colliders = Physics.OverlapSphere(transform.position, 5, LayerMask.GetMask(new string[] { "Character" }));
            foreach (Collider c in colliders)
            {
                if (c.transform.root.TryGetComponent(out Attributes attributes))
                {
                    if (!PlayerDataManager.Singleton.CanHit(this.attributes, attributes)) { continue; }
                    targetAttributes = attributes;
                    break;
                }
            }
            Vector3 targetPosition = targetAttributes ? targetAttributes.transform.position : currentPosition.Value;

            Vector3 lookDirection = targetPosition - currentPosition.Value;
            lookDirection.Scale(HORIZONTAL_PLANE);
            if (lookDirection == Vector3.zero) { lookDirection = transform.forward; }

            if (attributes.ShouldApplyAilmentRotation())
                currentRotation.Value = attributes.GetAilmentRotation();
            if (weaponHandler.IsAiming())
                currentRotation.Value = Quaternion.LookRotation(lookDirection);
            else
                currentRotation.Value = Quaternion.RotateTowards(currentRotation.Value, Quaternion.LookRotation(lookDirection), 1f / NetworkManager.NetworkTickSystem.TickRate * angularSpeed);

            Vector3 movement = Vector3.zero;
            Vector3 animDir = Vector3.zero;

            // Handle gravity
            RaycastHit[] allHits = Physics.SphereCastAll(currentPosition.Value + currentRotation.Value * gravitySphereCastPositionOffset,
                                            gravitySphereCastRadius, Physics.gravity, Physics.gravity.magnitude, ~LayerMask.GetMask(new string[] { "NetworkPrediction" }), QueryTriggerInteraction.Ignore);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
            Vector3 gravity = Vector3.zero;
            bool bHit = false;
            foreach (RaycastHit hit in allHits)
            {
                if (hit.transform.root == transform) { continue; }
                gravity += 1f / NetworkManager.NetworkTickSystem.TickRate * Mathf.Clamp01(hit.distance) * Physics.gravity;
                bHit = true;
                break;
            }
            if (!bHit) { gravity += 1f / NetworkManager.NetworkTickSystem.TickRate * Physics.gravity; }
            isGrounded = bHit;

            // Apply movement
            Vector3 rootMotion = animationHandler.ApplyNetworkRootMotion() * Mathf.Clamp01(runSpeed - attributes.GetMovementSpeedDecreaseAmount() + attributes.GetMovementSpeedIncreaseAmount());
            if (animationHandler.ShouldApplyRootMotion())
            {
                movement = attributes.IsRooted() ? Vector3.zero : rootMotion;
            }
            else
            {
                switch (PlayerDataManager.Singleton.GetGameMode())
                {
                    case PlayerDataManager.GameMode.None:
                        // Roam until a player gets close, then attack them
                        if (targetAttributes)
                        {
                            if (Vector3.Distance(currentPosition.Value, targetAttributes.transform.position) > 3)
                            {
                                Vector3 dir = targetPosition - currentPosition.Value;
                                Vector3 targetDirection = new Vector3(dir.x, 0, dir.z) * (attributes.IsFeared() ? -1 : 1);
                                targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
                                targetDirection *= isGrounded ? Mathf.Max(0, runSpeed - attributes.GetMovementSpeedDecreaseAmount()) + attributes.GetMovementSpeedIncreaseAmount() : 0;

                                movement = attributes.IsRooted() ? Vector3.zero : 1f / NetworkManager.NetworkTickSystem.TickRate * Time.timeScale * targetDirection;

                                animDir = new Vector3(targetDirection.x, 0, targetDirection.z);
                                animDir = transform.InverseTransformDirection(Vector3.ClampMagnitude(animDir, 1));
                            }
                            else
                            {
                                //weaponHandler.SendMessage("OnLightAttack");
                            }
                        }
                        break;
                    case PlayerDataManager.GameMode.FreeForAll:
                        // Path find to the nearest player and fight them
                        break;
                    case PlayerDataManager.GameMode.TeamElimination:
                        break;
                    case PlayerDataManager.GameMode.EssenceWar:
                        break;
                    case PlayerDataManager.GameMode.OutputRush:
                        break;
                    default:
                        Debug.LogError("Game Mode: " + PlayerDataManager.Singleton.GetGameMode() + " is not implemented for BotController");
                        break;
                }
            }

            currentPosition.Value += movement + gravity;

            moveForwardTarget.Value = animDir.z;
            moveSidesTarget.Value = animDir.x;
        }

        public override void ReceiveOnCollisionEnterMessage(Collision collision)
        {
            if (!IsServer) { return; }
            currentPosition.Value = movementPredictionRigidbody.position;
        }

        public override void ReceiveOnCollisionStayMessage(Collision collision)
        {
            if (!IsServer) { return; }
            currentPosition.Value = movementPredictionRigidbody.position;
        }

        [SerializeField] private float runAnimationTransitionSpeed = 5;
        private void Update()
        {
            if (!IsSpawned) { return; }

            UpdateLocomotion();
            animationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveForward"), moveForwardTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
            animationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveSides"), moveSidesTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
        }

        [SerializeField] private Rigidbody movementPredictionRigidbody;
        public static readonly Vector3 HORIZONTAL_PLANE = new Vector3(1, 0, 1);
        private float positionStrength = 1;
        private void FixedUpdate()
        {
            Vector3 deltaPos = currentPosition.Value - movementPredictionRigidbody.position;
            movementPredictionRigidbody.velocity = 1f / Time.fixedDeltaTime * deltaPos * Mathf.Pow(positionStrength, 90f * Time.fixedDeltaTime);
        }

        private void UpdateLocomotion()
        {
            float runSpeed = 5;

            Vector3 movement = Time.deltaTime * (NetworkManager.NetworkTickSystem.TickRate / 2) * (movementPredictionRigidbody.position - transform.position);
            transform.position += movement;

            animationHandler.Animator.speed = (Mathf.Max(0, 5 - attributes.GetMovementSpeedDecreaseAmount()) + attributes.GetMovementSpeedIncreaseAmount()) / runSpeed;

            if (attributes.ShouldApplyAilmentRotation())
                transform.rotation = attributes.GetAilmentRotation();
            else if (weaponHandler.IsAiming())
                transform.rotation = Quaternion.Slerp(transform.rotation, currentRotation.Value, Time.deltaTime * NetworkManager.NetworkTickSystem.TickRate);
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, currentRotation.Value, Time.deltaTime * NetworkManager.NetworkTickSystem.TickRate);
        }

        private float runSpeed = 5;
        private bool isGrounded = true;
        private float positionThreshold = 2;

        private NetworkVariable<float> moveForwardTarget = new NetworkVariable<float>();
        private NetworkVariable<float> moveSidesTarget = new NetworkVariable<float>();
        //private void FixedUpdate()
        //{
        //    if (!CanMove()) { return; }

        //    Vector3 movement = Vector3.zero;
        //    if (moveToPlayer)
        //    {
        //        if (Vector3.Distance(NetworkManager.LocalClient.PlayerObject.transform.position, transform.position) > positionThreshold)
        //        {
        //            Vector3 target = new Vector3(NetworkManager.LocalClient.PlayerObject.transform.position.x, transform.position.y, NetworkManager.LocalClient.PlayerObject.transform.position.z);
        //            Vector3 dir = Vector3.ClampMagnitude(target - transform.position, 1);
        //            movement = isGrounded ? runSpeed * Time.fixedDeltaTime * dir : Vector3.zero;
        //            Vector3 animDir = transform.rotation * new Vector3(-dir.x, dir.y, dir.z);

        //            if (attributes.GetAilment() != ActionClip.Ailment.Death)
        //            {
        //                if (dir == Vector3.zero)
        //                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.identity, Time.deltaTime * 540);
        //                else
        //                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 540);
        //            }

        //            animationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveForward"), animDir.z, Time.deltaTime * runAnimationTransitionSpeed));
        //            animationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveSides"), animDir.x, Time.deltaTime * runAnimationTransitionSpeed));
        //        }
        //        else
        //        {
        //            animationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveForward"), 0, Time.deltaTime * runAnimationTransitionSpeed));
        //            animationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveSides"), 0, Time.deltaTime * runAnimationTransitionSpeed));
        //        }
        //    }

        //    if (attributes.ShouldApplyAilmentRotation())
        //    {
        //        transform.rotation = attributes.GetAilmentRotation();
        //    }

        //    if (animationHandler.ShouldApplyRootMotion())
        //    {
        //        movement = animationHandler.ApplyLocalRootMotion();
        //    }

        //    if (canLightAttack)
        //    {
        //        if (Vector3.Distance(NetworkManager.LocalClient.PlayerObject.transform.position, transform.position) < positionThreshold)
        //        {
        //            SendMessage("OnLightAttack");
        //        }
        //    }

        //    // Handle gravity
        //    RaycastHit[] allHits = Physics.SphereCastAll(targetMovementPredictionRigidbodyPosition + transform.rotation * new Vector3(0, 0.5f, 0),
        //                                    0.5f, Physics.gravity, Physics.gravity.magnitude, ~LayerMask.GetMask(new string[] { "NetworkPrediction" }), QueryTriggerInteraction.Ignore);
        //    System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
        //    Vector3 gravity = Vector3.zero;
        //    bool bHit = false;
        //    foreach (RaycastHit hit in allHits)
        //    {
        //        if (hit.transform.root == transform) { continue; }
        //        gravity += Time.fixedDeltaTime * Mathf.Clamp01(hit.distance) * Physics.gravity;
        //        bHit = true;
        //        break;
        //    }
        //    if (!bHit) { gravity += Physics.gravity * Time.fixedDeltaTime; }
        //    isGrounded = bHit;
        //    if (attributes.GetAilment() != ActionClip.Ailment.Death)
        //    {
        //        targetMovementPredictionRigidbodyPosition += gravity;
        //        targetMovementPredictionRigidbodyPosition += movement;
        //    }
        //    Vector3 deltaPos = targetMovementPredictionRigidbodyPosition - movementPredictionRigidbody.position;
        //    movementPredictionRigidbody.velocity = 1f / Time.fixedDeltaTime * deltaPos * Mathf.Pow(positionStrength, 90f * Time.fixedDeltaTime);
        //}

        private NetworkVariable<Vector3> currentPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> currentRotation = new NetworkVariable<Quaternion>();

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(currentPosition.Value + currentRotation.Value * gravitySphereCastPositionOffset, gravitySphereCastRadius);
            }
            else
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(currentPosition.Value, 0.25f);
            }
        }
    }
}