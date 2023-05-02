using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

namespace LightPat.Core
{
    public class DuelManager : GameLogicManager
    {
        [SerializeField] private TextMeshProUGUI timerDisplay;
        [SerializeField] private NetworkVariable<float> roundTimeInSeconds = new NetworkVariable<float>();

        private void Update()
        {
            if (IsServer)
            {
                roundTimeInSeconds.Value -= Time.deltaTime;
            }

            timerDisplay.SetText(roundTimeInSeconds.Value.ToString("F4"));
        }
    }
}