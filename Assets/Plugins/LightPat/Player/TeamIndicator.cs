using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using Unity.Netcode;

namespace LightPat.Player
{
    public class TeamIndicator : MonoBehaviour
    {
        public GameObject indicatorPrefab;
        public Vector3 indicatorLocalPosition;
        public float glowAmount = 1;

        private Color glowColor = Color.blue;

        private void Start()
        {
            GameObject indicatorInstance = Instantiate(indicatorPrefab, transform);
            indicatorInstance.transform.localPosition = indicatorLocalPosition;
        }

        private void Update()
        {
            if (ClientManager.Singleton)
            {
                if (!ClientManager.Singleton.GetClientDataDictionary().ContainsKey(NetworkManager.Singleton.LocalClientId)) { return; }

                foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
                {
                    foreach (Material mat in renderer.materials)
                    {
                        if (mat.HasProperty("_Glow"))
                        {
                            Team team = ClientManager.Singleton.GetClient(NetworkManager.Singleton.LocalClientId).team;
                            if (team == Team.Red)
                            {
                                mat.SetColor("_Color", Color.red);
                                mat.SetFloat("_Glow", glowAmount);
                            }
                            else if (team == Team.Blue)
                            {
                                mat.SetColor("_Color", Color.blue);
                                mat.SetFloat("_Glow", glowAmount);
                            }
                            else
                            {
                                mat.SetColor("_Color", Color.black);
                                mat.SetFloat("_Glow", 0);
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
                {
                    foreach (Material mat in renderer.materials)
                    {
                        if (mat.HasProperty("_Glow"))
                        {
                            mat.SetColor("_Color", glowColor);
                            mat.SetFloat("_Glow", glowAmount);
                        }
                    }
                }
            }
        }
    }
}
