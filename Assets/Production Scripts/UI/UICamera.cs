using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Vi.Utility;

namespace Vi.UI
{
    [RequireComponent(typeof(Camera))]
    public class UICamera : MonoBehaviour
    {
        [SerializeField] private string[] layerMask = new string[] { "UI", "UIParticle" };

        private static List<UICamera> UICameras = new List<UICamera>();

        private Camera attachedUICamera;
        private AudioListener audioListener;
        private UniversalAdditionalCameraData cameraData;

        private Camera[] allCameras;
        //private UniversalAdditionalCameraData[] allCameraDatas;
        private void Awake()
        {
            UICameras.Add(this);

            attachedUICamera = GetComponent<Camera>();
            attachedUICamera.cullingMask = LayerMask.GetMask(layerMask);
            attachedUICamera.depth = -1;

            cameraData = GetComponent<UniversalAdditionalCameraData>();

            allCameras = GetComponentsInChildren<Camera>();
            //allCameraDatas = GetComponentsInChildren<UniversalAdditionalCameraData>();
        }

        private static Camera mainCamera;
        private static UniversalAdditionalCameraData mainCameraData;
        private void FindMainCameraForThis()
        {
            if (mainCamera)
            {
                if (!mainCamera.enabled) { mainCamera = null; return; }

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
            else if (mainCamera) { return mainCamera; }
            else { return UICameras[^1].attachedUICamera; }
        }

        private bool lastCamState;
        private void Update()
        {
            FindMainCameraForThis();

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) { attachedUICamera.enabled = false; }
            //else if (NetworkManager.Singleton.IsServer) { cam.enabled = false; }
            //else if (MainCamera & !SceneLoadingUI.IsDisplaying) { cam.enabled = false; }
            else { attachedUICamera.enabled = UICameras[^1] == this & !mainCamera; }

            if (attachedUICamera.enabled != lastCamState)
            {
                if (attachedUICamera.enabled)
                {
                    attachedUICamera.depth = 0;
                    if (!audioListener & !FindMainCamera.MainCamera) { audioListener = gameObject.AddComponent<AudioListener>(); }
                }
                else
                {
                    attachedUICamera.depth = -1;
                    if (audioListener) { Destroy(audioListener); }
                }
            }

            lastCamState = attachedUICamera.enabled;

            if (mainCamera)
            {
                if (audioListener) { Destroy(audioListener); }

                //if (cameraData.renderType == CameraRenderType.Base)
                //{
                //    cameraData.renderType = CameraRenderType.Overlay;
                //    for (int i = 0; i < allCameras.Length; i++)
                //    {
                //        mainCameraData.cameraStack.Add(allCameras[i]);
                //    }
                //}
            }
            else if (attachedUICamera.enabled)
            {
                if (!audioListener & !FindMainCamera.MainCamera) { audioListener = gameObject.AddComponent<AudioListener>(); }
                
                //if (cameraData.renderType == CameraRenderType.Overlay)
                //{
                //    for (int i = 0; i < allCameras.Length; i++)
                //    {
                //        mainCameraData.cameraStack.Remove(allCameras[i]);
                //    }
                //    cameraData.renderType = CameraRenderType.Base;
                //}
            }

            if (mainCamera) { if (audioListener) { Destroy(audioListener); } }
        }

        private void OnDestroy()
        {
            //if (cameraData.renderType == CameraRenderType.Overlay)
            //{
            //    for (int i = 0; i < allCameras.Length; i++)
            //    {
            //        mainCameraData.cameraStack.Remove(allCameras[i]);
            //    }
            //}
            UICameras.Remove(this);
        }
    }
}