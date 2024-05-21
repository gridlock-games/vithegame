using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.PlatformAPI
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
    Nintendo
  }
  public class PlatformController : MonoBehaviour
  {
    [SerializeField] GamePlatform activePlayform = GamePlatform.inHouse;
    // Start is called before the first frame update
    void Start()
    {
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

  }
}