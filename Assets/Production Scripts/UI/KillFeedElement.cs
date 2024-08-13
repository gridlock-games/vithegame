using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core.GameModeManagers;
using Unity.Netcode;
using Vi.Core;
using Vi.Core.CombatAgents;

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
            KeyValuePair<int, Attributes> localPlayerKvp = PlayerDataManager.Singleton.GetLocalPlayerObject();
            if (localPlayerKvp.Value)
            {
                killerText.color = killHistoryElement.killerTeam == PlayerDataManager.Team.Competitor | killHistoryElement.killerTeam == PlayerDataManager.Team.Peaceful ? Color.white : localPlayerKvp.Value.GetTeam() == killHistoryElement.killerTeam ? Color.cyan : Color.red;
                assistText.color = killHistoryElement.assistTeam == PlayerDataManager.Team.Competitor | killHistoryElement.assistTeam == PlayerDataManager.Team.Peaceful ? Color.white : localPlayerKvp.Value.GetTeam() == killHistoryElement.assistTeam ? Color.cyan : Color.red;
                victimText.color = killHistoryElement.victimTeam == PlayerDataManager.Team.Competitor | killHistoryElement.victimTeam == PlayerDataManager.Team.Peaceful ? Color.white : localPlayerKvp.Value.GetTeam() == killHistoryElement.victimTeam ? Color.cyan : Color.red;
            }
            else // We are a spectator or the server
            {
                // TODO Set this to be the team color
                killerText.color = killHistoryElement.killerTeam == PlayerDataManager.Team.Competitor | killHistoryElement.killerTeam == PlayerDataManager.Team.Peaceful ? Color.white : PlayerDataManager.GetTeamColor(killHistoryElement.killerTeam);
                assistText.color = killHistoryElement.killerTeam == PlayerDataManager.Team.Competitor | killHistoryElement.killerTeam == PlayerDataManager.Team.Peaceful ? Color.white : PlayerDataManager.GetTeamColor(killHistoryElement.assistTeam);
                victimText.color = killHistoryElement.killerTeam == PlayerDataManager.Team.Competitor | killHistoryElement.killerTeam == PlayerDataManager.Team.Peaceful ? Color.white : PlayerDataManager.GetTeamColor(killHistoryElement.victimTeam);
            }

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
        }

        private static readonly Color nonLocalDeathBackgroundColor = new Color(23 / 255f, 32 / 255f, 44 / 255f, 200 / 255f);
        private static readonly Color localDeathBackgroundColor = new Color(200 / 255f, 0, 0, 200 / 255f);

        public bool IsNotRunning() { return Time.time - initializationTime > 5; }

        private List<Material> tintMaterialInstances = new List<Material>();
        private RectTransform rt;
        private void Start()
        {
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>())
            {
                graphic.material = new Material(graphic.material);
                tintMaterialInstances.Add(graphic.material);
                Color newColor = graphic.color;
                newColor.a = 0;
                graphic.material.color = newColor;
            }

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
        }

        private Coroutine colorFadeCoroutine;
        private IEnumerator FadeKillFeedColor(Color targetColor)
        {
            while (true)
            {
                List<Color> colorList = new List<Color>();
                for (int i = 0; i < tintMaterialInstances.Count; i++)
                {
                    Color colorResult = Vector4.MoveTowards(tintMaterialInstances[i].color, targetColor, Time.deltaTime * fadeTime);
                    tintMaterialInstances[i].color = colorResult;
                    colorList.Add(colorResult);
                }
                if (colorList.TrueForAll(item => item == targetColor)) { break; }
                yield return null;
            }
        }
    }
}