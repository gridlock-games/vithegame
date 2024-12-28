using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using Vi.Utility;
using MagicaCloth2;

namespace Vi.Core
{
    [DisallowMultipleComponent]
    public class GlowRenderer : MonoBehaviour
    {
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

        private bool isBearer;
        public void RenderIsBearer(bool isBearer)
        {
            this.isBearer = isBearer;
        }

        private void OnEnable()
        {
            lastBlockTime = -5;
            lastHealTime = -5;
            lastHitTime = -5;
            isInvincible = false;
            isUninterruptable = false;
            canFlashAttack = false;
            currentColor = default;
            lastColor = default;
            isBearer = false;
        }

        public void RegisterChildRenderers()
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                RegisterRenderer(renderer);
            }
        }

        private Dictionary<Renderer, List<Material>> glowMaterialInstances = new Dictionary<Renderer, List<Material>>();

        public void RegisterRenderer(Renderer renderer)
        {
            if (glowMaterialInstances.ContainsKey(renderer)) { return; }
            if (renderer.GetComponent<MagicaCloth>()) { return; }

            NetworkObject netObj = GetComponentInParent<NetworkObject>();
            if (!netObj) { return; }

            if (renderer.TryGetComponent(out SkinnedMeshRenderer skinnedMeshRenderer))
            {
                skinnedMeshRenderer.updateWhenOffscreen = netObj.IsLocalPlayer;
            }

            foreach (Material m in renderer.materials)
            {
                if (m.HasColor(_GlowColor))
                {
                    m.SetColor(_GlowColor, defaultColor);
                    if (glowMaterialInstances.ContainsKey(renderer))
                    {
                        glowMaterialInstances[renderer].Add(m);
                    }
                    else
                    {
                        glowMaterialInstances.Add(renderer, new List<Material>() { m });
                    }
                }
            }
        }

        public void UnregisterRenderer(Renderer renderer)
        {
            if (!glowMaterialInstances.ContainsKey(renderer)) { return; }
            
            List<Material> newMatList = renderer.materials.ToList();
            foreach (Material glowMaterialInstance in glowMaterialInstances[renderer])
            {
                newMatList.Remove(glowMaterialInstance);
                Destroy(glowMaterialInstance);
            }
            renderer.materials = newMatList.ToArray();

            glowMaterialInstances.Remove(renderer);
        }

        public void UnregisterAllRenderers()
        {
            foreach (Renderer r in glowMaterialInstances.Keys.ToArray())
            {
                UnregisterRenderer(r);
            }
        }

        private readonly Color invincibleColor = new Color(1, 1, 0);
        private readonly Color uninterruptableColor = new Color(1, 1, 1);
        private readonly Color hitColor = new Color(1, 0, 0);
        private readonly Color healColor = new Color(0, 1, 0);
        private readonly Color blockColor = new Color(0, 0, 1);
        private readonly Color flashAttackColor = new Color(239 / (float)255, 91 / (float)255, 37 / (float)255);
        private readonly Color bearerColor = new Color(239f / 255, 147f / 255, 39f / 255);

        private readonly Color defaultColor = new Color(0, 0, 0, 0);
        private const float colorChangeSpeed = 40;

        private Color currentColor;
        private Color lastColor;

        private void Update()
        {
            Color colorTarget = defaultColor;

            if (Time.time - lastHitTime < 0.25f)
            {
                colorTarget = hitColor;
            }
            else if (isInvincible)
            {
                colorTarget = invincibleColor;
            }
            else if (isUninterruptable)
            {
                colorTarget = uninterruptableColor;
            }
            else if (Time.time - lastHealTime < 0.25f)
            {
                colorTarget = healColor;
            }
            else if (Time.time - lastBlockTime < 0.25f)
            {
                colorTarget = blockColor;
            }
            else if (canFlashAttack)
            {
                colorTarget = flashAttackColor;
            }
            else if (isBearer)
            {
                colorTarget = bearerColor;
            }

            float emissivePower = 2;
            if (isBearer)
            {
                emissivePower = 5;
            }

            currentColor = Vector4.MoveTowards(currentColor, colorTarget, colorChangeSpeed * Time.deltaTime);
            if (lastColor != currentColor)
            {
                foreach (List<Material> materialList in glowMaterialInstances.Values)
                {
                    foreach (Material glowMaterialInstance in materialList)
                    {
                        glowMaterialInstance.SetColor(_GlowColor, currentColor);
                        glowMaterialInstance.SetFloat(_EmissivePower, emissivePower);
                    }
                }
            }
            
            lastColor = currentColor;
        }

        private readonly int _GlowColor = Shader.PropertyToID("_GlowColor");
        private readonly int _EmissivePower = Shader.PropertyToID("_GlowEmissivePower");
    }
}