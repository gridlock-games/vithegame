using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LylekGames.Tools
{
    public class CameraContoller : MonoBehaviour
    {
        public Camera mainCamera;
        public GameObject focusPoint;
        [Tooltip("X - Side to side, Y Up and Down, Z Forward and Backward (Positive or Negative value)")]
        public Vector3 offset = new Vector3(0, 0, -4);
        public bool lookAtFocusPoint = false;
        public bool smoothFollow = false;
        [Range(1.0f, 10.0f)]
        public float speed = 5.0f;
        [Space]
        [Range(0.1f, 1.0f)]
        [Tooltip("Zoom intensity.")]
        public float zoom = 0.5f;
        [Range(3, 10)]
        [Tooltip("Max distance when zooming out.")]
        public float maxOut = 5;
        [Range(0.1f, 1.0f)]
        [Tooltip("Max distance when zooming in.")]
        public float maxIn = 0.5f;

        public void Update()
        {
            Vector3 destination = focusPoint.transform.position + (focusPoint.transform.right * offset.x) + (focusPoint.transform.up * offset.y) + (focusPoint.transform.forward * offset.z);

            if (smoothFollow)
                mainCamera.transform.position = Vector3.MoveTowards(mainCamera.transform.position, destination, speed * Time.deltaTime);
            else
                mainCamera.transform.position = destination;

            if (lookAtFocusPoint)
                transform.LookAt(focusPoint.transform.position);

            if (Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                float distance = Vector3.Distance(mainCamera.transform.position, focusPoint.transform.position);

                if (distance > maxIn)
                {
                    Vector3 newOffset = offset;
                    newOffset.z += zoom;
                    offset = newOffset;
                }
            }
            if (Input.GetAxis("Mouse ScrollWheel") < 0)
            {
                float distance = Vector3.Distance(mainCamera.transform.position, focusPoint.transform.position);

                if (distance < maxOut)
                {
                    Vector3 newOffset = offset;
                    newOffset.z -= zoom;
                    offset = newOffset;
                }
            }
        }
    }
}