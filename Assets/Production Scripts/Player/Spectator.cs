using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Vi.Core;
using Vi.Utility;
using UnityEngine.Rendering.Universal;
using Vi.Core.MovementHandlers;
using Vi.Core.CombatAgents;

namespace Vi.Player
{
    public class Spectator : MovementHandler
    {
        [SerializeField] private float moveSpeed = 7;

        private List<Attributes> playerList = new List<Attributes>();
        public void SetPlayerList(List<Attributes> playerList)
        {
            this.playerList = playerList;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsLocalPlayer)
            {
                gameObject.tag = "MainCamera";

                RefreshStatus();

                GetComponent<PlayerInput>().enabled = true;
                GetComponent<Camera>().enabled = true;
                GetComponent<AudioListener>().enabled = true;
                GetComponent<ActionMapHandler>().enabled = true;
            }
            else
            {
                GetComponent<PlayerInput>().enabled = false;
                GetComponent<Camera>().enabled = false;
                GetComponent<AudioListener>().enabled = false;
                GetComponent<ActionMapHandler>().enabled = false;
            }
            PlayerDataManager.Singleton.AddSpectatorInstance(OwnerClientId, NetworkObject);

            if (IsServer)
            {
                StartCoroutine(ShowToOwnerPlayerAfterSpawn());
            }
        }

        private IEnumerator ShowToOwnerPlayerAfterSpawn()
        {
            yield return new WaitUntil(() => IsSpawned);
            NetworkObject.NetworkShow(OwnerClientId);
        }

        public override void OnNetworkDespawn()
        {
            PlayerDataManager.Singleton.RemoveSpectatorInstance(OwnerClientId);

            if (IsLocalPlayer)
            {
                Cursor.lockState = CursorLockMode.None;
            }

            gameObject.tag = "Untagged";
        }

        void OnLook(InputValue value)
        {
            lookInput = value.Get<Vector2>();
        }

        private bool isSprinting;
        void OnSprint(InputValue value)
        {
            isSprinting = value.isPressed;
        }

        private bool isAscending;
        void OnAscend(InputValue value)
        {
            if (!CanLook()) { return; }

            followTarget = null;
            isAscending = value.isPressed;
        }

        private bool isDescending;
        void OnDescend(InputValue value)
        {
            if (!CanLook()) { return; }

            isDescending = value.isPressed;
        }

        private bool shouldViewEnvironment;
        private Vector3 environmentViewPosition;
        private Quaternion environmentViewRotation;
        private bool shouldViewEnvironmentModifier;
        void OnEnvironmentViewModifier(InputValue value)
        {
            shouldViewEnvironmentModifier = value.isPressed;
        }

        void OnFollowPlayer1()
        {
            if (!CanLook()) { return; }

            int index = 0;
            if (shouldViewEnvironmentModifier)
            {
                if (PlayerDataManager.Singleton.GetEnvironmentViewPoints().Length > index)
                {
                    shouldViewEnvironment = true;
                    environmentViewPosition = PlayerDataManager.Singleton.GetEnvironmentViewPoints()[index].position;
                    environmentViewRotation = PlayerDataManager.Singleton.GetEnvironmentViewPoints()[index].rotation;
                }
            }
            if (index >= playerList.Count) { return; }
            followTarget = playerList[index];
        }

        void OnFollowPlayer2()
        {
            if (!CanLook()) { return; }

            int index = 1;
            if (shouldViewEnvironmentModifier)
            {
                if (PlayerDataManager.Singleton.GetEnvironmentViewPoints().Length > index)
                {
                    shouldViewEnvironment = true;
                    environmentViewPosition = PlayerDataManager.Singleton.GetEnvironmentViewPoints()[index].position;
                    environmentViewRotation = PlayerDataManager.Singleton.GetEnvironmentViewPoints()[index].rotation;
                }
            }
            if (index >= playerList.Count) { return; }
            followTarget = playerList[index];
        }

        void OnFollowPlayer3()
        {
            if (!CanLook()) { return; }

            int index = 2;
            if (shouldViewEnvironmentModifier)
            {
                if (PlayerDataManager.Singleton.GetEnvironmentViewPoints().Length > index)
                {
                    shouldViewEnvironment = true;
                    environmentViewPosition = PlayerDataManager.Singleton.GetEnvironmentViewPoints()[index].position;
                    environmentViewRotation = PlayerDataManager.Singleton.GetEnvironmentViewPoints()[index].rotation;
                }
            }
            if (index >= playerList.Count) { return; }
            followTarget = playerList[index];
        }

        void OnFollowPlayer4()
        {
            if (!CanLook()) { return; }

            int index = 3;
            if (shouldViewEnvironmentModifier)
            {
                if (PlayerDataManager.Singleton.GetEnvironmentViewPoints().Length > index)
                {
                    shouldViewEnvironment = true;
                    environmentViewPosition = PlayerDataManager.Singleton.GetEnvironmentViewPoints()[index].position;
                    environmentViewRotation = PlayerDataManager.Singleton.GetEnvironmentViewPoints()[index].rotation;
                }
            }
            if (index >= playerList.Count) { return; }
            followTarget = playerList[index];
        }

        void OnFollowPlayer5()
        {
            if (!CanLook()) { return; }

            int index = 4;
            if (shouldViewEnvironmentModifier)
            {
                if (PlayerDataManager.Singleton.GetEnvironmentViewPoints().Length > index)
                {
                    shouldViewEnvironment = true;
                    environmentViewPosition = PlayerDataManager.Singleton.GetEnvironmentViewPoints()[index].position;
                    environmentViewRotation = PlayerDataManager.Singleton.GetEnvironmentViewPoints()[index].rotation;
                }
            }
            if (index >= playerList.Count) { return; }
            followTarget = playerList[index];
        }

        void OnFollowPlayer6()
        {
            if (!CanLook()) { return; }

            int index = 5;
            if (shouldViewEnvironmentModifier)
            {
                if (PlayerDataManager.Singleton.GetEnvironmentViewPoints().Length > index)
                {
                    shouldViewEnvironment = true;
                    environmentViewPosition = PlayerDataManager.Singleton.GetEnvironmentViewPoints()[index].position;
                    environmentViewRotation = PlayerDataManager.Singleton.GetEnvironmentViewPoints()[index].rotation;
                }
            }
            if (index >= playerList.Count) { return; }
            followTarget = playerList[index];
        }

        void OnFollowPlayer7()
        {
            if (!CanLook()) { return; }

            int index = 6;
            if (shouldViewEnvironmentModifier)
            {
                if (PlayerDataManager.Singleton.GetEnvironmentViewPoints().Length > index)
                {
                    shouldViewEnvironment = true;
                    environmentViewPosition = PlayerDataManager.Singleton.GetEnvironmentViewPoints()[index].position;
                    environmentViewRotation = PlayerDataManager.Singleton.GetEnvironmentViewPoints()[index].rotation;
                }
            }
            if (index >= playerList.Count) { return; }
            followTarget = playerList[index];
        }

        void OnFollowPlayer8()
        {
            if (!CanLook()) { return; }

            int index = 7;
            if (shouldViewEnvironmentModifier)
            {
                if (PlayerDataManager.Singleton.GetEnvironmentViewPoints().Length > index)
                {
                    shouldViewEnvironment = true;
                    environmentViewPosition = PlayerDataManager.Singleton.GetEnvironmentViewPoints()[index].position;
                    environmentViewRotation = PlayerDataManager.Singleton.GetEnvironmentViewPoints()[index].rotation;
                }
            }
            if (index >= playerList.Count) { return; }
            followTarget = playerList[index];
        }

        void OnFollowPlayer9()
        {
            if (!CanLook()) { return; }

            int index = 8;
            if (shouldViewEnvironmentModifier)
            {
                if (PlayerDataManager.Singleton.GetEnvironmentViewPoints().Length > index)
                {
                    shouldViewEnvironment = true;
                    environmentViewPosition = PlayerDataManager.Singleton.GetEnvironmentViewPoints()[index].position;
                    environmentViewRotation = PlayerDataManager.Singleton.GetEnvironmentViewPoints()[index].rotation;
                }
            }
            if (index >= playerList.Count) { return; }
            followTarget = playerList[index];
        }

        void OnFollowPlayer10()
        {
            if (!CanLook()) { return; }

            int index = 9;
            if (shouldViewEnvironmentModifier)
            {
                if (PlayerDataManager.Singleton.GetEnvironmentViewPoints().Length > index)
                {
                    shouldViewEnvironment = true;
                    environmentViewPosition = PlayerDataManager.Singleton.GetEnvironmentViewPoints()[index].position;
                    environmentViewRotation = PlayerDataManager.Singleton.GetEnvironmentViewPoints()[index].rotation;
                }
            }
            if (index >= playerList.Count) { return; }
            followTarget = playerList[index];
        }

        void OnIncrementFollowPlayer()
        {
            if (!CanLook()) { return; }

            if (followTarget == null)
            {
                if (playerList.Count > 0) { followTarget = playerList[0]; }
            }
            else
            {
                int index = playerList.IndexOf(followTarget);
                index += 1;
                if (index >= 0 & index < playerList.Count)
                {
                    followTarget = playerList[index];
                }
                else if (playerList.Count > 0)
                {
                    followTarget = playerList[0];
                }
            }
        }

        void OnDecrementFollowPlayer()
        {
            if (!CanLook()) { return; }

            if (followTarget == null)
            {
                if (playerList.Count > 0) { followTarget = playerList[^1]; }
            }
            else
            {
                int index = playerList.IndexOf(followTarget);
                index -= 1;
                if (index >= 0 & index < playerList.Count)
                {
                    followTarget = playerList[index];
                }
                else if (playerList.Count > 0)
                {
                    followTarget = playerList[^1];
                }
            }
        }


        private Unity.Netcode.Transports.UTP.UnityTransport networkTransport;
        private UniversalAdditionalCameraData cameraData;
        public Camera cam;
        protected override void Awake()
        {
            base.Awake();
            networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            cam = GetComponent<Camera>();
            cameraData = GetComponent<UniversalAdditionalCameraData>();
            RefreshStatus();
        }

        private Vector3 targetPosition;
        private void Start()
        {
            targetPosition = transform.position;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            followTargetOffset = defaultFollowTargetOffset;
        }

        private Attributes followTarget;
        private float followCamAngleOffset;
        private UIDeadZoneElement[] joysticks = new UIDeadZoneElement[0];

        private const float lerpSpeed = 8;
        private static readonly Vector3 defaultFollowTargetOffset = new Vector3(0, 3, -3);
        private static readonly Vector3 followTargetLookAtPositionOffset = new Vector3(0, 0.75f, 0);

        private Vector3 followTargetOffset;

        private const float zoomSensitivity = 0.2f;
        void OnZoom(InputValue value)
        {
            Vector2 scrollVal = value.Get<Vector2>();
            if (scrollVal.y > 0)
            {
                followTargetOffset.z += zoomSensitivity;
            }
            else if (scrollVal.y < 0)
            {
                followTargetOffset.z -= zoomSensitivity;
            }

            // Limit how much you can zoom in
            if (followTargetOffset.z > 1) { followTargetOffset.z = 1; }
        }

        protected override void RefreshStatus()
        {
            base.RefreshStatus();
            cameraData.renderPostProcessing = FasterPlayerPrefs.Singleton.GetBool("PostProcessingEnabled");
            cam.farClipPlane = FasterPlayerPrefs.Singleton.GetInt("RenderDistance");
            cam.fieldOfView = FasterPlayerPrefs.Singleton.GetFloat("FieldOfView");
        }

        private new void Update()
        {
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            base.Update();

            if (!IsLocalPlayer) { return; }

#if UNITY_IOS || UNITY_ANDROID
            // If on a mobile platform
            if (UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled)
            {
                Vector2 lookInputToAdd = Vector2.zero;
                if (playerInput.currentActionMap != null)
                {
                    if (playerInput.currentActionMap.name == playerInput.defaultActionMap)
                    {
                        foreach (UnityEngine.InputSystem.EnhancedTouch.Touch touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
                        {
                            if (joysticks.Length == 0) { joysticks = GetComponentsInChildren<UIDeadZoneElement>(); }

                            bool isTouchingJoystick = false;
                            foreach (UIDeadZoneElement joystick in joysticks)
                            {
                                if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)joystick.transform.parent, touch.startScreenPosition, cam))
                                {
                                    isTouchingJoystick = true;
                                    break;
                                }
                            }

                            if (!isTouchingJoystick & touch.startScreenPosition.x > Screen.width / 2f)
                            {
                                lookInputToAdd += touch.delta;
                            }
                        }
                    }
                }
                lookInput += lookInputToAdd;
            }
#endif

            if (moveInput != Vector2.zero)
            {
                shouldViewEnvironment = false;
                environmentViewPosition = Vector3.zero;
                environmentViewRotation = Quaternion.identity;
                followTarget = null;
                followCamAngleOffset = 0;
            }

            if (shouldViewEnvironment)
            {
                followTargetOffset = defaultFollowTargetOffset;

                transform.position = Vector3.Lerp(transform.position, environmentViewPosition, Time.deltaTime * lerpSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, environmentViewRotation, Time.deltaTime * lerpSpeed);

                targetPosition = transform.position;
            }
            else if (followTarget)
            {
                Vector3 targetPosition = followTarget.NetworkCollider.transform.position + followTarget.transform.rotation * Quaternion.Euler(0, followCamAngleOffset, 0) * followTargetOffset;
                Quaternion targetRotation = Quaternion.LookRotation(followTarget.NetworkCollider.transform.position + followTargetLookAtPositionOffset - transform.position);

                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * lerpSpeed);
                
                this.targetPosition = transform.position;

                followCamAngleOffset += GetLookInput().x;
            }
            else
            {
                followTargetOffset = defaultFollowTargetOffset;

                float verticalSpeed = 0;
                if (isAscending) { verticalSpeed = 1; }
                if (isDescending) { verticalSpeed = -1; }

                if (CanLook())
                {
                    targetPosition += (isSprinting ? moveSpeed * 2 : moveSpeed) * Time.deltaTime * (transform.rotation * new Vector3(moveInput.x, verticalSpeed, moveInput.y));
                    transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpSpeed);
                }
                
                Vector2 lookInput = GetLookInput();
                float xAngle = transform.eulerAngles.x - lookInput.y;
                if (xAngle > 85 & xAngle < 275)
                {
                    if (Mathf.Abs(xAngle - 85) > Mathf.Abs(xAngle - 275))
                    {
                        xAngle = 275;
                    }
                    else
                    {
                        xAngle = 85;
                    }
                }
                transform.eulerAngles = new Vector3(xAngle, transform.eulerAngles.y + lookInput.x, 0);
            }
            ResetLookInput();
        }
    }
}