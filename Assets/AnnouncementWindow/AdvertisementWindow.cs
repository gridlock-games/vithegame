using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AdvertisementWindow : MonoBehaviour
{
  [SerializeField] AdvertiseItem currentItem;
  [SerializeField] Image adsImagePlacecard;
  // Start is called before the first frame update
  void Awake()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

  public void updateEvent(AdvertiseItem adsItem)
  {
    currentItem = adsItem;
    adsImagePlacecard.sprite = currentItem.adImage;
  }

  //on click
  void clickEvent()
  {

  }
}
