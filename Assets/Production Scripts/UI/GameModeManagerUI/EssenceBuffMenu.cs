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
    public class EssenceBuffMenu : MonoBehaviour
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
        public void Initialize(ActionMapHandler actionMapHandler, GameModeManager.EssenceBuffOption[] essenceBuffOptions)
        {
            actionMapHandler.SetExternalUI(actionMapHandler);
            this.actionMapHandler = actionMapHandler;

            Dictionary<GameModeManager.EssenceBuffOption, int> indexCrosswalk = new Dictionary<GameModeManager.EssenceBuffOption, int>();
            int counter = 0;
            foreach (GameModeManager.EssenceBuffOption essenceBuffOption in essenceBuffOptions)
            {
                indexCrosswalk.Add(essenceBuffOption, counter);
                counter++;
            }

            SessionProgressionHandler sessionProgressionHandler = actionMapHandler.GetComponent<SessionProgressionHandler>();
            essenceCountText.text = sessionProgressionHandler.Essences.ToString();

            foreach (GameModeManager.EssenceBuffOption essenceBuffOption in essenceBuffOptions.OrderBy(i => Guid.NewGuid()).Take(3))
            {
                EssenceBuffOption instance = Instantiate(essenceBuffOptionPrefab.gameObject, essenceBuffOptionParent).GetComponent<EssenceBuffOption>();
                instance.Initialize(sessionProgressionHandler, essenceBuffOption, indexCrosswalk[essenceBuffOption]);
                instances.Add(instance);
            }
            canvas.enabled = true;
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
    }
}