using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;
using System.Linq;
using UnityEngine.SceneManagement;

namespace LightPat.Core
{
    public class GameLogicManager : NetworkBehaviour
    {
        private TeamSpawnManager spawnManager;

        public virtual void OnPlayerKill(ulong killerClientId) { }

        public virtual void OnPlayerDeath(ulong deathClientId) { }

        private Dictionary<Team, int> spawnCountDict = new Dictionary<Team, int>();
        public KeyValuePair<Vector3, Quaternion> GetSpawnOrientation(Team team)
        {
            if (GetComponent<TeamSpawnManager>()) { spawnManager = GetComponent<TeamSpawnManager>(); }

            if (!spawnCountDict.ContainsKey(team))
                spawnCountDict.Add(team, 0);

            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;

            bool spawnPointFound = false;
            TeamSpawnPoint[] spawnPoints = spawnManager.GetSpawnPoints(ClientManager.Singleton.gameMode.Value);
            foreach (TeamSpawnPoint teamSpawnPoint in spawnPoints)
            {
                if (teamSpawnPoint.team == team)
                {
                    spawnPointFound = true;

                    // If we are out of bounds with our spawn count, reset the count
                    if (spawnCountDict[team] >= teamSpawnPoint.spawnPositions.Length)
                        spawnCountDict[team] = 0;
                    
                    spawnPosition = teamSpawnPoint.spawnPositions[spawnCountDict[team]];
                    spawnRotation = Quaternion.Euler(teamSpawnPoint.spawnRotation);
                    break;
                }
            }

            if (!spawnPointFound)
            {
                Debug.LogError("No spawn point found for team: " + team + ". It will use the first spawn point in the list");

                if (spawnPoints.Length > 1)
                {
                    spawnPosition = spawnPoints[0].spawnPositions[0];
                    spawnRotation = Quaternion.Euler(spawnPoints[0].spawnRotation);
                }
                else
                {
                    Debug.LogError("Game Logic Manager's spawn point array length is less than 1");
                }
            }

            spawnCountDict[team] += 1;
            return new KeyValuePair<Vector3, Quaternion>(spawnPosition, spawnRotation);
        }

        protected void ResetSpawnCounts()
        {
            Team[] teamKeys = spawnCountDict.Keys.ToArray();
            foreach (Team team in teamKeys)
            {
                spawnCountDict[team] = 0;
            }
        }

        private void Start()
        {
            SceneManager.sceneLoaded += FindSpawnPoints;
        }

        private new void OnDestroy()
        {
            SceneManager.sceneLoaded -= FindSpawnPoints;
            base.OnDestroy();
        }

        private void FindSpawnPoints(Scene scene, LoadSceneMode mode)
        {
            foreach (GameObject g in scene.GetRootGameObjects())
            {
                if (g.TryGetComponent(out TeamSpawnManager spawnManager))
                {
                    this.spawnManager = spawnManager;
                    break;
                }
            }
        }
    }

    public enum Team
    {
        Environment,
        Competitor,
        Red,
        Blue,
        Spectator
    }

    public enum GameMode
    {
        Duel,
        TeamElimination,
        TeamDeathmatch
    }
}