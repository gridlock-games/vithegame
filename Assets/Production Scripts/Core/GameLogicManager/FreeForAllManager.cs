using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using Vi.Core.CombatAgents;
using Vi.Core.DynamicEnvironmentElements;

namespace Vi.Core.GameModeManagers
{
    public class FreeForAllManager : GameModeManager
    {
        [Header("Free For All Specific")]
        [SerializeField] private DamageCircle damageCirclePrefab;
        [SerializeField] private int killsToWinRound = 2;

        private DamageCircle damageCircleInstance;

        public int GetKillsToWinRound() { return killsToWinRound; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                roundResultMessage.Value = "Free For All Starting! ";
                StartCoroutine(CreateDamageCircle());
            }
        }

        private IEnumerator CreateDamageCircle()
        {
            yield return new WaitUntil(() => PlayerDataManager.Singleton.HasPlayerSpawnPoints());
            damageCircleInstance = Instantiate(damageCirclePrefab.gameObject).GetComponent<DamageCircle>();
            damageCircleInstance.NetworkObject.Spawn(true);
        }

        public override void OnPlayerKill(CombatAgent killer, CombatAgent victim)
        {
            base.OnPlayerKill(killer, victim);
            if (gameOver.Value) { return; }
            if (killer is Attributes killerAttributes)
            {
                int killerIndex = scoreList.IndexOf(new PlayerScore(killerAttributes.GetPlayerDataId()));
                if (scoreList[killerIndex].killsThisRound >= killsToWinRound)
                {
                    OnRoundEnd(new int[] { killerAttributes.GetPlayerDataId() });
                }
            }
        }

        protected override void OnGameEnd(int[] winningPlayersDataIds)
        {
            base.OnGameEnd(winningPlayersDataIds);
            damageCircleInstance.NetworkObject.Despawn(true);
            roundResultMessage.Value = "Game Over! ";
            gameEndMessage.Value = PlayerDataManager.Singleton.GetPlayerData(winningPlayersDataIds[0]).character.name + " Wins the Free for All!";
        }

        protected override void OnRoundEnd(int[] winningPlayersDataIds)
        {
            base.OnRoundEnd(winningPlayersDataIds);
            damageCircleInstance.ResetDamageCircle();
            if (gameOver.Value) { return; }
            string message;
            if (winningPlayersDataIds.Length > 1)
            {
                message = winningPlayersDataIds.Length.ToString() + " Players are Tied for First Place! ";
            }
            else if (winningPlayersDataIds.Length == 0)
            {
                message = "Round Draw! ";
            }
            else
            {
                message = PlayerDataManager.Singleton.GetPlayerData(winningPlayersDataIds[0]).character.name + " Has Won the Round! ";
            }
            roundResultMessage.Value = message;
        }

        protected override void OnRoundTimerEnd()
        {
            List<int> highestKillIdList = new List<int>();
            foreach (PlayerScore playerScore in GetHighestKillPlayersThisRound())
            {
                highestKillIdList.Add(playerScore.id);
            }

            if (highestKillIdList.Count > 1)
            {
                float highestHP = -1;
                int winnerId = (int)NetworkManager.ServerClientId;
                foreach (int id in highestKillIdList)
                {
                    Attributes attributes = PlayerDataManager.Singleton.GetPlayerObjectById(id);
                    if (attributes)
                    {
                        if (attributes.GetHP() > highestHP)
                        {
                            winnerId = attributes.GetPlayerDataId();
                            highestHP = attributes.GetHP();
                        }
                    }
                }

                if (winnerId == (int)NetworkManager.ServerClientId)
                {
                    Debug.LogError("FFA winner id is the server client id! This should never happen!");
                    OnRoundEnd(new int[0]);
                }
                else
                {
                    OnRoundEnd(new int[] { winnerId });
                }
            }
            else if (highestKillIdList.Count == 1)
            {
                OnRoundEnd(highestKillIdList.ToArray());
            }
            //else if (!overtime.Value)
            //{
            //    roundTimer.Value = overtimeDuration;
            //    overtime.Value = true;
            //}
            else
            {
                OnRoundEnd(new int[0]);
            }
        }

        public override string GetLeftScoreString()
        {
            if (!NetworkManager.LocalClient.PlayerObject) { return ""; }

            int localPlayerKey = PlayerDataManager.Singleton.GetLocalPlayerObject().Key;
            int localIndex = scoreList.IndexOf(new PlayerScore(localPlayerKey));
            if (localIndex == -1)
            {
                // If we're a spectator
                List<PlayerScore> scoreList = new List<PlayerScore>();
                PlayerScore localPlayerScore;
                foreach (PlayerScore playerScore in this.scoreList)
                {
                    if (playerScore.id == PlayerDataManager.Singleton.GetLocalPlayerObject().Key)
                    {
                        localPlayerScore = playerScore;
                    }
                    else
                    {
                        scoreList.Add(playerScore);
                    }
                }
                // Find player score with second highest kills
                scoreList = scoreList.OrderByDescending(item => item.killsThisRound).ToList();
                if (scoreList.Count > 1)
                    return PlayerDataManager.Singleton.GetPlayerData(scoreList[1].id).character.name + ": " + scoreList[1].killsThisRound.ToString();
                else
                    return string.Empty;
            }
            else
            {
                return PlayerDataManager.Singleton.GetPlayerData(localPlayerKey).character.name + ": " + scoreList[localIndex].killsThisRound;
            }
        }

        public override string GetRightScoreString()
        {
            if (!NetworkManager.LocalClient.PlayerObject) { return ""; }

            List<PlayerScore> scoreList = new List<PlayerScore>();
            PlayerScore localPlayerScore;
            var localKvp = PlayerDataManager.Singleton.GetLocalPlayerObject();
            foreach (PlayerScore playerScore in this.scoreList)
            {
                if (playerScore.id == localKvp.Key)
                {
                    localPlayerScore = playerScore;
                }
                else
                {
                    scoreList.Add(playerScore);
                }
            }

            // Find player score with highest kills and highest HP
            Dictionary<int, float> hpDict = new Dictionary<int, float>();
            foreach (PlayerScore score in scoreList)
            {
                if (PlayerDataManager.Singleton.IdHasLocalPlayer(score.id))
                {
                    Attributes obj = PlayerDataManager.Singleton.GetPlayerObjectById(score.id);
                    hpDict.Add(score.id, obj ? obj.GetHP() : -1);
                }
                else
                {
                    hpDict.Add(score.id, -1);
                }
            }

            scoreList = scoreList.OrderByDescending(item => item.killsThisRound).ThenByDescending(item => hpDict[item.id]).ToList();
            if (scoreList.Count > 0)
                return PlayerDataManager.Singleton.GetPlayerData(scoreList[0].id).character.name + ": " + scoreList[0].killsThisRound.ToString();
            else
                return string.Empty;
        }
    }
}