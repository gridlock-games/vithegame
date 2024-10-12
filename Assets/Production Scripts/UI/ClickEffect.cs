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

            //Vector3 startPos = UnityEngine.InputSystem.Mouse.current.position.value;
            //startPos.z = cam.nearClipPlane;
            //Debug.Log(startPos + " " + cam.ScreenToWorldPoint(startPos));
            //Vector3 pos = cam.ScreenToWorldPoint(startPos);
            //emitSettings.position = pos;
            //particle.Emit(emitSettings, 1);

            foreach (UnityEngine.InputSystem.EnhancedTouch.Touch touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
            {
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    Vector3 startPos = touch.screenPosition;
                    startPos.z = cam.nearClipPlane;
                    Debug.Log(startPos + " " + cam.ScreenToWorldPoint(startPos));
                    Vector3 pos = cam.ScreenToWorldPoint(startPos);
                    emitSettings.position = pos;
                    particle.Emit(emitSettings, 1);
                }
            }
        }
#endif
    }
}