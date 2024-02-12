using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CDNDownloadManager : MonoBehaviour
{
  CDNDownloadAndCheck dac = new CDNDownloadAndCheck();
  StorageCheck sc = new StorageCheck();

  [SerializeField]
  MessageNotificationObject mno;

  [SerializeField]
  List<string> setToUpdate = new List<string>();
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
}
