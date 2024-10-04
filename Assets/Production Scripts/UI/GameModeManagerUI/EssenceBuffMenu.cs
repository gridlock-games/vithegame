using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.Core.GameModeManagers;
using Vi.Player;
using UnityEngine.UI;
using System.Linq;
using System;

namespace Vi.UI
{
    public class EssenceBuffMenu : MonoBehaviour, ExternalUI
    {
        [SerializeField] private EssenceBuffOption essenceBuffOptionPrefab;
        [SerializeField] private Transform essenceBuffOptionParent;
        [SerializeField] private Text essenceCountText;

        private Canvas canvas;
        private void Awake()
        {
            canvas = GetComponent<Canvas>();
        }

        private List<EssenceBuffOption> instances = new List<EssenceBuffOption>();
        private ActionMapHandler actionMapHandler;
        public void Initialize(ActionMapHandler actionMapHandler)
        {
            actionMapHandler.SetExternalUI(this);
            this.actionMapHandler = actionMapHandler;
            
            Dictionary<GameModeManager.EssenceBuffOption, int> indexCrosswalk = new Dictionary<GameModeManager.EssenceBuffOption, int>();
            int counter = 0;
            foreach (GameModeManager.EssenceBuffOption essenceBuffOption in GameModeManager.Singleton.EssenceBuffOptions)
            {
                indexCrosswalk.Add(essenceBuffOption, counter);
                counter++;
            }

            SessionProgressionHandler sessionProgressionHandler = actionMapHandler.GetComponent<SessionProgressionHandler>();
            essenceCountText.text = sessionProgressionHandler.Essences.ToString();

            bool allNotInteractable = true;
            foreach (GameModeManager.EssenceBuffOption essenceBuffOption in GameModeManager.Singleton.EssenceBuffOptions.OrderBy(i => Guid.NewGuid()).Take(3))
            {
                EssenceBuffOption instance = Instantiate(essenceBuffOptionPrefab.gameObject, essenceBuffOptionParent).GetComponent<EssenceBuffOption>();
                instance.Initialize(this, sessionProgressionHandler, essenceBuffOption, indexCrosswalk[essenceBuffOption]);
                instances.Add(instance);

                if (instance.IsInteractable()) { allNotInteractable = false; }
            }

            if (allNotInteractable) { CloseMenu(); }
            else { canvas.enabled = true; }
        }

        public void OnEssenceBuffOptionSelected(int essenceBuffOptionIndex)
        {
            if (int.TryParse(essenceCountText.text, out int oldEssenceCount))
            {
                int newEssenceCount = oldEssenceCount - GameModeManager.Singleton.EssenceBuffOptions[essenceBuffOptionIndex].requiredEssenceCount;

                essenceCountText.text = newEssenceCount.ToString();

                bool allNotInteractable = true;
                foreach (EssenceBuffOption instance in instances)
                {
                    if (instance.Refresh(newEssenceCount, essenceBuffOptionIndex)) { allNotInteractable = false; }
                }

                if (allNotInteractable) { CloseMenu(); }
            }
            else
            {
                Debug.LogError("Unable to parse essence count from essence count text!");
            }
        }

        public void CloseMenu()
        {
            foreach (EssenceBuffOption instance in instances)
            {
                Destroy(instance.gameObject);
            }
            instances.Clear();

            if (actionMapHandler)
            {
                actionMapHandler.SetExternalUI(null);
            }
            canvas.enabled = false;
        }

        public void OnDestroy()
        {
            if (actionMapHandler)
            {
                if (actionMapHandler.gameObject.activeInHierarchy)
                {
                    actionMapHandler.SetExternalUI(null);
                }
            }
        }

        public void OnPause()
        {
            CloseMenu();
        }
    }
}