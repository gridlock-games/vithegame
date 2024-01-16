using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AdvertisementUI : MonoBehaviour
{
  [SerializeField] private List<AdvertiseItem> advertiseItems;
  int currentItem;
  public AdvertisementWindow window;


  private void Awake()
  {
    window = this.gameObject.GetComponent<AdvertisementWindow>();
    StartCoroutine(AdvertisementCoroutine());
  }
  // Start is called before the first frame update
  void Start()
    {
    
    }

    // Update is called once per frame
    void Update()
    {
        
    }

  void nextItem()
  {
    if (currentItem >= advertiseItems.Count - 1)
    {
      currentItem = 0;
    }
    else
    {
      currentItem++;
    }
  }

  void updateImage()
  {
    window.updateEvent(advertiseItems[currentItem]);
  }

  IEnumerator AdvertisementCoroutine()
  {

        while (true)
        {
      updateImage();
      yield return new WaitForSeconds(5);
      nextItem();
    }
    }
}
