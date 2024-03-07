using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExitGame : MonoBehaviour
{
  [SerializeField] Button exitButton;

    // Start is called before the first frame update
    void Start()
    {
    exitButton = this.GetComponent<Button>();
    exitButton.onClick.AddListener(LeaveGame);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LeaveGame()
  {
    Application.Quit();
  }
}
