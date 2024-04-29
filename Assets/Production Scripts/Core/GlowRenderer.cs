using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Vi.Core
{
    public class GlowRenderer : MonoBehaviour
    {
        [SerializeField] private Material glowMaterial;

        private const float colorChangeSpeed = 2;
        private const float fresnelPower = 2.0f;

        private float lastHitTime = -5;
        public void RenderHit()
        {
            lastHitTime = Time.time;
        }

        private float lastHealTime = -5;
        public void RenderHeal()
        {
            lastHealTime = Time.time;
        }

        private bool isInvincible;
        public void RenderInvincible(bool isInvincible)
        {
            this.isInvincible = isInvincible;
        }

        private bool isUninterruptable;
        public void RenderUninterruptable(bool isUninterruptable)
        {
            this.isUninterruptable = isUninterruptable;
        }

        private float lastBlockTime = -5;
        public void RenderBlock()
        {
            lastBlockTime = Time.time;
        }

        private bool canFlashAttack;
        public void RenderFlashAttack(bool canFlashAttack)
        {
            this.canFlashAttack = canFlashAttack;
        }

        private List<Material> glowMaterialInstances = new List<Material>();

        private void Start()
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                RegisterNewRenderer(renderer);
            }
        }

        public void RegisterNewRenderer(Renderer renderer)
        {
            NetworkObject netObj = GetComponentInParent<NetworkObject>();

            if (renderer.TryGetComponent(out SkinnedMeshRenderer skinnedMeshRenderer))
            {
                skinnedMeshRenderer.updateWhenOffscreen = netObj.IsLocalPlayer;
            }

            List<Material> materialList = new List<Material>();

            foreach (Material m in renderer.materials)
            {
                materialList.Add(m);
            }

            int materialCount = 1;
            for (int i = 0; i < materialCount; i++)
            {
                materialList.Add(glowMaterial);
            }

            renderer.materials = materialList.ToArray();

            foreach (Material m in renderer.materials)
            {
                if (m.name.Replace("(Instance)", "").Trim() == glowMaterial.name.Trim())
                {
                    glowMaterialInstances.Add(m);
                }
            }
        }

        private readonly Color defaultColor = new Color(0, 0, 0, 0);
        private const float defaultFresnelPower = 5;
        private void Update()
        {
            Color colorTarget = defaultColor;
            float fresnelPowerTarget = defaultFresnelPower;

            if (isInvincible)
            {
                colorTarget = new Color(1, 1, 0);
                fresnelPowerTarget = fresnelPower;
            }
            else if (isUninterruptable)
            {
                colorTarget = new Color(1, 1, 1);
                fresnelPowerTarget = fresnelPower;
            }
            else if (Time.time - lastHitTime < 0.25f)
            {
                colorTarget = new Color(1, 0, 0);
                fresnelPowerTarget = fresnelPower;
            }
            else if (Time.time - lastHealTime < 0.25f)
            {
                colorTarget = new Color(0, 1, 0);
                fresnelPowerTarget = fresnelPower;
            }
            else if (Time.time - lastBlockTime < 0.25f)
            {
                colorTarget = new Color(0, 0, 1);
                fresnelPowerTarget = fresnelPower;
            }
            else if (canFlashAttack)
            {
                colorTarget = new Color(239 / (float)255, 91 / (float)255, 37 / (float)255);
                fresnelPowerTarget = fresnelPower;
            }

            foreach (Material glowMaterialInstance in glowMaterialInstances)
            {
                glowMaterialInstance.SetFloat("_FresnelPower", Mathf.Lerp(glowMaterialInstance.GetFloat("_FresnelPower"), fresnelPowerTarget, colorChangeSpeed * Time.deltaTime));
                //glowMaterialInstance.color = Color.Lerp(glowMaterialInstance.color, colorTarget, colorChangeSpeed * Time.deltaTime);
                if (glowMaterialInstance.color != colorTarget) { glowMaterialInstance.color = colorTarget; }
            }
        }
    }
}