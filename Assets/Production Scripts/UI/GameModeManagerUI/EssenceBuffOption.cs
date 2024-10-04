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

        public void Initialize(SessionProgressionHandler sessionProgressionHandler, GameModeManager.EssenceBuffOption essenceBuffOption, int essenceBuffIndex)
        {
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
            button.onClick.AddListener(() => ApplyBuffOption(sessionProgressionHandler, essenceBuffIndex));
        }

        private void ApplyBuffOption(SessionProgressionHandler sessionProgressionHandler, int essenceBuffIndex)
        {
            sessionProgressionHandler.RedeemEssenceBuff(essenceBuffIndex);
        }
    }
}