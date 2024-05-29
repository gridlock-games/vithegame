using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vi.UI
{
  public class SceneLoadingInfoUI : MonoBehaviour
  {
    [SerializeField] private List<Sprite> imageList;
    [SerializeField] private Image backgroundImage;
    private bool ongoingEvent;
    // Start is called before the first frame update

    // Update is called once per frame
    private void Awake()
    {
      ChangeBackground();
    }

    private void ChangeBackground()
    {
      if (backgroundImage != null)
      {
        Sprite sprite = imageList[0];
        if (!NetworkCheck())
        {
          int selectedImage = Random.Range(0, imageList.Count);
          sprite = imageList[selectedImage];
        }
        else
        {
          Debug.Log("Retreving Image");
        }
        //Load Image from selected
        backgroundImage.sprite = sprite;
        //End loading
      }
    }

    private bool NetworkCheck()
    {
      if (!ongoingEvent)
      {
        //Check API
        return false;
      }
      return false;
    }
  }
}