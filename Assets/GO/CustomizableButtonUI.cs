using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomizableButtonUI : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public class buttonSetupData
{
  public string deviceModel;
  public Vector2 deviceResolution;

  public List<buttonUIData> buttonList;
}
public class buttonUIData
{
  public string buttonID;
  public bool isUserActive;
  public Vector2 buttonPosition;
}