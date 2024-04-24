using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Vi.Core;

namespace Vi.UI
{
    public class PlatformUIDefinition : MonoBehaviour
    {
        [SerializeField] private InputActionAsset controlsAsset;
        [SerializeField] private UIDefinition[] platformUIDefinitions;
        [SerializeField] private UIDefinition liveplatformUIDefinition;
        [SerializeField] private List<MoveUIDefinition> deSeralizedObject;
        [SerializeField] private MoveUIDefIdentifier[] moveUIDefinitions;
        [SerializeField] private ControlSchemeTextDefinition[] controlSchemeTextDefinitions;

        private void setCorrectPlatformUiDefinition()
        {
            //LiveplatformUIDefinition = platformUIDefinitions[0];

            foreach (var platformItem in platformUIDefinitions)
            {
                //Check the platform identifier and then disregards the rest we don't need to check anymore
                if (platformItem.platforms.Contains(Application.platform))
                {
                    liveplatformUIDefinition = platformItem;
                    break;
                }
            }
        }

        private void LoadAndSetCorrectIDtoGameObject()
        {
            //Load data from Playerdef
            String previousModifcationdataString = PlayerPrefs.GetString("ButtonUiLayout");
            Debug.Log(previousModifcationdataString);
            //MoveUIDefinition_Class[] deconvert = JsonUtility.FromJson<MoveUIDefinition_Class[]>(previousModifcationdataString);
            MoveUIDefinition_Class[] deconvert = JsonConvert.DeserializeObject<MoveUIDefinition_Class[]>(previousModifcationdataString);
            Debug.Log(deconvert);

            //Convert from MoveUIDefinition_Class to MoveUIDefinition
            foreach (var item in deconvert)
            {
                MoveUIDefinition uiDef = new MoveUIDefinition();
                uiDef.objectID = item.objectID;
                uiDef.newAnchoredPosition = new Vector2(item.newAnchoredPosition[0], item.newAnchoredPosition[1]);
                uiDef.anchorMinOverride = new Vector2(item.anchorMinOverride[0], item.anchorMinOverride[1]);
                uiDef.shouldOverrideAnchors = item.shouldOverrideAnchors;
                uiDef.anchorMaxOverride = new Vector2(item.anchorMaxOverride[0], item.anchorMaxOverride[1]);
                uiDef.pivotOverride = new Vector2(item.pivotOverride[0], item.pivotOverride[1]);

                deSeralizedObject.Add(uiDef);
            }

            //set the prelive data to deseralized data

            liveplatformUIDefinition.objectsToMove = deSeralizedObject.ToArray();

            foreach (var movedef in moveUIDefinitions)
            {
                for (int i = 0; i < liveplatformUIDefinition.objectsToMove.Length; i++)
                {
                    if (movedef.objectID == liveplatformUIDefinition.objectsToMove[i].objectID && movedef.actualGameObject != null)
                    {
                        liveplatformUIDefinition.objectsToMove[i].gameObjectToMove = movedef.actualGameObject;
                        break;
                    }
                }
            }
        }

        private void Start()
        {
            setCorrectPlatformUiDefinition();
            LoadAndSetCorrectIDtoGameObject();
            ChangeUILayout();
        }

        private void ChangeUILayout()
        {
            //foreach (UIDefinition platformUIDefinition in platformUIDefinitions)
            //{
            foreach (GameObject g in liveplatformUIDefinition.gameObjectsToEnable)
            {
                g.SetActive(liveplatformUIDefinition.platforms.Contains(Application.platform));
            }

            foreach (MoveUIDefinition moveUIDefinition in liveplatformUIDefinition.objectsToMove)
            {
                if (moveUIDefinition.gameObjectToMove != null)
                {
                    if (liveplatformUIDefinition.platforms.Contains(Application.platform))
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

            foreach (GameObject g in liveplatformUIDefinition.gameObjectsToDestroy)
            {
                if (liveplatformUIDefinition.platforms.Contains(Application.platform)) { Destroy(g); }
            }
            //}
        }

        private void Update()
        {
            if (liveplatformUIDefinition.objectsToMove != null)
            {
                foreach (MoveUIDefinition moveUIDefinition in liveplatformUIDefinition.objectsToMove)
                {
                    if (moveUIDefinition.gameObjectToMove != null)
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
            ControlSchemeDef();
        }

        private void ControlSchemeDef()
        {
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

        public UIDefinition[] GetPlatformUIDefinitions()
        {
            return platformUIDefinitions;
        }
    }

    [Serializable]
    public struct ControlSchemeTextDefinition
    {
        public RuntimePlatform[] platforms;
        public Text textElement;
        public string action;
        public string stringBeforeBinding;
        public string stringAfterBinding;
    }

    [Serializable]
    public struct UIDefinition
    {
        public RuntimePlatform[] platforms;
        public GameObject[] gameObjectsToEnable;
        public GameObject[] gameObjectsToDestroy;
        public MoveUIDefinition[] objectsToMove;
    }

    [Serializable]
    public struct MoveUIDefinition
    {
        public string objectID;
        public GameObject gameObjectToMove;
        public Vector2 newAnchoredPosition;
        public bool shouldOverrideAnchors;
        public Vector2 anchorMinOverride;
        public Vector2 anchorMaxOverride;
        public Vector2 pivotOverride;
    }

    [Serializable]
    public struct MoveUIDefIdentifier
    {
        public string objectID;
        public GameObject actualGameObject;
    }

    [Serializable]
    public class MoveUIDefinition_Class
    {
        public string objectID;
        public GameObject gameObjectToMove;
        public float[] newAnchoredPosition;
        public bool shouldOverrideAnchors;
        public float[] anchorMinOverride;
        public float[] anchorMaxOverride;
        public float[] pivotOverride;
    }
}