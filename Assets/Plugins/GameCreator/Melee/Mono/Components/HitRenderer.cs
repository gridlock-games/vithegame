using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreator.Melee
{
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class HitRenderer : MonoBehaviour
    {
        [SerializeField] private Material[] hitMaterials;
        [SerializeField] private Material[] healMaterials;
        [SerializeField] private float meshColorResetDelay = 0.75f;

        private SkinnedMeshRenderer skinnedMeshRenderer;
        private Material[] defaultMaterialList;

        public void RenderHit()
        {
            StartCoroutine(ResetMeshColorAfterDelay(meshColorResetDelay, hitMaterials));
        }

        public void RenderHeal()
        {
            StartCoroutine(ResetMeshColorAfterDelay(meshColorResetDelay, healMaterials));
        }

        void Start()
        {
            skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            defaultMaterialList = skinnedMeshRenderer.materials;
        }

        private IEnumerator ResetMeshColorAfterDelay(float delay, Material[] materialChanges)
        {
            skinnedMeshRenderer.materials = materialChanges;
            yield return new WaitForSeconds(delay);
            skinnedMeshRenderer.materials = defaultMaterialList;
        }
    }
}