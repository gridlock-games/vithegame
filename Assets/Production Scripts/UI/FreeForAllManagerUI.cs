using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
    public class FreeForAllManagerUI : MonoBehaviour
    {
        [SerializeField] protected Text leftScoreText;
        [SerializeField] protected Text rightScoreText;
        [SerializeField] protected Text roundTimerText;
        [SerializeField] protected Text nextGameActionText;
        [SerializeField] protected Text roundResultText;

        protected FreeForAllManager freeForAllManager;

        protected void Start()
        {
            freeForAllManager = GetComponentInParent<FreeForAllManager>();

            nextGameActionText.enabled = false;
            roundResultText.enabled = false;

            leftScoreText.text = "Your Team: ";
            rightScoreText.text = "Enemy Team: ";
        }

        protected void Update()
        {
            roundTimerText.text = freeForAllManager.GetRoundTimerDisplayString();
            leftScoreText.text = freeForAllManager.GetLeftScoreString();
            rightScoreText.text = freeForAllManager.GetRightScoreString();
        }
    }
}