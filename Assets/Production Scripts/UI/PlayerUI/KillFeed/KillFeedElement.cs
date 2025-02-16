using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.Core.GameModeManagers;

namespace Vi.UI
{
    public class KillFeedElement : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private GameObject killerParent;
        [SerializeField] private Text killerText;
        [SerializeField] private GameObject assistParent;
        [SerializeField] private Text assistText;
        [SerializeField] private GameObject victimParent;
        [SerializeField] private Text victimText;
        [SerializeField] private Image causeOfDeathImage;
        [SerializeField] private Image localPlayerBackgroundImage;
        [SerializeField] private RectTransform transformToCopyWidthFrom;

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

            transform.SetAsFirstSibling();

            // Evaluate colors
            killerText.color = PlayerDataManager.Singleton.GetRelativeHealthBarColor(killHistoryElement.killerTeam);
            assistText.color = PlayerDataManager.Singleton.GetRelativeHealthBarColor(killHistoryElement.assistTeam);
            victimText.color = PlayerDataManager.Singleton.GetRelativeHealthBarColor(killHistoryElement.victimTeam);

            localPlayerBackgroundImage.enabled = false;
            backgroundImage.color = nonLocalDeathBackgroundColor;
            if (NetworkManager.Singleton.SpawnManager != null)
            {
                // Local Player Kill
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(killHistoryElement.killerNetObjId, out NetworkObject netObj))
                {
                    if (netObj.IsLocalPlayer)
                    {
                        localPlayerBackgroundImage.color = Color.red;
                        localPlayerBackgroundImage.enabled = true;
                        killerText.color = Color.white;
                    }
                }

                // Local Player Assist
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(killHistoryElement.assistNetObjId, out netObj))
                {
                    if (netObj.IsLocalPlayer)
                    {
                        localPlayerBackgroundImage.color = Color.yellow;
                        localPlayerBackgroundImage.enabled = true;
                        assistText.color = Color.white;
                    }
                }

                // Local Player Death
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(killHistoryElement.victimNetObjId, out netObj))
                {
                    if (netObj.IsLocalPlayer)
                    {
                        backgroundImage.color = localDeathBackgroundColor;
                        victimText.color = Color.white;
                    }
                }
            }
            initializationTime = Time.time;

            causeOfDeathImage.transform.SetSiblingIndex(causeOfDeathImageOriginalSiblingIndex);
            contentSizeFitter.enabled = true;
            horizontalLayoutGroup.enabled = true;
            setSiblingIndexNextFrameCount = 3;
        }

        private int setSiblingIndexNextFrameCount;

        private static readonly Color nonLocalDeathBackgroundColor = new Color(23 / 255f, 32 / 255f, 44 / 255f, 200 / 255f);
        private static readonly Color localDeathBackgroundColor = new Color(200 / 255f, 0, 0, 200 / 255f);

        public bool IsNotRunning() { return Time.time - initializationTime > 5; }

        private Canvas canvas;
        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            causeOfDeathImageOriginalSiblingIndex = causeOfDeathImage.transform.GetSiblingIndex();
        }

        private RectTransform rt;
        private Material materialInstance;
        private HorizontalLayoutGroup horizontalLayoutGroup;
        private ContentSizeFitter contentSizeFitter;

        private int causeOfDeathImageOriginalSiblingIndex;

        private void Start()
        {
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>())
            {
                if (!materialInstance)
                {
                    materialInstance = new Material(graphic.material);
                }

                graphic.material = materialInstance;
                Color newColor = graphic.color;
                newColor.a = 0;
                graphic.material.color = newColor;
            }

            horizontalLayoutGroup = GetComponentInChildren<HorizontalLayoutGroup>();
            contentSizeFitter = GetComponentInChildren<ContentSizeFitter>();

            rt = (RectTransform)transform;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, transformToCopyWidthFrom.sizeDelta.x);
        }

        private const float fadeTime = 4;
        private Color lastTargetColor;
        private float lastWidthEvaluated;
        private void Update()
        {
            Color targetColor = Color.white;
            if (IsNotRunning()) { targetColor.a = 0; }
            else { canvas.enabled = true; }

            if (lastTargetColor != targetColor)
            {
                if (colorFadeCoroutine != null) { StopCoroutine(colorFadeCoroutine); }
                colorFadeCoroutine = StartCoroutine(FadeKillFeedColor(targetColor));
                lastTargetColor = targetColor;
            }

            float targetWidth = transformToCopyWidthFrom.sizeDelta.x;
            if (!Mathf.Approximately(targetWidth, lastWidthEvaluated))
            {
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
                lastWidthEvaluated = targetWidth;

            }

            if (setSiblingIndexNextFrameCount > 0)
            {
                setSiblingIndexNextFrameCount--;

                if (setSiblingIndexNextFrameCount == 0)
                {
                    // Disable for sibling reordering
                    contentSizeFitter.enabled = false;
                    horizontalLayoutGroup.enabled = false;

                    causeOfDeathImage.transform.SetAsFirstSibling();
                }
            }
        }

        private Coroutine colorFadeCoroutine;
        private IEnumerator FadeKillFeedColor(Color targetColor)
        {
            while (true)
            {
                List<Color> colorList = new List<Color>();
                Color colorResult = Vector4.MoveTowards(materialInstance.color, targetColor, Time.deltaTime * fadeTime);
                materialInstance.color = colorResult;
                colorList.Add(colorResult);
                if (colorList.TrueForAll(item => item == targetColor))
                {
                    if (IsNotRunning())
                    {
                        canvas.enabled = false;
                    }
                    break;
                }
                yield return null;
            }
        }
    }
}