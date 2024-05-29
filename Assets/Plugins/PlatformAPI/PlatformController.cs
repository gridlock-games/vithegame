using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_STANDALONE_WIN
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
    Nintendo
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

      if (activePlayform == GamePlatform.Steam)
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
#if UNITY_STANDALONE_WIN
      this.gameObject.AddComponent<SteamManager>();
      return SteamManager.Initialized;
#endif
    }

    bool AttemptDiscord()
    {
#if UNITY_STANDALONE_WIN
      this.gameObject.AddComponent<DiscordManager>();
      return true;
#endif
    }
  }
}