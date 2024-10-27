using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Utility;

namespace Vi.UI
{
    public class AlertBox : MonoBehaviour
    {
        [SerializeField] private Text alertText;
        
        public void SetText(string newText)
        {
            alertText.text = newText;
        }

        public void OpenViDiscord()
        {
            Application.OpenURL(FasterPlayerPrefs.persistentDiscordInviteLink);
        }

        public void DestroyAlert()
        {
            Destroy(gameObject);
        }
    }
}