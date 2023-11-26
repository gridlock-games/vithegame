using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Vi.UI
{
    [RequireComponent(typeof(Camera))]
    public class UICamera : MonoBehaviour
    {
        private static List<UICamera> UICameras = new List<UICamera>();

        private Camera cam;
        private void Awake()
        {
            UICameras.Add(this);
            cam = GetComponent<Camera>();
            cam.cullingMask = LayerMask.GetMask(new string[] { "UI" });
            cam.depth = -1;
        }

        private void Update()
        {
            if (NetworkManager.Singleton.IsServer) { cam.enabled = false; }
            else { cam.enabled = UICameras[^1] == this; }
        }

        private void OnDestroy()
        {
            UICameras.Remove(this);
        }
    }
}