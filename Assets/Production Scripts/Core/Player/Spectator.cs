using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Vi.Core;

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

        void OnFollowPlayer1()
        {
            if (0 >= playerList.Count) { return; }
            followTarget = playerList[0];
        }

        void OnFollowPlayer2()
        {
            if (1 >= playerList.Count) { return; }
            followTarget = playerList[1];
        }

        void OnFollowPlayer3()
        {
            if (2 >= playerList.Count) { return; }
            followTarget = playerList[2];
        }

        void OnFollowPlayer4()
        {
            if (3 >= playerList.Count) { return; }
            followTarget = playerList[3];
        }

        void OnFollowPlayer5()
        {
            if (4 >= playerList.Count) { return; }
            followTarget = playerList[4];
        }

        void OnFollowPlayer6()
        {
            if (5 >= playerList.Count) { return; }
            followTarget = playerList[5];
        }

        void OnFollowPlayer7()
        {
            if (6 >= playerList.Count) { return; }
            followTarget = playerList[6];
        }

        void OnFollowPlayer8()
        {
            if (7 >= playerList.Count) { return; }
            followTarget = playerList[7];
        }

        void OnFollowPlayer9()
        {
            if (8 >= playerList.Count) { return; }
            followTarget = playerList[8];
        }

        void OnFollowPlayer10()
        {
            if (9 >= playerList.Count) { return; }
            followTarget = playerList[9];
        }

        private Vector3 targetPosition;
        protected new void Start()
        {
            base.Start();
            targetPosition = transform.position;
        }

        private Attributes followTarget;
        private void Update()
        {
            if (!IsLocalPlayer) { return; }

            if (moveInput != Vector2.zero) { followTarget = null; }

            if (followTarget)
            {
                transform.position = followTarget.transform.position + followTarget.transform.rotation * new Vector3(0, 3, -3);
                transform.LookAt(followTarget.transform);

                targetPosition = transform.position;
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