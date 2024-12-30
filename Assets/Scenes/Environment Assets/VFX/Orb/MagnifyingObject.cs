using UnityEngine;
using System.Collections;

public class MagnifyingObject : MonoBehaviour
{
    Renderer _renderer;
    Camera _cam;

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        _cam = Camera.main;
    }

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

    void Update()
    {
        FindMainCamera();

        if (!_cam) { return; }

        Vector3 screenPoint = _cam.WorldToScreenPoint(transform.position);
        screenPoint.x = screenPoint.x / Screen.width;
        screenPoint.y = screenPoint.y / Screen.height;
        _renderer.material.SetVector("ObjScrnPosition", screenPoint);
    }
}
