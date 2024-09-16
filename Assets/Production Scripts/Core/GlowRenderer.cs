using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using Vi.Utility;

namespace Vi.Core
{
    [DisallowMultipleComponent]
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

        private void Start()
        {
            if (!GetComponent<PooledObject>())
            {
                foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
                {
                    RegisterNewRenderer(renderer);
                }
            }
        }

        private Dictionary<Renderer, List<Material>> glowMaterialInstances = new Dictionary<Renderer, List<Material>>();

        public void RegisterNewRenderer(Renderer renderer)
        {
            NetworkObject netObj = GetComponentInParent<NetworkObject>();
            if (!netObj.IsSpawned) { return; }

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

        private readonly Color invincibleColor = new Color(1, 1, 0);
        private readonly Color uninterruptableColor = new Color(1, 1, 1);
        private readonly Color hitColor = new Color(1, 0, 0);
        private readonly Color healColor = new Color(0, 1, 0);
        private readonly Color blockColor = new Color(0, 0, 1);
        private readonly Color flashAttackColor = new Color(239 / (float)255, 91 / (float)255, 37 / (float)255);

        private readonly Color defaultColor = new Color(0, 0, 0, 0);
        private const float defaultFresnelPower = 5;

        private float currentFresnelPower;
        private float lastFresnelPower;

        private Color lastColor;
        private void Update()
        {
            Color colorTarget = defaultColor;
            float fresnelPowerTarget = defaultFresnelPower;

            if (isInvincible)
            {
                colorTarget = invincibleColor;
                fresnelPowerTarget = fresnelPower;
            }
            else if (isUninterruptable)
            {
                colorTarget = uninterruptableColor;
                fresnelPowerTarget = fresnelPower;
            }
            else if (Time.time - lastHitTime < 0.25f)
            {
                colorTarget = hitColor;
                fresnelPowerTarget = fresnelPower;
            }
            else if (Time.time - lastHealTime < 0.25f)
            {
                colorTarget = healColor;
                fresnelPowerTarget = fresnelPower;
            }
            else if (Time.time - lastBlockTime < 0.25f)
            {
                colorTarget = blockColor;
                fresnelPowerTarget = fresnelPower;
            }
            else if (canFlashAttack)
            {
                colorTarget = flashAttackColor;
                fresnelPowerTarget = fresnelPower;
            }
            
            currentFresnelPower = Mathf.MoveTowards(currentFresnelPower, fresnelPowerTarget, colorChangeSpeed * Time.deltaTime);
            if (!Mathf.Approximately(currentFresnelPower, lastFresnelPower) | lastColor != colorTarget)
            {
                foreach (List<Material> materialList in glowMaterialInstances.Values)
                {
                    foreach (Material glowMaterialInstance in materialList)
                    {
                        if (!Mathf.Approximately(currentFresnelPower, lastFresnelPower))
                            glowMaterialInstance.SetFloat(_FresnelPower, currentFresnelPower);

                        if (lastColor != colorTarget)
                            glowMaterialInstance.SetColor(_Color, colorTarget);
                    }
                }
            }
            
            lastFresnelPower = currentFresnelPower;
            lastColor = colorTarget;
        }

        private readonly int _FresnelPower = Shader.PropertyToID("_FresnelPower");
        private readonly int _Color = Shader.PropertyToID("_Color");
    }
}