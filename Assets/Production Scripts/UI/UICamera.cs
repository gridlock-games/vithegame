using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vi.UI
{
    [RequireComponent(typeof(Camera))]
    public class UICamera : MonoBehaviour
    {
        [SerializeField] private string[] layerMask = new string[] { "UI" };

        private static List<UICamera> UICameras = new List<UICamera>();

        private Camera cam;
        private AudioListener audioListener;
        private void Awake()
        {
            UICameras.Add(this);

            cam = GetComponent<Camera>();
            cam.cullingMask = LayerMask.GetMask(layerMask);
            cam.depth = -1;
        }

        public static Camera MainCamera { get; private set; }
        private void FindMainCamera()
        {
            if (MainCamera)
            {
                if (MainCamera.gameObject.CompareTag("MainCamera"))
                {
                    return;
                }
            }
            MainCamera = Camera.main;
        }

        public static Camera GetActiveUICamera()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return null;
            }
            else { return UICameras[^1].cam; }
        }

        private bool lastCamState;
        private void Update()
        {
            FindMainCamera();

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) { cam.enabled = false; }
            //else if (NetworkManager.Singleton.IsServer) { cam.enabled = false; }
            else if (MainCamera) { cam.enabled = false; }
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
        }

        private void OnDestroy()
        {
            UICameras.Remove(this);
        }
    }
}