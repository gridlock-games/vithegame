using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;

namespace Vi.UI
{
    public class GameModeOption : MonoBehaviour
    {
        [SerializeField] private Text gameModeText;

        public void Initialize(PlayerDataManager.GameMode gameMode)
        {
            gameModeText.text = gameMode.ToString();
        }

        private Image image;
        private void Awake()
        {
            image = GetComponent<Image>();
        }
    }
}