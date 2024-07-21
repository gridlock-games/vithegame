using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using jomarcentermjm.ExternalFileHandler;
using Newtonsoft.Json;
using UnityEngine.Networking;
using Unity.Netcode;
using TMPro;


namespace Vi.UI
{
  public class ViNewsService : MonoBehaviour
  {
    [SerializeField] private string newsImageServerLocation = "https://vi-assets.com/imagestorage/News/";
    [SerializeField] private Texture2D defaultLoadingImage;

    private string NewsAPIURL = "154.90.35.191";
    private List<NewsData> newsData = new List<NewsData>();
    private List<Button> newsButtonList = new List<Button>();

    [SerializeField] NewsButtonUI newsButton;

    [SerializeField] GameObject newsBtnListLayout;
    [SerializeField] NewsDataLayout newsInfoLayout;
    [SerializeField] GameObject homeLayout;

    [SerializeField] List<NewsButtonSelection> newsButtonSelections = new List<NewsButtonSelection>();

    [Header("Date time localization")]
    [SerializeField] string dateTimeMessage;
    [SerializeField] Text dateTimeText;

    [Header("Article Window")]
    //NewsArticle
    [SerializeField] TextMeshProUGUI newsArticleTMP;
    [SerializeField] TextMeshProUGUI newsTitleTMP;
    [SerializeField] Image newsArticleImage;

    public void CloseUI()
    {
      Destroy(gameObject);
    }

    private void Update()
    {
      dateTimeText.text = $"{dateTimeMessage} {System.DateTime.Now.ToString("MM/dd/yyyy")} {System.DateTime.Now.ToString("hh:mm:ss")}";
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
        //UpdateNewsUI(0);
      }

      //Start Updating the UI

      //Popup a connection error message
      else
      {
        Debug.Log("Can't load server");
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

        recreatedNewsBtn.transform.parent = newsBtnListLayout.transform;
        int countValue = i;
        recreatedNewsBtn.GetComponent<Button>().onClick.AddListener(() => UpdateNewsUI(countValue));

        newsButtonList.Add(recreatedNewsBtn.GetComponent<Button>());
      }

      UpdateNewsUI(0);
    }


    public void UpdateNewsUI(int newsSelectionID)
    {
      Debug.Log(newsSelectionID);
      Sprite toConvert = Sprite.Create(defaultLoadingImage, new Rect(0, 0, defaultLoadingImage.width, defaultLoadingImage.height), new Vector2(defaultLoadingImage.width / 2, defaultLoadingImage.height / 2));
      newsArticleImage.sprite = toConvert;

      if (ExternalFileLoaderWeb.Singleton)
      {
        Debug.Log("Loading Image"); 
        StartCoroutine(ExternalFileLoaderWeb.Singleton.DoImageWebRequest(newsImageServerLocation + newsData[newsSelectionID].newsBody.bannerImg, networkedImage));
      }
      else
      {
        Debug.LogWarning("ExternalFileLoaderWeb is disable or not existence, please create a gameobject with ExternalFileLoaderWeb to load images from the web");
      }

      //Change text
      newsArticleTMP.text = newsData[newsSelectionID].newsBody.newsContent;
      newsTitleTMP.text = newsData[newsSelectionID].newsTitle;
    }


    private void networkedImage(Texture2D networkFile)
    {
      Sprite toConvert = Sprite.Create(networkFile, new Rect(0, 0, networkFile.width, networkFile.height), new Vector2(networkFile.width / 2, networkFile.height / 2));
      newsArticleImage.sprite = toConvert;
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

  [Serializable]
  public class NewsButtonSelection
  {
    public string typeName = "normal";
    public Sprite typeIcon = null;
  }
}
