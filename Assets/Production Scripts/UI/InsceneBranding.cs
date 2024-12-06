using UnityEngine;

namespace Vi.UI
{
    public class InsceneBranding : MonoBehaviour
    {
        private Camera mainCamera;
        private void FindMainCamera()
        {
            if (mainCamera)
            {
                if (mainCamera.gameObject.CompareTag("MainCamera"))
                {
                    return;
                }
            }
            mainCamera = Camera.main;
        }

        private void Update()
        {
            
        }
    }
}