using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.Utility;

namespace Vi.UI
{
    public class GameModeInfoUI : MonoBehaviour
    {
        [SerializeField] private GameModeInfoUICard gameModeInfoUICardPrefab;
        [SerializeField] private RectTransform gameModeInfoUICardParent;
        [SerializeField] private Text headerText;
        [SerializeField] private Button okButton;
        [SerializeField] private List<GameModeInfoUIData> gameModeInfoUIDatas;

        public void Initialize(PlayerDataManager.GameMode gameMode)
        {
            headerText.text = StringUtility.FromCamelCase(gameMode.ToString());

            foreach (Transform child in gameModeInfoUICardParent)
            {
                Destroy(child.gameObject);
            }

            var data = gameModeInfoUIDatas.Find(item => item.gameMode == gameMode);
            for (int i = 0; i < data.gameModeSprites.Length; i++)
            {
                GameModeInfoUICard gameModeInfoUICard = Instantiate(gameModeInfoUICardPrefab.gameObject, gameModeInfoUICardParent).GetComponent<GameModeInfoUICard>();
                gameModeInfoUICard.Initialize(data.gameModeSprites[i], data.cardHeaders[i], data.gameModeMessages[i]);
            }

            okButtonText = okButton.GetComponentInChildren<Text>();
            okButton.interactable = false;
            if (okButtonCoroutine != null) { StopCoroutine(okButtonCoroutine); }
            okButtonCoroutine = PersistentLocalObjects.Singleton.StartCoroutine(ActivateOKButton());
        }

        private Coroutine okButtonCoroutine;
        private Text okButtonText;
        private IEnumerator ActivateOKButton()
        {
            float elapsedTime = 0;
            while (elapsedTime < 5)
            {
                okButtonText.text = "Okay " + Mathf.CeilToInt(5 - elapsedTime).ToString();
                yield return null;
                elapsedTime += Time.deltaTime;
            }

            okButton.interactable = true;
            okButtonText.text = "Okay";
        }

        [System.Serializable]
        private struct GameModeInfoUIData
        {
            public PlayerDataManager.GameMode gameMode;
            public Sprite[] gameModeSprites;
            public string[] cardHeaders;
            public string[] gameModeMessages;
        }
    }
}