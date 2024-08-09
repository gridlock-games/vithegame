using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if !UNITY_SERVER && !UNITY_ANDROID && !UNITY_IOS
using Steamworks;
#endif
namespace jomarcentermjm.PlatformAPI
{
  public enum GamePlatform
  {
    inHouse,
    Steam,
    EpicGames,
    GOG,
    PlayStore,
    GameCenter,
    PSN,
    Xbox,
    meta,
    Nintendo,
    serverHeadless
  }
  public class PlatformController : MonoBehaviour
  {
    [SerializeField] GamePlatform activePlayform = GamePlatform.inHouse;
    // Start is called before the first frame update

    public static PlatformController instance;

    private void Awake()
    {
      if (instance == null)
        instance = this;
      else
        Destroy(gameObject);

      DontDestroyOnLoad(gameObject);
    }


    void Start()
    {

      if (activePlayform == GamePlatform.Steam && Application.platform != RuntimePlatform.LinuxServer)
      {
        AttmptSteam();
        AttemptDiscord();
      }
    }

    public GamePlatform getPlatform()
    {
      return activePlayform;
    }

    bool AttmptSteam()
    {
      Debug.Log("Running Steam check");
#if !UNITY_SERVER && !UNITY_ANDROID && !UNITY_IOS
      this.gameObject.AddComponent<SteamManager>();
      return SteamManager.Initialized;
#endif
      return false;
    }

    bool AttemptDiscord()
    {
#if !UNITY_SERVER && !UNITY_ANDROID && !UNITY_IOS
      this.gameObject.AddComponent<DiscordManager>();
      return true;
#endif
      return false;
    }
  }
}