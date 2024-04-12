using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Vi.Core;
using UnityEngine.InputSystem.OnScreen;

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
            if (IsLocalPlayer)
            {
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
        }

        public override void OnNetworkDespawn()
        {
            PlayerDataManager.Singleton.RemoveSpectatorInstance(OwnerClientId);
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
            followTarget = null;
            isAscending = value.isPressed;
        }

        private bool isDescending;
        void OnDescend(InputValue value)
        {
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

        private Vector3 targetPosition;
        private void Start()
        {
            targetPosition = transform.position;
        }

        private void OnEnable()
        {
            if (IsLocalPlayer)
                UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            if (IsLocalPlayer)
                UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
        }

        private Attributes followTarget;
        private OnScreenStick[] joysticks = new OnScreenStick[0];
        private void Update()
        {
            if (!IsLocalPlayer) { return; }


            #if UNITY_IOS || UNITY_ANDROID
            // If on a mobile platform
            lookInput = Vector2.zero;
            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput.currentActionMap.name == playerInput.defaultActionMap)
            {
                foreach (UnityEngine.InputSystem.EnhancedTouch.Touch touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
                {
                    if (touch.isTap)
                    {
                        // Interact action?
                    }
                    else
                    {
                        if (joysticks.Length == 0) { joysticks = GetComponentsInChildren<OnScreenStick>(); }

                        foreach (OnScreenStick joystick in joysticks)
                        {
                            if (!RectTransformUtility.RectangleContainsScreenPoint(joystick.transform.parent.GetComponent<RectTransform>(), touch.startScreenPosition) & touch.screenPosition.x > Screen.width / 2f)
                            {
                                lookInput += touch.delta;
                            }
                        }
                    }
                }
            }
            # endif

            if (moveInput != Vector2.zero)
            {
                shouldViewEnvironment = false;
                environmentViewPosition = Vector3.zero;
                environmentViewRotation = Quaternion.identity;
                followTarget = null;
            }

            if (shouldViewEnvironment)
            {
                transform.position = Vector3.Lerp(transform.position, environmentViewPosition, Time.deltaTime * 8);
                transform.rotation = Quaternion.Slerp(transform.rotation, environmentViewRotation, Time.deltaTime * 8);

                targetPosition = transform.position;
            }
            else if (followTarget)
            {
                Vector3 targetPosition = followTarget.transform.position + followTarget.transform.rotation * new Vector3(0, 3, -3);
                Quaternion targetRotation = Quaternion.LookRotation(followTarget.transform.position - transform.position);

                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 8);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8);
                
                this.targetPosition = transform.position;
            }
            else
            {
                float verticalSpeed = 0;
                if (isAscending) { verticalSpeed = 1; }
                if (isDescending) { verticalSpeed = -1; }

                targetPosition += (isSprinting ? moveSpeed * 2 : moveSpeed) * Time.deltaTime * (transform.rotation * new Vector3(moveInput.x, verticalSpeed, moveInput.y));
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 8);

                float xAngle = transform.eulerAngles.x - GetLookInput().y;
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
                transform.eulerAngles = new Vector3(xAngle, transform.eulerAngles.y + GetLookInput().x, 0);
            }
        }
    }
}