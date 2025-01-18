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
        [SerializeField] UIParticleSystem clickParticle;

        private void OnEnable()
        {
            RefreshStatus();
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

            Camera particleCam;
            if (WebRequestManager.Singleton)
            {
                if (WebRequestManager.Singleton.IsLoggedIn)
                {
                    if (!pointerEffectsEnabled)
                    {
                        particleCam = UICamera.GetActiveUIParticleCamera();
                        if (particleCam) { particleCam.enabled = false; }
                        return;
                    }
                }
            }
            
            FindPlayerInput();

            if (playerInput)
            {
                if (playerInput.isActiveAndEnabled)
                {
                    if (playerInput.currentActionMap?.name != "UI")
                    {
                        clickParticle.gameObject.SetActive(false);

                        //particleCam = UICamera.GetActiveUIParticleCamera();
                        //if (particleCam) { particleCam.enabled = false; }

                        return;
                    }
                }
            }

            Camera cam = UICamera.GetActiveUICamera();
            if (!cam)
            {
                particleCam = UICamera.GetActiveUIParticleCamera();
                if (particleCam) { particleCam.enabled = false; }
                clickParticle.gameObject.SetActive(false);
                return;
            }

            particleCam = UICamera.GetActiveUIParticleCamera();
            if (particleCam) { particleCam.enabled = true; }

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (!UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled) { clickParticle.gameObject.SetActive(false); return; }

            clickParticle.gameObject.SetActive(true);

            foreach (UnityEngine.InputSystem.EnhancedTouch.Touch touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
            {
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    clickParticle.PlayScreenPoint(touch.screenPosition);
                }
            }
#else
            if (Mouse.current != null)
            {
                clickParticle.gameObject.SetActive(true);

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    clickParticle.PlayScreenPoint(Mouse.current.position.value);
                }
            }
#endif
        }
    }
}