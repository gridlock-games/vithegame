using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BugReportFormJSON
{
  public string osVersion;
  public string deviceName;
  public string userName;
  public string captureDateTime;
  public string deviceProcessor;
  public string deviceVideoCard;

  public string briefDescription;
  public string reproductionStep;
  public string additionalReport;
}
public class BugReportSystem : MonoBehaviour
{
  Texture2D captureTexture;
  RawImage screenshotUI;

  BugReportFormJSON bugReportFormJSON;

  //UI
  [SerializeField] InputField briefDescriptionIF;
  [SerializeField] InputField reproductionStepIF;
  [SerializeField] InputField additionalReportIF;

  [SerializeField] Toggle doSendScreenShot;



    // Start is called before the first frame update
    void Start()
    {
    bugReportFormJSON = new BugReportFormJSON();
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
    bugReportFormJSON.captureDateTime = System.DateTime.UtcNow.ToString();

    //Get the username of the reporter

    //Gather basic device infomation
    bugReportFormJSON.osVersion = SystemInfo.operatingSystem;
    bugReportFormJSON.deviceName = $"{SystemInfo.deviceName} - {SystemInfo.deviceModel} - {SystemInfo.deviceType}";
    bugReportFormJSON.deviceVideoCard = $"{SystemInfo.graphicsDeviceVendor} - {SystemInfo.graphicsDeviceName} - {SystemInfo.graphicsDeviceVersion}";
    bugReportFormJSON.deviceProcessor = $"{SystemInfo.processorType}";

    //Prints out the data on user UI for transpaency

  }

  void SendReportToServer()
  {
    //Gather all the details provided by the user store them as sendable data
    bugReportFormJSON.briefDescription = briefDescriptionIF.text;
    bugReportFormJSON.reproductionStep = reproductionStepIF.text;
    bugReportFormJSON.additionalReport = additionalReportIF.text;

    //Save as human readable file as a backup
    CompileToTxtFile();

    //Save as a JSON file for easier file conversion purposes

    //prep data for transfer

    //Transfer String Data

    //Transfer Image Data
  }

  private void CompileToTxtFile()
  {
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

      //Save to bug report folder
  }
}
