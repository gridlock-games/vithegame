using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LylekGames.Tools
{
    public static class CameraExtension
    {
        public static void CopyProperties(this Camera camera, Camera cameraProperties)
        {
            camera.gameObject.tag = cameraProperties.gameObject.tag;

            camera.nearClipPlane = cameraProperties.nearClipPlane;
            camera.farClipPlane = cameraProperties.farClipPlane;
            camera.cullingMask = cameraProperties.cullingMask;
            camera.fieldOfView = cameraProperties.fieldOfView;
            camera.clearFlags = cameraProperties.clearFlags;
            camera.backgroundColor = cameraProperties.backgroundColor;
        }
    }
}