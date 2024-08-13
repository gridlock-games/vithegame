using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.Core.CombatAgents;

namespace Vi.UI
{
    public class TeamIndicator : MonoBehaviour
    {
        private const float glowAmount = 2.46f;

        private CombatAgent combatAgent;
        private Renderer[] renderers;
        private void Start()
        {
            combatAgent = GetComponentInParent<CombatAgent>();
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
                renderers[i].enabled = combatAgent.IsSpawned;
                if (!renderers[i].enabled) { continue; }

                foreach (Material mat in rendererMaterialCache[i])
                {
                    if (combatAgent.IsSpawned)
                    {
                        mat.color = combatAgent.GetRelativeTeamColor();
                        mat.SetFloat(_Transparency, combatAgent.GetTeam() == PlayerDataManager.Team.Competitor | combatAgent.GetTeam() == PlayerDataManager.Team.Peaceful ? 0 : 1);
                    }
                    else
                    {
                        mat.color = Color.black;
                        mat.SetFloat(_Transparency, 0);
                    }
                    mat.SetFloat(_Glow, glowAmount);
                }
                renderers[i].enabled = combatAgent.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death;
            }
        }
    }
}