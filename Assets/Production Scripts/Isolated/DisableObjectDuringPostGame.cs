using UnityEngine;
using Vi.Core.GameModeManagers;

namespace Vi.Isolated
{
    public class DisableObjectDuringPostGame : MonoBehaviour
    {
        private void Update()
        {
            if (!GameModeManager.Singleton) { return; }

            if (GameModeManager.Singleton.GetPostGameStatus() != GameModeManager.PostGameStatus.None)
            {
                gameObject.SetActive(false);
            }
        }
    }
}

