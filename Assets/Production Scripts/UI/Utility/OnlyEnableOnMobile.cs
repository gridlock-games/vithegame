using UnityEngine;
using Vi.Utility;

namespace Vi.UI
{
    public class OnlyEnableOnMobile : MonoBehaviour
    {
        private void Start()
        {
            gameObject.SetActive(FasterPlayerPrefs.IsMobilePlatform);
        }
    }
}