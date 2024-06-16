using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core.GameModeManagers;
using Unity.Netcode;

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
            Debug.Log(killHistoryElement.killType);
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

            localPlayerBackgroundImage.enabled = false;
            backgroundImage.color = nonLocalDeathBackgroundColor;
            if (NetworkManager.Singleton.SpawnManager != null)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(killHistoryElement.killerNetObjId, out NetworkObject netObj))
                {
                    localPlayerBackgroundImage.color = Color.red;
                    localPlayerBackgroundImage.enabled = netObj.IsLocalPlayer;
                }
                else if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(killHistoryElement.assistNetObjId, out netObj))
                {
                    localPlayerBackgroundImage.color = Color.yellow;
                    localPlayerBackgroundImage.enabled = netObj.IsLocalPlayer;
                }
                else if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(killHistoryElement.victimNetObjId, out netObj))
                {
                    if (netObj.IsLocalPlayer) { backgroundImage.color = localDeathBackgroundColor; }
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
        private void Update()
        {
            for (int i = 0; i < tintMaterialInstances.Count; i++)
            {
                Color targetColor = Color.white;
                if (IsNotRunning()) { targetColor.a = 0; }
                tintMaterialInstances[i].color = Vector4.MoveTowards(tintMaterialInstances[i].color, targetColor, Time.deltaTime * fadeTime);
            }
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, transformToCopyWidthFrom.sizeDelta.x);
        }
    }
}