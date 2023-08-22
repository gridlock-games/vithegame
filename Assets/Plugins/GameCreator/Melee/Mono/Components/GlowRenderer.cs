using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCreator.Melee
{
    public class GlowRenderer : MonoBehaviour
    {
        [SerializeField] private Material glowMaterial;

        private const float colorChangeSpeed = 2;

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

        private float lastUninterruptable = -5;
        public void RenderUninterruptable()
        {
            lastUninterruptable = Time.time;
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

        private List<Material> glowMaterialInstances = new List<Material>();
        private bool allowRender;

        private void Start()
        {
            allowRender = SceneManager.GetActiveScene().name != "Hub";
            if (!allowRender) { return; }

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
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
        }

        private Color defaultColor = new Color(0, 0, 0, 0);
        private void Update()
        {
            if (!allowRender) { return; }

            // Invincible
            if (isInvincible)
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.color = new Color(1, 1, 0);
                }
                return;
            }
            else
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.color = Color.Lerp(glowMaterialInstance.color, defaultColor, colorChangeSpeed * Time.deltaTime);
                }
            }

            // UnInterruptable
            if (isUninterruptable)
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.color = new Color(1, 1, 1);
                }
                return;
            }
            else
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.color = Color.Lerp(glowMaterialInstance.color, defaultColor, colorChangeSpeed * Time.deltaTime);
                }
            }

            // Hit
            if (Time.time - lastHitTime < 0.25f)
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.color = new Color(1, 0, 0);
                }
                return;
            }
            else
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.color = Color.Lerp(glowMaterialInstance.color, defaultColor, colorChangeSpeed * Time.deltaTime);
                }
            }

            // Heal
            if (Time.time - lastHealTime < 0.25f)
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.color = new Color(0, 1, 0);
                }
                return;
            }
            else
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.color = Color.Lerp(glowMaterialInstance.color, defaultColor, colorChangeSpeed * Time.deltaTime);
                }
            }

            // Block
            if (Time.time - lastBlockTime < 0.25f)
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.color = new Color(0, 0, 1);
                }
                return;
            }
            else
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.color = Color.Lerp(glowMaterialInstance.color, defaultColor, colorChangeSpeed * Time.deltaTime);
                }
            }

            // Uninterruptable
            if (Time.time - lastUninterruptable < 0.25f)
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.color = new Color(1, 1, 1);
                }
                return;
            }
            else
            {
                foreach (Material glowMaterialInstance in glowMaterialInstances)
                {
                    glowMaterialInstance.color = Color.Lerp(glowMaterialInstance.color, defaultColor, colorChangeSpeed * Time.deltaTime);
                }
            }
        }
    }
}