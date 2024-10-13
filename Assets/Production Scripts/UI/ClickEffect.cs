using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.UI
{
    public class ClickEffect : MonoBehaviour
    {
        [SerializeField] ParticleSystem particle;

#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
        ParticleSystem.EmitParams emitSettings;

        private void Start()
        {
            emitSettings = new ParticleSystem.EmitParams();
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
        }

        private Camera mainCamera;
        private void FindMainCamera()
        {
            if (mainCamera)
            {
                if (mainCamera.gameObject.CompareTag("MainCamera"))
                {
                    return;
                }
            }
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (!UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled) { particle.gameObject.SetActive(false); return; }

            FindMainCamera();
            if (mainCamera) { particle.gameObject.SetActive(false); return; }
            Camera cam = UICamera.GetActiveUICamera();
            if (!cam) { particle.gameObject.SetActive(false); return; }

            particle.gameObject.SetActive(true);

#if UNITY_EDITOR
            if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector3 startPosEditor = UnityEngine.InputSystem.Mouse.current.position.value;
                startPosEditor.z = cam.nearClipPlane;
                emitSettings.position = cam.ScreenToWorldPoint(startPosEditor);
                particle.Emit(emitSettings, 1);
            }
#endif

            foreach (UnityEngine.InputSystem.EnhancedTouch.Touch touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
            {
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    Vector3 startPos = touch.screenPosition;
                    startPos.z = cam.nearClipPlane;
                    emitSettings.position = cam.ScreenToWorldPoint(startPos);
                    particle.Emit(emitSettings, 1);
                }
            }
        }
#endif
    }
}