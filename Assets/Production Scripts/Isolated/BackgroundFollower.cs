using UnityEngine;

namespace Vi.Isolated
{
    [ExecuteAlways]
    public class BackgroundFollower : MonoBehaviour
    {
        public Camera targetCamera; // Assign your camera in the Inspector
        public float distanceFromCamera = 10f; // Adjust as needed
        public Material backgroundMaterial; // Assign the quad's material
        public Texture backgroundTexture; // Assign the background texture
        public Vector2 fineTuningTextureOffset; // Fine tuning of texture offset

        void LateUpdate()
        {
            if (targetCamera != null)
            {
                // Position the quad
                transform.position = targetCamera.transform.position + targetCamera.transform.forward * distanceFromCamera;

                // Match the rotation of the camera
                transform.rotation = targetCamera.transform.rotation;

                // Calculate the size to fit the view frustum
                float height = 2f * distanceFromCamera * Mathf.Tan(Mathf.Deg2Rad * targetCamera.fieldOfView / 2f);
                float width = height * targetCamera.aspect;

                // Scale the quad
                transform.localScale = new Vector3(width, height, 1f);

                // Calculate texture tiling to respect image's aspect ratio
                float textureAspect = (float)backgroundTexture.width / backgroundTexture.height;
                float quadAspect = width / height;

                Vector2 tiling = Vector2.one;
                if (quadAspect > textureAspect)
                {
                    // Quad is wider than the texture
                    tiling.x = quadAspect / textureAspect;
                }
                else
                {
                    // Quad is taller than the texture
                    tiling.y = textureAspect / quadAspect;
                }

                backgroundMaterial.mainTextureScale = tiling;

                // Adjust texture offset to center properly, with fine-tuning
                Vector2 offset = (Vector2.one - tiling) / 2f;

                // Optional: Add a vertical adjustment for alignment
                offset += fineTuningTextureOffset;

                backgroundMaterial.mainTextureOffset = offset;
            }
        }
    }
}