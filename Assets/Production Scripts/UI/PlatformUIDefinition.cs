using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Vi.Core;
using UnityEngine.InputSystem.OnScreen;
using Newtonsoft.Json;

namespace Vi.UI
{
    public class PlatformUIDefinition : MonoBehaviour
    {
        [SerializeField] private string customizablePlayerPrefName;
        [SerializeField] private InputActionAsset controlsAsset;
        [SerializeField] private UIDefinition[] platformUIDefinitions;
        [SerializeField] private ControlSchemeTextDefinition[] controlSchemeTextDefinitions;
        [SerializeField] private ControlSchemeDefinition[] controlSchemeDefinitions;

        public UIDefinition[] GetPlatformUIDefinitions() { return platformUIDefinitions; }

        public static bool UIElementIsAbleToBeModified(GameObject g) { return g.GetComponent<Button>() | (g.GetComponent<OnScreenButton>() & !g.GetComponent<CustomOnScreenStick>()); }

        [System.Serializable]
        public struct UIDefinition
        {
            public RuntimePlatform[] platforms;
            public GameObject[] gameObjectsToEnable;
            public GameObject[] gameObjectsToDisable;
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

        [System.Serializable]
        private struct ControlSchemeDefinition
        {
            public string controlSchemeName;
            public GameObject[] gameObjectsToEnable;
            public GameObject[] gameObjectsToDestroy;
            public MoveUIDefinition[] objectsToMove;
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

        PlayerInput playerInput;
        private void FindPlayerInput()
        {
            if (playerInput) { return; }
            if (!PlayerDataManager.Singleton) { return; }
            Attributes localPlayer = PlayerDataManager.Singleton.GetLocalPlayerObject().Value;
            if (localPlayer) { playerInput = localPlayer.GetComponent<PlayerInput>(); }
        }

        private string lastEvaluatedControlScheme;
        private void EvaluateControlSchemeDefinitions()
        {
            if (!playerInput) { return; }
            if (playerInput.currentControlScheme == lastEvaluatedControlScheme) { return; }
            foreach (ControlSchemeDefinition controlSchemeDefinition in controlSchemeDefinitions)
            {
                foreach (GameObject g in controlSchemeDefinition.gameObjectsToEnable)
                {
                    g.SetActive(playerInput.currentControlScheme == controlSchemeDefinition.controlSchemeName);
                }

                foreach (MoveUIDefinition moveUIDefinition in controlSchemeDefinition.objectsToMove)
                {
                    if (playerInput.currentControlScheme == controlSchemeDefinition.controlSchemeName)
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

                foreach (GameObject g in controlSchemeDefinition.gameObjectsToDestroy)
                {
                    if (playerInput.currentControlScheme == controlSchemeDefinition.controlSchemeName) { Destroy(g); }
                }
            }

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

            lastEvaluatedControlScheme = playerInput.currentControlScheme;
        }

        private void EvaluateUIDefinitionsOnFirstFrame()
        {
            foreach (UIDefinition platformUIDefinition in platformUIDefinitions)
            {
                foreach (GameObject g in platformUIDefinition.gameObjectsToEnable)
                {
                    g.SetActive(platformUIDefinition.platforms.Contains(Application.platform));
                }

                foreach (GameObject g in platformUIDefinition.gameObjectsToDisable)
                {
                    g.SetActive(!platformUIDefinition.platforms.Contains(Application.platform));
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
            lastEvaluatedResolution = Screen.currentResolution;
        }

        private Resolution lastEvaluatedResolution;
        private void EvaluateUIDefinitionsInUpdate()
        {
            if (Screen.currentResolution.width == lastEvaluatedResolution.width & Screen.currentResolution.height == lastEvaluatedResolution.height) { return; }
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
            lastEvaluatedResolution = Screen.currentResolution;
        }

        private Dictionary<RectTransform, Vector2> originalPositionMap = new Dictionary<RectTransform, Vector2>();
        private void Awake()
        {
            FindPlayerInput();
            EvaluateUIDefinitionsOnFirstFrame();
            EvaluateControlSchemeDefinitions();

            foreach (RectTransform child in GetComponentsInChildren<RectTransform>(true))
            {
                if (UIElementIsAbleToBeModified(child.gameObject)) { originalPositionMap.Add(child, child.anchoredPosition); }
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
                foreach (KeyValuePair<RectTransform, Vector2> kvp in originalPositionMap)
                {
                    kvp.Key.anchoredPosition = kvp.Value;
                }
            }
        }

        private void Update()
        {
            FindPlayerInput();
            EvaluateUIDefinitionsInUpdate();
            EvaluateControlSchemeDefinitions();
        }
    }
}