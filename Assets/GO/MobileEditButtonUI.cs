using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.UI
{
  public class MobileEditButtonUI : MonoBehaviour
  {
    public RectTransform[] editableUIObjects;

    //[SerializeField] public MoveUIDefinition[] convertedObject;
    [SerializeField] public List<MoveUIDefinition_Class> convertedObject;
    [SerializeField] public UIDefinition[] platformUIDefinitions;


    // Start is called before the first frame update
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
    }

    public void SaveAsData()
    {
      convertedObject = new List<MoveUIDefinition_Class>();
      for (int i = 0; i < editableUIObjects.Length; i++)
      {
        MoveUIDefinition_Class toConvertRaw = new MoveUIDefinition_Class();

        toConvertRaw.objectID = editableUIObjects[i].gameObject.GetComponent<DragableUIObject>().moveUIDefIdentifier.objectID;
        if (toConvertRaw.objectID == null || toConvertRaw.objectID == "")
        {
          toConvertRaw.objectID = "unassign" + i;
        }
        //convertedObject[i].gameObjectToMove = editableUIObjects[i].gameObject.GetComponent<DragableUIObject>().moveUIDefIdentifier.actualGameObject; //Remove if broke/useless
        toConvertRaw.newAnchoredPosition = new float[] { editableUIObjects[i].anchoredPosition.x, editableUIObjects[i].anchoredPosition.y };
        toConvertRaw.anchorMinOverride = new float[] { editableUIObjects[i].anchorMin.x, editableUIObjects[i].anchorMin.y }; 
        toConvertRaw.shouldOverrideAnchors = false;
        toConvertRaw.anchorMaxOverride = new float[] { editableUIObjects[i].anchorMax.x, editableUIObjects[i].anchorMax.y };
        toConvertRaw.pivotOverride = new float[] { editableUIObjects[i].pivot.x, editableUIObjects[i].pivot.y };
        convertedObject.Add(toConvertRaw);
      }

      Debug.Log("Saving");
      //Save the data to the user input
      if (convertedObject != null)
      {
        //var convertedData = JsonUtility.ToJson(convertedObject);
        
        var convertedData = JsonConvert.SerializeObject(convertedObject);
        Debug.Log(convertedData);
        PlayerPrefs.SetString("ButtonUiLayout", convertedData);
        Debug.Log("Completed");
      }

      //Show error if there a problem saving/or is null

      //Then close the Ui and return to the menu - Also Reload Platform UI Definition if needed.
      ClosePrefab();
    }

    public void ClosePrefab()
    {
      Destroy(this.gameObject);
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