using UnityEngine;
using UnityEngine.UI;
using Vi.Utility;

namespace Vi.UI
{
    [RequireComponent(typeof(Canvas))]
    public class WorldSpaceCanvas : MonoBehaviour
    {
        private Canvas canvas;
        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            canvas.worldCamera = FindMainCamera.MainCamera;

            if (canvas.renderMode != RenderMode.WorldSpace)
            {
                Debug.LogWarning("Canvas render mode isn't world space! " + this + " " + canvas.renderMode);
            }
        }

        private void Update()
        {
            canvas.worldCamera = FindMainCamera.MainCamera;
        }
    }
}

