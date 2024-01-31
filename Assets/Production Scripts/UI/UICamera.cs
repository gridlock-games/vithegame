using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering;

namespace Vi.UI
{
    [RequireComponent(typeof(Camera))]
    public class UICamera : MonoBehaviour
    {
        [SerializeField] private string[] layerMask = new string[] { "UI" };

        private static List<UICamera> UICameras = new List<UICamera>();

        private Camera cam;
        private void Awake()
        {
            UICameras.Add(this);
            cam = GetComponent<Camera>();
            cam.cullingMask = LayerMask.GetMask(layerMask);
            cam.depth = -1;
        }

        private void Update()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) { cam.enabled = false; }
            //else if (NetworkManager.Singleton.IsServer) { cam.enabled = false; }
            else if (Camera.main) { cam.enabled = false; }
            else { cam.enabled = UICameras[^1] == this; }
        }

        private void OnDestroy()
        {
            UICameras.Remove(this);
        }
    }
}