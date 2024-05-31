using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class NewsManager : MonoBehaviour
{
  [SerializeField] private UIDocument newsUIDocument;
  [SerializeField] private VisualTreeAsset newsButtonUIDocument;
  [SerializeField] private VisualTreeAsset newsDataUIDocument;
  [SerializeField] private string newsImageServerLocation = "https://vi-assets.com/imagestorage/News/";

  private string NewsAPIURL = "154.90.35.191";
  private List<NewsData> newsData = new List<NewsData>();
  private List<Button> newsButtonList = new List<Button>();

  // Start is called before the first frame update
  private void Awake()
  {
    StartCoroutine("GetNewsData");
    addQuitButton();
  }

  // Update is called once per frame
  private void Update()
  {
  }

  //Run this during the loading sequence.
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
  }

  private void addQuitButton()
  {
    Button exitButton = GetComponent<UIDocument>().rootVisualElement.Q<Button>("ExitButton");
    exitButton.clicked += () => { Destroy(this.gameObject); };
  }
  private void UpdateListUI()
  {
    ScrollView newsList = GetComponent<UIDocument>().rootVisualElement.Q<ScrollView>("NewsList");
    newsList.Q().ClearClassList();
    for (int i = 0; i < newsData.Count; i++)
    {
      TemplateContainer newsListContainer = newsButtonUIDocument.Instantiate() ;
      newsListContainer.Q<Label>("NewsTitle").text = newsData[i].newsTitle;
      var button = newsListContainer.Q<Button>();
      newsListContainer.tabIndex = i;
      //I know its weird setup but that unity
      button.clicked += () => { UpdateNewsUI(newsListContainer.tabIndex); };
      newsList.Add(newsListContainer);

    }
  }

  private void UpdateNewsUI(int newsSelectionID)
  {
    Debug.Log(newsSelectionID);
    VisualElement newsBody = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("NewsBody");
    IMGUIContainer newsContainer = newsBody.Q<IMGUIContainer>("NewsImage");
    Label newsdata = newsBody.Q<Label>("NewsData");
    //Update image
    if (ExternalFileLoaderWeb.Singleton)
    {
      StartCoroutine(ExternalFileLoaderWeb.Singleton.DoImageWebRequest(newsImageServerLocation + newsData[newsSelectionID].newsBody.bannerImg, networkedImage));
    }
    else
    {
      Debug.LogWarning("ExternalFileLoaderWeb is disable or not existence, please create a gameobject with ExternalFileLoaderWeb to load images from the web");
    }

    //Change text
    newsdata.text = newsData[newsSelectionID].newsBody.newsContent;
  }

  private void networkedImage(Texture2D networkFile)
  {
    VisualElement newsBody = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("NewsBody");
    IMGUIContainer newsContainer = newsBody.Q<IMGUIContainer>("NewsImage");
    newsContainer.style.backgroundImage = networkFile;
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