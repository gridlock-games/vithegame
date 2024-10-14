using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Vi.UI
{
    [RequireComponent(typeof(Camera))]
    public class UICamera : MonoBehaviour
    {
        [SerializeField] private string[] layerMask = new string[] { "UI" };

        private static List<UICamera> UICameras = new List<UICamera>();

        private Camera cam;
        private AudioListener audioListener;
        private UniversalAdditionalCameraData cameraData;
        private void Awake()
        {
            UICameras.Add(this);

            cam = GetComponent<Camera>();
            cam.cullingMask = LayerMask.GetMask(layerMask);
            cam.depth = -1;

            cameraData = GetComponent<UniversalAdditionalCameraData>();
        }

        private static Camera mainCamera;
        private static UniversalAdditionalCameraData mainCameraData;
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
            if (mainCamera)
            {
                mainCameraData = mainCamera.GetComponent<UniversalAdditionalCameraData>();
            }
        }

        public static Camera GetActiveUICamera()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return null;
            }
            //else if (MainCamera & !SceneLoadingUI.IsDisplaying) { return MainCamera; }
            else { return UICameras[^1].cam; }
        }

        private bool lastCamState;
        private void Update()
        {
            FindMainCamera();

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) { cam.enabled = false; }
            //else if (NetworkManager.Singleton.IsServer) { cam.enabled = false; }
            //else if (MainCamera & !SceneLoadingUI.IsDisplaying) { cam.enabled = false; }
            else { cam.enabled = UICameras[^1] == this; }

            if (cam.enabled != lastCamState)
            {
                if (cam.enabled)
                {
                    cam.depth = 0;
                    if (!audioListener) { audioListener = gameObject.AddComponent<AudioListener>(); }
                }
                else
                {
                    cam.depth = -1;
                    if (audioListener) { Destroy(audioListener); }
                }
            }

            lastCamState = cam.enabled;

            if (mainCamera)
            {
                if (audioListener) { Destroy(audioListener); }

                if (cameraData.renderType == CameraRenderType.Base)
                {
                    cameraData.renderType = CameraRenderType.Overlay;
                    mainCameraData.cameraStack.Add(cam);
                }
            }
            else if (cam.enabled)
            {
                if (!audioListener) { audioListener = gameObject.AddComponent<AudioListener>(); }
                
                if (cameraData.renderType == CameraRenderType.Overlay)
                {
                    mainCameraData.cameraStack.Remove(cam);
                    cameraData.renderType = CameraRenderType.Base;
                }
            }

            if (mainCamera) { if (audioListener) { Destroy(audioListener); } }
        }

        private void OnDestroy()
        {
            UICameras.Remove(this);
        }
    }
}