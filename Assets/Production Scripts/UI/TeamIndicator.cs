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
        }

        private void Update()
        {
            foreach (Renderer r in renderers)
            {
                foreach (Material mat in r.materials)
                {
                    if (mat.HasProperty("_Glow"))
                    {
                        mat.color = attributes.GetRelativeTeamColor();
                        mat.SetFloat("_Glow", glowAmount);
                        mat.SetFloat("_Transparency", mat.color == Color.black ? 0 : 1);
                    }
                }
            }
        }
    }
}