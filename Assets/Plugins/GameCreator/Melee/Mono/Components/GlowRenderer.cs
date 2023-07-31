using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace GameCreator.Melee
{
    public class GlowRenderer : MonoBehaviour
    {
        [SerializeField] private Material glowMaterial;

        private List<Material> glowMaterialInstances = new List<Material>();

        private void Start()
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                List<Material> materialList = new List<Material>();
                //List<Material> materialList = skinnedMeshRenderer.materials.ToList();

                // Add the same number of glow materials as there are materials on this renderer
                //for (int i = 0; i < materialList.Count; i++)
                //{
                //    //materialList.Add(glowMaterial);
                //    //skinnedMeshRenderer.materials = materialList.ToArray();
                //    //glowMaterialInstances.Add(skinnedMeshRenderer.materials[^1]);
                //}

                foreach (Material m in renderer.materials)
                {
                    materialList.Add(m);
                }
                materialList.Add(glowMaterial);
                renderer.materials = materialList.ToArray();
                glowMaterialInstances.Add(renderer.materials[^1]);
            }
        }

        const float RedCyclesPerSecond = 0.5f;
        float red;

        private void Update()
        {
            red += RedCyclesPerSecond * Time.deltaTime;
            if (red > 1.0f)
            {
                red = 0.0f;
            }

            foreach (Material glowMaterialInstance in glowMaterialInstances)
            {
                glowMaterialInstance.color = new Color(red, 0, 0, 0);
            }
        }
    }
}