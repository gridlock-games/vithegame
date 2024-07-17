using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AccountSettingsMenu : MonoBehaviour
{
  public string discordLink = "https://discord.gg/cBzHcgQ3xh";
  public string customerSupportLink = "https://discord.gg/cBzHcgQ3xh";
  public string termsofServiceLink = "https://www.gridlock-games.com/";
  public string privacyPolicyLink = "https://www.gridlock-games.com/";
  public string myAccountLink = "https://www.gridlock-games.com/";
  // Start is called before the first frame update
  void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

  public void openDiscordCommunitySite()
  {
    Application.OpenURL(discordLink);
  }

  public void openCSSite()
  {
    Application.OpenURL(customerSupportLink);
  }

  public void openTOSSite()
  {
    Application.OpenURL(termsofServiceLink);
  }

  public void openPrivacySite()
  {
    Application.OpenURL(privacyPolicyLink);
  }

  public void openAccountLink()
  {
    //Make a Game to website quick authentication system for easy login
    Application.OpenURL(myAccountLink);
  }
}
