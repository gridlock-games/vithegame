using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
    public class EssenceBuffOption : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text requiredEssenceCountText;

        private GameModeManager.EssenceBuffOption essenceBuffOption;
        private int essenceBuffIndex;
        public void Initialize(EssenceBuffMenu menu, SessionProgressionHandler sessionProgressionHandler, GameModeManager.EssenceBuffOption essenceBuffOption, int essenceBuffIndex)
        {
            this.essenceBuffOption = essenceBuffOption;
            this.essenceBuffIndex = essenceBuffIndex;

            titleText.text = essenceBuffOption.title;
            descriptionText.text = essenceBuffOption.description;
            iconImage.color = essenceBuffOption.iconColor;
            iconImage.sprite = essenceBuffOption.iconSprite;
            requiredEssenceCountText.text = essenceBuffOption.requiredEssenceCount.ToString();
            requiredEssenceCountText.color = sessionProgressionHandler.Essences < essenceBuffOption.requiredEssenceCount ? Color.red : Color.white;

            //TODO add server rpc to apply essence buff here
            Button button = GetComponent<Button>();
            button.interactable = sessionProgressionHandler.Essences >= essenceBuffOption.requiredEssenceCount;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ApplyBuffOption(menu, sessionProgressionHandler, essenceBuffIndex));
        }

        public bool IsInteractable()
        {
            return GetComponent<Button>().interactable;
        }

        private void ApplyBuffOption(EssenceBuffMenu menu, SessionProgressionHandler sessionProgressionHandler, int essenceBuffIndex)
        {
            sessionProgressionHandler.RedeemEssenceBuff(essenceBuffIndex);
            GetComponent<Button>().interactable = false;
            menu.OnEssenceBuffOptionSelected(essenceBuffIndex);
        }

        public bool Refresh(int newEssenceCount, int selectedEssenceBuffIndex)
        {
            requiredEssenceCountText.color = newEssenceCount < essenceBuffOption.requiredEssenceCount ? Color.red : Color.white;
            Button button = GetComponent<Button>();
            if (essenceBuffIndex == selectedEssenceBuffIndex)
            {
                button.interactable = essenceBuffOption.stackable & newEssenceCount >= essenceBuffOption.requiredEssenceCount;
            }
            else
            {
                if (button.interactable)
                {
                    button.interactable = newEssenceCount >= essenceBuffOption.requiredEssenceCount;
                }
            }
            return button.interactable;
        }
    }
}