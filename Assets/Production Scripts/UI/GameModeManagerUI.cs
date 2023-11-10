using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;

namespace Vi.UI
{
    public class GameModeManagerUI : MonoBehaviour
    {
        [SerializeField] protected Text leftScoreText;
        [SerializeField] protected Text rightScoreText;
        [SerializeField] protected Text roundTimerText;
        [SerializeField] protected Text nextGameActionText;
        [SerializeField] protected Text roundResultText;

        protected GameModeManager gameModeManager;

        protected void Start()
        {
            gameModeManager = GetComponentInParent<GameModeManager>();

            nextGameActionText.enabled = false;
            roundResultText.enabled = false;

            leftScoreText.text = "Your Team: ";
            rightScoreText.text = "Enemy Team: ";
        }

        protected void Update()
        {
            roundTimerText.text = gameModeManager.GetRoundTimerDisplayString();
        }
    }
}