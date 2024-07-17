using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using jomarcentermjm.ExternalFileHandler;
using Newtonsoft.Json;
using UnityEngine.Networking;
using Unity.Netcode;


namespace Vi.UI
{
  public class ViNewsService : MonoBehaviour
  {
    [SerializeField] private string newsImageServerLocation = "https://vi-assets.com/imagestorage/News/";
    [SerializeField] private Texture2D defaultLoadingImage;

    private string NewsAPIURL = "154.90.35.191/";
    private List<NewsData> newsData = new List<NewsData>();
    private List<Button> newsButtonList = new List<Button>();

    [SerializeField] NewsButtonUI newsButton;

    [SerializeField] GameObject newsBtnListLayout;
    [SerializeField] NewsDataLayout newsInfoLayout;
    [SerializeField] GameObject homeLayout;

    [SerializeField] List<NewsButtonSelection> newsButtonSelections = new List<NewsButtonSelection>();

    void CloseUI()
    {
      Destroy(gameObject);
    }
    void OnEnable()
    {
      StartCoroutine("GetNewsData");
    }

    private IEnumerator GetNewsData()
    {
      UnityWebRequest getRequest = UnityWebRequest.Get(NewsAPIURL + "/game/getActiveNews");
      yield return getRequest.SendWebRequest();

      if (getRequest.result == UnityWebRequest.Result.Success)
      {
        Debug.Log(getRequest.downloadHandler.text);
        var rawData = getRequest.downloadHandler.text;
        newsData = JsonConvert.DeserializeObject<List<NewsData>>(rawData);

        UpdateListUI();
        UpdateNewsUI(0);
      }

      //Start Updating the UI

      //Popup a connection error message
      else
      {
      }

      getRequest.Dispose();
    }

    private void UpdateListUI()
    {


      //Clear the contents/Child
      foreach (Transform child in newsBtnListLayout.transform)
      {
        Destroy(child.gameObject);
      }

      for (int i = 0; i < newsData.Count; i++)
      {
        GameObject recreatedNewsBtn = Instantiate(newsButton.gameObject);
        NewsButtonUI recreatedNewsBtnNb = recreatedNewsBtn.GetComponent<NewsButtonUI>();
        recreatedNewsBtnNb.updateContents(newsData[i].newsTitle, newsButtonSelections[0].typeIcon);
      }
    }


    private void UpdateNewsUI(int newsSelectionID)
    {
      Debug.Log(newsSelectionID);
      newsInfoLayout.UpdateImageUI(defaultLoadingImage);

      if (ExternalFileLoaderWeb.Singleton)
      {
        StartCoroutine(ExternalFileLoaderWeb.Singleton.DoImageWebRequest(newsImageServerLocation + newsData[newsSelectionID].newsBody.bannerImg, networkedImage));
      }
      else
      {
        Debug.LogWarning("ExternalFileLoaderWeb is disable or not existence, please create a gameobject with ExternalFileLoaderWeb to load images from the web");
      }

      //Change text
      newsInfoLayout.UpdateArticleUI(newsData[newsSelectionID].newsBody.newsContent, newsData[newsSelectionID].newsTitle);
    }


    private void networkedImage(Texture2D networkFile)
    {
      newsInfoLayout.UpdateImageUI(networkFile);
    }

  }

  [Serializable]
  public class NewsBody
  {
    public string bannerImg { get; set; }
    public string newsContent { get; set; }
  }

  [Serializable]
  public class NewsData
  {
    public string newsTitle { get; set; }
    public NewsBody newsBody { get; set; }
    public string dateCreated { get; set; }
    public bool isActive { get; set; }
    public string id { get; set; }
  }

  public class NewsButtonSelection
  {
    public string typeName = "normal";
    public Sprite typeIcon = null;
  }
}
