using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Vi.Core;
using Vi.ScriptableObjects;
using Vi.Utility;
using Vi.Core.CombatAgents;
using Vi.ProceduralAnimations;

namespace Vi.Player
{
    public class PlayerMovementHandler : MovementHandler
    {
        [SerializeField] private CameraController cameraController;

        [Header("Locomotion Settings")]
        [SerializeField] private float angularSpeed = 540;
        [Header("Animation Settings")]
        [SerializeField] private float runAnimationTransitionSpeed = 5;

        public override void SetOrientation(Vector3 newPosition, Quaternion newRotation)
        {
            movementPrediction.SetOrientation(newPosition, newRotation);
        }

        public override Vector3 GetPosition() { return movementPrediction.CurrentPosition; }

        public override void SetImmovable(bool isImmovable)
        {
            //rb.constraints = isImmovable ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.FreezeRotation;
        }

        public bool IsCameraAnimating() { return cameraController.IsAnimating; }

        public Transform TargetToLockOn { get; private set; }
        public void LockOnTarget(Transform target)
        {
            TargetToLockOn = target;
        }

        public void SetCameraRotation(float rotationX, float rotationY)
        {
            cameraController.SetRotation(rotationX, rotationY);
        }

        public override void Flinch(Vector2 flinchAmount)
        {
            cameraController.AddRotation(flinchAmount.x, flinchAmount.y);
        }

        private float GetTickRateDeltaTime()
        {
            return NetworkManager.NetworkTickSystem.LocalTime.FixedDeltaTime;
        }

        private float GetRootMotionSpeed()
        {
            return Mathf.Clamp01(weaponHandler.GetWeapon().GetMovementSpeed(weaponHandler.IsBlocking) - attributes.StatusAgent.GetMovementSpeedDecreaseAmount() + attributes.StatusAgent.GetMovementSpeedIncreaseAmount());
        }

        public float GetRunSpeed()
        {
            return Mathf.Max(0, weaponHandler.GetWeapon().GetMovementSpeed(weaponHandler.IsBlocking) - attributes.StatusAgent.GetMovementSpeedDecreaseAmount()) + attributes.StatusAgent.GetMovementSpeedIncreaseAmount();
        }

        [Header("Network Prediction")]
        [SerializeField] private Vector3 gravitySphereCastPositionOffset = new Vector3(0, 0.75f, 0);
        [SerializeField] private float gravitySphereCastRadius = 0.75f;
        RaycastHit[] rootMotionHits = new RaycastHit[10];
        public PlayerNetworkMovementPrediction.StatePayload ProcessMovement(PlayerNetworkMovementPrediction.InputPayload inputPayload)
        {
            if (!CanMove() | attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                velocity = Vector3.zero;
                return new PlayerNetworkMovementPrediction.StatePayload(inputPayload.tick, inputPayload, true, movementPrediction.CurrentPosition, Quaternion.identity);
            }

            Vector3 newPosition = rb.position;

            return new PlayerNetworkMovementPrediction.StatePayload(inputPayload.tick, inputPayload, false, newPosition, Quaternion.identity);
        }

        public override void ReceiveOnCollisionEnterMessage(Collision collision)
        {
            // Set falling velocity here
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

        private bool IsGrounded()
        {
            if (groundColliders.Count > 0)
            {
                return true;
            }
            else
            {
                return Physics.CheckSphere(rb.position, gravitySphereCastRadius, LayerMask.GetMask(layersToAccountForInMovement), QueryTriggerInteraction.Ignore);
            }
        }

        private void FixedUpdate()
        {
            PlayerNetworkMovementPrediction.InputPayload inputPayload = new PlayerNetworkMovementPrediction.InputPayload(0, GetMoveInput(), rb.rotation);

            Quaternion newRotation = movementPrediction.CurrentRotation;
            if (IsOwner)
            {
                Vector3 camDirection = cameraController.GetCamDirection();
                camDirection.Scale(HORIZONTAL_PLANE);

                if (attributes.ShouldApplyAilmentRotation())
                    newRotation = attributes.GetAilmentRotation();
                else if (attributes.AnimationHandler.IsGrabAttacking())
                    newRotation = inputPayload.rotation;
                else if (weaponHandler.IsAiming() & !attributes.ShouldPlayHitStop())
                    newRotation = Quaternion.LookRotation(camDirection);
                else if (!attributes.ShouldPlayHitStop())
                    newRotation = Quaternion.LookRotation(camDirection);
            }
            else
            {
                newRotation = inputPayload.rotation;
            }

            // Apply movement
            Vector3 rootMotion = newRotation * attributes.AnimationHandler.ApplyNetworkRootMotion() * GetRootMotionSpeed();
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
                else if (weaponHandler.CurrentActionClip.limitAttackMotionBasedOnTarget & (weaponHandler.IsInAnticipation | weaponHandler.IsAttacking) | attributes.AnimationHandler.IsLunging())
                {
                    movement = rootMotion;
#if UNITY_EDITOR
                    ExtDebug.DrawBoxCastBox(movementPrediction.CurrentPosition + ActionClip.boxCastOriginPositionOffset, ActionClip.boxCastHalfExtents, newRotation * Vector3.forward, newRotation, ActionClip.boxCastDistance, Color.blue, GetTickRateDeltaTime());
#endif
                    int rootMotionHitCount = Physics.BoxCastNonAlloc(movementPrediction.CurrentPosition + ActionClip.boxCastOriginPositionOffset,
                        ActionClip.boxCastHalfExtents, (newRotation * Vector3.forward).normalized, rootMotionHits,
                        newRotation, ActionClip.boxCastDistance, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);

                    List<(NetworkCollider, float, RaycastHit)> angleList = new List<(NetworkCollider, float, RaycastHit)>();

                    for (int i = 0; i < rootMotionHitCount; i++)
                    {
                        if (rootMotionHits[i].transform.root.TryGetComponent(out NetworkCollider networkCollider))
                        {
                            if (PlayerDataManager.Singleton.CanHit(attributes, networkCollider.CombatAgent) & !networkCollider.CombatAgent.IsInvincible())
                            {
                                Quaternion targetRot = Quaternion.LookRotation(networkCollider.transform.position - movementPrediction.CurrentPosition, Vector3.up);
                                angleList.Add((networkCollider,
                                    Mathf.Abs(targetRot.eulerAngles.y - newRotation.eulerAngles.y),
                                    rootMotionHits[i]));
                            }
                        }
                    }

                    angleList.Sort((x, y) => x.Item2.CompareTo(y.Item2));
                    foreach ((NetworkCollider networkCollider, float angle, RaycastHit hit) in angleList)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(networkCollider.transform.position - movementPrediction.CurrentPosition, Vector3.up);
                        if (angle < ActionClip.maximumRootMotionLimitRotationAngle)
                        {
                            movement = Vector3.ClampMagnitude(movement, hit.distance);
                            break;
                        }
                    }
                }
                else
                {
                    movement = rootMotion;
                }
            }
            else
            {
                Vector3 targetDirection = newRotation * (new Vector3(inputPayload.moveInput.x, 0, inputPayload.moveInput.y) * (attributes.StatusAgent.IsFeared() ? -1 : 1));
                targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
                targetDirection *= GetRunSpeed();
                movement = attributes.StatusAgent.IsRooted() | attributes.AnimationHandler.IsReloading() ? Vector3.zero : targetDirection;
            }

            if (attributes.AnimationHandler.IsFlinching()) { movement *= AnimationHandler.flinchingMovementSpeedMultiplier; }

            float stairMovement = 0;
            float yOffset = 0.2f;
            Vector3 startPos = movementPrediction.CurrentPosition;
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
                stairMovement = startPos.y - movementPrediction.CurrentPosition.y - yOffset;

                if (stairMovement > 0.5f)
                {
                    stairMovement = 0;
                    break;
                }
            }

            movement.y += stairMovement * 4;

            bool wasPlayerHit = Physics.CapsuleCast(movementPrediction.CurrentPosition, movementPrediction.CurrentPosition + bodyHeightOffset, bodyRadius, movement.normalized, out RaycastHit playerHit, movement.magnitude, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);
            //bool wasPlayerHit = Physics.Raycast(movementPrediction.CurrentPosition + bodyHeightOffset / 2, movement.normalized, out RaycastHit playerHit, movement.magnitude, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);
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
                    Quaternion targetRot = Quaternion.LookRotation(playerHit.transform.root.position - movementPrediction.CurrentPosition, Vector3.up);
                    float angle = targetRot.eulerAngles.y - Quaternion.LookRotation(movement, Vector3.up).eulerAngles.y;

                    if (angle > 180) { angle -= 360; }

                    if (angle > -20 & angle < 20)
                    {
                        movement = Vector3.zero;
                    }
                }
            }

            if (IsGrounded())
            {
                rb.AddForce(new Vector3(movement.x, 0, movement.z) - new Vector3(rb.velocity.x, 0, rb.velocity.z), ForceMode.VelocityChange);
            }
        }

        private void LateUpdate()
        {
            transform.position = rb.transform.position;

            if (cameraController)
            {
                Vector3 camDirection = cameraController.GetCamDirection();
                camDirection.Scale(HORIZONTAL_PLANE);

                if (attributes.ShouldApplyAilmentRotation())
                    transform.rotation = attributes.GetAilmentRotation();
                else if (attributes.AnimationHandler.IsGrabAttacking())
                    transform.rotation = movementPrediction.CurrentRotation;
                else if (weaponHandler.IsAiming() & !attributes.ShouldPlayHitStop())
                    transform.rotation = Quaternion.LookRotation(camDirection);
                else if (!attributes.ShouldPlayHitStop())
                    transform.rotation = Quaternion.LookRotation(camDirection);
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, movementPrediction.CurrentRotation, (weaponHandler.IsAiming() ? GetTickRateDeltaTime() : Time.deltaTime) * CameraController.orbitSpeed);
            }
        }

        private const float drag = 1;

        public override void OnNetworkSpawn()
        {
            if (IsLocalPlayer)
            {
                cameraController.gameObject.AddComponent<AudioListener>();
                cameraController.Camera.enabled = true;

                playerInput.enabled = true;
                string rebinds = FasterPlayerPrefs.Singleton.GetString("Rebinds");
                playerInput.actions.LoadBindingOverridesFromJson(rebinds);

                GetComponent<ActionMapHandler>().enabled = true;
                UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();

                rb.useGravity = true;
            }
            else
            {
                Destroy(cameraController.gameObject);
                Destroy(playerInput);
            }
            rb.collisionDetectionMode = IsServer ? CollisionDetectionMode.Continuous : CollisionDetectionMode.Discrete;
        }

        public override void OnNetworkDespawn()
        {
            if (IsLocalPlayer)
            {
                UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
                Cursor.lockState = CursorLockMode.None;
            }
        }

        private new void OnDestroy()
        {
            base.OnDestroy();
            if (cameraController) { Destroy(cameraController.gameObject); }
            if (rb) { Destroy(rb.gameObject); }
        }

        private PlayerNetworkMovementPrediction movementPrediction;
        private Attributes attributes;
        private new void Awake()
        {
            base.Awake();
            movementPrediction = GetComponent<PlayerNetworkMovementPrediction>();
            attributes = GetComponent<Attributes>();
            RefreshStatus();
        }

        private void Start()
        {
            rb.transform.SetParent(null, true);
            if (NetSceneManager.Singleton.IsSceneGroupLoaded("Tutorial Room"))
            {
                cameraController.PlayAnimation("TutorialIntro");
            }
        }

        private Camera mainCamera;
        private void FindMainCamera()
        {
            if (mainCamera) { return; }
            mainCamera = Camera.main;
        }

        private UIDeadZoneElement[] joysticks = new UIDeadZoneElement[0];
        RaycastHit[] interactableHits = new RaycastHit[10];
        private new void Update()
        {
            base.Update();
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            FindMainCamera();

            if (weaponHandler.CurrentActionClip.GetClipType() == ActionClip.ClipType.GrabAttack)
            {
                SetImmovable(attributes.AnimationHandler.IsGrabAttacking());
            }
            else
            {
                SetImmovable(attributes.IsGrabbed());
            }

            if (!IsSpawned) { return; }

            Vector2 walkCycleAnims = movementPrediction.GetWalkCycleAnimationParameters();
            attributes.AnimationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(attributes.AnimationHandler.Animator.GetFloat("MoveForward"), walkCycleAnims.y, Time.deltaTime * runAnimationTransitionSpeed));
            attributes.AnimationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(attributes.AnimationHandler.Animator.GetFloat("MoveSides"), walkCycleAnims.x, Time.deltaTime * runAnimationTransitionSpeed));
            attributes.AnimationHandler.Animator.SetBool("IsGrounded", IsGrounded());
            attributes.AnimationHandler.Animator.SetFloat("VerticalSpeed", rb.velocity.y);

#if UNITY_IOS || UNITY_ANDROID
            // If on a mobile platform
            if (IsLocalPlayer & UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled)
            {
                Vector2 lookInputToAdd = Vector2.zero;
                if (playerInput.currentActionMap.name == playerInput.defaultActionMap)
                {
                    foreach (UnityEngine.InputSystem.EnhancedTouch.Touch touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
                    {
                        if (joysticks.Length == 0) { joysticks = GetComponentsInChildren<UIDeadZoneElement>(); }

                        bool isTouchingJoystick = false;
                        foreach (UIDeadZoneElement joystick in joysticks)
                        {
                            if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)joystick.transform.parent, touch.startScreenPosition))
                            {
                                isTouchingJoystick = true;
                                break;
                            }
                        }

                        if (!isTouchingJoystick)
                        {
                            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                            {
                                int interactableHitCount = Physics.RaycastNonAlloc(mainCamera.ScreenPointToRay(touch.screenPosition),
                                    interactableHits, 10, LayerMask.GetMask(interactableRaycastLayers), QueryTriggerInteraction.Ignore);

                                float minDistance = 0;
                                bool minDistanceInitialized = false;
                                NetworkInteractable networkInteractable = null;
                                for (int i = 0; i < interactableHitCount; i++)
                                {
                                    if (interactableHits[i].distance > minDistance & minDistanceInitialized) { continue; }
                                    networkInteractable = interactableHits[i].transform.root.GetComponent<NetworkInteractable>();
                                    minDistance = interactableHits[i].distance;
                                    minDistanceInitialized = true;
                                }
                                if (networkInteractable) { networkInteractable.Interact(gameObject); }
                            }
                        }
                        
                        if (!isTouchingJoystick & touch.startScreenPosition.x > Screen.width / 2f)
                        {
                            lookInputToAdd += touch.delta;
                        }
                    }
                }
            lookInput += lookInputToAdd;
            }
#endif
            UpdateLocomotion();
            if (attributes.GetAilment() != ActionClip.Ailment.Death) { CameraFollowTarget = null; }
        }

        public override Vector3 GetVelocity() { return velocity; }

        Vector3 velocity;
        public override void AddForce(Vector3 force)
        {
            if (!attributes.IsGrabbed() & !attributes.AnimationHandler.IsGrabAttacking()) { velocity += force * Time.fixedDeltaTime; }
        }

        void OnAddForce()
        {
            if (Application.isEditor)
            {
                rb.AddForce((transform.forward + Vector3.up) * 50, ForceMode.VelocityChange);
            }
        }

        private void RefreshStatus()
        {
            autoAim = FasterPlayerPrefs.Singleton.GetBool("AutoAim");
        }

        private float GetAnimatorSpeed()
        {
            return (Mathf.Max(0, weaponHandler.GetWeapon().GetRunSpeed() - attributes.StatusAgent.GetMovementSpeedDecreaseAmount()) + attributes.StatusAgent.GetMovementSpeedIncreaseAmount()) / weaponHandler.GetWeapon().GetRunSpeed() * (attributes.AnimationHandler.IsAtRest() ? 1 : (weaponHandler.IsInRecovery ? weaponHandler.CurrentActionClip.recoveryAnimationSpeed : weaponHandler.CurrentActionClip.animationSpeed));
        }

        private bool autoAim;
        RaycastHit[] cameraHits = new RaycastHit[10];
        private void UpdateLocomotion()
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

            if (autoAim)
            {
                if (weaponHandler.CurrentActionClip.useRotationalTargetingSystem & cameraController & !weaponHandler.CurrentActionClip.mustBeAiming)
                {
                    if (weaponHandler.IsInAnticipation | weaponHandler.IsAttacking | attributes.AnimationHandler.IsLunging())
                    {
                        ExtDebug.DrawBoxCastBox(cameraController.CameraPositionClone.transform.position + ActionClip.boxCastOriginPositionOffset, ActionClip.boxCastHalfExtents, cameraController.CameraPositionClone.transform.forward, cameraController.CameraPositionClone.transform.rotation, ActionClip.boxCastDistance, Color.yellow, Time.deltaTime);
                        int cameraHitsCount = Physics.BoxCastNonAlloc(cameraController.CameraPositionClone.transform.position + ActionClip.boxCastOriginPositionOffset,
                            ActionClip.boxCastHalfExtents, cameraController.CameraPositionClone.transform.forward.normalized, cameraHits,
                            cameraController.CameraPositionClone.transform.rotation, ActionClip.boxCastDistance,
                            LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);
                        
                        List<(NetworkCollider, float, RaycastHit)> angleList = new List<(NetworkCollider, float, RaycastHit)>();
                        for (int i = 0; i < cameraHitsCount; i++)
                        {
                            if (cameraHits[i].transform.root.TryGetComponent(out NetworkCollider networkCollider))
                            {
                                if (PlayerDataManager.Singleton.CanHit(attributes, networkCollider.CombatAgent) & !networkCollider.CombatAgent.IsInvincible())
                                {
                                    Quaternion targetRot = Quaternion.LookRotation(networkCollider.transform.position + targetSystemOffset - cameraController.CameraPositionClone.transform.position, Vector3.up);
                                    angleList.Add((networkCollider,
                                        Mathf.Abs(targetRot.eulerAngles.y - cameraController.CameraPositionClone.transform.eulerAngles.y) + Mathf.Abs((targetRot.eulerAngles.x < 180 ? targetRot.eulerAngles.x : targetRot.eulerAngles.x - 360) - (cameraController.CameraPositionClone.transform.eulerAngles.x < 180 ? cameraController.CameraPositionClone.transform.eulerAngles.x : cameraController.CameraPositionClone.transform.eulerAngles.x - 360)),
                                        cameraHits[i]));
                                }
                            }
                        }
                        
                        angleList.Sort((x, y) => x.Item2.CompareTo(y.Item2));
                        foreach ((NetworkCollider networkCollider, float angle, RaycastHit hit) in angleList)
                        {
                            Quaternion targetRot = Quaternion.LookRotation(networkCollider.transform.position + targetSystemOffset - cameraController.CameraPositionClone.transform.position, Vector3.up);
                            if (angle < weaponHandler.CurrentActionClip.maximumTargetingRotationAngle)
                            {
                                cameraController.AddRotation(Mathf.Clamp(((targetRot.eulerAngles.x < 180 ? targetRot.eulerAngles.x : targetRot.eulerAngles.x - 360) - (cameraController.CameraPositionClone.transform.eulerAngles.x < 180 ? cameraController.CameraPositionClone.transform.eulerAngles.x : cameraController.CameraPositionClone.transform.eulerAngles.x - 360)) * Time.deltaTime * LimbReferences.rotationConstraintOffsetSpeed, -LimbReferences.rotationConstraintOffsetSpeed, LimbReferences.rotationConstraintOffsetSpeed),
                                    Mathf.Clamp((targetRot.eulerAngles.y - cameraController.CameraPositionClone.transform.eulerAngles.y) * Time.deltaTime * LimbReferences.rotationConstraintOffsetSpeed, -LimbReferences.rotationConstraintOffsetSpeed, LimbReferences.rotationConstraintOffsetSpeed));
                                break;
                            }
                        }
                    }
                }
            }
        }

        public static readonly Vector3 targetSystemOffset = new Vector3(0, 1, 0);

        void OnLook(InputValue value)
        {
            lookInput = value.Get<Vector2>() * (attributes.StatusAgent.IsFeared() ? -1 : 1);
        }

        public void OnDodge()
        {
            if (attributes.AnimationHandler.IsReloading()) { return; }
            float angle = Vector3.SignedAngle(transform.rotation * new Vector3(moveInput.x, 0, moveInput.y) * (attributes.StatusAgent.IsFeared() ? -1 : 1), transform.forward, Vector3.up);
            attributes.AnimationHandler.PlayAction(weaponHandler.GetWeapon().GetDodgeClip(angle));
        }

        private string[] interactableRaycastLayers = new string[]
        {
            "Default",
            "NetworkPrediction",
            "Projectile",
            "ProjectileCollider"
        };

        void OnInteract()
        {
            int interactableHitsCount = Physics.RaycastNonAlloc(mainCamera.transform.position, mainCamera.transform.forward.normalized,
                interactableHits, 15, LayerMask.GetMask(interactableRaycastLayers), QueryTriggerInteraction.Ignore);

            for (int i = 0; i < interactableHitsCount; i++)
            {
                if (interactableHits[i].transform.root.TryGetComponent(out NetworkInteractable networkInteractable))
                {
                    networkInteractable.Interact(gameObject);
                    break;
                }
            }
        }

        public CombatAgent CameraFollowTarget { get; private set; }
        public void OnIncrementFollowPlayer()
        {
            if (attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                List<CombatAgent> spectatableAttributesList = PlayerDataManager.Singleton.GetActiveCombatAgents(attributes).FindAll(item => (!PlayerDataManager.Singleton.CanHit(attributes, item) | item.GetTeam() == PlayerDataManager.Team.Competitor) & item.GetAilment() != ActionClip.Ailment.Death);
                if (CameraFollowTarget == null)
                {
                    if (spectatableAttributesList.Count > 0) { CameraFollowTarget = spectatableAttributesList[0]; }
                }
                else
                {
                    int index = spectatableAttributesList.IndexOf(CameraFollowTarget);
                    index += 1;
                    if (index >= 0 & index < spectatableAttributesList.Count)
                    {
                        CameraFollowTarget = spectatableAttributesList[index];
                    }
                    else if (spectatableAttributesList.Count > 0)
                    {
                        CameraFollowTarget = spectatableAttributesList[0];
                    }
                }
            }
        }

        public void OnDecrementFollowPlayer()
        {
            if (attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                List<CombatAgent> spectatableAttributesList = PlayerDataManager.Singleton.GetActiveCombatAgents(attributes).FindAll(item => (!PlayerDataManager.Singleton.CanHit(attributes, item) | item.GetTeam() == PlayerDataManager.Team.Competitor) & item.GetAilment() != ActionClip.Ailment.Death);
                if (CameraFollowTarget == null)
                {
                    if (spectatableAttributesList.Count > 0) { CameraFollowTarget = spectatableAttributesList[^1]; }
                }
                else
                {
                    int index = spectatableAttributesList.IndexOf(CameraFollowTarget);
                    index -= 1;
                    if (index >= 0 & index < spectatableAttributesList.Count)
                    {
                        CameraFollowTarget = spectatableAttributesList[index];
                    }
                    else if (spectatableAttributesList.Count > 0)
                    {
                        CameraFollowTarget = spectatableAttributesList[^1];
                    }
                }
            }
        }

        protected void OnDrawGizmos()
        {
            if (!Application.isPlaying) { return; }

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(movementPrediction.CurrentPosition, 0.3f);

            //Gizmos.color = Color.white;
            //Gizmos.DrawSphere(Vector3.MoveTowards(transform.position, movementPrediction.CurrentPosition, Time.deltaTime), 0.3f);

            //Gizmos.color = Color.green;
            //Gizmos.DrawSphere(movementPrediction.CurrentPosition + transform.rotation * gravitySphereCastPositionOffset, gravitySphereCastRadius);
        }
    }
}

