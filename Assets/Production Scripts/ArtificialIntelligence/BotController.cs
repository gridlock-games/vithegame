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

            for (int i = 0; i < Mathf.Min(collision.contactCount, 1); i++)
            {
                Vector3 normal = collision.GetContact(0).normal;
                Vector3 newVelocity;
                newVelocity.x = Mathf.MoveTowards(velocity.x, 0, Mathf.Abs(normal.x) * friction * Time.fixedDeltaTime);
                newVelocity.y = Mathf.MoveTowards(velocity.y, 0, Mathf.Abs(normal.y) * friction * Time.fixedDeltaTime);
                newVelocity.z = Mathf.MoveTowards(velocity.z, 0, Mathf.Abs(normal.z) * friction * Time.fixedDeltaTime);
                velocity = newVelocity;
            }

            currentPosition.Value = networkColliderRigidbody.position;
        }

        private const float friction = 1;

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

            for (int i = 0; i < Mathf.Min(collision.contactCount, 1); i++)
            {
                Vector3 normal = collision.GetContact(0).normal;
                Vector3 newVelocity;
                newVelocity.x = Mathf.MoveTowards(velocity.x, 0, Mathf.Abs(normal.x) * friction * Time.fixedDeltaTime);
                newVelocity.y = Mathf.MoveTowards(velocity.y, 0, Mathf.Abs(normal.y) * friction * Time.fixedDeltaTime);
                newVelocity.z = Mathf.MoveTowards(velocity.z, 0, Mathf.Abs(normal.z) * friction * Time.fixedDeltaTime);
                velocity = newVelocity;
            }

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
        private new void Awake()
        {
            base.Awake();
            attributes = GetComponent<Attributes>();
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

        private float GetTickRateDeltaTime()
        {
            return NetworkManager.NetworkTickSystem.LocalTime.FixedDeltaTime * Time.timeScale;
        }

        private float GetRootMotionSpeed()
        {
            return Mathf.Clamp01(weaponHandler.GetWeapon().GetMovementSpeed(weaponHandler.IsBlocking) - attributes.StatusAgent.GetMovementSpeedDecreaseAmount() + attributes.StatusAgent.GetMovementSpeedIncreaseAmount());
        }

        [SerializeField] private float runAnimationTransitionSpeed = 5;
        [SerializeField] private float gravitySphereCastRadius = 0.75f;
        [SerializeField] private Vector3 gravitySphereCastPositionOffset = new Vector3(0, 0.75f, 0);
        private NetworkVariable<float> moveForwardTarget = new NetworkVariable<float>();
        private NetworkVariable<float> moveSidesTarget = new NetworkVariable<float>();
        private NetworkVariable<Vector3> currentPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> currentRotation = new NetworkVariable<Quaternion>();
        private NetworkVariable<bool> isGrounded = new NetworkVariable<bool>();
        
        RaycastHit[] allHits = new RaycastHit[10];
        private void ProcessMovementTick()
        {
            Vector3 lookDirection = targetAttributes ? (targetAttributes.transform.position - currentPosition.Value).normalized : (NextPosition - currentPosition.Value).normalized;
            lookDirection.Scale(HORIZONTAL_PLANE);

            float randomMaxAngleOfRotation = Random.Range(60f, 120f);

            Quaternion newRotation = currentRotation.Value;
            if (attributes.ShouldApplyAilmentRotation())
                newRotation = attributes.GetAilmentRotation();
            else if (attributes.AnimationHandler.IsGrabAttacking())
                newRotation = currentRotation.Value;
            else if (weaponHandler.IsAiming() & !attributes.ShouldPlayHitStop())
                newRotation = Quaternion.RotateTowards(currentRotation.Value, lookDirection != Vector3.zero ? Quaternion.LookRotation(lookDirection) : currentRotation.Value, randomMaxAngleOfRotation * GetTickRateDeltaTime());
            else if (!attributes.ShouldPlayHitStop())
                newRotation = Quaternion.RotateTowards(currentRotation.Value, lookDirection != Vector3.zero ? Quaternion.LookRotation(lookDirection) : currentRotation.Value, randomMaxAngleOfRotation * GetTickRateDeltaTime());

            currentRotation.Value = newRotation;

            // This method is only called on the server
            if (!CanMove() | attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                moveForwardTarget.Value = 0;
                moveSidesTarget.Value = 0;
                isGrounded.Value = true;
                velocity = Vector3.zero;
                return;
            }

            CalculatePath(currentPosition.Value, NavMesh.AllAreas);

            Vector3 inputDir = NextPosition - currentPosition.Value;
            inputDir.y = 0;
            inputDir = transform.InverseTransformDirection(inputDir).normalized;
            
            if (Vector3.Distance(Destination, currentPosition.Value) < stoppingDistance)
            {
                inputDir = Vector3.zero;
            }

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
                bHit = true;
                if (Mathf.Approximately(allHits[i].distance, 0)) { continue; }
                if (allHits[i].distance > minDistance & minDistanceInitialized) { continue; }
                amountToAddToGravity = GetTickRateDeltaTime() * Mathf.Clamp01(allHits[i].distance) * Physics.gravity;
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
                    gravity += GetTickRateDeltaTime() * Physics.gravity;
                }
            }

            Vector3 animDir = Vector3.zero;
            // Apply movement
            Vector3 rootMotion = attributes.AnimationHandler.ApplyNetworkRootMotion() * GetRootMotionSpeed();
            Vector3 movement;
            if (attributes.ShouldPlayHitStop())
            {
                movement = Vector3.zero;
            }
            else if (attributes.AnimationHandler.ShouldApplyRootMotion())
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
                targetDirection *= isGrounded.Value ? GetRunSpeed() : 0;
                movement = attributes.StatusAgent.IsRooted() | attributes.AnimationHandler.IsReloading() ? Vector3.zero : GetTickRateDeltaTime() * targetDirection;
                animDir = new Vector3(targetDirection.x, 0, targetDirection.z);
            }

            if (attributes.AnimationHandler.IsFlinching()) { movement *= AnimationHandler.flinchingMovementSpeedMultiplier; }

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
#if UNITY_EDITOR
                Debug.DrawRay(startPos, movement.normalized, Color.cyan, GetTickRateDeltaTime());
#endif
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
            if (weaponHandler.GetWeapon().IsWalking(weaponHandler.IsBlocking))
            {
                moveForwardTarget.Value = animDir.z / 2;
                moveSidesTarget.Value = animDir.x / 2;
            }
            else
            {
                moveForwardTarget.Value = animDir.z;
                moveSidesTarget.Value = animDir.x;
            }
            
            bool wasPlayerHit = Physics.CapsuleCast(currentPosition.Value, currentPosition.Value + bodyHeightOffset, bodyRadius, movement.normalized, out RaycastHit playerHit, movement.magnitude, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);
            //bool wasPlayerHit = Physics.Raycast(currentPosition.Value + bodyHeightOffset / 2, movement.normalized, out RaycastHit playerHit, movement.magnitude, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);
            if (wasPlayerHit)
            {
                bool collidersIgnoreEachOther = false;
                foreach (Collider c in attributes.NetworkCollider.Colliders)
                {
                    if (Physics.GetIgnoreCollision(playerHit.collider, c))
                    {
                        collidersIgnoreEachOther = true;
                        break;
                    }
                }

                if (!collidersIgnoreEachOther)
                {
                    Quaternion targetRot = Quaternion.LookRotation(playerHit.transform.root.position - currentPosition.Value, Vector3.up);
                    float angle = targetRot.eulerAngles.y - Quaternion.LookRotation(movement, Vector3.up).eulerAngles.y;

                    if (angle > 180) { angle -= 360; }

                    if (angle > -20 & angle < 20)
                    {
                        movement = Vector3.zero;
                    }
                }
            }

            float multiplier = 1.0f - drag * GetTickRateDeltaTime();
            if (multiplier < 0.0f) multiplier = 0.0f;
            velocity = multiplier * velocity;
            movement += velocity;

            if (networkColliderRigidbody.SweepTest(movement.normalized, out RaycastHit movementHit, movement.magnitude, QueryTriggerInteraction.Ignore))
            {
                if (movementHit.distance > 0.5f)
                {
                    movement = Vector3.ClampMagnitude(movement, movementHit.distance);
                    velocity = Vector3.zero;
                }
            }

            Vector3 newPosition;
            if ((attributes.AnimationHandler.ShouldApplyRootMotion() & weaponHandler.CurrentActionClip.shouldIgnoreGravity) | !Mathf.Approximately(stairMovement, 0))
            {
                newPosition = currentPosition.Value + movement;
            }
            else
            {
                newPosition = currentPosition.Value + movement + gravity;
            }

            currentPosition.Value = newPosition;
        }

        private const float drag = 1;

        private List<CombatAgent> activePlayers = new List<CombatAgent>();
        private void UpdateActivePlayersList() { activePlayers = PlayerDataManager.Singleton.GetActiveCombatAgents(attributes); }

        private CombatAgent targetAttributes;

        private new void Update()
        {
            base.Update();

            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            if (PlayerDataManager.Singleton.LocalPlayersWasUpdatedThisFrame) { UpdateActivePlayersList(); }

            if (weaponHandler.CurrentActionClip.GetClipType() == ActionClip.ClipType.GrabAttack)
            {
                SetImmovable(attributes.AnimationHandler.IsGrabAttacking());
            }
            else
            {
                SetImmovable(attributes.IsGrabbed());
            }
            
            if (!IsSpawned) { return; }

            if (attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                SetDestination(currentPosition.Value, true);
            }

            UpdateLocomotion();
            attributes.AnimationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(attributes.AnimationHandler.Animator.GetFloat("MoveForward"), moveForwardTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
            attributes.AnimationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(attributes.AnimationHandler.Animator.GetFloat("MoveSides"), moveSidesTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
            attributes.AnimationHandler.Animator.SetBool("IsGrounded", isGrounded.Value);
        }

        public override Vector3 GetVelocity() { return velocity; }

        Vector3 velocity;
        public override void AddForce(Vector3 force)
        {
            if (!attributes.IsGrabbed() & !attributes.AnimationHandler.IsGrabAttacking()) { velocity += force * Time.fixedDeltaTime; }
        }

        private void RefreshStatus()
        {
            disableBots = FasterPlayerPrefs.Singleton.GetBool("DisableBots");
            canOnlyLightAttack = FasterPlayerPrefs.Singleton.GetBool("BotsCanOnlyLightAttack");
        }

        private bool disableBots;
        private bool canOnlyLightAttack;

        private IEnumerator EvaluateBotLogic()
        {
            while (true)
            {
                if (IsOwner)
                {
                    activePlayers.Sort((x, y) => Vector3.Distance(x.transform.position, currentPosition.Value).CompareTo(Vector3.Distance(y.transform.position, currentPosition.Value)));

                    targetAttributes = null;
                    foreach (CombatAgent player in activePlayers)
                    {
                        if (player.GetAilment() == ActionClip.Ailment.Death) { continue; }
                        if (!PlayerDataManager.Singleton.CanHit(attributes, player)) { continue; }
                        targetAttributes = player;
                        break;
                    }

                    if (disableBots)
                    {
                        SetDestination(currentPosition.Value, true);
                    }
                    else
                    {
                        if (targetAttributes)
                        {
                            SetDestination(targetAttributes.transform.position, true);
                        }
                        else
                        {
                            if (Vector3.Distance(Destination, transform.position) <= stoppingDistance)
                            {
                                float walkRadius = 500;
                                Vector3 randomDirection = Random.insideUnitSphere * walkRadius;
                                randomDirection += transform.position;
                                NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, walkRadius, 1);
                                SetDestination(hit.position, true);
                            }
                        }
                        EvaluteAction();
                    }
                }
                else if (disableBots)
                {
                    if (new Vector2(Destination.x, Destination.z) != new Vector2(currentPosition.Value.x, currentPosition.Value.z)) { SetDestination(currentPosition.Value, true); }
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private const float lightAttackDistance = 3;
        private const float heavyAttackDistance = 7;

        private const float chargeAttackDuration = 1;
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
                if (Vector3.Distance(Destination, transform.position) < lightAttackDistance)
                {
                    if (weaponHandler.CanAim) { weaponHandler.HeavyAttack(true); }
                    else { weaponHandler.HeavyAttack(false); }

                    weaponHandler.LightAttack(true);
                }
                return;
            }

            if (Time.time - lastWeaponSwapTime > weaponSwapDuration | attributes.LoadoutManager.WeaponNameThatCanFlashAttack != null)
            {
                attributes.LoadoutManager.SwitchWeapon();
                lastWeaponSwapTime = Time.time;
            }

            if (targetAttributes)
            {
                if (weaponHandler.CanADS)
                {
                    weaponHandler.AimDownSights(true);
                    if (Vector3.Distance(Destination, transform.position) < heavyAttackDistance)
                    {
                        weaponHandler.LightAttack(true);
                        EvaluateAbility();
                    }
                }
                else
                {
                    if (Vector3.Distance(Destination, transform.position) < lightAttackDistance)
                    {
                        if (!isHeavyAttacking)
                        {
                            weaponHandler.LightAttack(true);
                            EvaluateAbility();
                        }
                    }
                    else if (Vector3.Distance(Destination, transform.position) < heavyAttackDistance)
                    {
                        if (!weaponHandler.CanADS)
                        {
                            if (!isHeavyAttacking & Time.time - lastChargeAttackTime > chargeWaitDuration) { StartCoroutine(HeavyAttack()); }
                        }
                        EvaluateAbility();
                    }
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

        private float positionStrength = 1;
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
            }
        }

        private float GetRunSpeed()
        {
            return Mathf.Max(0, weaponHandler.GetWeapon().GetMovementSpeed(weaponHandler.IsBlocking) - attributes.StatusAgent.GetMovementSpeedDecreaseAmount()) + attributes.StatusAgent.GetMovementSpeedIncreaseAmount();
        }

        private float GetAnimatorSpeed()
        {
            return (Mathf.Max(0, weaponHandler.GetWeapon().GetRunSpeed() - attributes.StatusAgent.GetMovementSpeedDecreaseAmount()) + attributes.StatusAgent.GetMovementSpeedIncreaseAmount()) / weaponHandler.GetWeapon().GetRunSpeed() * (attributes.AnimationHandler.IsAtRest() ? 1 : (weaponHandler.IsInRecovery ? weaponHandler.CurrentActionClip.recoveryAnimationSpeed : weaponHandler.CurrentActionClip.animationSpeed));
        }

        private void UpdateLocomotion()
        {
            if (velocity.magnitude > 0.01f)
            {
                transform.position = currentPosition.Value;
            }
            else if (Vector3.Distance(transform.position, currentPosition.Value) > 2)
            {
                Debug.Log("Teleporting player: " + OwnerClientId + " " + name);
                transform.position = currentPosition.Value;
            }
            else
            {
                Vector3 newPosition;
                Vector2 horizontalPosition;
                if (attributes.AnimationHandler.ShouldApplyRootMotion())
                {
                    horizontalPosition = Vector2.MoveTowards(new Vector2(transform.position.x, transform.position.z),
                        new Vector2(currentPosition.Value.x, currentPosition.Value.z),
                        attributes.AnimationHandler.ApplyLocalRootMotion().magnitude * GetRootMotionSpeed() + velocity.sqrMagnitude / Time.fixedDeltaTime);
                }
                else
                {
                    horizontalPosition = Vector2.MoveTowards(new Vector2(transform.position.x, transform.position.z),
                        new Vector2(currentPosition.Value.x, currentPosition.Value.z),
                        Time.deltaTime * GetRunSpeed() + velocity.sqrMagnitude / Time.fixedDeltaTime);
                }
                newPosition.x = horizontalPosition.x;
                newPosition.z = horizontalPosition.y;
                newPosition.y = Mathf.MoveTowards(transform.position.y, currentPosition.Value.y, Time.deltaTime * -Physics.gravity.y + velocity.y / Time.fixedDeltaTime);

                if (attributes.ShouldShake())
                {
                    newPosition += Random.insideUnitSphere * (Time.deltaTime * CombatAgent.ShakeAmount);
                }

                transform.position = newPosition;
            }

            if (weaponHandler.CurrentActionClip != null)
            {
                if (attributes.ShouldPlayHitStop())
                {
                    attributes.AnimationHandler.Animator.speed = 0;
                }
                else
                {
                    if (attributes.IsGrabbed())
                    {
                        CombatAgent grabAssailant = attributes.GetGrabAssailant();
                        if (grabAssailant)
                        {
                            if (grabAssailant.AnimationHandler)
                            {
                                attributes.AnimationHandler.Animator.speed = grabAssailant.AnimationHandler.Animator.speed;
                            }
                        }
                    }
                    else
                    {
                        attributes.AnimationHandler.Animator.speed = GetAnimatorSpeed();
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

        private void LateUpdate()
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, currentRotation.Value, (weaponHandler.IsAiming() ? GetTickRateDeltaTime() : Time.deltaTime) * 15);
        }

        void OnDodge()
        {
            Vector3 moveInput = transform.InverseTransformDirection(NextPosition - currentPosition.Value).normalized;
            float angle = Vector3.SignedAngle(transform.rotation * new Vector3(moveInput.x, 0, moveInput.z) * (attributes.StatusAgent.IsFeared() ? -1 : 1), transform.forward, Vector3.up);
            attributes.AnimationHandler.PlayAction(weaponHandler.GetWeapon().GetDodgeClip(angle));
        }
    }
}