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
    GamePlatform activePlayform = GamePlatform.inHouse;
    // Start is called before the first frame update
    void Start()
    {
      if (activePlayform == GamePlatform.Steam)
      {
        AttmptSteam();
      }
    }

    // Update is called once per frame
    void Update()
    {

    }

    bool AttmptSteam()
    {
      this.gameObject.AddComponent<SteamManager>();
      return SteamManager.Initialized;
    }

  }
}