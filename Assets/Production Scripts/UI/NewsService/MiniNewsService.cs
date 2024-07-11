using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
namespace Vi.UI
{
  public class MiniNewsService : MonoBehaviour
  {

    private string NewsAPIURL = "154.90.35.191/";
    private List<NewsData> newsData = new List<NewsData>();
    public int latestNewsCount = 3;

    public GameObject newsListObject;
    public NewsMiniWindowDataObject newsListDataObject;
    private List<GameObject> newsWindowObject = new List<GameObject>();
    // Start is called before the first frame update
    void Start()
    {
      StartCoroutine(GetNewsData());
    }

    // Update is called once per frame
    void Update()
    {

    }
    private IEnumerator GetNewsData()
    {
      Debug.Log("Run network check");
      UnityWebRequest getRequest = UnityWebRequest.Get(NewsAPIURL + "/game/getActiveNews");
      yield return getRequest.SendWebRequest();

      if (getRequest.result == UnityWebRequest.Result.Success)
      {
        Debug.Log(getRequest.downloadHandler.text);
        var rawData = getRequest.downloadHandler.text;
        newsData = JsonConvert.DeserializeObject<List<NewsData>>(rawData);
      }

      //Start Updating the UI

      //Popup a connection error message
      else
      {
        Debug.Log("Connection Problem");
        NewsData nd = new NewsData() { newsTitle = "Cannot connect to server"};
        newsData.Add(nd);
      }

      UpdateNewsContents();

      getRequest.Dispose();
    }

    private void UpdateNewsContents()
    {
      //Clean up list content
      newsWindowObject.Clear();
      //Only get the first 3 news data
      int newsCounter = 0;
      if (newsData.Count <= 2) {
        newsCounter = newsData.Count;
          }

      for (int i = 0; i < newsCounter; i++)
      {
        Debug.Log("Printing News Data");
        var nldo = Instantiate(newsListDataObject.gameObject);
        nldo.GetComponent<NewsMiniWindowDataObject>().updateContent(newsData[i].newsTitle, Color.red);

        //Add content to list
        newsWindowObject.Add(nldo);

        nldo.transform.parent = newsListObject.transform;
      }
      }

  }
} 