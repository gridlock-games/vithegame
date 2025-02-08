using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace Vi.UI
{
    public class LeaderboardElement : MonoBehaviour
    {
        [SerializeField] private Text playerNameText;
        [Header("Kills Element")]
        [SerializeField] private Text killsText;
        [SerializeField] private Text deathsText;
        [SerializeField] private Text assistsText;
        [SerializeField] private Text KDAText;
        [Header("Horde Mode Element")]
        [SerializeField] private Text dateText;
        [SerializeField] private Text wavesClearedText;
        [SerializeField] private Text clearTimeText;
        [SerializeField] private Text damageDealtText;

        public void Initialize(LeaderboardManager.HordeLeaderboardEntry hordeLeaderboardEntry)
        {
            dateText.text = hordeLeaderboardEntry.dateCreated;
            playerNameText.text = hordeLeaderboardEntry.record.playerName;
            wavesClearedText.text = hordeLeaderboardEntry.record.wave.ToString();
            clearTimeText.text = hordeLeaderboardEntry.record.clearTime.ToString("F2");
            damageDealtText.text = hordeLeaderboardEntry.record.damageDealt.ToString("F2");
        }

        public void Initialize(LeaderboardManager.KillsLeaderboardEntry killsLeaderboardEntry)
        {
            playerNameText.text = killsLeaderboardEntry.record.playerName;
            killsText.text = killsLeaderboardEntry.record.kills.ToString();
            deathsText.text = killsLeaderboardEntry.record.deaths.ToString();
            assistsText.text = killsLeaderboardEntry.record.assists.ToString();
            KDAText.text = killsLeaderboardEntry.record.KDA.ToString("F2");
        }
    }
}