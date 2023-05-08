using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class HitRenderer : MonoBehaviour
    {
        [SerializeField] private Material[] hitMaterials;
        [SerializeField] private float meshColorResetDelay = 0.75f;

        private SkinnedMeshRenderer skinnedMeshRenderer;
        private Material[] defaultMaterialList;

        public void Invoke()
        {
            StartCoroutine(ResetMeshColorAfterDelay(meshColorResetDelay));
        }

        void Start()
        {
            skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            defaultMaterialList = skinnedMeshRenderer.materials;
        }

        private IEnumerator ResetMeshColorAfterDelay(float delay)
        {
            skinnedMeshRenderer.materials = hitMaterials;
            yield return new WaitForSeconds(delay);
            skinnedMeshRenderer.materials = defaultMaterialList;
        }
    }
}