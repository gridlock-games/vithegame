using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Core;
using Unity.Collections;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Vi.Player;
using Vi.Utility;
using Vi.Core.CombatAgents;

namespace Vi.Player.TextChat
{
    public class TextChat : MonoBehaviour
    {
        [SerializeField] private Canvas textChatButtonCanvas;
        [SerializeField] private Canvas textChatParentCanvas;
        [SerializeField] private Scrollbar chatScrollbar;
        [SerializeField] private RectTransform textChatElementParent;
        [SerializeField] private InputField textChatInputField;
        [SerializeField] private GameObject textChatElementPrefab;
        [SerializeField] private Button openTextChatButton;
        [SerializeField] private Text textChatMessageNumberText;

        public void SendTextChat()
        {
            TextChatManager.Singleton.SendTextChat(PlayerDataManager.Singleton.LocalPlayerData.character.name.ToString(), PlayerDataManager.Singleton.LocalPlayerData.team, textChatInputField.text);
            textChatInputField.text = "";
            if (!FasterPlayerPrefs.IsMobilePlatform) { textChatInputField.ActivateInputField(); }
        }

        public void OpenTextChat()
        {
            if (actionMapHandler)
            {
                actionMapHandler.OnTextChat();
            }
        }

        public void CloseTextChat()
        {
            textChatParentCanvas.enabled = false;
            if (FasterPlayerPrefs.IsMobilePlatform)
            {
                textChatButtonCanvas.enabled = true;
            }
            else
            {
                textChatButtonCanvas.enabled = unreadMessageCount > 0;
            }

            if (actionMapHandler)
            {
                actionMapHandler.OnTextChatClose();
            }
        }

        private InputAction textChatAction;
        private int unreadMessageCount;
        public void OnTextChat()
        {
            if (playerInput & textChatAction != null)
            {
                if (!textChatAction.enabled & playerInput.currentActionMap.name == playerInput.defaultActionMap) { return; }
            }

            textChatParentCanvas.enabled = !textChatParentCanvas.enabled;
            if (textChatParentCanvas.enabled)
            {
                ScrollToBottomOfTextChat();
                if (actionMapHandler)
                {
                    actionMapHandler.OnTextChatOpen();
                }
                if (!FasterPlayerPrefs.IsMobilePlatform) { textChatInputField.ActivateInputField(); }
                unreadMessageCount = 0;
                textChatMessageNumberText.text = "";
            }
            else
            {
                if (actionMapHandler)
                {
                    actionMapHandler.OnTextChatClose();
                }
            }
            textChatButtonCanvas.enabled = FasterPlayerPrefs.IsMobilePlatform ? !textChatParentCanvas.enabled : !textChatParentCanvas.enabled & unreadMessageCount > 0;
        }

        public void ScrollToBottomOfTextChat()
        {
            chatScrollbar.value = 0;
            StartCoroutine(ScrollToBottomOfTextChatAfterOneFrame());
        }

        private IEnumerator ScrollToBottomOfTextChatAfterOneFrame()
        {
            yield return null;
            yield return null;
            chatScrollbar.value = 0;
        }

        public void ScrollToTopOfTextChat() { chatScrollbar.value = 1; }
        public void ScrollALittleDownTextChat() { chatScrollbar.value -= 0.1f; }
        public void ScrollALittleUpTextChat() { chatScrollbar.value += 0.1f; }

        public void DisplayNextTextElement(TextChatManager.TextChatElement textChatElement)
        {
            Text text = Instantiate(textChatElementPrefab, textChatElementParent).GetComponent<Text>();
            text.text = textChatElement.GetMessageUIValue();
            if (textChatParentCanvas.enabled)
            {
                ScrollToBottomOfTextChat();
            }
            else
            {
                unreadMessageCount++;
                if (unreadMessageCount == 0)
                {
                    textChatMessageNumberText.text = "";
                }
                else if (unreadMessageCount > 99)
                {
                    textChatMessageNumberText.text = "99+";
                }
                else
                {
                    textChatMessageNumberText.text = unreadMessageCount.ToString();
                }

                if (!FasterPlayerPrefs.IsMobilePlatform)
                {
                    textChatButtonCanvas.enabled = unreadMessageCount > 0;
                }
            }
        }

        public void DisplayConnectionMessage(string connectionMessage)
        {
            Text text = Instantiate(textChatElementPrefab, textChatElementParent).GetComponent<Text>();
            text.text = connectionMessage;
            if (textChatParentCanvas.enabled)
            {
                ScrollToBottomOfTextChat();
            }
        }

        private PlayerInput playerInput;
        private ActionMapHandler actionMapHandler;
        private void Awake()
        {
            textChatParentCanvas.enabled = false;
            if (FasterPlayerPrefs.IsMobilePlatform)
            {
                textChatButtonCanvas.enabled = true;
            }
            else
            {
                textChatButtonCanvas.enabled = unreadMessageCount > 0;
            }

            actionMapHandler = transform.root.GetComponent<ActionMapHandler>();
            playerInput = transform.root.GetComponent<PlayerInput>();
            if (playerInput)
            {
                textChatAction = playerInput.actions.FindAction("TextChat");
            }
            TextChatManager.Singleton.RegisterTextChatInstance(this);
        }

        private void OnDestroy()
        {
            TextChatManager.Singleton.UnregisterTextChatInstance(this);
        }
    }
}