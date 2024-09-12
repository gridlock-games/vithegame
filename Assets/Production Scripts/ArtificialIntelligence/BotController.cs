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
    public class BotController : PhysicsMovementHandler
    {
        private Attributes attributes;
        private new void Awake()
        {
            base.Awake();
            attributes = GetComponent<Attributes>();
            RefreshStatus();
        }

        private void Start()
        {
            Rigidbody.transform.SetParent(null, true);
            UpdateActivePlayersList();
        }

        private new void OnDestroy()
        {
            base.OnDestroy();
            if (Rigidbody) { Destroy(Rigidbody.gameObject); }
        }

        private List<CombatAgent> activePlayers = new List<CombatAgent>();
        private void UpdateActivePlayersList() { activePlayers = PlayerDataManager.Singleton.GetActiveCombatAgents(attributes); }

        private CombatAgent targetAttributes;

        private const float runAnimationTransitionSpeed = 5;
        private new void Update()
        {
            base.Update();

            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            if (PlayerDataManager.Singleton.LocalPlayersWasUpdatedThisFrame) { UpdateActivePlayersList(); }

            if (!IsSpawned) { return; }

            if (IsServer)
            {
                if (attributes.GetAilment() == ActionClip.Ailment.Death) { SetDestination(Rigidbody.position, true); }
            }

            UpdateAnimatorParameters();
            UpdateAnimatorSpeed();
            EvaluateBotLogic();

            transform.position = Rigidbody.transform.position;
            transform.rotation = EvaluateRotation();
            if (IsServer) { currentRotation.Value = transform.rotation; }
        }

        private Quaternion EvaluateRotation()
        {
            if (IsServer)
            {
                Vector3 camDirection = targetAttributes ? (targetAttributes.transform.position - Rigidbody.position).normalized : (NextPosition - Rigidbody.position).normalized;
                camDirection.Scale(HORIZONTAL_PLANE);

                if (attributes.ShouldApplyAilmentRotation())
                    return attributes.GetAilmentRotation();
                else if (attributes.IsGrabbing())
                    return transform.rotation;
                else if (attributes.IsGrabbed())
                {
                    CombatAgent grabAssailant = attributes.GetGrabAssailant();
                    if (grabAssailant)
                    {
                        Vector3 rel = grabAssailant.MovementHandler.GetPosition() - GetPosition();
                        rel = Vector3.Scale(rel, HORIZONTAL_PLANE);
                        return Quaternion.LookRotation(rel, Vector3.up);
                    }
                }
                else if (!attributes.ShouldPlayHitStop())
                    return Quaternion.LerpUnclamped(transform.rotation, Quaternion.LookRotation(camDirection), Time.deltaTime * Random.Range(0.1f, 5));

                return transform.rotation;
            }
            else
            {
                return Quaternion.Slerp(transform.rotation, currentRotation.Value, (weaponHandler.IsAiming() ? GetTickRateDeltaTime() : Time.deltaTime) * 15);
            }
        }

        private void UpdateAnimatorParameters()
        {
            Vector2 walkCycleAnims = GetWalkCycleAnimationParameters();
            attributes.AnimationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(attributes.AnimationHandler.Animator.GetFloat("MoveForward"), walkCycleAnims.y, Time.deltaTime * runAnimationTransitionSpeed));
            attributes.AnimationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(attributes.AnimationHandler.Animator.GetFloat("MoveSides"), walkCycleAnims.x, Time.deltaTime * runAnimationTransitionSpeed));
            attributes.AnimationHandler.Animator.SetBool("IsGrounded", IsGrounded());
            attributes.AnimationHandler.Animator.SetFloat("VerticalSpeed", Rigidbody.velocity.y);
        }

        private Vector2 GetWalkCycleAnimationParameters()
        {
            if (attributes.AnimationHandler.ShouldApplyRootMotion())
            {
                return Vector2.zero;
            }
            else if (!CanMove() | attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                return Vector2.zero;
            }
            else
            {
                Vector2 moveInput = Vector3.Distance(Destination, GetPosition()) < 0.5f ? Vector2.zero : GetPathMoveInput();
                Vector2 animDir = new Vector2(moveInput.x, moveInput.y) * (attributes.StatusAgent.IsFeared() ? -1 : 1);
                animDir = Vector2.ClampMagnitude(animDir, 1);

                if (attributes.WeaponHandler.IsBlocking)
                {
                    switch (attributes.WeaponHandler.GetWeapon().GetBlockingLocomotion())
                    {
                        case Weapon.BlockingLocomotion.NoMovement:
                            animDir = Vector2.zero;
                            break;
                        case Weapon.BlockingLocomotion.CanWalk:
                            animDir /= 2;
                            break;
                        case Weapon.BlockingLocomotion.CanRun:
                            break;
                        default:
                            Debug.LogError("Unsure how to handle blocking locomotion type: " + attributes.WeaponHandler.GetWeapon().GetBlockingLocomotion());
                            break;
                    }
                }
                return animDir;
            }
        }

        private void UpdateAnimatorSpeed()
        {
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
        }

        private bool disableBots;
        private bool canOnlyLightAttack;
        private void RefreshStatus()
        {
            disableBots = FasterPlayerPrefs.Singleton.GetBool("DisableBots");
            canOnlyLightAttack = FasterPlayerPrefs.Singleton.GetBool("BotsCanOnlyLightAttack");
        }

        private void LateUpdate()
        {
            if (attributes.ShouldShake()) { transform.position += Random.insideUnitSphere * (Time.deltaTime * CombatAgent.ShakeAmount); }
        }

        private void EvaluateBotLogic()
        {
            if (IsServer)
            {
                activePlayers.Sort((x, y) => Vector3.Distance(x.transform.position, Rigidbody.position).CompareTo(Vector3.Distance(y.transform.position, Rigidbody.position)));

                targetAttributes = null;
                foreach (CombatAgent player in activePlayers)
                {
                    if (player.GetAilment() == ActionClip.Ailment.Death) { continue; }
                    if (!PlayerDataManager.Singleton.CanHit(attributes, player)) { continue; }
                    if (SetDestination(player.transform.position, true)) { targetAttributes = player; }
                    break;
                }

                if (disableBots)
                {
                    SetDestination(Rigidbody.position, true);
                }
                else
                {
                    if (!targetAttributes)
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

        List<Collider> groundColliders = new List<Collider>();
        ContactPoint[] stayContacts = new ContactPoint[3];
        public override void ReceiveOnCollisionStayMessage(Collision collision)
        {
            int contactCount = collision.GetContacts(stayContacts);
            for (int i = 0; i < contactCount; i++)
            {
                if (stayContacts[i].normal.y >= 0.9f)
                {
                    if (!groundColliders.Contains(collision.collider)) { groundColliders.Add(collision.collider); }
                    break;
                }
                else // Normal is not pointing up
                {
                    if (groundColliders.Contains(collision.collider)) { groundColliders.Remove(collision.collider); }
                }
            }
        }

        public override void ReceiveOnCollisionExitMessage(Collision collision)
        {
            if (groundColliders.Contains(collision.collider))
            {
                groundColliders.Remove(collision.collider);
            }
        }

        private const float isGroundedSphereCheckRadius = 0.6f;
        private bool IsGrounded()
        {
            if (groundColliders.Count > 0)
            {
                return true;
            }
            else
            {
                return Physics.CheckSphere(Rigidbody.position, isGroundedSphereCheckRadius, LayerMask.GetMask(layersToAccountForInMovement), QueryTriggerInteraction.Ignore);
            }
        }

        public override void OnNetworkSpawn()
        {
            Rigidbody.isKinematic = !IsServer & !IsOwner;
        }

        private NetworkVariable<Vector3> currentPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> currentRotation = new NetworkVariable<Quaternion>();

        void FixedUpdate()
        {
            if (IsServer)
            {
                Move();
                currentPosition.Value = Rigidbody.position;
            }
            else
            {
                Rigidbody.MovePosition(currentPosition.Value);
            }
        }

        private void Move()
        {
            Vector3 rootMotion = attributes.AnimationHandler.ApplyRootMotion();

            if (!IsSpawned) { return; }

            CalculatePath(Rigidbody.position, NavMesh.AllAreas);

            if (!CanMove() | attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                Rigidbody.velocity = Vector3.zero;
                return;
            }

            if (IsAffectedByExternalForce & !attributes.IsGrabbed() & !attributes.IsGrabbing()) { Rigidbody.isKinematic = false; return; }

            Vector2 moveInput = GetPathMoveInput();
            Quaternion newRotation = transform.rotation;

            // Apply movement
            Vector3 movement = Vector3.zero;
            if (attributes.IsGrabbing())
            {
                Rigidbody.isKinematic = true;
                return;
            }
            else if (attributes.IsGrabbed() & attributes.GetAilment() == ActionClip.Ailment.None)
            {
                CombatAgent grabAssailant = attributes.GetGrabAssailant();
                if (grabAssailant)
                {
                    Rigidbody.isKinematic = true;
                    Rigidbody.MovePosition(grabAssailant.MovementHandler.GetPosition() + (grabAssailant.MovementHandler.GetRotation() * Vector3.forward));
                    return;
                }
            }
            else if (attributes.ShouldPlayHitStop())
            {
                movement = Vector3.zero;
            }
            else if (attributes.IsPulled())
            {
                CombatAgent pullAssailant = attributes.GetPullAssailant();
                if (pullAssailant)
                {
                    movement = pullAssailant.MovementHandler.GetPosition() - GetPosition();
                }
            }
            else if (attributes.AnimationHandler.ShouldApplyRootMotion())
            {
                if (attributes.StatusAgent.IsRooted() & attributes.GetAilment() != ActionClip.Ailment.Knockup & attributes.GetAilment() != ActionClip.Ailment.Knockdown)
                {
                    movement = Vector3.zero;
                }
                else
                {
                    movement = newRotation * rootMotion * GetRootMotionSpeed();
                }
            }
            else if (attributes.AnimationHandler.IsAtRest())
            {
                Vector3 targetDirection = newRotation * (new Vector3(moveInput.x, 0, moveInput.y) * (attributes.StatusAgent.IsFeared() ? -1 : 1));
                targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
                targetDirection *= GetRunSpeed();
                movement = attributes.StatusAgent.IsRooted() | attributes.AnimationHandler.IsReloading() ? Vector3.zero : targetDirection;
            }

            Rigidbody.isKinematic = false;

            if (attributes.AnimationHandler.IsFlinching()) { movement *= AnimationHandler.flinchingMovementSpeedMultiplier; }

            float stairMovement = 0;
            Vector3 startPos = Rigidbody.position;
            startPos.y += stairStepHeight;
            while (Physics.Raycast(startPos, movement.normalized, out RaycastHit stairHit, 1, LayerMask.GetMask(layersToAccountForInMovement), QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Angle(movement.normalized, stairHit.normal) < 140)
                {
                    break;
                }
#if UNITY_EDITOR
                Debug.DrawRay(startPos, movement.normalized, Color.cyan, GetTickRateDeltaTime());
#endif
                startPos.y += stairStepHeight;
                stairMovement += stairStepHeight;

                if (stairMovement > maxStairStepHeight)
                {
                    stairMovement = 0;
                    break;
                }
            }

            if (Physics.CapsuleCast(Rigidbody.position, Rigidbody.position + bodyHeightOffset, bodyRadius, movement.normalized, out RaycastHit playerHit, movement.magnitude * Time.fixedDeltaTime, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore))
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
                    Quaternion targetRot = Quaternion.LookRotation(playerHit.transform.root.position - Rigidbody.position, Vector3.up);
                    float angle = targetRot.eulerAngles.y - Quaternion.LookRotation(movement, Vector3.up).eulerAngles.y;

                    if (angle > 180) { angle -= 360; }

                    if (angle > -20 & angle < 20)
                    {
                        movement = Vector3.zero;
                    }
                }
            }

            bool evaluateForce = true;
            if (weaponHandler.CurrentActionClip.shouldIgnoreGravity)
            {
                if (attributes.AnimationHandler.IsActionClipPlaying(weaponHandler.CurrentActionClip))
                {
                    Rigidbody.AddForce(movement - Rigidbody.velocity, ForceMode.VelocityChange);
                    evaluateForce = false;
                }
            }

            if (evaluateForce)
            {
                if (IsGrounded())
                {
                    Rigidbody.AddForce(new Vector3(movement.x, 0, movement.z) - new Vector3(Rigidbody.velocity.x, 0, Rigidbody.velocity.z), ForceMode.VelocityChange);
                    if (Rigidbody.velocity.y > 0 & Mathf.Approximately(stairMovement, 0)) // This is to prevent slope bounce
                    {
                        Rigidbody.AddForce(new Vector3(0, -Rigidbody.velocity.y, 0), ForceMode.VelocityChange);
                    }
                }
                else // Decelerate horizontal movement while aiRigidbodyorne
                {
                    Vector3 counterForce = Vector3.Slerp(Vector3.zero, new Vector3(-Rigidbody.velocity.x, 0, -Rigidbody.velocity.z), aiRigidbodyorneHorizontalDragMultiplier);
                    Rigidbody.AddForce(counterForce, ForceMode.VelocityChange);
                }
            }
            Rigidbody.AddForce(new Vector3(0, stairMovement, 0), ForceMode.VelocityChange);
            Rigidbody.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
        }

        private const float stairStepHeight = 0.01f;
        private const float maxStairStepHeight = 0.5f;

        private const float aiRigidbodyorneHorizontalDragMultiplier = 0.1f;

        private const float gravityScale = 2;

        void OnDodge()
        {
            Vector2 moveInput = GetPathMoveInput();
            float angle = Vector3.SignedAngle(transform.rotation * new Vector3(moveInput.x, 0, moveInput.y) * (attributes.StatusAgent.IsFeared() ? -1 : 1), transform.forward, Vector3.up);
            attributes.AnimationHandler.PlayAction(weaponHandler.GetWeapon().GetDodgeClip(angle));
        }
    }
}