using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Vi.Core;
using UnityEditor;
using Newtonsoft.Json;

namespace Vi.UI
{
    public class PlatformUIDefinition : MonoBehaviour
    {
        [SerializeField] private string customizablePlayerPrefName;
        [SerializeField] private InputActionAsset controlsAsset;
        [SerializeField] private UIDefinition[] platformUIDefinitions;
        [SerializeField] private ControlSchemeTextDefinition[] controlSchemeTextDefinitions;

        public UIDefinition[] GetPlatformUIDefinitions() { return platformUIDefinitions; }

        [System.Serializable]
        public struct UIDefinition
        {
            public RuntimePlatform[] platforms;
            public GameObject[] gameObjectsToEnable;
            public GameObject[] gameObjectsToDestroy;
            public MoveUIDefinition[] objectsToMove;
        }

        [System.Serializable]
        public struct MoveUIDefinition
        {
            public GameObject gameObjectToMove;
            public Vector2 newAnchoredPosition;
            public bool shouldOverrideAnchors;
            public Vector2 anchorMinOverride;
            public Vector2 anchorMaxOverride;
            public Vector2 pivotOverride;
        }

        [System.Serializable]
        private struct ControlSchemeTextDefinition
        {
            public RuntimePlatform[] platforms;
            public Text textElement;
            public string action;
            public string stringBeforeBinding;
            public string stringAfterBinding;
        }

        public struct PositionOverrideDefinition
        {
            public string gameObjectPath;
            public float newAnchoredX;
            public float newAnchoredY;
        }

        public static string GetGameObjectPath(GameObject obj)
        {
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }

        public GameObject GetGameObjectFromPath(string path)
        {
            Transform g = transform;
            foreach (string step in path.Split("/"))
            {
                if (string.IsNullOrWhiteSpace(step)) { continue; }
                if (step == name.Replace("(Clone)", "")) { continue; }
                g = g.Find(step);
            }
            return g.gameObject;
        }

        private void Start()
        {
            foreach (UIDefinition platformUIDefinition in platformUIDefinitions)
            {
                foreach (GameObject g in platformUIDefinition.gameObjectsToEnable)
                {
                    g.SetActive(platformUIDefinition.platforms.Contains(Application.platform));
                }

                foreach (MoveUIDefinition moveUIDefinition in platformUIDefinition.objectsToMove)
                {
                    if (platformUIDefinition.platforms.Contains(Application.platform))
                    {
                        RectTransform rt = (RectTransform)moveUIDefinition.gameObjectToMove.transform;
                        if (moveUIDefinition.shouldOverrideAnchors)
                        {
                            rt.anchorMin = moveUIDefinition.anchorMinOverride;
                            rt.anchorMax = moveUIDefinition.anchorMaxOverride;
                            rt.pivot = moveUIDefinition.pivotOverride;
                        }
                        rt.anchoredPosition = moveUIDefinition.newAnchoredPosition;
                    }
                }

                foreach (GameObject g in platformUIDefinition.gameObjectsToDestroy)
                {
                    if (platformUIDefinition.platforms.Contains(Application.platform)) { Destroy(g); }
                }
            }
        }

        private Dictionary<Transform, Vector3> originalPositionMap = new Dictionary<Transform, Vector3>();
        private void Awake()
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                originalPositionMap.Add(child, child.localPosition);
            }
        }

        private void OnEnable()
        {
            if (PlayerPrefs.HasKey(customizablePlayerPrefName))
            {
                List<PositionOverrideDefinition> positionOverrideDefinitions = JsonConvert.DeserializeObject<List<PositionOverrideDefinition>>(PlayerPrefs.GetString(customizablePlayerPrefName));

                foreach (PositionOverrideDefinition positionOverrideDefinition in positionOverrideDefinitions)
                {
                    GameObject g = GetGameObjectFromPath(positionOverrideDefinition.gameObjectPath);
                    ((RectTransform)g.transform).anchoredPosition = new Vector2(positionOverrideDefinition.newAnchoredX, positionOverrideDefinition.newAnchoredY);
                }
            }
            else
            {
                foreach (KeyValuePair<Transform, Vector3> kvp in originalPositionMap)
                {
                    kvp.Key.localPosition = kvp.Value;
                }
            }
        }

        private void Update()
        {
            foreach (UIDefinition platformUIDefinition in platformUIDefinitions)
            {
                foreach (MoveUIDefinition moveUIDefinition in platformUIDefinition.objectsToMove)
                {
                    if (platformUIDefinition.platforms.Contains(Application.platform))
                    {
                        RectTransform rt = (RectTransform)moveUIDefinition.gameObjectToMove.transform;
                        if (moveUIDefinition.shouldOverrideAnchors)
                        {
                            rt.anchorMin = moveUIDefinition.anchorMinOverride;
                            rt.anchorMax = moveUIDefinition.anchorMaxOverride;
                            rt.pivot = moveUIDefinition.pivotOverride;
                        }
                        rt.anchoredPosition = moveUIDefinition.newAnchoredPosition;
                    }
                }
            }

            if (PlayerDataManager.Singleton)
            {
                Attributes localPlayer = PlayerDataManager.Singleton.GetLocalPlayerObject().Value;
                if (localPlayer)
                {
                    if (localPlayer.TryGetComponent(out PlayerInput playerInput))
                    {
                        foreach (ControlSchemeTextDefinition controlSchemeTextDefinition in controlSchemeTextDefinitions)
                        {
                            if (controlSchemeTextDefinition.platforms.Contains(Application.platform))
                            {
                                InputControlScheme controlScheme = controlsAsset.FindControlScheme(playerInput.currentControlScheme).Value;

                                foreach (InputBinding binding in playerInput.actions[controlSchemeTextDefinition.action].bindings)
                                {
                                    bool shouldBreak = false;
                                    foreach (InputDevice device in System.Array.FindAll(InputSystem.devices.ToArray(), item => controlScheme.SupportsDevice(item)))
                                    {
                                        if (binding.path.ToLower().Contains(device.name.ToLower()))
                                        {
                                            controlSchemeTextDefinition.textElement.text = controlSchemeTextDefinition.stringBeforeBinding.Replace("\\n", "\n") + binding.ToDisplayString() + controlSchemeTextDefinition.stringAfterBinding.Replace("\\n", "\n");
                                            shouldBreak = true;
                                            break;
                                        }
                                    }
                                    if (shouldBreak) { break; }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}