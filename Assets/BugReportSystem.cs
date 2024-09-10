using Newtonsoft.Json;
using Proyecto26;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BugReportFormJSON
{
  public string osVersion;
  public string deviceName;
  public string userName;
  public string characterName;
  public string characterID;
  public string captureDateTime;
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
  public string debuglog;
  public string reportScreenshotBytes;
}

public class BugReportSystem : MonoBehaviour
{
  private string reportServerAPI = "http://localhost:1337";
  private Texture2D captureTexture;
  string imageBase64;
  private byte[] reportScreenshotbyte;
  private BugReportFormJSON bugReportFormJSON;

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

  // Start is called before the first frame update
  private void Start()
  {//Take quick second screenshot
    StartCoroutine(TakeScreenShot());
  }

  // Update is called once per frame
  private void Update()
  {
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

  //Save screenshot in user folder after upload for reference
  private void SaveScreenShot()
  {
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
    //Gather all the details provided by the user store them as sendable data
    bugReportFormJSON.briefDescription = briefDescriptionIF.text;
    bugReportFormJSON.reproductionStep = reproductionStepIF.text;
    bugReportFormJSON.additionalReport = additionalReportIF.text;

    string generatedFolderName;
    string generatedFileName;
    //generate text file content
    CompileToTxtFile();

    //generate report files
    string compiledData = CompileToTxtFile();
    uploadScreenshot = doSendScreenShot.enabled;
    //prep files for backup
    if (uploadScreenshot)
    {
      reportScreenshotbyte = captureTexture.EncodeToPNG();
      imageBase64 = System.Convert.ToBase64String(reportScreenshotbyte);
      Debug.Log(reportScreenshotbyte);
      //Save screenshot if applicable
      SaveScreenShot();
    }

    //Save the data to user PC

    //Generate a server friendly data
    BugReportFormServerData bugReportFormServerData = new BugReportFormServerData()
    {
      username = "TestingUsername",
      generationdate = bugReportFormJSON.captureDateTime,
      userDetails = $"reporter username: [tba] \nreporter Character Name: [TBA]",
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
      debuglog = "TO BE ADDED",
      reportScreenshotBytes = imageBase64
    };


    //Upload to Server
    StartCoroutine(BeginServerUpload(bugReportFormServerData));
  }

  private void CompiletoJSONFile()
  {
  }

  private string CompileToTxtFile(string Debuglogcontents = "(NO DEBUG CONTENTS)")
  {
    //Compile and combine all the data into one stringable object.
    compiledStringData = $"Vi Bug Report data - USER COPY \n" +
      $"generation date: {bugReportFormJSON.captureDateTime} \n" +
      $"reporter username: [tba]" +
      $"reporter Character Name: [tba]" +
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
      $"---END OF FILE---";

    return compiledStringData;
  }

  private IEnumerator SaveDataContent(string dataToTxt, string fileName, string FolderName)
  {
    yield return new WaitForSeconds(1);
  }

  private IEnumerator SaveScreenshotContent(string dataToTxt, string fileName, string FolderName)
  {
    yield return new WaitForSeconds(1);
  }

  private IEnumerator BeginServerUpload(BugReportFormServerData bfsd)
  {
    yield return new WaitForSeconds(1);
    string convertedBody = JsonConvert.SerializeObject(bfsd);
    Debug.Log(convertedBody);
    RestClient.Request(new RequestHelper
    {
      Method = "POST",
      Uri = $"{reportServerAPI}/uploadbugreport",
      ContentType = "application/json",
      Body = bfsd
    }).Then(
                response =>
                {
                  //getting long code
                  Debug.Log(response.Text);
                }).Catch(errorMessage =>
                {
                  Debug.LogError(errorMessage);
                });
    //Destroy the report window
  }
}