using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Vi.Player.TextChat;

namespace Vi.UI
{
    public class CloseTextChatOnOutsideClick : MonoBehaviour, IPointerDownHandler
    {
        private TextChat textChat;
        private void Awake()
        {
            textChat = GetComponentInParent<TextChat>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            textChat.CloseTextChat(true);
        }
    }
}