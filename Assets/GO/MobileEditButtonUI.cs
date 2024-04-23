using GameCreator.Pool;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Vi.UI
{
  public class MobileEditButtonUI : MonoBehaviour
  {

    public RectTransform[] editableUIObjects;
    public int uisettingID;
    [SerializeField] MoveUIDefinition[] convertedObject;
    [SerializeField] private UIDefinition[] platformUIDefinitions;
    [SerializeField] PlatformUIDefinition platformUIDefinition;
    // Start is called before the first frame update
    void Start()
    {
      LoadPreviousData();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void LoadPreviousData()
    {
      String previousModifcationdataString = PlayerPrefs.GetString("ButtonUiLayout");
      convertedObject = JsonConvert.DeserializeObject<MoveUIDefinition[]>(previousModifcationdataString);
      //Get the Platform UI Definition and update the text
      
    }


      public void SaveAsData()
    {
      convertedObject = new MoveUIDefinition[editableUIObjects.Length];
      for (int i = 0; i < editableUIObjects.Length; i++)
      {
        convertedObject[i].objectID = editableUIObjects[i].gameObject.GetComponent<DragableUIObject>().moveUIDefIdentifier.objectID;
        convertedObject[i].gameObjectToMove = editableUIObjects[i].gameObject.GetComponent<DragableUIObject>().moveUIDefIdentifier.actualGameObject; //Remove if broke/useless
        convertedObject[i].newAnchoredPosition = editableUIObjects[i].anchoredPosition;
        convertedObject[i].anchorMinOverride = editableUIObjects[i].anchorMin;
        convertedObject[i].shouldOverrideAnchors = false;
        convertedObject[i].anchorMaxOverride = editableUIObjects[i].anchorMax;
        convertedObject[i].pivotOverride = editableUIObjects[i].pivot;
      }


      //Save the data to the user input
      if (convertedObject != null)
      {
        String convertedData = JsonConvert.SerializeObject(convertedObject);
        PlayerPrefs.SetString("ButtonUiLayout", convertedData);
      }

      //Show error if there a problem saving/or is null
    }
    //private struct MoveUIDefinition
    //{
    //  public GameObject gameObjectToMove;
    //  public Vector2 newAnchoredPosition;
    //  public bool shouldOverrideAnchors;
    //  public Vector2 anchorMinOverride;
    //  public Vector2 anchorMaxOverride;
    //  public Vector2 pivotOverride;
    //}

  }
}