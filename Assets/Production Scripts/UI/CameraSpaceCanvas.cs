using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.UI
{
    [RequireComponent(typeof(Canvas))]
    public class CameraSpaceCanvas : MonoBehaviour
    {
        private Canvas canvas;
        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.planeDistance = 100;
        }

        private void Start()
        {
            canvas.worldCamera = UICamera.GetActiveUICamera();
        }

        private void Update()
        {
            canvas.worldCamera = UICamera.GetActiveUICamera();
        }
    }
}