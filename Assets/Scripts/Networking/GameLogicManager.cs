using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace LightPat.Core
{
    public class GameLogicManager : NetworkBehaviour
    {
        public TeamSpawnPoint[] spawnPoints;

        private void OnDrawGizmos()
        {
            foreach (TeamSpawnPoint spawnPoint in spawnPoints)
            {
                Gizmos.color = (Color)typeof(Color).GetProperty(spawnPoint.team.ToString().ToLowerInvariant()).GetValue(null, null);
                Gizmos.DrawWireSphere(spawnPoint.spawnPosition, 2);
                Gizmos.DrawRay(spawnPoint.spawnPosition, Quaternion.Euler(spawnPoint.spawnRotation) * Vector3.forward * 5);
            }
        }
    }

    [System.Serializable]
    public class TeamSpawnPoint
    {
        public Team team;
        public Vector3 spawnPosition;
        public Vector3 spawnRotation;
    }

    public enum Team
    {
        Environment,
        Red,
        Blue
    }

    public enum GameMode
    {
        Duel,
        TeamDeathmatch
    }
}