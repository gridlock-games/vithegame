using System;
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
    [SerializeField] private UIDefinition LiveplatformUIDefinition;
    [SerializeField] private MoveUIDefIdentifier[] moveUIDefinitions;
    [SerializeField] private ControlSchemeTextDefinition[] controlSchemeTextDefinitions; 


    private void setCorrectPlatformUiDefinition()
    {
      foreach (var platformItem in platformUIDefinitions)
      {
        //Check the platform identifier and then disregards the rest we don't need to check anymore
        if (platformItem.platforms.Contains(Application.platform))
        {
          LiveplatformUIDefinition = platformItem;
          break;
        }
      }
    }

    private void Start()
    {
      setCorrectPlatformUiDefinition();
      ChangeUILayout();
    }

    private void ChangeUILayout()
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

    private void Update()
    {
      foreach (MoveUIDefinition moveUIDefinition in LiveplatformUIDefinition.objectsToMove)
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

  [System.Serializable]
  public struct ControlSchemeTextDefinition
  {
    public RuntimePlatform[] platforms;
    public Text textElement;
    public string action;
    public string stringBeforeBinding;
    public string stringAfterBinding;
  }

  [System.Serializable]
  public struct UIDefinition
  {
    public int uidefID;
    public RuntimePlatform[] platforms;
    public GameObject[] gameObjectsToEnable;
    public GameObject[] gameObjectsToDestroy;
    public MoveUIDefinition[] objectsToMove;
  }

  [System.Serializable]
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

  [System.Serializable]
  public struct MoveUIDefIdentifier
  {
    public string objectID;
    public GameObject actualGameObject;
  }
}