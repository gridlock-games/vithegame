using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Vi.Core;

namespace Vi.UI
{
    public class PlatformUIDefinition : MonoBehaviour
    {
        [SerializeField] private InputActionAsset controlsAsset;
        [SerializeField] private UIDefinition[] platformUIDefinitions;
        [SerializeField] private ControlSchemeTextDefinition[] controlSchemeTextDefinitions;

        [System.Serializable]
        private struct UIDefinition
        {
            public RuntimePlatform[] platforms;
            public GameObject[] gameObjectsToEnable;
            public GameObject[] gameObjectsToDestroy;
            public MoveUIDefinition[] objectsToMove;
        }

        [System.Serializable]
        private struct MoveUIDefinition
        {
            public GameObject gameObjectToMove;
            public Vector2 newAnchoredPosition;
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
                        moveUIDefinition.gameObjectToMove.GetComponent<RectTransform>().anchoredPosition = moveUIDefinition.newAnchoredPosition;
                    }
                }

                foreach (GameObject g in platformUIDefinition.gameObjectsToDestroy)
                {
                    if (platformUIDefinition.platforms.Contains(Application.platform)) { Destroy(g); }
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
                        moveUIDefinition.gameObjectToMove.GetComponent<RectTransform>().anchoredPosition = moveUIDefinition.newAnchoredPosition;
                    }
                }
            }

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