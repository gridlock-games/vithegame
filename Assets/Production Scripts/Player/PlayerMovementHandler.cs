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
using Unity.Collections;

namespace Vi.Player
{
    public class PlayerMovementHandler : MovementHandler
    {
        [SerializeField] private CameraController cameraController;
        [SerializeField] private Transform rigidbodyRotationClone;

        public override void SetOrientation(Vector3 newPosition, Quaternion newRotation)
        {
            if (!IsServer) { Debug.LogError("PlayerMovementHandler.SetOrientation() should only be called on the server!"); return; }

            rb.position = newPosition;
            rb.velocity = Vector3.zero;

            SetRotationClientRpc(newRotation);
        }

        [Rpc(SendTo.Owner)] private void SetRotationClientRpc(Quaternion newRotation) { SetCameraRotation(newRotation.eulerAngles.x, newRotation.eulerAngles.y); }

        public override Vector3 GetPosition() { return rb.position; }

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

        public struct InputPayload : INetworkSerializable, System.IEquatable<InputPayload>
        {
            public int tick;
            public Vector2 moveInput;
            public Quaternion rotation;
            public FixedString32Bytes tryingToPlayActionClipName;

            public InputPayload(int tick, Vector2 moveInput, Quaternion rotation, ActionClip tryingToPlayActionClip)
            {
                this.tick = tick;
                this.moveInput = moveInput;
                this.rotation = rotation;
                tryingToPlayActionClipName = tryingToPlayActionClip ? tryingToPlayActionClip.name : "";
            }

            public bool Equals(InputPayload other)
            {
                return tick == other.tick & moveInput == other.moveInput & rotation == other.rotation;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref moveInput);
                serializer.SerializeValue(ref rotation);
                serializer.SerializeValue(ref tryingToPlayActionClipName);
            }
        }

        public struct StatePayload : INetworkSerializable
        {
            public int tick;
            public Vector2 moveInput;
            public Vector3 position;
            public Vector3 velocity;
            public Quaternion rotation;

            public StatePayload(InputPayload inputPayload, Rigidbody rb, Quaternion rotation)
            {
                tick = inputPayload.tick;
                moveInput = inputPayload.moveInput;
                position = rb.position;
                velocity = rb.velocity;
                this.rotation = rotation;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tick);
                serializer.SerializeValue(ref moveInput);
                serializer.SerializeValue(ref position);
                serializer.SerializeValue(ref rotation);
                serializer.SerializeValue(ref velocity);
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
                return Physics.CheckSphere(rb.position, isGroundedSphereCheckRadius, LayerMask.GetMask(layersToAccountForInMovement), QueryTriggerInteraction.Ignore);
            }
        }

        private const float serverReconciliationThreshold = 0.0001f;
        private void HandleServerReconciliation()
        {
            lastProcessedState = latestServerState.Value;

            int serverStateBufferIndex = latestServerState.Value.tick % BUFFER_SIZE;
            float positionError = Vector3.Distance(latestServerState.Value.position, stateBuffer[serverStateBufferIndex].position);

            if (positionError > serverReconciliationThreshold)
            {
                Debug.Log(OwnerClientId + " Position Error: " + positionError);

                // Update buffer at index of latest server state
                stateBuffer[serverStateBufferIndex] = latestServerState.Value;

                // Now re-simulate the rest of the ticks up to the current tick on the client
                Physics.autoSimulation = false;
                rb.position = latestServerState.Value.position;
                rb.velocity = latestServerState.Value.velocity;
                Physics.Simulate(Time.fixedDeltaTime);

                int tickToProcess = latestServerState.Value.tick + 1;
                while (tickToProcess < physicsTick)
                {
                    int bufferIndex = tickToProcess % BUFFER_SIZE;

                    // Process new movement with reconciled state
                    StatePayload statePayload = Move(inputBuffer[bufferIndex]);
                    Physics.Simulate(Time.fixedDeltaTime);

                    // Update buffer with recalculated state
                    stateBuffer[bufferIndex] = statePayload;

                    tickToProcess++;
                }
                Physics.autoSimulation = true;
            }
        }

        private int physicsTick;
        private void FixedUpdate()
        {
            if (!IsSpawned) { return; }

            if (!IsOwner & !IsServer)
            {
                // Sync position here with latest server state
                rb.MovePosition(latestServerState.Value.position);
            }

            if (IsServer)
            {
                if (serverInputQueue.TryDequeue(out InputPayload inputPayload))
                {
                    StatePayload statePayload = Move(inputPayload);
                    stateBuffer[statePayload.tick % BUFFER_SIZE] = statePayload;
                    latestServerState.Value = statePayload;
                }
            }

            if (IsOwner)
            {
                if (!latestServerState.Equals(default(StatePayload)) &&
                   (lastProcessedState.Equals(default(StatePayload)) ||
                   !latestServerState.Equals(lastProcessedState)))
                {
                    HandleServerReconciliation();
                }

                InputPayload inputPayload = new InputPayload(physicsTick, GetMoveInput(), EvaluateRotation(), attributes.AnimationHandler.GetFirstActionClipInQueue());
                inputBuffer[inputPayload.tick % BUFFER_SIZE] = inputPayload;
                physicsTick++;

                // This would mean we are the host. The server handles inputs from the server input queue
                if (!IsServer)
                {
                    StatePayload statePayload = Move(inputPayload);
                    stateBuffer[inputPayload.tick % BUFFER_SIZE] = statePayload;
                }
            }
        }

        private StatePayload Move(InputPayload inputPayload)
        {
            if (!CanMove() | attributes.GetAilment() == ActionClip.Ailment.Death)
            {
                rb.velocity = Vector3.zero;
                return new StatePayload(inputPayload, rb, inputPayload.rotation);
            }

            Vector2 moveInput = inputPayload.moveInput;
            Quaternion newRotation = inputPayload.rotation;

            if (inputPayload.tryingToPlayActionClipName != "")
            {
                Debug.Log(inputPayload.tick + " " + inputPayload.tryingToPlayActionClipName);
                if (IsServer)
                {
                    attributes.AnimationHandler.PlayActionOnServer(inputPayload.tryingToPlayActionClipName.ToString(), false);
                }
                else
                {
                    attributes.AnimationHandler.PlayPredictedActionOnClient(inputPayload.tryingToPlayActionClipName.ToString());
                }
            }
            
            // Apply movement
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
                    movement = newRotation * attributes.AnimationHandler.ApplyRootMotion(Time.fixedDeltaTime) * GetRootMotionSpeed();
                }
            }
            else
            {
                Vector3 targetDirection = newRotation * (new Vector3(moveInput.x, 0, moveInput.y) * (attributes.StatusAgent.IsFeared() ? -1 : 1));
                targetDirection = Vector3.ClampMagnitude(Vector3.Scale(targetDirection, HORIZONTAL_PLANE), 1);
                targetDirection *= GetRunSpeed();
                movement = attributes.StatusAgent.IsRooted() | attributes.AnimationHandler.IsReloading() ? Vector3.zero : targetDirection;
            }

            if (attributes.AnimationHandler.IsFlinching()) { movement *= AnimationHandler.flinchingMovementSpeedMultiplier; }

            float stairMovement = 0;
            Vector3 startPos = rb.position;
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

            if (Physics.CapsuleCast(rb.position, rb.position + bodyHeightOffset, bodyRadius, movement.normalized, out RaycastHit playerHit, movement.magnitude * Time.fixedDeltaTime, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore))
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
                    Quaternion targetRot = Quaternion.LookRotation(playerHit.transform.root.position - rb.position, Vector3.up);
                    float angle = targetRot.eulerAngles.y - Quaternion.LookRotation(movement, Vector3.up).eulerAngles.y;

                    if (angle > 180) { angle -= 360; }

                    if (angle > -20 & angle < 20)
                    {
                        movement = Vector3.zero;
                    }
                }
            }

            if (!IsAffectedByExternalForce)
            {
                bool evaluateForce = true;
                if (weaponHandler.CurrentActionClip.shouldIgnoreGravity)
                {
                    if (attributes.AnimationHandler.IsActionClipPlaying(weaponHandler.CurrentActionClip))
                    {
                        rb.AddForce(movement - rb.velocity, ForceMode.VelocityChange);
                        evaluateForce = false;
                    }
                }

                if (evaluateForce)
                {
                    if (IsGrounded())
                    {
                        rb.AddForce(new Vector3(movement.x, 0, movement.z) - new Vector3(rb.velocity.x, 0, rb.velocity.z), ForceMode.VelocityChange);
                        if (rb.velocity.y > 0 & Mathf.Approximately(stairMovement, 0)) // This is to prevent slope bounce
                        {
                            rb.AddForce(new Vector3(0, -rb.velocity.y, 0), ForceMode.VelocityChange);
                        }
                    }
                    else // Decelerate horizontal movement while airborne
                    {
                        Vector3 counterForce = Vector3.Slerp(Vector3.zero, new Vector3(-rb.velocity.x, 0, -rb.velocity.z), airborneHorizontalDragMultiplier);
                        rb.AddForce(counterForce, ForceMode.VelocityChange);
                    }
                }
            }

            rb.AddForce(new Vector3(0, stairMovement, 0), ForceMode.VelocityChange);

            return new StatePayload(inputPayload, rb, newRotation);
        }

        private const float stairStepHeight = 0.01f;
        private const float maxStairStepHeight = 0.5f;

        private const float airborneHorizontalDragMultiplier = 0.1f;

        private Quaternion EvaluateRotation()
        {
            Quaternion rot = transform.rotation;
            if (cameraController)
            {
                Vector3 camDirection = cameraController.GetCamDirection();
                camDirection.Scale(HORIZONTAL_PLANE);

                if (attributes.ShouldApplyAilmentRotation())
                    rot = attributes.GetAilmentRotation();
                else if (attributes.AnimationHandler.IsGrabAttacking())
                {
                    CombatAgent grabVictim = attributes.GetGrabVictim();
                    if (grabVictim)
                    {
                        Vector3 rel = grabVictim.MovementHandler.GetPosition() - GetPosition();
                        rel = Vector3.Scale(rel, HORIZONTAL_PLANE);
                        rot = Quaternion.LookRotation(rel, Vector3.up);
                    }
                    else
                    {
                        rot = Quaternion.LookRotation(camDirection);
                    }
                }
                else if (!attributes.ShouldPlayHitStop())
                    rot = Quaternion.LookRotation(camDirection);
            }
            else
            {
                rot = Quaternion.Slerp(transform.rotation, latestServerState.Value.rotation, (weaponHandler.IsAiming() ? GetTickRateDeltaTime() : Time.deltaTime) * CameraController.orbitSpeed);
            }
            return rot;
        }

        private void LateUpdate()
        {
            transform.position = rb.transform.position;

            if (attributes.ShouldShake()) { transform.position += Random.insideUnitSphere * (Time.deltaTime * CombatAgent.ShakeAmount); }

            transform.rotation = EvaluateRotation();

            rigidbodyRotationClone.rotation = transform.rotation;
        }

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
            }
            else
            {
                Destroy(cameraController.gameObject);
                Destroy(playerInput);
            }
            rb.useGravity = true;
            rb.isKinematic = !IsServer & !IsOwner;
            rb.collisionDetectionMode = IsServer ? CollisionDetectionMode.Continuous : CollisionDetectionMode.Discrete;

            if (IsServer)
            {
                inputBuffer.OnListChanged += OnInputBufferChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsLocalPlayer)
            {
                UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
                Cursor.lockState = CursorLockMode.None;
            }

            if (IsServer)
            {
                inputBuffer.OnListChanged -= OnInputBufferChanged;
            }
        }

        private void OnInputBufferChanged(NetworkListEvent<InputPayload> networkListEvent)
        {
            if (networkListEvent.Type == NetworkListEvent<InputPayload>.EventType.Value)
            {
                serverInputQueue.Enqueue(networkListEvent.Value);
            }
            else
            {
                Debug.Log("We shouldn't be receiving an event for a network list event type of: " + networkListEvent.Type);
            }
        }

        private new void OnDestroy()
        {
            base.OnDestroy();
            if (cameraController) { Destroy(cameraController.gameObject); }
            if (rb) { Destroy(rb.gameObject); }
        }

        private const int BUFFER_SIZE = 1024;

        private StatePayload[] stateBuffer;
        private NetworkList<InputPayload> inputBuffer;
        private NetworkVariable<StatePayload> latestServerState = new NetworkVariable<StatePayload>();
        private StatePayload lastProcessedState;
        private Queue<InputPayload> serverInputQueue;

        private Attributes attributes;
        private new void Awake()
        {
            base.Awake();
            attributes = GetComponent<Attributes>();
            rb.isKinematic = true;
            RefreshStatus();

            stateBuffer = new StatePayload[BUFFER_SIZE];
            inputBuffer = new NetworkList<InputPayload>(new InputPayload[BUFFER_SIZE], NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Owner);
            serverInputQueue = new Queue<InputPayload>();
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

        private const float runAnimationTransitionSpeed = 5;
        private UIDeadZoneElement[] joysticks = new UIDeadZoneElement[0];
        RaycastHit[] interactableHits = new RaycastHit[10];
        private new void Update()
        {
            base.Update();
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            FindMainCamera();

            if (!IsSpawned) { return; }

            if (weaponHandler.CurrentActionClip.GetClipType() == ActionClip.ClipType.GrabAttack)
            {
                SetImmovable(attributes.AnimationHandler.IsGrabAttacking());
            }
            else
            {
                SetImmovable(attributes.IsGrabbed());
            }

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
            UpdateAnimatorParameters();
            UpdateAnimatorSpeed();
            AutoAim();
            if (attributes.GetAilment() != ActionClip.Ailment.Death) { CameraFollowTarget = null; }
        }

        private void RefreshStatus()
        {
            autoAim = FasterPlayerPrefs.Singleton.GetBool("AutoAim");
        }

        private float GetAnimatorSpeed()
        {
            return (Mathf.Max(0, weaponHandler.GetWeapon().GetRunSpeed() - attributes.StatusAgent.GetMovementSpeedDecreaseAmount()) + attributes.StatusAgent.GetMovementSpeedIncreaseAmount()) / weaponHandler.GetWeapon().GetRunSpeed() * (attributes.AnimationHandler.IsAtRest() ? 1 : (weaponHandler.IsInRecovery ? weaponHandler.CurrentActionClip.recoveryAnimationSpeed : weaponHandler.CurrentActionClip.animationSpeed));
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
                Vector2 moveInput = IsOwner ? GetMoveInput() : latestServerState.Value.moveInput;
                Vector2 animDir = (new Vector2(moveInput.x, moveInput.y) * (attributes.StatusAgent.IsFeared() ? -1 : 1));
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

        private void UpdateAnimatorParameters()
        {
            Vector2 walkCycleAnims = GetWalkCycleAnimationParameters();
            attributes.AnimationHandler.Animator.SetFloat("MoveForward", Mathf.MoveTowards(attributes.AnimationHandler.Animator.GetFloat("MoveForward"), walkCycleAnims.y, Time.deltaTime * runAnimationTransitionSpeed));
            attributes.AnimationHandler.Animator.SetFloat("MoveSides", Mathf.MoveTowards(attributes.AnimationHandler.Animator.GetFloat("MoveSides"), walkCycleAnims.x, Time.deltaTime * runAnimationTransitionSpeed));
            attributes.AnimationHandler.Animator.SetBool("IsGrounded", IsGrounded());
            attributes.AnimationHandler.Animator.SetFloat("VerticalSpeed", rb.velocity.y);
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

        private bool autoAim;
        RaycastHit[] cameraHits = new RaycastHit[10];
        private void AutoAim()
        {
            if (!autoAim) { return; }
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

            //Gizmos.color = Color.yellow;
            //Gizmos.DrawSphere(rb.position, 0.3f);

            //Gizmos.color = Color.blue;
            //Gizmos.DrawWireSphere(rb.position, isGroundedSphereCheckRadius);
        }
    }
}

