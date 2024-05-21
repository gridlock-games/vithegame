using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
      DontDestroyOnLoad(gameObject);

      if (activePlayform == GamePlatform.Steam)
      {
        AttmptSteam();
      }
    }

    public GamePlatform getPlatform()
    {
      return activePlayform;
    }

    bool AttmptSteam()
    {
      this.gameObject.AddComponent<SteamManager>();
      return SteamManager.Initialized;
    }

    bool AttemptDiscord()
    {
      this.gameObject.AddComponent<DiscordManager>();
      return true;
    }

  }
}