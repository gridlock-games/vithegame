using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BugReportSystem : MonoBehaviour
{
  Texture2D captureTexture;
  RawImage screenshotUI;

  //RawData
  string osVersion;
  string deviceName;
  string userName;
  string captureDateTime;
  string deviceProcessor;
  string deviceVideoCard;

  string briefDescription;
  string reproductionStep;
  string additionalReport;

  //UI
  [SerializeField] InputField briefDescriptionIF;
  [SerializeField] InputField reproductionStepIF;
  [SerializeField] InputField additionalReportIF;

  [SerializeField] Toggle doSendScreenShot;



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

  void TakeScreenShot()
  {
    Texture2D screenImage = new Texture2D(Screen.width, Screen.height);
    //Take a screenshot from Screen
    screenImage.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
    screenImage.Apply();
    captureTexture = screenImage;
    //Show screenshot to UI
    screenshotUI.texture = captureTexture;
  }

  void GatherUserData()
  {
    //Capture Date and time
    captureDateTime = System.DateTime.UtcNow.ToString();

    //Get the username of the reporter

    //Gather basic device infomation
    osVersion = SystemInfo.operatingSystem;
    deviceName = $"{SystemInfo.deviceName} - {SystemInfo.deviceModel} - {SystemInfo.deviceType}";
    deviceVideoCard = $"{SystemInfo.graphicsDeviceVendor} - {SystemInfo.graphicsDeviceName} - {SystemInfo.graphicsDeviceVersion}";
    deviceProcessor = $"{SystemInfo.processorType}";

    //Prints out the data on user UI for transpaency

  }

  void SendReportToServer()
  {
    //Gather all the details provided by the user store them as sendable data
    briefDescription = briefDescriptionIF.text;
    reproductionStep = reproductionStepIF.text;
    additionalReport = additionalReportIF.text;

    //Compile and combine all the data into one stringable object.
    string compiledStringData = $"Vi Bug Report data \n" +
      $"generation date: {captureDateTime} \n" +
      $"reporter username: [tba]" +
      $"<System Information> \n" +
      $"os: {osVersion} \n" +
      $"device: {deviceName} \n" +
      $"Processor: {deviceProcessor} \n" +
      $"VideoCard: {deviceVideoCard} \n" +
      $"<REPORT INFO> \n" +
      $"brief Description: \n" +
      $"{briefDescription} \n" +
      $"Reproduction Step: \n" +
      $"{reproductionStep} \n" +
      $"Additional Infotmation: \n" +
      $"{additionalReport} \n" +
      $"screenshot attached: {doSendScreenShot}";

    //prep data for transfer

    //Transfer String Data

    //Transfer Image Data
  }
}
