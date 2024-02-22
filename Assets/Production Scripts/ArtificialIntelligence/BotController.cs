using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;
using Vi.ScriptableObjects;
using UnityEngine.AI;

namespace Vi.ArtificialIntelligence
{
    public class BotController : MovementHandler
    {
        [SerializeField] private Rigidbody networkColliderRigidbody;

        public override void SetOrientation(Vector3 newPosition, Quaternion newRotation)
        {
            currentPosition.Value = newPosition;
            currentRotation.Value = newRotation;
            networkColliderRigidbody.position = newPosition;
            if (!navMeshAgent.Warp(newPosition)) { Debug.LogError("Warp unsuccessful!"); }
        }

        public override void ReceiveOnCollisionEnterMessage(Collision collision)
        {
            if (!IsServer) { return; }
            currentPosition.Value = networkColliderRigidbody.position;
        }

        public override void ReceiveOnCollisionStayMessage(Collision collision)
        {
            if (!IsServer) { return; }
            currentPosition.Value = networkColliderRigidbody.position;
        }

        public override void OnNetworkSpawn()
        {
            currentPosition.Value = transform.position;
            currentRotation.Value = transform.rotation;
            if (IsServer) { NetworkManager.NetworkTickSystem.Tick += ProcessMovementTick; }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) { NetworkManager.NetworkTickSystem.Tick -= ProcessMovementTick; }
        }

        private NavMeshAgent navMeshAgent;
        private Attributes attributes;
        private AnimationHandler animationHandler;
        private WeaponHandler weaponHandler;

        private void Awake()
        {
            animationHandler = GetComponent<AnimationHandler>();
            attributes = GetComponent<Attributes>();
            navMeshAgent = GetComponent<NavMeshAgent>();
            weaponHandler = GetComponent<WeaponHandler>();
            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;
            navMeshAgent.updateUpAxis = false;
        }

        private new void Start()
        {
            base.Start();
            networkColliderRigidbody.transform.SetParent(null, true);
        }

        private new void OnDestroy()
        {
            base.OnDestroy();
            if (networkColliderRigidbody) { Destroy(networkColliderRigidbody.gameObject); }
        }

        [SerializeField] private float angularSpeed = 540;
        [SerializeField] private float runSpeed = 5;
        [SerializeField] private float runAnimationTransitionSpeed = 5;
        [SerializeField] private float gravitySphereCastRadius = 0.75f;
        [SerializeField] private Vector3 gravitySphereCastPositionOffset = new Vector3(0, 0.75f, 0);
        [SerializeField] private float stairHeight = 0.5f;
        [SerializeField] private float rampCheckHeight = 0.1f;
        private NetworkVariable<float> moveForwardTarget = new NetworkVariable<float>();
        private NetworkVariable<float> moveSidesTarget = new NetworkVariable<float>();
        private NetworkVariable<Vector3> currentPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> currentRotation = new NetworkVariable<Quaternion>();
        private NetworkVariable<bool> isGrounded = new NetworkVariable<bool>();
        public static readonly Vector3 HORIZONTAL_PLANE = new Vector3(1, 0, 1);
        private void ProcessMovementTick()
        {
            // This method is only called on the server
            if (!CanMove() | attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                moveForwardTarget.Value = 0;
                moveSidesTarget.Value = 0;
                navMeshAgent.nextPosition = currentPosition.Value;
                return;
            }

            Vector3 inputDir = transform.InverseTransformDirection(navMeshAgent.nextPosition - currentPosition.Value).normalized;
            
            Vector3 lookDirection = (navMeshAgent.nextPosition - currentPosition.Value).normalized;
            lookDirection.Scale(HORIZONTAL_PLANE);

            Quaternion newRotation;
            if (attributes.ShouldApplyAilmentRotation())
                newRotation = attributes.GetAilmentRotation();
            if (weaponHandler.IsAiming())
                newRotation = lookDirection != Vector3.zero ? Quaternion.LookRotation(lookDirection) : currentRotation.Value;
            else
                newRotation = lookDirection != Vector3.zero ? Quaternion.RotateTowards(currentRotation.Value, Quaternion.LookRotation(lookDirection), 1f / NetworkManager.NetworkTickSystem.TickRate * angularSpeed) : currentRotation.Value;

            // Handle gravity
            RaycastHit[] allHits = Physics.SphereCastAll(currentPosition.Value + currentRotation.Value * gravitySphereCastPositionOffset,
                                            gravitySphereCastRadius, Physics.gravity, gravitySphereCastPositionOffset.magnitude, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore);
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
            Vector3 gravity = Vector3.zero;
            bool bHit = false;
            foreach (RaycastHit hit in allHits)
            {
                gravity += 1f / NetworkManager.NetworkTickSystem.TickRate * Mathf.Clamp01(hit.distance) * Physics.gravity;
                bHit = true;
                break;
            }
            if (!bHit) { gravity += 1f / NetworkManager.NetworkTickSystem.TickRate * Physics.gravity; }
            if (IsServer) { isGrounded.Value = bHit; }

            Vector3 animDir = Vector3.zero;
            // Apply movement
            Vector3 rootMotion = animationHandler.ApplyNetworkRootMotion() * Mathf.Clamp01(runSpeed - attributes.GetMovementSpeedDecreaseAmount() + attributes.GetMovementSpeedIncreaseAmount());
            Vector3 movement;
            if (animationHandler.ShouldApplyRootMotion())
            {
                movement = attributes.IsRooted() ? Vector3.zero : rootMotion;
            }
            else
            {
                //Vector3 targetDirection = inputPayload.rotation * (new Vector3(inputPayload.inputVector.x, 0, inputPayload.inputVector.y) * (attributes.IsFeared() ? -1 : 1));
                Vector3 targetDirection = newRotation * (new Vector3(inputDir.x, 0, inputDir.z) * (attributes.IsFeared() ? -1 : 1));
                targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
                targetDirection *= isGrounded.Value ? Mathf.Max(0, runSpeed - attributes.GetMovementSpeedDecreaseAmount()) + attributes.GetMovementSpeedIncreaseAmount() : 0;
                movement = attributes.IsRooted() ? Vector3.zero : 1f / NetworkManager.NetworkTickSystem.TickRate * Time.timeScale * targetDirection;
                animDir = new Vector3(targetDirection.x, 0, targetDirection.z);
            }

            Debug.DrawRay(currentPosition.Value, movement.normalized * 1, Color.red, 1f / NetworkManager.NetworkTickSystem.TickRate);
            // If we hit an object in the direction we are moving, we need to check if it is a stair/climbable
            if (Physics.Raycast(currentPosition.Value, movement.normalized, out RaycastHit lowerHit, 1, LayerMask.GetMask(new string[] { "Default" }), QueryTriggerInteraction.Ignore))
            {
                Debug.DrawRay(currentPosition.Value + transform.up * rampCheckHeight, movement.normalized, Color.cyan, 1f / NetworkManager.NetworkTickSystem.TickRate);
                // Check if we are walking up a ramp
                if (Physics.Raycast(currentPosition.Value + transform.up * rampCheckHeight, movement.normalized, out RaycastHit rampHit, 1, LayerMask.GetMask(new string[] { "Default" }), QueryTriggerInteraction.Ignore))
                {
                    // If the distances of the lowerHit and rampHit are the same, that means we are climbing a stairs
                    if (Mathf.Approximately(rampHit.distance, lowerHit.distance))
                    {
                        Debug.DrawRay(currentPosition.Value + transform.up * stairHeight, movement.normalized * lowerHit.distance, Color.black, 1f / NetworkManager.NetworkTickSystem.TickRate);
                        if (!Physics.Raycast(currentPosition.Value + transform.up * stairHeight, movement.normalized, lowerHit.distance + 0.1f, LayerMask.GetMask(new string[] { "Default" }), QueryTriggerInteraction.Ignore))
                        {
                            //Debug.Log(Time.time + " climbing stairs " + lowerHit.collider.name + " " + rampHit.collider.name);
                            movement.y += stairHeight / 2;
                        }
                    }
                }
            }

            animDir = transform.InverseTransformDirection(Vector3.ClampMagnitude(animDir, 1));
            if (IsOwner)
            {
                moveForwardTarget.Value = animDir.z;
                moveSidesTarget.Value = animDir.x;
            }

            currentPosition.Value += movement + gravity;
            currentRotation.Value = newRotation;
            navMeshAgent.nextPosition = currentPosition.Value;
        }

        private void Update()
        {
            if (!CanMove()) { return; }
            if (!IsSpawned) { return; }

            if (attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                if (navMeshAgent.isOnNavMesh) { navMeshAgent.destination = currentPosition.Value; }
            }
            else
            {
                UpdateLocomotion();
                animationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveForward"), moveForwardTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
                animationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveSides"), moveSidesTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
                animationHandler.Animator.SetBool("IsGrounded", isGrounded.Value);

                if (IsOwner)
                {
                    List<Attributes> activePlayers = PlayerDataManager.Singleton.GetActivePlayerObjects(attributes);
                    activePlayers.Sort((x, y) => Vector3.Distance(x.transform.position, currentPosition.Value).CompareTo(Vector3.Distance(y.transform.position, currentPosition.Value)));
                    Attributes targetAttributes = null;
                    foreach (Attributes player in activePlayers)
                    {
                        if (player.GetAilment() == ActionClip.Ailment.Death) { continue; }
                        targetAttributes = player;
                        break;
                    }

                    if (targetAttributes)
                    {
                        if (navMeshAgent.isOnNavMesh) { navMeshAgent.destination = targetAttributes.transform.position; }

                        if (Vector3.Distance(navMeshAgent.destination, transform.position) < 3)
                        {
                            weaponHandler.SendMessage("OnLightAttack");
                        }
                    }
                }
            }
        }

        private float positionStrength = 1;
        //private float rotationStrength = 1;
        void FixedUpdate()
        {
            if (Vector3.Distance(networkColliderRigidbody.position, currentPosition.Value) > 4)
            {
                networkColliderRigidbody.position = currentPosition.Value;
            }
            else
            {
                Vector3 deltaPos = currentPosition.Value - networkColliderRigidbody.position;
                networkColliderRigidbody.velocity = 1f / Time.fixedDeltaTime * deltaPos * Mathf.Pow(positionStrength, 90f * Time.fixedDeltaTime);

                //(movementPrediction.CurrentRotation * Quaternion.Inverse(transform.rotation)).ToAngleAxis(out float angle, out Vector3 axis);
                //if (angle > 180.0f) angle -= 360.0f;
                //movementPredictionRigidbody.angularVelocity = 1f / Time.fixedDeltaTime * 0.01745329251994f * angle * Mathf.Pow(rotationStrength, 90f * Time.fixedDeltaTime) * axis;
            }
        }

        private void UpdateLocomotion()
        {
            if (Vector3.Distance(transform.position, currentPosition.Value) > 2)
            {
                //Debug.Log("Teleporting player: " + OwnerClientId);
                transform.position = currentPosition.Value;
            }
            else
            {
                Vector3 movement = Time.deltaTime * (NetworkManager.NetworkTickSystem.TickRate / 2) * (currentPosition.Value - transform.position);
                transform.position += movement;
            }

            animationHandler.Animator.speed = (Mathf.Max(0, runSpeed - attributes.GetMovementSpeedDecreaseAmount()) + attributes.GetMovementSpeedIncreaseAmount()) / runSpeed * weaponHandler.CurrentActionClip.animationSpeed;

            if (attributes.ShouldApplyAilmentRotation())
                transform.rotation = attributes.GetAilmentRotation();
            else if (weaponHandler.IsAiming())
                transform.rotation = Quaternion.Slerp(transform.rotation, currentRotation.Value, Time.deltaTime * NetworkManager.NetworkTickSystem.TickRate);
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, currentRotation.Value, Time.deltaTime * NetworkManager.NetworkTickSystem.TickRate);
        }

        void OnDodge()
        {
            Vector3 moveInput = (navMeshAgent.nextPosition - currentPosition.Value).normalized;
            float angle = Vector3.SignedAngle(transform.rotation * new Vector3(moveInput.x, 0, moveInput.z), transform.forward, Vector3.up);
            animationHandler.PlayAction(weaponHandler.GetWeapon().GetDodgeClip(angle));
        }
    }
}