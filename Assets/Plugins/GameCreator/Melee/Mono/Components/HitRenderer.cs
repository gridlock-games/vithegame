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
        [SerializeField] private float meshColorResetDelay = 0.25f;

        [SerializeField] private Material[] invincibleMaterials;

        private SkinnedMeshRenderer skinnedMeshRenderer;
        private Material[] defaultMaterialList;

        public void RenderInvinicible(bool isInvinicible)
        {
            if (isInvinicible)
            {
                skinnedMeshRenderer.materials = invincibleMaterials;
            }
            else
            {
                skinnedMeshRenderer.materials = defaultMaterialList;
            }
        }

        public void RenderHit()
        {
            Debug.Log("Render Hit");
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

        private bool rendererRunning;
        private IEnumerator ResetMeshColorAfterDelay(float delay, Material[] materialChanges)
        {
            yield return new WaitUntil(() => !rendererRunning);
            rendererRunning = true;
            skinnedMeshRenderer.materials = materialChanges;
            yield return new WaitForSeconds(delay);
            skinnedMeshRenderer.materials = defaultMaterialList;
            rendererRunning = false;
        }
    }
}