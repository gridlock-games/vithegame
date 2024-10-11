using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using Vi.Utility;

namespace Vi.Core.MeshSlicing
{
    public class ExplodableMesh : MonoBehaviour
    {
        private Renderer thisRenderer;
        private void Awake()
        {
            thisRenderer = GetComponent<Renderer>();
        }

        public PooledObject[] Explode()
        {
            List<PooledObject> instances = new List<PooledObject>();
            for (int i = 0; i < explosionMeshes.Length; i++)
            {
                PooledObject obj = ObjectPoolingManager.SpawnObject(explosionPiecePrefab, transform.position, transform.rotation);

                obj.GetComponent<MeshFilter>().mesh = explosionMeshes[i];
                MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
                meshRenderer.materials = rendererMaterials;

                BoxCollider boxCollider = obj.GetComponent<BoxCollider>();
                boxCollider.center = meshRenderer.localBounds.center;
                boxCollider.size = meshRenderer.localBounds.extents;

                Rigidbody rb = obj.GetComponent<Rigidbody>();
                rb.useGravity = true;
                rb.interpolation = RigidbodyInterpolation.Interpolate;

                rb.AddExplosionForce(5, thisRenderer.bounds.center, 10, 0, ForceMode.VelocityChange);

                instances.Add(obj);
            }
            return instances.ToArray();
        }

        [SerializeField] private PooledObject explosionPiecePrefab;
        [SerializeField] private Material[] rendererMaterials;
        [SerializeField] private Mesh[] explosionMeshes;

#if UNITY_EDITOR
        [ContextMenu("Find Explosion Meshes")]
        private void FindExplosionMeshes()
        {
            if (TryGetComponent(out SkinnedMeshRenderer skinnedMeshRenderer))
            {
                rendererMaterials = skinnedMeshRenderer.sharedMaterials;
            }
            else if (TryGetComponent(out MeshRenderer meshRenderer))
            {
                rendererMaterials = meshRenderer.sharedMaterials;
            }
            else
            {
                Debug.LogError("Explodable mesh should only be on a skinned mesh renderer or a mesh renderer");
            }

            string folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                List<Mesh> meshes = new List<Mesh>();
                foreach (string path in Directory.GetFiles(folderPath, "*.asset", SearchOption.TopDirectoryOnly))
                {
                    Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (mesh)
                    {
                        meshes.Add(mesh);
                    }
                }
                explosionMeshes = meshes.ToArray();
            }
            else
            {
                Debug.LogError("Selection is not a valid folder");
            }
            EditorUtility.SetDirty(this);
        }

        [ContextMenu("Find Explosion Materials")]
        private void FindExplosionMaterials()
        {
            if (TryGetComponent(out SkinnedMeshRenderer skinnedMeshRenderer))
            {
                rendererMaterials = skinnedMeshRenderer.sharedMaterials;
            }
            else
            {
                Debug.LogError("Explodable mesh should only be on a skinned mesh renderer");
            }
            EditorUtility.SetDirty(this);
        }
#endif
    }
}