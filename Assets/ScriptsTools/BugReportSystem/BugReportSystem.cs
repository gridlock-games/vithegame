using Newtonsoft.Json;
using Proyecto26;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BugReportFormJSON
{
  public string osVersion;
  public string deviceName;
  public string userName;
  public string characterName;
  public string characterID;
  public string captureDateTime;
  public string gameVersion;
  public string deviceProcessor;
  public string deviceVideoCard;

  public string matchMapName;
  public string matchModeName;

  public string briefDescription;
  public string reproductionStep;
  public string additionalReport;
}

public class BugReportFormServerData
{
  public string username;
  public string generationdate;
  public string userDetails;
  public string matchInformation;
  public string systemspecData;
  public string userreportA;
  public string userreportB;
  public string userreportC;
  public bool hasDebugLogs;
  public string debuglog;
  public bool hasScreenshot;
  public string reportScreenshotBytes;
}

public class BugReportSystem : MonoBehaviour
{
  private string reportServerAPI = "http://38.54.25.140:7752";
  private Texture2D captureTexture;
  private string imageBase64;
  private byte[] reportScreenshotbyte;
  private BugReportFormJSON bugReportFormJSON;

  private string debugLogContent;

  private string screenshotFileName;
  private string bugreportFileFolderLocation;
  private string compiledStringData;

  private bool uploadScreenshot = false;

  //UI
  [SerializeField] private GameObject reportUiWindow;

  [SerializeField] private InputField briefDescriptionIF;
  [SerializeField] private InputField reproductionStepIF;
  [SerializeField] private InputField additionalReportIF;

  [SerializeField] private Text usernameTextBox;
  [SerializeField] private Text characterNameTextBox;
  [SerializeField] private Text captureDateTimeTextBox;
  [SerializeField] private Text osVersionTextBox;
  [SerializeField] private Text deviceNameTextBox;

  [SerializeField] private Toggle doSendScreenShot;
  [SerializeField] private RawImage screenshotUI;

  [SerializeField] private Button sendButton;
  [SerializeField] private Button exitButton;
  [SerializeField] private GameObject BugReportStatusWindow;
  [SerializeField] private Text BugReportStatus;

  // Start is called before the first frame update
  private void Start()
  {//Take quick second screenshot
    StartCoroutine(TakeScreenShot());
    //Get debug logs
    GetDebugLogs();
  }

  // Update is called once per frame
  private void Update()
  {
  }

  private void GetDebugLogs()
  {
    //DebugOverlay debugOverlay = GameObject.FindFirstObjectByType(typeof(DebugOverlay)).GetComponent<DebugOverlay>();
    //debugLogContent = debugOverlay.RetreveDebugLog();
    debugLogContent = "To be added";
  }
    
  
  private IEnumerator TakeScreenShot()
  {
    yield return new WaitForEndOfFrame();
    Texture2D screenImage = new Texture2D(Screen.width, Screen.height);
    screenImage.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
    screenImage.Apply();
    captureTexture = screenImage;
    //Show screenshot to UI
    screenshotUI.texture = captureTexture;
    yield return new WaitForEndOfFrame();
    bugReportFormJSON = new BugReportFormJSON();

    //Hide device name text for non-mobile user

    //Gather User Data
    GatherUserData();
    //Open form window
    reportUiWindow.SetActive(true);
  }

  private void GatherUserData()
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
    bugReportFormJSON.gameVersion = $"{Application.version.ToString()}";

    //Prints out the data on user UI for transpaency
    ShowUserinfo();
  }

  private void ShowUserinfo()
  {
    if (bugReportFormJSON.userName != null)
      usernameTextBox.text = bugReportFormJSON.userName;
    if (bugReportFormJSON.characterName != null)
      characterNameTextBox.text = bugReportFormJSON.characterName;
    captureDateTimeTextBox.text = bugReportFormJSON.captureDateTime;
    osVersionTextBox.text = bugReportFormJSON.osVersion;
    deviceNameTextBox.text = bugReportFormJSON.deviceName;
  }

  public void SendReportToServer()
  {
    //Show the status window and disable all controls
    sendButton.interactable = false;
    exitButton.interactable = false;
    briefDescriptionIF.interactable = false;
    reproductionStepIF.interactable= false;
    additionalReportIF.interactable = false;  
    BugReportStatusWindow.SetActive(true);
    BugReportStatus.text = "gathering information";
    //Gather all the details provided by the user store them as sendable data
    bugReportFormJSON.briefDescription = briefDescriptionIF.text;
    bugReportFormJSON.reproductionStep = reproductionStepIF.text;
    bugReportFormJSON.additionalReport = additionalReportIF.text;

    //generate report files
    
    uploadScreenshot = doSendScreenShot.isOn;
    string compiledData = CompileToTxtFile(uploadScreenshot);
    //Generate Folders
    BugReportStatus.text = "saving report files";
    string removesymbolDateTime = bugReportFormJSON.captureDateTime.Replace("/", string.Empty).Replace("\\", string.Empty).Replace(":", string.Empty).Replace(" ", string.Empty).ToString();
    string userReportFolder = $"{bugReportFormJSON.userName}_{removesymbolDateTime}";
    string reportFolderLocation = createDebugFolder(userReportFolder);

    string generatedUserReportFile = userReportFolder + "_userreport";
    string generatedDebugReportFile = userReportFolder + "_debugreport";
    //Save text files
    StartCoroutine(SaveDataContent(compiledData, generatedUserReportFile, reportFolderLocation));
    //Save screenshot to user folder
    if (uploadScreenshot)
    {
      string generatedImageReportFile = userReportFolder + "_screenshot";
      reportScreenshotbyte = captureTexture.EncodeToPNG();
      imageBase64 = System.Convert.ToBase64String(reportScreenshotbyte);
      Debug.Log(reportScreenshotbyte);
      //Save screenshot if applicable
      StartCoroutine(SaveScreenshotContent(reportScreenshotbyte, generatedImageReportFile, reportFolderLocation));
    }
    //Generate a server friendly data
    BugReportFormServerData bugReportFormServerData = new BugReportFormServerData()
    {
      username = "TestingUsername",
      generationdate = bugReportFormJSON.captureDateTime,
      userDetails = $"reporter username: [tba] \nreporter Character Name: [TBA] \nGameVersion: {bugReportFormJSON.gameVersion}",
      matchInformation = $"Map Name: {bugReportFormJSON.matchMapName} \n Stage Name: {bugReportFormJSON.matchModeName}",
      systemspecData = $"os: {bugReportFormJSON.osVersion} \n" +
      $"device: {bugReportFormJSON.deviceName} \n" +
      $"Processor: {bugReportFormJSON.deviceProcessor} \n" +
      $"VideoCard: {bugReportFormJSON.deviceVideoCard} \n" +
      $"<Match Info>" +
      $"Map Name: {bugReportFormJSON.matchMapName} \n" +
      $"Stage Name: {bugReportFormJSON.matchModeName} \n",
      userreportA = bugReportFormJSON.briefDescription,
      userreportB = bugReportFormJSON.reproductionStep,
      userreportC = bugReportFormJSON.additionalReport,
      hasDebugLogs = true,
      debuglog = debugLogContent,
      hasScreenshot = uploadScreenshot,
      reportScreenshotBytes = imageBase64
    };

    //Upload to Server
    StartCoroutine(BeginServerUpload(bugReportFormServerData));
  }

  private string createDebugFolder(string generatedUserFolder)
  {
    //Create a debug folder if not yet created
    var userFolder = Application.persistentDataPath + "/bugreport";
    if (!Directory.Exists(userFolder))
    {
      Directory.CreateDirectory(userFolder);
      Debug.Log("Bug report Folder Created at: " + userFolder);
    }

    var currentReportFolder = userFolder + $"/{generatedUserFolder}";
    if (!Directory.Exists(currentReportFolder))
    {
      Directory.CreateDirectory(currentReportFolder);
      Debug.Log("Bug report Folder Created at: " + currentReportFolder);
    }

    return currentReportFolder;
  }

  private void CompiletoJSONFile()
  {
  }

  private string CompileToTxtFile(bool hasScreenshot, string Debuglogcontents = "(NO DEBUG CONTENTS)")
  {
    BugReportStatus.text = "compiling report";
    string screenShotStatus = "";
    if(!hasScreenshot)
    {
      screenShotStatus = "---No Screenshot Generated---\n";
    }
    //Compile and combine all the data into one stringable object.
    compiledStringData = $"Vi Bug Report data - USER COPY \n" +
      $"generation date: {bugReportFormJSON.captureDateTime} \n" +
      $"reporter username: [tba]\n" +
      $"reporter Character Name: [tba]\n" +
      $"Game Version: {bugReportFormJSON.gameVersion}"+
      $"<System Information> \n" +
      $"os: {bugReportFormJSON.osVersion} \n" +
      $"device: {bugReportFormJSON.deviceName} \n" +
      $"Processor: {bugReportFormJSON.deviceProcessor} \n" +
      $"VideoCard: {bugReportFormJSON.deviceVideoCard} \n" +
      $"<Match Info>" +
      $"Map Name: {bugReportFormJSON.matchMapName} \n" +
      $"Stage Name: {bugReportFormJSON.matchModeName} \n" +
      $"<Report Info> \n" +
      $"brief Description: \n" +
      $"{bugReportFormJSON.briefDescription} \n" +
      $"Reproduction Step: \n" +
      $"{bugReportFormJSON.reproductionStep} \n" +
      $"Additional Infotmation: \n" +
      $"{bugReportFormJSON.additionalReport} \n" +
      $"<Debug Data> \n" +
      $"{Debuglogcontents} \n" +
      $"screenshot generated: {screenShotStatus}" +
      $"---END OF FILE---";

    return compiledStringData;
  }

  private IEnumerator SaveDataContent(string dataToTxt, string fileName, string FolderName)
  {
    yield return new WaitForSeconds(1);
    string filePath = FolderName + $"/{fileName}.txt";

    if (!File.Exists(filePath))
    {
      File.WriteAllText(filePath, dataToTxt);
      Debug.Log("User Report File Created: " + filePath);
    }
    else
    {
      Debug.Log("File already exists");
    }
  }

  private IEnumerator SaveScreenshotContent(byte[] dataToTxt, string fileName, string FolderName)
  {
    BugReportStatus.text = "saving screenshot";
    yield return new WaitForSeconds(1);

    string filePath = FolderName + $"/{fileName}.png";

    if (!File.Exists(filePath))
    {
      File.WriteAllBytes(filePath, dataToTxt);
      Debug.Log("Image File Created: " + filePath);
    }
    else
    {
      Debug.Log("File already exists");
    }
  }

  private IEnumerator BeginServerUpload(BugReportFormServerData bfsd)
  {
    bool successfulUpload = false;
    BugReportStatus.text = "uploading files to server";
    yield return new WaitForSeconds(1);
    string convertedBody = JsonConvert.SerializeObject(bfsd);
    Debug.Log(convertedBody);
    yield return RestClient.Request(new RequestHelper
    {
      Method = "POST",
      Uri = $"{reportServerAPI}/uploadbugreport",
      ContentType = "application/json",
      Body = bfsd
    }).Then(
                response =>
                {
                  BugReportStatus.text = "upload successful.\nClosing Report Window";
                  Debug.Log(response.Text);
                  //Destroy the report window
                  successfulUpload = true;
                }).Catch(errorMessage =>
                {
                  BugReportStatus.text = "upload Failed. Please try again";
                  sendButton.interactable = true;
                  exitButton.interactable = true;
                  Debug.LogError(errorMessage);
                });

    exitButton.interactable = enabled;
    if (successfulUpload)
    {
      yield return new WaitForSeconds(3);
      CloseReportWindow();
    }
  }

  public void CloseReportWindow()
  {
    Destroy(this.gameObject);
  }
}