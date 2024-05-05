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

            Update();
        }

        private readonly int _Glow = Shader.PropertyToID("_Glow");
        private readonly int _Transparency = Shader.PropertyToID("_Transparency");

        private void Update()
        {
            foreach (Renderer r in renderers)
            {
                r.enabled = attributes.IsSpawned;

                foreach (Material mat in r.materials)
                {
                    if (mat.HasProperty("_Glow"))
                    {
                        //mat.color = attributes.GetRelativeTeamColor();
                        
                        if (PlayerDataManager.Singleton.ContainsId(attributes.GetPlayerDataId()))
                            mat.color = PlayerDataManager.GetTeamColor(attributes.GetTeam());
                        else
                            mat.color = Color.black;

                        mat.SetFloat(_Glow, glowAmount);
                        mat.SetFloat(_Transparency, mat.color == Color.black ? 0 : 1);
                    }
                }
                r.enabled = attributes.GetAilment() != ScriptableObjects.ActionClip.Ailment.Death;
            }
        }
    }
}