using UnityEngine;
using UnityEngine.InputSystem;
using Vi.Utility;

namespace Vi.UI
{
    [RequireComponent(typeof(ParticleSystem))]
    public class UIParticleSystem : MonoBehaviour
    {
        ParticleSystem.EmitParams particleEmitSettings;
        public ParticleSystem ps { get; private set; }

        private void Awake()
        {
            ps = GetComponent<ParticleSystem>();
            particleEmitSettings = new ParticleSystem.EmitParams();

            if (gameObject.layer != LayerMask.NameToLayer("UIParticle"))
            {
                Debug.LogWarning(this + " isn't in the UI Particle layer!");
            }
        }

        public void PlayWorldPoint(Vector3 worldSpacePosition)
        {
            Camera cam = UICamera.GetActiveUICamera();
            if (cam == null) { Debug.LogWarning("No UI camera!"); return; }
            if (!cam.enabled) { Debug.LogWarning("UI Particle Camera is disabled!"); }

            Vector3 screenPoint = cam.WorldToScreenPoint(worldSpacePosition);
            screenPoint.z = cam.nearClipPlane;

            worldSpacePosition = cam.ScreenToWorldPoint(screenPoint);

            particleEmitSettings.position = worldSpacePosition;
            ps.Emit(particleEmitSettings, 1);
        }

        public void PlayScreenPoint(Vector3 screenPoint)
        {
            Camera cam = UICamera.GetActiveUICamera();
            if (cam == null) { Debug.LogWarning("No UI camera!"); return; }
            if (!cam.enabled) { Debug.LogWarning("UI Particle Camera is disabled!"); }

            screenPoint.z = cam.nearClipPlane;

            particleEmitSettings.position = cam.ScreenToWorldPoint(screenPoint);
            ps.Emit(particleEmitSettings, 1);
        }
    }
}