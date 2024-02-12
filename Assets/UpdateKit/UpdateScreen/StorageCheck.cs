using UnityEngine;
using UnityEngine.Events;

public class StorageCheck : MonoBehaviour
{
  [SerializeField] private float sizeheadwaySpace = 1000.0f; //Mimicing 1Gb
  [SerializeField] private GameObject notificationWindowCanvas;
  public string FileSizeError = $"This device storage space is not suffience to download additional content.";
  private UnityEvent refreshGame;

  // Start is called before the first frame update
  private void Start()
  {
    refreshGame.AddListener(RestartSoftware);
  }

  // Update is called once per frame
  private void Update()
  {
  }

  public bool DeterminedFreeSizeOutput(float deviceFileSize)
  {
    float emptyStorageSize = GetDeviceFreeStorage();
    if (emptyStorageSize + sizeheadwaySpace < deviceFileSize)
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
  private void NotifyUser()
  {
    GameObject notificationWindow = notificationWindowCanvas.GetComponentInChildren<MessageNotificationObject>().gameObject;
    MessageNotificationObject messageNotification = notificationWindow.GetComponent<MessageNotificationObject>();
    messageNotification.ShowDialogueBox(FileSizeError, "Retry", refreshGame);
    Instantiate(notificationWindowCanvas);
  }

  private void RestartSoftware()
  {
  }
}