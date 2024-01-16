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
    if (currentItem >= advertiseItems.Count)
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

  }
}
