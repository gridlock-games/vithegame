using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
    public class KillFeedElement : MonoBehaviour
    {
        [SerializeField] private Text killerText;
        [SerializeField] private Text victimText;
        [SerializeField] private Image causeOfDeathImage;

        private float initializationTime = Mathf.NegativeInfinity;
        public void Initialize(GameModeManager.KillHistoryElement killHistoryElement)
        {
            if (killHistoryElement.killType == GameModeManager.KillHistoryElement.KillType.Player)
            {
                killerText.gameObject.SetActive(true);
                killerText.text = killHistoryElement.killerName.ToString();
                victimText.text = killHistoryElement.victimName.ToString();
                causeOfDeathImage.sprite = killHistoryElement.GetKillFeedIcon(killHistoryElement);
            }
            else if (killHistoryElement.killType == GameModeManager.KillHistoryElement.KillType.Environment)
            {
                killerText.gameObject.SetActive(false);
                victimText.text = killHistoryElement.victimName.ToString();
                causeOfDeathImage.sprite = killHistoryElement.GetKillFeedIcon(killHistoryElement);
            }
            else
            {
                Debug.LogError("Not sure how to handle kill history kill type: " + killHistoryElement.killType);
            }

            initializationTime = Time.time;
        }

        public bool IsNotRunning() { return Time.time - initializationTime > 5; }

        private Graphic[] graphics = new Graphic[0];
        private List<Color> graphicColors = new List<Color>();
        private void Start()
        {
            graphics = GetComponentsInChildren<Graphic>();
            foreach (Graphic graphic in graphics)
            {
                graphicColors.Add(graphic.color);
                Color newColor = graphic.color;
                newColor.a = 0;
                graphic.color = newColor;
            }
        }

        private const float fadeTime = 10;
        private void Update()
        {
            for (int i = 0; i < graphics.Length; i++)
            {
                Color targetColor = graphicColors[i];
                if (IsNotRunning()) { targetColor.a = 0; }
                graphics[i].color = Color.Lerp(graphics[i].color, targetColor, Time.deltaTime * fadeTime);
            }
        }
    }
}