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

        private Color defaultColor = new Color(0, 0, 0, 0);
        private float defaultFresnelPower = 5;
        private void Update()
        {
            // Invincible
            if (isInvincible)
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.SetFloat("_FresnelPower", fresnelPower);
                    glowMaterialInstance.color = new Color(1, 1, 0);
                }
                return;
            }
            else
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.SetFloat("_FresnelPower", Mathf.Lerp(glowMaterialInstance.GetFloat("_FresnelPower"), defaultFresnelPower, colorChangeSpeed * Time.deltaTime));
                    glowMaterialInstance.color = Color.Lerp(glowMaterialInstance.color, defaultColor, colorChangeSpeed * Time.deltaTime);
                }
            }

            // UnInterruptable
            if (isUninterruptable)
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.SetFloat("_FresnelPower", fresnelPower);
                    glowMaterialInstance.color = new Color(1, 1, 1);
                }
                return;
            }
            else
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.SetFloat("_FresnelPower", Mathf.Lerp(glowMaterialInstance.GetFloat("_FresnelPower"), defaultFresnelPower, colorChangeSpeed * Time.deltaTime));
                    glowMaterialInstance.color = Color.Lerp(glowMaterialInstance.color, defaultColor, colorChangeSpeed * Time.deltaTime);
                }
            }

            // Hit
            if (Time.time - lastHitTime < 0.25f)
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.SetFloat("_FresnelPower", fresnelPower);
                    glowMaterialInstance.color = new Color(1, 0, 0);
                }
                return;
            }
            else
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.SetFloat("_FresnelPower", Mathf.Lerp(glowMaterialInstance.GetFloat("_FresnelPower"), defaultFresnelPower, colorChangeSpeed * Time.deltaTime));
                    glowMaterialInstance.color = Color.Lerp(glowMaterialInstance.color, defaultColor, colorChangeSpeed * Time.deltaTime);
                }
            }

            // Heal
            if (Time.time - lastHealTime < 0.25f)
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.SetFloat("_FresnelPower", fresnelPower);
                    glowMaterialInstance.color = new Color(0, 1, 0);
                }
                return;
            }
            else
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.SetFloat("_FresnelPower", Mathf.Lerp(glowMaterialInstance.GetFloat("_FresnelPower"), defaultFresnelPower, colorChangeSpeed * Time.deltaTime));
                    glowMaterialInstance.color = Color.Lerp(glowMaterialInstance.color, defaultColor, colorChangeSpeed * Time.deltaTime);
                }
            }

            // Block
            if (Time.time - lastBlockTime < 0.25f)
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.SetFloat("_FresnelPower", fresnelPower);
                    glowMaterialInstance.color = new Color(0, 0, 1);
                }
                return;
            }
            else
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.SetFloat("_FresnelPower", Mathf.Lerp(glowMaterialInstance.GetFloat("_FresnelPower"), defaultFresnelPower, colorChangeSpeed * Time.deltaTime));
                    glowMaterialInstance.color = Color.Lerp(glowMaterialInstance.color, defaultColor, colorChangeSpeed * Time.deltaTime);
                }
            }

            // Flash Attack
            if (canFlashAttack)
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.SetFloat("_FresnelPower", fresnelPower);
                    glowMaterialInstance.color = new Color(1, 128 / 255, 0);
                }
                return;
            }
            else
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.SetFloat("_FresnelPower", Mathf.Lerp(glowMaterialInstance.GetFloat("_FresnelPower"), defaultFresnelPower, colorChangeSpeed * Time.deltaTime));
                    glowMaterialInstance.color = Color.Lerp(glowMaterialInstance.color, defaultColor, colorChangeSpeed * Time.deltaTime);
                }
            }
        }
    }
}