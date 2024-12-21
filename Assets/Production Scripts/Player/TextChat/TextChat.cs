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
            string username;
            PlayerDataManager.Team team;
            if (NetworkManager.Singleton.IsClient)
            {
                username = PlayerDataManager.Singleton.LocalPlayerData.character.name.ToString();
                team = PlayerDataManager.Singleton.LocalPlayerData.team;
            }
            else
            {
                username = "SERVER";
                team = PlayerDataManager.Team.Peaceful;
            }

            TextChatManager.Singleton.SendTextChat(username, team, textChatInputField.text);
            textChatInputField.text = "";
            if (!FasterPlayerPrefs.IsMobilePlatform) { textChatInputField.ActivateInputField(); }
        }

        public void OpenTextChat()
        {
            if (actionMapHandler)
            {
                actionMapHandler.OnTextChat();
            }
            else
            {
                OnTextChat();
            }
        }

        private bool ShouldUseTextChatOpenButton()
        {
            if (FasterPlayerPrefs.IsMobilePlatform) { return true; }
            return !actionMapHandler;
        }

        public void CloseTextChat(bool evaluateCursorLockMode)
        {
            textChatParentCanvas.enabled = false;
            if (ShouldUseTextChatOpenButton())
            {
                textChatButtonCanvas.enabled = true;
            }
            else
            {
                textChatButtonCanvas.enabled = unreadMessageCount > 0;
            }

            if (actionMapHandler)
            {
                actionMapHandler.OnTextChatClose(evaluateCursorLockMode);
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
                    actionMapHandler.OnTextChatClose(true);
                }
            }
            textChatButtonCanvas.enabled = ShouldUseTextChatOpenButton() ? !textChatParentCanvas.enabled : !textChatParentCanvas.enabled & unreadMessageCount > 0;
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

                if (!ShouldUseTextChatOpenButton())
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

        private void OnEnable()
        {
            CloseTextChat(false);
        }

        private void OnDisable()
        {
            CloseTextChat(false);
        }

        private PlayerInput playerInput;
        private ActionMapHandler actionMapHandler;
        private void Awake()
        {
            TextChatManager.Singleton.RegisterTextChatInstance(this);

            actionMapHandler = transform.root.GetComponent<ActionMapHandler>();
            playerInput = transform.root.GetComponent<PlayerInput>();
            if (playerInput)
            {
                textChatAction = playerInput.actions.FindAction("TextChat");
            }

            textChatParentCanvas.enabled = false;
            if (ShouldUseTextChatOpenButton())
            {
                textChatButtonCanvas.enabled = true;
            }
            else
            {
                textChatButtonCanvas.enabled = unreadMessageCount > 0;
            }
        }

        private void OnDestroy()
        {
            if (TextChatManager.DoesExist())
            {
                TextChatManager.Singleton.UnregisterTextChatInstance(this);
            }
        }
    }
}