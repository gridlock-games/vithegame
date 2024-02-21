using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class CDNDownloadManager : MonoBehaviour
{
  CDNDownloadAndCheck dac = new CDNDownloadAndCheck();
  StorageCheck sc = new StorageCheck();

  [SerializeField]
  MessageNotificationObject mno;

  [SerializeField]
  List<string> updateSetList = new List<string>();

  [SerializeField]
  List<string> setToUpdate = new List<string>();

  float downloadSize = 0;
  [SerializeField]
  UnityEvent downloadOption;

  // Start is called before the first frame update
  void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

  void beginCheck()
  {

  }
  void FailureNotification(string message)
  {

  }

  void StartUpdateNotification()
  {
    bool checkSuccessful = false;

    (checkSuccessful, updateSetList,downloadSize) = dac.CheckingForUpdate(updateSetList);

    if (checkSuccessful)
    {
      //Notify user of the update
      mno.ShowDialogueBox($"A new update is avalable, Do you want to down now? \n\n FileSize {downloadSize}", "Download Update", downloadOption);
    }
  }

  void StartUpdateDownload()
  {
    dac.DownloadExternalFiles(setToUpdate);
  }

}
