using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class MagnifyingObject : MonoBehaviour
{
    Renderer _renderer;
    Camera _cam;

    private UniversalRenderPipelineAsset pipeline;

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        _cam = Camera.main;

        pipeline = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;

        if (!pipeline.supportsCameraOpaqueTexture)
        {
            pipeline.supportsCameraOpaqueTexture = true;
            StartCoroutine(DisableOpaqueTexture());
        }
    }

    private IEnumerator DisableOpaqueTexture()
    {
        yield return null;
        yield return null;
        yield return null;
        pipeline.supportsCameraOpaqueTexture = false;
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
