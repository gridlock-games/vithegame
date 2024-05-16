using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.Utility;

namespace Vi.UI
{
    public class GameModeOption : MonoBehaviour
    {
        [SerializeField] private Text gameModeText;

        private PlayerDataManager.GameMode gameMode;

        public IEnumerator Initialize(PlayerDataManager.GameMode gameMode, Sprite gameModeIcon)
        {
            this.gameMode = gameMode;
            gameModeText.text = StringUtility.FromCamelCase(gameMode.ToString());
            GetComponent<Image>().sprite = gameModeIcon;

            yield return new WaitUntil(() => PlayerDataManager.Singleton);
            yield return new WaitUntil(() => PlayerDataManager.Singleton.IsSpawned);

            GetComponent<Button>().onClick.AddListener(delegate { PlayerDataManager.Singleton.SetGameMode(gameMode); });
        }

        private Button button;
        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void Update()
        {
            if (!PlayerDataManager.DoesExist()) { button.interactable = false; return; }
            if (!PlayerDataManager.Singleton.IsSpawned) { button.interactable = false; return; }

            button.interactable = PlayerDataManager.Singleton.GetGameMode() != gameMode;
        }
    }
}