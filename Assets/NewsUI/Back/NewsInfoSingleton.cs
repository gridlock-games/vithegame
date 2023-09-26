using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewsInfoSingleton : MonoBehaviour
{

  public static NewsInfoSingleton Instance;

  public bool newsSeen;
  public bool neverSeenAgain;

  public GameObject infoWindow;

  private void Awake()
  {
    if (Instance != null)
    {
      Destroy(gameObject);
      return;
    }

    Instance = this;
    DontDestroyOnLoad(gameObject);
   
    //load Data here

    //Check new UI Status
    ShowNews();
  }

  void ShowNews()
  {
    if (newsSeen || neverSeenAgain)
    {
      //Don't do anything

      //for redundency
      //infoWindow.SetActive(false);
    }
    else
    {
      var windowGameObject = Instantiate(infoWindow);
      windowGameObject.GetComponent<NewsInfoWindow>().newsinfosingleton = this;
      //Get component to assigned this object
    }
  }
}
