using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobileEditButtonUI : MonoBehaviour
{

  public RectTransform[] editableUIObjects;
  public int uisettingID;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SaveAsData()
  {
    MoveUIDefinition[] convertedObject = new MoveUIDefinition[editableUIObjects.Length];
        for (int i = 0; i < editableUIObjects.Length; i++)
        {
      convertedObject[i].gameObjectToMove = editableUIObjects[i].gameObject;
      convertedObject[i].newAnchoredPosition = editableUIObjects[i].anchoredPosition;
      convertedObject[i].anchorMinOverride = editableUIObjects[i].anchorMin;
      convertedObject[i].shouldOverrideAnchors = false;
      convertedObject[i].anchorMaxOverride = editableUIObjects[i].anchorMax;
      convertedObject[i].pivotOverride = editableUIObjects[i].pivot;
    }

        //Save the data to the user input
    }

  //Note please put this outside to make this accessible
  private struct MoveUIDefinition
  {
    public GameObject gameObjectToMove;
    public Vector2 newAnchoredPosition;
    public bool shouldOverrideAnchors;
    public Vector2 anchorMinOverride;
    public Vector2 anchorMaxOverride;
    public Vector2 pivotOverride;
  }
}
