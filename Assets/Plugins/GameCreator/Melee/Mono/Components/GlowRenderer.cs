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

        private bool isInvincible;
        public void RenderInvincible(bool isInvincible)
        {
            this.isInvincible = isInvincible;
        }

        public void RenderUninterruptable(bool isUninterruptable)
        {

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
                materialList.Add(glowMaterial);
                renderer.materials = materialList.ToArray();
                glowMaterialInstances.Add(renderer.materials[^1]);
            }
        }

        private void Update()
        {
            if (!allowRender) { return; }

            if (GetComponentInParent<Unity.Netcode.NetworkObject>().IsLocalPlayer)
            {
                Debug.Log(isInvincible);
            }

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
                    glowMaterialInstance.color = Color.Lerp(glowMaterialInstance.color, new Color(0, 0, 0), colorChangeSpeed * Time.deltaTime);
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
                    glowMaterialInstance.color = Color.Lerp(glowMaterialInstance.color, new Color(0, 0, 0), colorChangeSpeed * Time.deltaTime);
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
                    glowMaterialInstance.color = Color.Lerp(glowMaterialInstance.color, new Color(0, 0, 0), colorChangeSpeed * Time.deltaTime);
                }
            }
        }
    }
}