using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;

namespace Vi.UI
{
    public class MapOption : MonoBehaviour
    {
        [SerializeField] private Text mapNameText;

        private string mapName;

        public IEnumerator Initialize(string mapName, Sprite mapIcon)
        {
            this.mapName = mapName;
            mapNameText.text = mapName;
            GetComponent<Image>().sprite = mapIcon;

            yield return new WaitUntil(() => PlayerDataManager.Singleton);
            yield return new WaitUntil(() => PlayerDataManager.Singleton.IsSpawned);

            GetComponent<Button>().onClick.AddListener(delegate { PlayerDataManager.Singleton.SetMap(mapName); });
        }

        private Button button;
        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void Update()
        {
            if (!PlayerDataManager.Singleton) { button.interactable = false; return; }
            if (!PlayerDataManager.Singleton.IsSpawned) { button.interactable = false; return; }

            button.interactable = PlayerDataManager.Singleton.GetMapName() != mapName;
        }
    }
}