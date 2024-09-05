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
    //todo: Get the actual username of the user (otherwise just the email from auth provider)

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

  void CompiletoJSONFile()
  {

  }

  private void CompileToTxtFile()
  {
    //Compile and combine all the data into one stringable object.
    string compiledStringData = $"Vi Bug Report data \n" +
      $"generation date: {bugReportFormJSON.captureDateTime} \n" +
      $"reporter username: [tba]" +
      $"<System Information> \n" +
      $"os: {bugReportFormJSON.osVersion} \n" +
      $"device: {bugReportFormJSON.deviceName} \n" +
      $"Processor: {bugReportFormJSON.deviceProcessor} \n" +
      $"VideoCard: {bugReportFormJSON.deviceVideoCard} \n" +
      $"<REPORT INFO> \n" +
      $"brief Description: \n" +
      $"{bugReportFormJSON.briefDescription} \n" +
      $"Reproduction Step: \n" +
      $"{bugReportFormJSON.reproductionStep} \n" +
      $"Additional Infotmation: \n" +
      $"{bugReportFormJSON.additionalReport} \n" +
      $"screenshot attached: {doSendScreenShot}";

    //Save to bug report folder
    SaveFileTextContent(compiledStringData);
  }

  public void SaveFileTextContent(string content)
  {

  }

  public void SaveFileImageContent()
  {

  }
}
