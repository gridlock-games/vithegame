using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Player
{
    public class SpectatorCamera : NetworkBehaviour
    {
        public float moveSpeed = 1;
        public float sensitivity = 0.1f;
        int fps;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                gameObject.AddComponent<GameCreator.Core.Hooks.HookCamera>();
                gameObject.AddComponent<AudioListener>();
                GetComponent<Camera>().enabled = true;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                GetComponent<Camera>().enabled = false;
            }
        }

        private void Update()
        {
            Vector2 moveInput = Vector2.zero;
            if (Input.GetKey(KeyCode.W)) { moveInput.y = 1; }
            if (Input.GetKey(KeyCode.S)) { moveInput.y = -1; }
            if (Input.GetKey(KeyCode.D)) { moveInput.x = 1; }
            if (Input.GetKey(KeyCode.A)) { moveInput.x = -1; }
            transform.Translate(new Vector3(moveInput.x, 0, moveInput.y) * moveSpeed);

            Vector2 lookInput = Vector2.zero;
            lookInput.x = Input.GetAxis("Mouse X");
            lookInput.y = Input.GetAxis("Mouse Y");
            lookInput *= sensitivity;
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x - lookInput.y, transform.localEulerAngles.y + lookInput.x, transform.localEulerAngles.z);

            fps = Mathf.RoundToInt((float)1.0 / Time.deltaTime);
        }

        private void OnGUI()
        {
            // FPS Label
            GUIStyle guiStyle = new GUIStyle();
            guiStyle.fontSize = 48;
            guiStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(Screen.currentResolution.width - 100, 50, 100, 50), fps.ToString(), guiStyle);
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }
}