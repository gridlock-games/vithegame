using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

namespace LightPat.Core
{
    public class DuelManager : GameLogicManager
    {
        [SerializeField] private TextMeshProUGUI redScoreText;
        [SerializeField] private TextMeshProUGUI blueScoreText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private TextMeshProUGUI timerDisplay;
        [SerializeField] private NetworkVariable<float> countdownTime = new NetworkVariable<float>();
        [SerializeField] private NetworkVariable<float> roundTimeInSeconds = new NetworkVariable<float>();

        private NetworkVariable<int> redScore = new NetworkVariable<int>();
        private NetworkVariable<int> blueScore = new NetworkVariable<int>();

        public override void OnPlayerDeath(Team team)
        {
            if (IsServer)
            {
                if (team == Team.Red)
                    redScore.Value += 1;
                else if (team == Team.Blue)
                    blueScore.Value += 1;
            }
        }

        private void Update()
        {
            if (IsServer)
            {
                if (countdownTime.Value > 0)
                {
                    countdownTime.Value -= Time.deltaTime;
                    if (countdownTime.Value < 0) { countdownTime.Value = 0; }
                    
                    countdownText.enabled = true;
                    timerDisplay.enabled = false;
                }
                else
                {
                    roundTimeInSeconds.Value -= Time.deltaTime;

                    countdownText.enabled = false;
                    timerDisplay.enabled = true;
                }
            }

            countdownText.SetText(countdownTime.Value.ToString("F0"));
            timerDisplay.SetText(roundTimeInSeconds.Value.ToString("F4"));

            redScoreText.SetText(redScore.Value.ToString());
            blueScoreText.SetText(blueScore.Value.ToString());
        }
    }
}