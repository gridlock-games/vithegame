using UnityEngine;

namespace Vi.Core.MeshSlicing
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    public class ExplodedMeshPiece : MonoBehaviour
    {
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Rigidbody rb;
        private BoxCollider boxCollider;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            rb = GetComponent<Rigidbody>();
            boxCollider = GetComponent<BoxCollider>();
        }

        public void Initialize(Mesh mesh, Material[] rendererMaterials, Renderer baseRenderer)
        {
            meshFilter.mesh = mesh;
            meshRenderer.sharedMaterials = rendererMaterials;

            boxCollider.center = meshRenderer.localBounds.center;
            boxCollider.size = meshRenderer.localBounds.extents;
            boxCollider.enabled = true;

            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            rb.AddExplosionForce(5, baseRenderer.bounds.center, 10, 0, ForceMode.VelocityChange);
        }

        private void OnDisable()
        {
            meshFilter.mesh = null;
            meshRenderer.sharedMaterials = new Material[0];
            boxCollider.enabled = false;
        }
    }
}