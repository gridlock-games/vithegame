using Newtonsoft.Json;
using Proyecto26;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Vi.UI
{
  public class GameScreenshot : MonoBehaviour
  {
    private Texture2D captureTexture;
    //[SerializeField] private RawImage screenshotUI;
    [SerializeField] private GameObject screenshotFrontground;
    private byte[] screenshotbyte;
    // Start is called before the first frame update
    void Start()
    {
      StartCoroutine(TakeScreenShot());
    }



    private IEnumerator TakeScreenShot()
    {
      //Show screenshot
      screenshotFrontground.SetActive(true);
      yield return new WaitForEndOfFrame();
      Texture2D screenImage = new Texture2D(Screen.width, Screen.height);
      screenImage.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
      screenImage.Apply();
      captureTexture = screenImage;
      //Show screenshot to UI
      //screenshotUI.texture = captureTexture;
      yield return new WaitForEndOfFrame();
      screenshotFrontground.SetActive(false);

      //Save and send a notification of screenshot
      ProcessScreenshot();
    }

    private void ProcessScreenshot()
    {
      var folderName = createScreenshotFolder();
      StartCoroutine(SaveScreenshotContent(folderName));
    }
      private IEnumerator SaveScreenshotContent(string FolderName)
    {
      screenshotbyte = captureTexture.EncodeToPNG();
      string screenshotDT = System.DateTime.UtcNow.ToString();
      string removesymbolDateTime = screenshotDT.Replace("/", string.Empty).Replace("\\", string.Empty).Replace(":", string.Empty).Replace(" ", string.Empty).ToString();

      yield return new WaitForSeconds(1);

      string filePath = FolderName + $"/{removesymbolDateTime}.png";

      if (!File.Exists(filePath))
      {
        File.WriteAllBytes(filePath, screenshotbyte);
        Debug.Log("Image File Created: " + filePath);
      }
      else
      {
        Debug.Log("File already exists");
      }

      //Destroy GameObject Once complete
    }

    private string createScreenshotFolder()
    {
      //Create a Screenshot folder if not yet created
      var userFolder = Application.persistentDataPath + "/screenshot";
      if (!Directory.Exists(userFolder))
      {
        Directory.CreateDirectory(userFolder);
        Debug.Log("Bug report Folder Created at: " + userFolder);
      }

      return userFolder;
    }
  }
}