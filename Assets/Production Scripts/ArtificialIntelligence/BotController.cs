using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;
using Vi.ScriptableObjects;
using UnityEngine.AI;
using Vi.Utility;
using Vi.Core.CombatAgents;

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
        }

        public override Vector3 GetPosition() { return currentPosition.Value; }

        public override void SetImmovable(bool isKinematic)
        {
            networkColliderRigidbody.constraints = isKinematic ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.FreezeRotation;
        }

        private Vector3 lastMovement;
        public override void ReceiveOnCollisionEnterMessage(Collision collision)
        {
            if (!IsServer) { return; }
            //if (collision.collider.GetComponent<NetworkCollider>())
            //{
            //    if (collision.relativeVelocity.magnitude > 1)
            //    {
            //        if (Vector3.Angle(lastMovement, collision.relativeVelocity) < 90) { networkColliderRigidbody.AddForce(-collision.relativeVelocity * collisionPushDampeningFactor, ForceMode.VelocityChange); }
            //    }
            //}
            currentPosition.Value = networkColliderRigidbody.position;
        }

        public override void ReceiveOnCollisionStayMessage(Collision collision)
        {
            if (!IsServer) { return; }
            //if (collision.collider.GetComponent<NetworkCollider>())
            //{
            //    if (collision.relativeVelocity.magnitude > 1)
            //    {
            //        if (Vector3.Angle(lastMovement, collision.relativeVelocity) < 90) { networkColliderRigidbody.AddForce(-collision.relativeVelocity * collisionPushDampeningFactor, ForceMode.VelocityChange); }
            //    }
            //}
            currentPosition.Value = networkColliderRigidbody.position;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.NetworkTickSystem.Tick += ProcessMovementTick;
                currentPosition.Value = transform.position;
                currentRotation.Value = transform.rotation;
            }
            networkColliderRigidbody.collisionDetectionMode = IsServer ? CollisionDetectionMode.Continuous : CollisionDetectionMode.Discrete;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer) { NetworkManager.NetworkTickSystem.Tick -= ProcessMovementTick; }
        }

        private Attributes attributes;
        private LoadoutManager loadoutManager;
        private AnimationHandler animationHandler;
        private new void Awake()
        {
            base.Awake();
            path = new NavMeshPath();
            animationHandler = GetComponent<AnimationHandler>();
            attributes = GetComponent<Attributes>();
            loadoutManager = GetComponent<LoadoutManager>();
            RefreshStatus();
        }

        private void Start()
        {
            networkColliderRigidbody.transform.SetParent(null, true);
            UpdateActivePlayersList();
            StartCoroutine(EvaluateBotLogic());
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
        private NetworkVariable<float> moveForwardTarget = new NetworkVariable<float>();
        private NetworkVariable<float> moveSidesTarget = new NetworkVariable<float>();
        private NetworkVariable<Vector3> currentPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> currentRotation = new NetworkVariable<Quaternion>();
        private NetworkVariable<bool> isGrounded = new NetworkVariable<bool>();
        
        private NavMeshPath path;
        private Vector3 nextPosition;
        private const float stoppingDistance = 2;

        RaycastHit[] allHits = new RaycastHit[10];

        private void ProcessMovementTick()
        {
            // This method is only called on the server
            if (!CanMove() | attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                moveForwardTarget.Value = 0;
                moveSidesTarget.Value = 0;
                lastMovement = Vector3.zero;
                return;
            }

            if (NavMesh.CalculatePath(currentPosition.Value, destination, NavMesh.AllAreas, path))
            {
                if (path.corners.Length > 1)
                {
                    nextPosition = path.corners[1];
                }
                else if (path.corners.Length > 0)
                {
                    nextPosition = path.corners[0];
                }
                else
                {
                    nextPosition = currentPosition.Value;
                }
            }
            else
            {
                nextPosition = destination;
            }

            Vector3 inputDir = nextPosition - currentPosition.Value;
            inputDir.y = 0;
            inputDir = transform.InverseTransformDirection(inputDir).normalized;
            
            if (Vector3.Distance(destination, currentPosition.Value) < stoppingDistance)
            {
                inputDir = Vector3.zero;
            }

            Vector3 lookDirection = targetAttributes ? (targetAttributes.transform.position - currentPosition.Value).normalized : (nextPosition - currentPosition.Value).normalized;
            lookDirection.Scale(HORIZONTAL_PLANE);

            float randomMaxAngleOfRotation = Random.Range(60f, 120f);

            Quaternion newRotation = currentRotation.Value;
            if (attributes.ShouldApplyAilmentRotation())
                newRotation = attributes.GetAilmentRotation();
            else if (animationHandler.IsGrabAttacking())
                newRotation = currentRotation.Value;
            else if (weaponHandler.IsAiming() & !attributes.ShouldPlayHitStop())
                newRotation = Quaternion.RotateTowards(currentRotation.Value, lookDirection != Vector3.zero ? Quaternion.LookRotation(lookDirection) : currentRotation.Value, randomMaxAngleOfRotation * (1f / NetworkManager.NetworkTickSystem.TickRate));
            else if (!attributes.ShouldPlayHitStop())
                newRotation = Quaternion.RotateTowards(currentRotation.Value, lookDirection != Vector3.zero ? Quaternion.LookRotation(lookDirection) : currentRotation.Value, randomMaxAngleOfRotation * (1f / NetworkManager.NetworkTickSystem.TickRate));

            // Handle gravity
            Vector3 gravity = Vector3.zero;
            int allHitsCount = Physics.SphereCastNonAlloc(currentPosition.Value + currentRotation.Value * gravitySphereCastPositionOffset,
                gravitySphereCastRadius, Physics.gravity.normalized, allHits, gravitySphereCastPositionOffset.magnitude,
                LayerMask.GetMask(layersToAccountForInMovement), QueryTriggerInteraction.Ignore);

            bool bHit = false;
            float minDistance = 0;
            bool minDistanceInitialized = false;
            Vector3 amountToAddToGravity = Vector3.zero;
            for (int i = 0; i < allHitsCount; i++)
            {
                if (allHits[i].distance > minDistance & minDistanceInitialized) { continue; }
                amountToAddToGravity = 1f / NetworkManager.NetworkTickSystem.TickRate * Mathf.Clamp01(allHits[i].distance) * Physics.gravity;
                bHit = true;
                minDistance = allHits[i].distance;
                minDistanceInitialized = true;
            }
            gravity += amountToAddToGravity;

            if (bHit)
            {
                isGrounded.Value = true;
            }
            else // If no sphere cast hit
            {
                if (Physics.Raycast(currentPosition.Value + currentRotation.Value * gravitySphereCastPositionOffset,
                    Physics.gravity, 1, LayerMask.GetMask(layersToAccountForInMovement), QueryTriggerInteraction.Ignore))
                {
                    isGrounded.Value = true;
                }
                else
                {
                    isGrounded.Value = false;
                    gravity += 1f / NetworkManager.NetworkTickSystem.TickRate * Physics.gravity;
                }
            }

            Vector3 animDir = Vector3.zero;
            // Apply movement
            Vector3 rootMotion = animationHandler.ApplyNetworkRootMotion() * Mathf.Clamp01(runSpeed - attributes.StatusAgent.GetMovementSpeedDecreaseAmount() + attributes.StatusAgent.GetMovementSpeedIncreaseAmount());
            Vector3 movement;
            if (attributes.ShouldPlayHitStop())
            {
                movement = Vector3.zero;
            }
            else if (animationHandler.ShouldApplyRootMotion())
            {
                if (attributes.StatusAgent.IsRooted() & attributes.GetAilment() != ActionClip.Ailment.Knockup & attributes.GetAilment() != ActionClip.Ailment.Knockdown)
                {
                    movement = Vector3.zero;
                }
                else
                {
                    movement = rootMotion;
                }
            }
            else
            {
                //Vector3 targetDirection = inputPayload.rotation * (new Vector3(inputPayload.inputVector.x, 0, inputPayload.inputVector.y) * (attributes.IsFeared() ? -1 : 1));
                Vector3 targetDirection = newRotation * (new Vector3(inputDir.x, 0, inputDir.z) * (attributes.StatusAgent.IsFeared() ? -1 : 1));
                targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
                targetDirection *= isGrounded.Value ? Mathf.Max(0, runSpeed - attributes.StatusAgent.GetMovementSpeedDecreaseAmount()) + attributes.StatusAgent.GetMovementSpeedIncreaseAmount() : 0;
                movement = attributes.StatusAgent.IsRooted() | animationHandler.IsReloading() ? Vector3.zero : 1f / NetworkManager.NetworkTickSystem.TickRate * Time.timeScale * targetDirection;
                animDir = new Vector3(targetDirection.x, 0, targetDirection.z);
            }

            if (animationHandler.IsFlinching()) { movement *= AnimationHandler.flinchingMovementSpeedMultiplier; }

            float stairMovement = 0;
            float yOffset = 0.2f;
            Vector3 startPos = currentPosition.Value;
            startPos.y += yOffset;
            while (Physics.Raycast(startPos, movement.normalized, out RaycastHit stairHit, 1, LayerMask.GetMask(layersToAccountForInMovement), QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Angle(movement.normalized, stairHit.normal) < 140)
                {
                    break;
                }

                if (Application.isEditor) { Debug.DrawRay(startPos, movement.normalized, Color.cyan, 1f / NetworkManager.NetworkTickSystem.TickRate); }
                startPos.y += yOffset;
                stairMovement = startPos.y - currentPosition.Value.y - yOffset;

                if (stairMovement > 0.5f)
                {
                    stairMovement = 0;
                    break;
                }
            }

            movement.y += stairMovement;

            animDir = transform.InverseTransformDirection(Vector3.ClampMagnitude(animDir, 1));
            if (IsOwner)
            {
                moveForwardTarget.Value = animDir.z;
                moveSidesTarget.Value = animDir.x;
            }

            bool wasPlayerHit = Physics.CapsuleCast(currentPosition.Value, currentPosition.Value + bodyHeightOffset, bodyRadius, movement.normalized, out RaycastHit playerHit, movement.magnitude, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);
            //bool wasPlayerHit = Physics.Raycast(currentPosition.Value + bodyHeightOffset / 2, movement.normalized, out RaycastHit playerHit, movement.magnitude, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);
            if (wasPlayerHit)
            {
                Quaternion targetRot = Quaternion.LookRotation(playerHit.transform.root.position - currentPosition.Value, Vector3.up);
                float angle = targetRot.eulerAngles.y - Quaternion.LookRotation(movement, Vector3.up).eulerAngles.y;

                if (angle > 180) { angle -= 360; }

                if (angle > -20 & angle < 20)
                {
                    movement = Vector3.zero;
                }
            }

            movement += forceAccumulated;
            forceAccumulated = Vector3.zero;

            Vector3 newPosition;
            if (Mathf.Approximately(movement.y, 0))
            {
                newPosition = currentPosition.Value + movement + gravity;
            }
            else
            {
                newPosition = currentPosition.Value + movement;
            }

            currentPosition.Value = newPosition;
            currentRotation.Value = newRotation;
            lastMovement = movement;
        }

        [SerializeField] private List<Attributes> activePlayers = new List<Attributes>();

        private void UpdateActivePlayersList()
        {
            activePlayers = PlayerDataManager.Singleton.GetActivePlayerObjects(attributes);
        }

        private Attributes targetAttributes;

        Vector3 destination;
        private new void Update()
        {
            base.Update();

            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            if (PlayerDataManager.Singleton.LocalPlayersWasUpdatedThisFrame) { UpdateActivePlayersList(); }

            if (weaponHandler.CurrentActionClip.GetClipType() == ActionClip.ClipType.GrabAttack)
            {
                SetImmovable(animationHandler.IsGrabAttacking());
            }
            else
            {
                SetImmovable(attributes.IsGrabbed());
            }
            
            if (!IsSpawned) { return; }

            if (attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                destination = currentPosition.Value;
            }
            else
            {
                UpdateLocomotion();
                animationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveForward"), moveForwardTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
                animationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(animationHandler.Animator.GetFloat("MoveSides"), moveSidesTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
                animationHandler.Animator.SetBool("IsGrounded", isGrounded.Value);
            }
        }
        
        private void RefreshStatus()
        {
            disableBots = bool.Parse(FasterPlayerPrefs.Singleton.GetString("DisableBots"));
            canOnlyLightAttack = bool.Parse(FasterPlayerPrefs.Singleton.GetString("BotsCanOnlyLightAttack"));
        }

        private bool disableBots;
        private bool canOnlyLightAttack;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            if (!Application.isPlaying) { return; }
            for (int i = 0; i < path.corners.Length; i++)
            {
                Gizmos.DrawSphere(path.corners[i], 0.5f);
                if (i == 0)
                {
                    Gizmos.DrawLine(transform.position, path.corners[i]);
                }
                else
                {
                    Gizmos.DrawLine(path.corners[i - 1], path.corners[i]);
                }
            }
        }

        private IEnumerator EvaluateBotLogic()
        {
            while (true)
            {
                if (IsOwner)
                {
                    activePlayers.Sort((x, y) => Vector3.Distance(x.transform.position, currentPosition.Value).CompareTo(Vector3.Distance(y.transform.position, currentPosition.Value)));

                    targetAttributes = null;
                    foreach (Attributes player in activePlayers)
                    {
                        if (player.GetAilment() == ActionClip.Ailment.Death) { continue; }
                        if (!PlayerDataManager.Singleton.CanHit(attributes, player)) { continue; }
                        targetAttributes = player;
                        break;
                    }

                    if (disableBots)
                    {
                        if (new Vector2(destination.x, destination.z) != new Vector2(currentPosition.Value.x, currentPosition.Value.z)) { destination = currentPosition.Value; }
                    }
                    else
                    {
                        if (targetAttributes)
                        {
                            if (new Vector2(destination.x, destination.z) != new Vector2(targetAttributes.transform.position.x, targetAttributes.transform.position.z)) { destination = targetAttributes.transform.position; }
                        }
                        else
                        {
                            if (Vector3.Distance(destination, transform.position) <= stoppingDistance)
                            {
                                float walkRadius = 500;
                                Vector3 randomDirection = Random.insideUnitSphere * walkRadius;
                                randomDirection += transform.position;
                                NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, walkRadius, 1);
                                destination = hit.position;
                            }
                        }
                        EvaluteAction();
                    }
                }
                else if (disableBots)
                {
                    if (new Vector2(destination.x, destination.z) != new Vector2(currentPosition.Value.x, currentPosition.Value.z)) { destination = currentPosition.Value; }
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private const float lightAttackDistance = 3;
        private const float heavyAttackDistance = 7;

        private const float chargeAttackDuration = 0.5f;
        private const float chargeWaitDuration = 2;
        private float lastChargeAttackTime;

        private const float dodgeWaitDuration = 5;
        private float lastDodgeTime;

        private const float weaponSwapDuration = 20;
        private float lastWeaponSwapTime;

        private const float abilityWaitDuration = 3;
        private float lastAbilityTime;

        private void EvaluteAction()
        {
            if (canOnlyLightAttack)
            {
                if (Vector3.Distance(destination, transform.position) < lightAttackDistance)
                {
                    if (weaponHandler.CanAim) { weaponHandler.HeavyAttack(true); }
                    else { weaponHandler.HeavyAttack(false); }

                    weaponHandler.LightAttack(true);
                }
                return;
            }

            if (Time.time - lastWeaponSwapTime > weaponSwapDuration | loadoutManager.WeaponNameThatCanFlashAttack != null)
            {
                loadoutManager.SwitchWeapon();
                lastWeaponSwapTime = Time.time;
            }

            if (targetAttributes)
            {
                if (Vector3.Distance(destination, transform.position) < lightAttackDistance)
                {
                    if (weaponHandler.CanADS) { weaponHandler.HeavyAttack(true); }
                    else { weaponHandler.HeavyAttack(false); }

                    weaponHandler.LightAttack(true);

                    EvaluateAbility();
                }
                else if (Vector3.Distance(destination, transform.position) < heavyAttackDistance)
                {
                    if (!isHeavyAttacking & Time.time - lastChargeAttackTime > chargeWaitDuration) { StartCoroutine(HeavyAttack()); }

                    if (weaponHandler.CanADS) { weaponHandler.LightAttack(true); }
                    else { weaponHandler.LightAttack(false); }

                    EvaluateAbility();
                }
            }

            if (Time.time - lastDodgeTime > dodgeWaitDuration)
            {
                OnDodge();
                lastDodgeTime = Time.time;
            }
        }

        private bool isHeavyAttacking;

        private IEnumerator HeavyAttack()
        {
            if (isHeavyAttacking) { yield break; }
            isHeavyAttacking = true;

            weaponHandler.HeavyAttack(true);

            yield return new WaitForSeconds(chargeAttackDuration);

            lastChargeAttackTime = Time.time;
            weaponHandler.HeavyAttack(false);

            isHeavyAttacking = false;
        }

        private void EvaluateAbility()
        {
            if (Time.time - lastAbilityTime > abilityWaitDuration)
            {
                if (attributes.GetRage() / attributes.GetMaxRage() >= 1)
                {
                    attributes.OnActivateRage();
                    lastAbilityTime = Time.time;
                    return;
                }

                int abilityNum = Random.Range(1, 5);
                if (abilityNum == 1)
                {
                    weaponHandler.Ability1(true);
                }
                else if (abilityNum == 2)
                {
                    weaponHandler.Ability2(true);
                }
                else if (abilityNum == 3)
                {
                    weaponHandler.Ability3(true);
                }
                else if (abilityNum == 4)
                {
                    weaponHandler.Ability4(true);
                }
                else
                {
                    Debug.LogError("Unsure how to handle ability num of - " + abilityNum);
                }
                lastAbilityTime = Time.time;
            }
        }

        Vector3 forceAccumulated;
        public override void AddForce(Vector3 force)
        {
            forceAccumulated += force * Time.fixedDeltaTime;
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

                if (attributes.ShouldShake())
                {
                    movement += Random.insideUnitSphere * (Time.deltaTime * Attributes.ShakeAmount);
                }

                transform.position += movement;
            }

            if (weaponHandler.CurrentActionClip != null)
            {
                if (attributes.ShouldPlayHitStop())
                {
                    animationHandler.Animator.speed = 0;
                }
                else
                {
                    if (attributes.IsGrabbed())
                    {
                        Attributes grabAssailant = attributes.GetGrabAssailant();
                        if (grabAssailant)
                        {
                            if (grabAssailant.TryGetComponent(out AnimationHandler assailantAnimationHandler))
                            {
                                animationHandler.Animator.speed = assailantAnimationHandler.Animator.speed;
                            }
                        }
                    }
                    else
                    {
                        animationHandler.Animator.speed = (Mathf.Max(0, weaponHandler.GetWeapon().GetRunSpeed() - attributes.StatusAgent.GetMovementSpeedDecreaseAmount()) + attributes.StatusAgent.GetMovementSpeedIncreaseAmount()) / weaponHandler.GetWeapon().GetRunSpeed() * (animationHandler.IsAtRest() ? 1 : (weaponHandler.IsInRecovery ? weaponHandler.CurrentActionClip.recoveryAnimationSpeed : weaponHandler.CurrentActionClip.animationSpeed));
                    }
                }
            }

            if (attributes.ShouldApplyAilmentRotation())
                transform.rotation = attributes.GetAilmentRotation();
            else if (weaponHandler.IsAiming())
                transform.rotation = Quaternion.Slerp(transform.rotation, currentRotation.Value, Time.deltaTime * NetworkManager.NetworkTickSystem.TickRate);
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, currentRotation.Value, Time.deltaTime * NetworkManager.NetworkTickSystem.TickRate);
        }

        void OnDodge()
        {
            Vector3 moveInput = transform.InverseTransformDirection(nextPosition - currentPosition.Value).normalized;
            float angle = Vector3.SignedAngle(transform.rotation * new Vector3(moveInput.x, 0, moveInput.z) * (attributes.StatusAgent.IsFeared() ? -1 : 1), transform.forward, Vector3.up);
            animationHandler.PlayAction(weaponHandler.GetWeapon().GetDodgeClip(angle));
        }
    }
}