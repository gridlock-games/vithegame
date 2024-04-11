using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomizableButtonUI : MonoBehaviour
{
  buttonSetupData buttonSetup;

  //set button Data;
  public void setNewButtonLocation()
  {

  }
}

public class buttonSetupData
{
  public string setid;

  //optional Setup
  public string deviceModel;
  public Vector2 deviceResolution;

  public List<buttonUIData> buttonList;

  public int findObject(string buttonID)
  {
    //find if the button Exist
    return 0;

  }

  public void setNewButtonLocation(string buttonID, bool enabled, Vector2 newPosition, float newSize)
  {
    //find if the button Exist
    
  }
}
public class buttonUIData
{
  public string buttonID { get; set; }
  public bool isUserActive { get; set; }
  public Vector2 buttonPosition { get; set; }
  public float buttonSize { get; set; }
}