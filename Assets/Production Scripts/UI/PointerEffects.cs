using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Vi.Utility;
using Vi.Core;

namespace Vi.UI
{
    public class PointerEffects : MonoBehaviour
    {
        [SerializeField] ParticleSystem clickParticle;

        ParticleSystem.EmitParams clickParticleEmitSettings;

        private void OnEnable()
        {
            RefreshStatus();
        }

        private void Start()
        {
            clickParticleEmitSettings = new ParticleSystem.EmitParams();
        }

        private PlayerInput playerInput;
        private void FindPlayerInput()
        {
            if (playerInput)
            {
                if (playerInput.isActiveAndEnabled) { return; }
            }

            if (NetworkManager.Singleton)
            {
                if (NetworkManager.Singleton.LocalClient.PlayerObject)
                {
                    NetworkManager.Singleton.LocalClient.PlayerObject.TryGetComponent(out playerInput);
                }
            }
        }

        private bool pointerEffectsEnabled;
        private void RefreshStatus()
        {
            pointerEffectsEnabled = FasterPlayerPrefs.Singleton.GetBool("PointerEffects");
        }

        private void Update()
        {
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame)
            {
                RefreshStatus();
            }

            if (WebRequestManager.Singleton.IsLoggedIn)
            {
                if (!pointerEffectsEnabled) { return; }
            }
            
            FindPlayerInput();

            if (playerInput)
            {
                if (playerInput.isActiveAndEnabled)
                {
                    if (playerInput.currentActionMap?.name != "UI") { clickParticle.gameObject.SetActive(false); return; }
                }
            }

            Camera cam = UICamera.GetActiveUICamera();
            if (!cam) { clickParticle.gameObject.SetActive(false); return; }

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (!UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled) { clickParticle.gameObject.SetActive(false); return; }

            clickParticle.gameObject.SetActive(true);

            foreach (UnityEngine.InputSystem.EnhancedTouch.Touch touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
            {
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    Vector3 startPos = touch.screenPosition;
                    startPos.z = cam.nearClipPlane;
                    clickParticleEmitSettings.position = cam.ScreenToWorldPoint(startPos);
                    clickParticle.Emit(clickParticleEmitSettings, 1);
                }
            }
#else
            if (Mouse.current != null)
            {
                clickParticle.gameObject.SetActive(true);

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Vector3 startPos = Mouse.current.position.value;
                    startPos.z = cam.nearClipPlane;
                    clickParticleEmitSettings.position = cam.ScreenToWorldPoint(startPos);
                    clickParticle.Emit(clickParticleEmitSettings, 1);
                }
            }
#endif
        }
    }
}