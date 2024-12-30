using UnityEngine;

namespace Vi.Utility
{
    public class FindMainCamera : MonoBehaviour
    {
        public static Camera MainCamera { get; private set; }

        private void GetMainCamera()
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

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            GetMainCamera();
        }

        private void Update()
        {
            GetMainCamera();
        }
    }
}

