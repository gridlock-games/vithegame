using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;

namespace Vi.UI
{
    public class TeamIndicator : MonoBehaviour
    {
        private const float glowAmount = 2.46f;

        private Attributes attributes;
        private Renderer[] renderers;
        private void Start()
        {
            attributes = GetComponentInParent<Attributes>();
            renderers = GetComponentsInChildren<Renderer>();

            for (int i = 0; i < renderers.Length; i++)
            {
                List<Material> materials = new List<Material>();
                renderers[i].GetMaterials(materials);
                materials = materials.FindAll(item => item.HasProperty(_Glow));
                rendererMaterialCache.Add(i, materials);
            }

            Update();
        }

        private readonly int _Glow = Shader.PropertyToID("_Glow");
        private readonly int _Transparency = Shader.PropertyToID("_Transparency");

        Dictionary<int, List<Material>> rendererMaterialCache = new Dictionary<int, List<Material>>();
        private void Update()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = attributes.IsSpawned;
                if (!renderers[i].enabled) { continue; }

                foreach (Material mat in rendererMaterialCache[i])
                {
                    if (PlayerDataManager.Singleton.ContainsId(attributes.GetPlayerDataId()))
                    {
                        mat.color = attributes.GetRelativeTeamColor();
                        mat.SetFloat(_Transparency, attributes.GetTeam() == PlayerDataManager.Team.Competitor | attributes.GetTeam() == PlayerDataManager.Team.Peaceful ? 0 : 1);
                    }
                    else
                    {
                        mat.color = Color.black;
                        mat.SetFloat(_Transparency, 0);
                    }
                    mat.SetFloat(_Glow, glowAmount);
                }
                renderers[i].enabled = attributes.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death;
            }
        }
    }
}