using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public class CameraSpaceCanvas : MonoBehaviour
    {
        private Canvas canvas;
        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
        }

        private void Start()
        {
            canvas.worldCamera = UICamera.GetActiveUICamera();
            canvas.planeDistance = canvas.worldCamera.nearClipPlane + 0.01f;
        }

        private void Update()
        {
            canvas.worldCamera = UICamera.GetActiveUICamera();
            canvas.planeDistance = canvas.worldCamera.nearClipPlane + 0.01f;
        }
    }
}