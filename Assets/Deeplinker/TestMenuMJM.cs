
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vi.UI;
public class TestMenuMJM : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

  public void gotoDeepLinkDebugScene()
  {
    SceneManager.LoadScene("DeeplinkTestScene");
  }
}
