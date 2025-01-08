using UnityEngine;
using Vi.Utility;

namespace Vi.UI
{
    public class OnlyEnableOnMobile : MonoBehaviour
    {
        [SerializeField] private bool inverted;

        private void Start()
        {
            gameObject.SetActive(inverted ? !FasterPlayerPrefs.IsMobilePlatform : FasterPlayerPrefs.IsMobilePlatform);
        }
    }
}