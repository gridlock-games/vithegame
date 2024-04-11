using System.Collections.Generic;
using UnityEngine;

public class CustomizableButtonUI : MonoBehaviour
{
  private List<buttonSetupData> buttonSetup;

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



  public void setNewButtonLocation(string ButtonID, bool enabled, Vector2 newPosition, float newSize)
  {
    //find if the button Exist
    int idnum = findObject(ButtonID);

    //If not exist create new button
    if (idnum == -1)
    {
      createNewCustomButton(ButtonID, enabled, newPosition, newSize);
    }
    //ModifyExistancing
    else
    {
      modifyCustomButton(idnum, enabled, newPosition, newSize);
    }
  }

  public void createNewCustomButton(string ButtonID, bool enabled, Vector2 newPosition, float newSize)
  {
    buttonList.Add(new buttonUIData()
    {
      buttonID = ButtonID,
      isUserActive = enabled,
      buttonPosition = newPosition,
      buttonSize = newSize
    });
  }

  public void modifyCustomButton(int ButtonID, bool enabled, Vector2 newPosition, float newSize)
  {
    buttonList[ButtonID].isUserActive = enabled;
    buttonList[ButtonID].buttonPosition = newPosition;
    buttonList[ButtonID].buttonSize = newSize;
  }
}



public class buttonUIData
{
  public string buttonID { get; set; }
  public bool isUserActive { get; set; }
  public Vector2 buttonPosition { get; set; }
  public float buttonSize { get; set; }
}