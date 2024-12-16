using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Core.CombatAgents;
using Vi.Core.Structures;
using Vi.Utility;
using Vi.ScriptableObjects;

namespace Vi.Core.GameModeManagers
{
    public class EssenceWarManager : GameModeManager
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                roundResultMessage.Value = "Essence War Starting! ";
            }

            bearerId.OnValueChanged += OnBearerIdChanged;
            lightScore.OnValueChanged += OnScoreChanged;
            corruptionScore.OnValueChanged += OnScoreChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            bearerId.OnValueChanged -= OnBearerIdChanged;
            lightScore.OnValueChanged -= OnScoreChanged;
            corruptionScore.OnValueChanged -= OnScoreChanged;
        }

        protected override void Start()
        {
            base.Start();
            playerList = PlayerDataManager.Singleton.GetActivePlayerObjects();
        }

        private List<Attributes> playerList = new List<Attributes>();

        private float lastWaveSpawnTime = Mathf.NegativeInfinity;
        private float lastOgreSpawnEventTime = Mathf.NegativeInfinity;
        private Mob ogreMobInstance;
        protected override void Update()
        {
            if (PlayerDataManager.Singleton.LocalPlayersWasUpdatedThisFrame)
            {
                playerList = PlayerDataManager.Singleton.GetActivePlayerObjects();
            }

            base.Update();
            if (!IsSpawned) { return; }

            if (IsServer)
            {
                if (!ShouldDisplayNextGameAction() & !IsGameOver())
                {
                    if (Time.time - lastWaveSpawnTime > 30)
                    {
                        if (SpawnWave())
                        {
                            lastWaveSpawnTime = Time.time;
                        }
                    }

                    if (IsViEssenceSpawned() | HasBearer())
                    {
                        lastOgreSpawnEventTime = Time.time;
                    }
                    else if (ogreMobInstance)
                    {
                        if (ogreMobInstance.IsSpawned)
                        {
                            lastOgreSpawnEventTime = Time.time;
                        }
                        else if (!ogreMobInstance.gameObject.activeInHierarchy)
                        {
                            ogreMobInstance = null;
                        }
                    }
                    else
                    {
                        if (Time.time - lastOgreSpawnEventTime > 40)
                        {
                            ogreMobInstance = SpawnMob(kingOgreMob, PlayerDataManager.Team.Environment, false);
                            lastOgreSpawnEventTime = Time.time;
                        }
                    }
                }
            }

            if (TryGetBearerInstance(out Attributes bearer))
            {
                foreach (Attributes player in playerList)
                {
                    if (player == bearer)
                    {
                        // Set this to the friendly structure
                        player.MovementHandler.ObjectiveHandler.SetObjective(null);
                        continue;
                    }

                    player.MovementHandler.ObjectiveHandler.SetObjective(bearer.MovementHandler.ObjectiveHandler);
                }
            }
            else if (TryGetViEssenceInstance(out EssenceWarViEssence essenceWarViEssenceInstance))
            {
                foreach (Attributes player in playerList)
                {
                    player.MovementHandler.ObjectiveHandler.SetObjective(essenceWarViEssenceInstance.ObjectiveHandler);
                }
            }
            else
            {
                foreach (Attributes player in playerList)
                {
                    player.MovementHandler.ObjectiveHandler.SetObjective(null);
                }
            }
        }

        [Header("Essence War Specific")]
        [SerializeField] private Mob kingOgreMob;
        [SerializeField] private Mob spiderMinionMob;
        private bool SpawnWave()
        {
            if (!PlayerDataManager.Singleton.HasPlayerSpawnPoints()) { return false; }

            for (int i = 0; i < 5; i++)
            {
                SpawnMob(spiderMinionMob, PlayerDataManager.Team.Light, false);
                SpawnMob(spiderMinionMob, PlayerDataManager.Team.Corruption, false);
            }

            return true;
        }

        [SerializeField] private EssenceWarViEssence viEssencePrefab;
        public bool IsViEssenceSpawned() { return NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(viEssenceNetObjId.Value); }

        private bool TryGetViEssenceInstance(out EssenceWarViEssence essenceWarViEssenceInstance)
        {
            essenceWarViEssenceInstance = null;
            if (!NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(viEssenceNetObjId.Value)) { return false; }
            essenceWarViEssenceInstance = NetworkManager.SpawnManager.SpawnedObjects[viEssenceNetObjId.Value].GetComponent<EssenceWarViEssence>();
            return essenceWarViEssenceInstance != null;
        }

        private NetworkVariable<ulong> viEssenceNetObjId = new NetworkVariable<ulong>();
        private void SpawnViEssence(Vector3 position, Quaternion rotation)
        {
            if (!IsServer) { Debug.LogError("SpawnViEssence should only be called on the server!"); return; }
            EssenceWarViEssence viEssence = ObjectPoolingManager.SpawnObject(viEssencePrefab.GetComponent<PooledObject>(), position + new Vector3(0, 1, 0), rotation).GetComponent<EssenceWarViEssence>();
            viEssence.Initialize(this);
            viEssence.NetworkObject.Spawn(true);
            viEssenceNetObjId.Value = viEssence.NetworkObjectId;
        }

        public void OnViEssenceActivation(Attributes newBearer)
        {
            lastOgreSpawnEventTime = Time.time;
            bearerId.Value = newBearer.GetPlayerDataId();
            newBearer.StatusAgent.TryAddStatus(new ActionClip.StatusPayload(ActionClip.Status.movementSpeedDecrease, 0.35f, true, 10, 0, false));
        }

        private NetworkVariable<int> bearerId = new NetworkVariable<int>();

        private bool HasBearer()
        {
            return PlayerDataManager.Singleton.IdHasLocalPlayer(bearerId.Value);
        }

        public bool TryGetBearerInstance(out Attributes bearer)
        {
            if (PlayerDataManager.Singleton.IdHasLocalPlayer(bearerId.Value))
            {
                bearer = PlayerDataManager.Singleton.GetPlayerObjectById(bearerId.Value);
                return true;
            }
            bearer = null;
            return false;
        }

        private void RemoveBearer()
        {
            bearerId.Value = (int)NetworkManager.ServerClientId;
        }

        private void OnBearerIdChanged(int prev, int current)
        {
            if (TryGetBearerInstance(out Attributes bearer))
            {
                bearer.GlowRenderer.RenderIsBearer(true);
            }
            else if (PlayerDataManager.Singleton.IdHasLocalPlayer(prev))
            {
                PlayerDataManager.Singleton.GetPlayerObjectById(prev).GlowRenderer.RenderIsBearer(false);
            }
        }

        private NetworkVariable<int> lightScore = new NetworkVariable<int>();
        private NetworkVariable<int> corruptionScore = new NetworkVariable<int>();

        private void OnScoreChanged(int prev, int current)
        {
            onScoreListChanged?.Invoke();
        }

        public void OnBearerReachedTotem(PlayerDataManager.Team team)
        {
            if (!IsServer) { Debug.LogError("OnBearerReachedTotem should only be called on the server!"); return; }

            if (team == PlayerDataManager.Team.Light)
            {
                lightScore.Value++;
            }
            else if (team == PlayerDataManager.Team.Corruption)
            {
                corruptionScore.Value++;
            }
            else
            {
                Debug.LogWarning("Unsure how to handle team on bearer reached totem " + team);
            }
            RemoveBearer();
        }

        public override void OnEnvironmentKill(CombatAgent victim)
        {
            base.OnEnvironmentKill(victim);

            if (TryGetBearerInstance(out Attributes bearer))
            {
                if (bearer == victim)
                {
                    RemoveBearer();
                }
            }

            if (victim.GetName().ToUpper().Contains("KING OGRE"))
            {
                SpawnViEssence(victim.MovementHandler.GetPosition(), victim.MovementHandler.GetRotation());
            }
        }

        public override void OnPlayerKill(CombatAgent killer, CombatAgent victim)
        {
            base.OnPlayerKill(killer, victim);

            if (TryGetBearerInstance(out Attributes bearer))
            {
                if (bearer == victim)
                {
                    RemoveBearer();
                }
            }

            if (victim.GetName().ToUpper().Contains("KING OGRE"))
            {
                SpawnViEssence(victim.MovementHandler.GetPosition(), victim.MovementHandler.GetRotation());
            }
        }

        public override void OnStructureKill(CombatAgent killer, Structure structure)
        {
            base.OnStructureKill(killer, structure);
            if (gameOver.Value) { return; }

            if (structure.GetName().ToUpper().Contains("TOTEM"))
            {
                PlayerDataManager.Team winningTeam;
                if (structure.GetTeam() == PlayerDataManager.Team.Light)
                {
                    winningTeam = PlayerDataManager.Team.Corruption;
                }
                else if (structure.GetTeam() == PlayerDataManager.Team.Corruption)
                {
                    winningTeam = PlayerDataManager.Team.Light;
                }
                else
                {
                    Debug.LogWarning("Unsure how to handle structure team " + structure.GetTeam());
                    winningTeam = PlayerDataManager.Team.Environment;
                }

                List<int> winningIdList = new List<int>();
                foreach (Attributes player in PlayerDataManager.Singleton.GetPlayerObjectsOnTeam(winningTeam))
                {
                    winningIdList.Add(player.GetPlayerDataId());
                }
                OnRoundEnd(winningIdList.ToArray());
            }
            else
            {
                Debug.LogWarning("Unsure how to handle structure kill " + structure.GetName());
            }
        }

        protected override void OnRoundEnd(int[] winningPlayersDataIds)
        {
            base.OnRoundEnd(winningPlayersDataIds);

            // Despawn vi essence and other mobs here?

            if (gameOver.Value) { return; }
            if (winningPlayersDataIds.Length == 0)
            {
                roundResultMessage.Value = "Round Draw! ";
            }
            else
            {
                string message = PlayerDataManager.Singleton.GetTeamText(PlayerDataManager.Singleton.GetPlayerData(winningPlayersDataIds[0]).team) + " Secured Round " + GetRoundCount().ToString() + " ";
                roundResultMessage.Value = message;
            }
        }

        protected override void OnGameEnd(int[] winningPlayersDataIds)
        {
            base.OnGameEnd(winningPlayersDataIds);
            roundResultMessage.Value = "Game Over! ";
            gameEndMessage.Value = PlayerDataManager.Singleton.GetTeamText(PlayerDataManager.Singleton.GetPlayerData(winningPlayersDataIds[0]).team) + " Wins the Match!";
        }

        public override string GetLeftScoreString()
        {
            if (!NetworkManager.LocalClient.PlayerObject) { return ""; }

            PlayerDataManager.Team localTeam = PlayerDataManager.Singleton.LocalPlayerData.team;
            if (localTeam == PlayerDataManager.Team.Spectator)
            {
                return PlayerDataManager.Singleton.GetTeamText(PlayerDataManager.Team.Light) + ": " + lightScore.Value.ToString();
            }
            else
            {
                if (localTeam == PlayerDataManager.Team.Light)
                {
                    return "Your Team: " + lightScore.Value.ToString();
                }
                else if (localTeam == PlayerDataManager.Team.Corruption)
                {
                    return "Your Team: " + corruptionScore.Value.ToString();
                }
                else
                {
                    Debug.LogError("Not sure how to handle team " + localTeam);
                    return string.Empty;
                }
            }
        }


        public PlayerDataManager.Team GetLeftScoreTeam()
        {
            if (!PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId)) { return PlayerDataManager.Team.Light; }

            PlayerDataManager.Team localTeam = PlayerDataManager.Singleton.LocalPlayerData.team;
            if (localTeam == PlayerDataManager.Team.Spectator)
            {
                return PlayerDataManager.Team.Light;
            }
            else
            {
                if (localTeam == PlayerDataManager.Team.Light)
                {
                    return PlayerDataManager.Team.Light;
                }
                else if (localTeam == PlayerDataManager.Team.Corruption)
                {
                    return PlayerDataManager.Team.Corruption;
                }
                else
                {
                    Debug.LogError("Not sure how to handle team " + localTeam);
                    return PlayerDataManager.Team.Light;
                }
            }
        }

        public override string GetRightScoreString()
        {
            if (!NetworkManager.LocalClient.PlayerObject) { return ""; }

            PlayerDataManager.Team localTeam = PlayerDataManager.Singleton.LocalPlayerData.team;
            if (localTeam == PlayerDataManager.Team.Spectator)
            {
                return PlayerDataManager.Singleton.GetTeamText(PlayerDataManager.Team.Corruption) + ": " + corruptionScore.Value.ToString();
            }
            else
            {
                if (localTeam == PlayerDataManager.Team.Light)
                {
                    return "Enemy Team: " + corruptionScore.Value.ToString();
                }
                else if (localTeam == PlayerDataManager.Team.Corruption)
                {
                    return "Enemy Team: " + lightScore.Value.ToString();
                }
                else
                {
                    Debug.LogError("Not sure how to handle team " + localTeam);
                    return string.Empty;
                }
            }
        }

        public PlayerDataManager.Team GetRightScoreTeam()
        {
            if (!PlayerDataManager.Singleton.ContainsId((int)NetworkManager.LocalClientId)) { return PlayerDataManager.Team.Corruption; }

            PlayerDataManager.Team localTeam = PlayerDataManager.Singleton.LocalPlayerData.team;
            if (localTeam == PlayerDataManager.Team.Spectator)
            {
                return PlayerDataManager.Team.Corruption;
            }
            else
            {
                if (localTeam == PlayerDataManager.Team.Light)
                {
                    return PlayerDataManager.Team.Corruption;
                }
                else if (localTeam == PlayerDataManager.Team.Corruption)
                {
                    return PlayerDataManager.Team.Light;
                }
                else
                {
                    Debug.LogError("Not sure how to handle team " + localTeam);
                    return PlayerDataManager.Team.Corruption;
                }
            }
        }
    }
}

