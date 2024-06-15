using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
    public class KillFeedElement : MonoBehaviour
    {
        [SerializeField] private GameObject killerParent;
        [SerializeField] private Text killerText;
        [SerializeField] private GameObject assistParent;
        [SerializeField] private Text assistText;
        [SerializeField] private GameObject victimParent;
        [SerializeField] private Text victimText;
        [SerializeField] private Image causeOfDeathImage;

        private float initializationTime = Mathf.NegativeInfinity;
        public void Initialize(GameModeManager.KillHistoryElement killHistoryElement)
        {
            switch (killHistoryElement.killType)
            {
                case GameModeManager.KillHistoryElement.KillType.Player:
                    killerParent.SetActive(true);
                    killerText.text = killHistoryElement.killerName.ToString();
                    assistParent.SetActive(false);
                    victimText.text = killHistoryElement.victimName.ToString();
                    causeOfDeathImage.sprite = killHistoryElement.GetKillFeedIcon(killHistoryElement);
                    break;
                case GameModeManager.KillHistoryElement.KillType.PlayerWithAssist:
                    killerParent.SetActive(true);
                    killerText.text = killHistoryElement.killerName.ToString();
                    assistParent.SetActive(true);
                    assistText.text = killHistoryElement.assistName.ToString();
                    victimText.text = killHistoryElement.victimName.ToString();
                    causeOfDeathImage.sprite = killHistoryElement.GetKillFeedIcon(killHistoryElement);
                    break;
                case GameModeManager.KillHistoryElement.KillType.Environment:
                    killerParent.SetActive(false);
                    assistParent.SetActive(false);
                    victimText.text = killHistoryElement.victimName.ToString();
                    causeOfDeathImage.sprite = killHistoryElement.GetKillFeedIcon(killHistoryElement);
                    break;
                default:
                    Debug.LogError("Unsure how to handle kill type " + killHistoryElement.killType);
                    break;
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