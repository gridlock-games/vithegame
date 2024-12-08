using UnityEngine;

namespace Vi.UI
{
    public class OnlyEnableOnMobile : MonoBehaviour
    {
        private void Start()
        {
            gameObject.SetActive(Application.platform == RuntimePlatform.Android | Application.platform == RuntimePlatform.IPhonePlayer);
        }
    }
}