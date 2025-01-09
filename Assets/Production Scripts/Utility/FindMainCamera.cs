using UnityEngine;

namespace Vi.Utility
{
    public class FindMainCamera : MonoBehaviour
    {
        public static Camera MainCamera
        {
            get
            {
                if (!_mainCamera) { GetMainCamera(); }
                return _mainCamera;
            }
        }

        private static Camera _mainCamera;

        private static void GetMainCamera()
        {
            if (_mainCamera)
            {
                if (_mainCamera.gameObject.CompareTag("MainCamera"))
                {
                    return;
                }
            }
            _mainCamera = Camera.main;
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

