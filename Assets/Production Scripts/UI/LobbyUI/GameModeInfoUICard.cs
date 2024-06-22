using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vi.UI
{
    public class GameModeInfoUICard : MonoBehaviour
    {
        [SerializeField] private Image infoImage;
        [SerializeField] private Text infoHeader;
        [SerializeField] private Text infoText;

        public void Initialize(Sprite infoImageSprite, string headerMessage, string infoTextMessage)
        {
            infoImage.sprite = infoImageSprite;
            infoHeader.text = headerMessage;
            infoText.text = infoTextMessage;
        }
    }
}