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

        public Color teamColor { get; private set; } = Color.red;

        private GameObject indicatorInstance;

        private void Update()
        {
            if (ClientManager.Singleton)
            {
                // If the local client isn't in the client manager, don't do anything
                if (!ClientManager.Singleton.GetClientDataDictionary().ContainsKey(NetworkManager.LocalClientId)) return;

                // If we are in a team game mode, create an indicator instance, otherwise destroy it
                GameMode[] teamGameModes = new GameMode[] { GameMode.TeamElimination, GameMode.TeamDeathmatch };
                if (teamGameModes.Contains(ClientManager.Singleton.gameMode.Value))
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

                Team localClientTeam = ClientManager.Singleton.GetClient(NetworkManager.LocalClientId).team;
                if (IsSpawned) // If we are spawned, meaning we are in gameplay
                {
                    if (!ClientManager.Singleton.GetClientDataDictionary().ContainsKey(OwnerClientId)) return;

                    Team ownerClientTeam = ClientManager.Singleton.GetClient(OwnerClientId).team;

                    foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
                    {
                        foreach (Material mat in renderer.materials)
                        {
                            if (mat.HasProperty("_Glow"))
                            {
                                // If we are spectating, just make the colors be team based
                                if (localClientTeam == Team.Spectator)
                                {
                                    if (ownerClientTeam == Team.Red)
                                    {
                                        mat.SetColor("_Color", Color.red);
                                        mat.SetFloat("_Glow", glowAmount);
                                        teamColor = Color.red;
                                    }
                                    else if (ownerClientTeam == Team.Blue)
                                    {
                                        mat.SetColor("_Color", Color.blue);
                                        mat.SetFloat("_Glow", glowAmount);
                                        teamColor = Color.blue;
                                    }
                                    else
                                    {
                                        if (indicatorInstance)
                                        {
                                            Destroy(indicatorInstance);
                                        }
                                    }
                                }
                                else // If we are a player, make the colors be relative
                                {
                                    if (ownerClientTeam == localClientTeam)
                                    {
                                        mat.SetColor("_Color", IsLocalPlayer ? Color.white : Color.cyan);
                                        mat.SetFloat("_Glow", glowAmount);
                                        teamColor = IsLocalPlayer ? Color.white : Color.cyan;
                                    }
                                    else
                                    {
                                        mat.SetColor("_Color", Color.red);
                                        mat.SetFloat("_Glow", glowAmount);
                                        teamColor = Color.red;
                                    }
                                }
                            }
                        }
                    }
                }
                else // If we are not spawned, meaning we are at the lobby
                {
                    foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
                    {
                        foreach (Material mat in renderer.materials)
                        {
                            if (mat.HasProperty("_Glow"))
                            {
                                if (localClientTeam == Team.Red)
                                {
                                    mat.SetColor("_Color", Color.red);
                                    mat.SetFloat("_Glow", glowAmount);
                                    teamColor = Color.red;
                                }
                                else if (localClientTeam == Team.Blue)
                                {
                                    mat.SetColor("_Color", Color.blue);
                                    mat.SetFloat("_Glow", glowAmount);
                                    teamColor = Color.blue;
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
}
