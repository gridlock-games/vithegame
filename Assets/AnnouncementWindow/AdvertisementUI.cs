using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class AdvertisementUI : MonoBehaviour
{
  [SerializeField] private List<AdvertiseItem> advertiseItems;
  int currentItem;
  public AdvertisementWindow window;
  private List<Image> itemsList = new List<Image>();

  [SerializeField] GameObject selectableItemPrefab;
  [SerializeField] GameObject selectableItemList;
  private int previousSelectedImage = 0;
  [SerializeField] private Sprite activeImage;
  [SerializeField] private Sprite inactiveImage;

  private void Awake()
  {
    window = this.gameObject.GetComponent<AdvertisementWindow>();
    setupItemList();
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


  void updateSetListButtons()
  {
    //change image from previous item
    itemsList[previousSelectedImage].sprite = inactiveImage;
    previousSelectedImage = currentItem;
    itemsList[currentItem].sprite = activeImage;
  }

  void setupItemList()
  {
    for (int i = 0; i < advertiseItems.Count; i++)
    {
      var newItem = Instantiate(selectableItemPrefab, selectableItemList.transform);
      itemsList.Add(newItem.GetComponent<Image>());
    }
  }

  IEnumerator AdvertisementCoroutine()
  {
    
    while (true)
        {
      updateSetListButtons();
      updateImage();
      yield return new WaitForSeconds(5);
      nextItem();
    }
    }

}
