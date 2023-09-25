using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewsInfoWindow : MonoBehaviour
{
  public NewsInfoSingleton newsinfosingleton;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

  //Close
  void CloseWindow()
  {
    newsinfosingleton.newsSeen = true;
    Destroy(this.gameObject);
  }
}
