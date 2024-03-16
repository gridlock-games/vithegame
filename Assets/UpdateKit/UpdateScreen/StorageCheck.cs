using UnityEngine;
using UnityEngine.Events;

//This Handles codes related to free space before download the assets. - To prevent devices bugs related to storage a 1GB headway (1000MB) was setup.
public class StorageCheck : MonoBehaviour
{
  [SerializeField] private float sizeheadwaySpace = 1000.0f; //Mimicing 1Gb
  [SerializeField] private GameObject notificationWindowCanvas;
  public string FileSizeError = $"This device storage space is not sufficient to download additional content.";
  private UnityEvent refreshGame;
  private float freeSpaceRequired;

  // Start is called before the first frame update
  private void Start()
  {
    refreshGame.AddListener(RestartSoftware);
  }

  // Update is called once per frame
  private void Update()
  {
  }

  public bool DeterminedFreeSizeOutput(float gameFileSize)
  {
    float emptyStorageSize = GetDeviceFreeStorage();
    freeSpaceRequired = emptyStorageSize;
    if (emptyStorageSize + sizeheadwaySpace < gameFileSize)
    {
      //Cancel the download and start notification
      NotifyUser();
      return false;
    }
    else
    {
      return true;
    }
  }

  public float GetDeviceFreeStorage()
  {
#if UNITY_ANDROID
    AndroidJavaObject statFs = new AndroidJavaObject("android.os.StatFs", Application.persistentDataPath);
    long freeBytes = statFs.Call<long>("getAvailableBytes");
    return (float)freeBytes;
  }

#endif
#if UNITY_IOS
    return 1000.0f;
  }

#endif
#if UNITY_EDITOR_WIN && !(UNITY_ANDROID || UNITY_IOS)
        return 1000.0f;
    }
#endif

  private void NotifyUser()
  {
    GameObject notificationWindow = notificationWindowCanvas.GetComponentInChildren<MessageNotificationObject>().gameObject;
    MessageNotificationObject messageNotification = notificationWindow.GetComponent<MessageNotificationObject>();
    messageNotification.ShowDialogueBox(FileSizeError + $"\nFree Space required: " + freeSpaceRequired, "Retry", refreshGame);
    Instantiate(notificationWindowCanvas);
  }

  private void RestartSoftware()
  {
  }
}