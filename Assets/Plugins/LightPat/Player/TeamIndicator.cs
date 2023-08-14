using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;
using Unity.Netcode;
using System.Linq;

namespace LightPat.Player
{
    public class TeamIndicator : NetworkBehaviour
    {
        public GameObject indicatorPrefab;
        public Vector3 indicatorLocalPosition;
        public float glowAmount = 1;

        private GameObject indicatorInstance;

        private void Update()
        {
            if (ClientManager.Singleton)
            {
                ulong targetClientId = IsSpawned ? OwnerClientId : NetworkManager.Singleton.LocalClientId;

                if (!ClientManager.Singleton.GetClientDataDictionary().ContainsKey(targetClientId)) { return; }

                GameMode[] teamGameModes = new GameMode[] { GameMode.TeamElimination, GameMode.TeamDeathmatch };
                Team team = ClientManager.Singleton.GetClient(targetClientId).team;
                if (teamGameModes.Contains(ClientManager.Singleton.gameMode.Value) & (team == Team.Red | team == Team.Blue))
                {
                    if (!indicatorInstance)
                    {
                        indicatorInstance = Instantiate(indicatorPrefab, transform);
                        indicatorInstance.transform.localPosition = indicatorLocalPosition;
                    }
                }
                else
                {
                    if (indicatorInstance)
                    {
                        Destroy(indicatorInstance);
                    }
                }
                
                foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
                {
                    foreach (Material mat in renderer.materials)
                    {
                        if (mat.HasProperty("_Glow"))
                        {
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
                                if (indicatorInstance)
                                {
                                    Destroy(indicatorInstance);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
