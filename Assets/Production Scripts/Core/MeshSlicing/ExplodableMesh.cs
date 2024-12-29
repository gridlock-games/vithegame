using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using Vi.Utility;
using UnityEngine.Animations.Rigging;
using System.Linq;

namespace Vi.Core.MeshSlicing
{
    public class ExplodableMesh : MonoBehaviour
    {
        private Renderer thisRenderer;
        private void Awake()
        {
            thisRenderer = GetComponent<Renderer>();
        }

        private ExplodableMeshController controller;
        private List<int> indexList;
        private void OnEnable()
        {
            controller = GetComponentInParent<ExplodableMeshController>();
            indexList = Enumerable.Range(0, explosionMeshes.Length).ToList();

            if (!controller)
            {
                Debug.LogWarning("Explodable mesh has no controller in the parent hierarchy!");
            }
        }

        public List<PooledObject> Explode(int instanceLimit)
        {
            int instanceCounter = 0;

            indexList.Shuffle();

            List<PooledObject> instances = new List<PooledObject>();
            foreach (int index in indexList)
            {
                if (instanceCounter > instanceLimit) { break; }

                PooledObject obj = ObjectPoolingManager.SpawnObject(explosionPiecePrefab, transform.position, transform.rotation);

                if (obj.TryGetComponent(out ExplodedMeshPiece explodedMeshPiece))
                {
                    explodedMeshPiece.Initialize(explosionMeshes[index], rendererMaterials, thisRenderer);
                }
                else
                {
                    Debug.LogWarning("No Exploded mesh piece component on prefab! " + this);
                }

                instances.Add(obj);
                instanceCounter++;
            }

            return instances;
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