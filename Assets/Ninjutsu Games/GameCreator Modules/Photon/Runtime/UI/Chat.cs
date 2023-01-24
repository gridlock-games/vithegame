#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY)
#define MOBILE
#endif

using GameCreator.Characters;
using GameCreator.Core.Hooks;
using System;
using System.Collections;
using System.Collections.Generic;
using GameCreator.Variables;
using Photon.Realtime;
#if USE_TMP
using TMPro;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

namespace NJG.PUN.UI
{
    /// <summary>
    /// Generic chat window functionality.
    /// </summary>
    public class Chat : MonoBehaviour
    {
        //static Chat mInst;
        [Header("UI Components")] public GameObject prefab;

        /// <summary>
        /// Input field used for chat input.
        /// </summary>
#if USE_TMP
        public TMP_InputField input;
#else
        public InputField input;
#endif
        public Image background;

        /// <summary>
        /// Root object for chat window's history. This allows you to position the chat window's text.
        /// </summary>
        public ScrollRect container;

        /// <summary>
        /// Maximum number of lines kept in the chat window before they start getting removed.
        /// </summary>
        [Header("UI Settings")] 
        public int maxLines = 1000;

        public int minVisibleLines = 3;

        /// <summary>
        /// Seconds that must elapse before a chat label starts to fade out.
        /// </summary>
        public float fadeOutStart = 10f;

        /// <summary>
        /// How long it takes for a chat label to fade out in seconds.
        /// </summary>
        public float fadeOutDuration = 5f;
        public float backgroundFadeOutDuration = 0.5f;

        /// <summary>
        /// Whether messages will fade out over time.
        /// </summary>
        public bool allowChatFading = true;

        /// <summary>
        /// Whether the activate the chat input when Return key gets pressed.
        /// </summary>
        public bool activateOnReturnKey = true;
        
        [VariableFilter(Variable.DataType.Number)]
        public VariableProperty unseenMessages = new VariableProperty();

        //IsFocused has 1 frame delay.
        [HideInInspector] public bool selected;

        public bool disableInputOnSubmit = false;
        public bool disablePlayerWhenTyping = true;

        private const char SPACE_CHAR = '\n';

        public UnityEvent onOpen;
        public UnityEvent onClose;

        private static WaitForEndOfFrame WFEOF = new WaitForEndOfFrame();

        private class ChatEntry
        {
            public CanvasGroup group;
            public Transform transform;
#if USE_TMP
            public TextMeshProUGUI label;
#else
            public Text label;
#endif
            public Color color;
            public float time;
            public int lines = 0;
            public float alpha = 0f;
            public bool isExpired = false;
            public bool shouldBeDestroyed = false;
            public bool fadedIn = false;
        }

        private List<ChatEntry> mChatEntries = new List<ChatEntry>();

        //private int mBackgroundHeight = -1;
        private bool mIgnoreNextEnter = false;
        private Color originalBgColor;
        private CanvasGroup scrollBarCanvas;
        private float uiTime;
        private Color fadedOutBgColor;
        private bool overInput;
        private bool overContainer;
        private int unSeenCount;
        private bool wasPlayerControllable;

        /// <summary>
        /// For things you want to do after OnSubmitInternal method has ran.
        /// </summary>
        // public UnityEvent LateEndEdit = new UnityEvent();
        protected virtual void Awake()
        {
            //mInst = this;
            originalBgColor = background.color;
            fadedOutBgColor = background.color;
            scrollBarCanvas = container.verticalScrollbar.GetComponent<CanvasGroup>();
            if(!scrollBarCanvas)scrollBarCanvas = container.verticalScrollbar.gameObject.AddComponent<CanvasGroup>();
            
            if(allowChatFading)
            {
                fadedOutBgColor.a = 0;
                background.color = fadedOutBgColor;

                scrollBarCanvas.alpha = 0;
            }
            prefab.SetActive(false);

            if (input != null)
            {
#if USE_TMP
                input.onSelect.AddListener(s => Select());
                input.onDeselect.AddListener(s => Deselect());
#else
                var eventTrigger = input.GetComponent<EventTrigger>();
                if (!eventTrigger) eventTrigger = input.gameObject.AddComponent<EventTrigger>();
                
                var onSel = new EventTrigger.Entry();
                onSel.callback.AddListener(e => Select());
                onSel.eventID = EventTriggerType.Select;
                eventTrigger.triggers.Add(onSel);
                
                var onUnsel = new EventTrigger.Entry();
                onUnsel.callback.AddListener(e => Deselect());
                onUnsel.eventID = EventTriggerType.Deselect;
                eventTrigger.triggers.Add(onUnsel);
                
                #endif
                
                input.onValueChanged.AddListener(OnValueChanged);
                input.onEndEdit.AddListener(OnSubmitInternal);
            }
            
            var eventTrigger2 = container.GetComponent<EventTrigger>();
            if (!eventTrigger2) eventTrigger2 = container.gameObject.AddComponent<EventTrigger>();
                
            var onHover = new EventTrigger.Entry();
            onHover.callback.AddListener(e =>
            {
                overContainer = true;
                Select();
            });
            onHover.eventID = EventTriggerType.PointerEnter;
            eventTrigger2.triggers.Add(onHover);
                
            var onExit = new EventTrigger.Entry();
            onExit.callback.AddListener(e =>
            {
                overContainer = false;
                Deselect();
            });
            onExit.eventID = EventTriggerType.PointerExit;
            eventTrigger2.triggers.Add(onExit);
            
            var eventTrigger3 = input.GetComponent<EventTrigger>();
            if (!eventTrigger3) eventTrigger3 = input.gameObject.AddComponent<EventTrigger>();
                
            var onHover2 = new EventTrigger.Entry();
            onHover2.callback.AddListener(e =>
            {
                overInput = true;
                Select();
            });
            onHover2.eventID = EventTriggerType.PointerEnter;
            eventTrigger3.triggers.Add(onHover2);
                
            var onExit2 = new EventTrigger.Entry();
            onExit2.callback.AddListener(e =>
            {
                overInput = false;
                Deselect();
            });
            onExit2.eventID = EventTriggerType.PointerExit;
            eventTrigger3.triggers.Add(onExit2);
        }

        private void OnDestroy()
        {
            if (input != null)
            {
#if USE_TMP
                input.onSelect.RemoveListener(s => Select());
                input.onDeselect.RemoveListener(s => Deselect());
#endif
                input.onValueChanged.RemoveListener(OnValueChanged);
                input.onEndEdit.RemoveListener(OnSubmitInternal);
            }
        }

        private void OnValueChanged(string input)
        {
            if (disablePlayerWhenTyping && 
                !string.IsNullOrEmpty(input) &&
                HookPlayer.Instance &&
                HookPlayer.Instance.Get<PlayerCharacter>().IsControllable())
            {
                HookPlayer.Instance.Get<PlayerCharacter>().characterLocomotion.SetIsControllable(false);
            }
        }

        public void Select()
        {
            // Debug.LogWarning("Select");
            uiTime = Time.unscaledTime;

            unSeenCount = 0;
            unseenMessages?.Set(unSeenCount, gameObject);

            selected = true;
            onOpen?.Invoke();

            PlayerCharacter pl = HookPlayer.Instance ? HookPlayer.Instance.Get<PlayerCharacter>() : null;
            
            if (disablePlayerWhenTyping && pl && pl.IsControllable())
            {
                wasPlayerControllable = pl.IsControllable();
                pl.characterLocomotion.SetIsControllable(false);
            }

            // if (placeholder) placeholder.SetActive(false);
        }

        public void Deselect()
        {
            if(overInput || overContainer) return;
            
            // Debug.LogWarning("Deselect");

            selected = false;
            onClose.Invoke();
            
            PlayerCharacter pl = HookPlayer.Instance ? HookPlayer.Instance.Get<PlayerCharacter>() : null;

            if (disablePlayerWhenTyping && pl && !pl.IsControllable() && wasPlayerControllable)
            {
                pl.characterLocomotion.SetIsControllable(true);
            }
        }

        /// <summary>
        /// Handle inputfield onEndEdit event.
        /// </summary>
        public void OnSubmitInternal(string content)
        {
            /*if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                input.text = string.Empty;

                if (!string.IsNullOrWhiteSpace(content))
                    OnSubmit(content);

                if (disableInputOnSubmit) input.interactable = false;
                // if (placeholder) placeholder.SetActive(true);

                mIgnoreNextEnter = true;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    input.text = string.Empty;

                    if (Input.GetMouseButtonDown(0)
                    ) //Workaround for mouse click triggering EventSystem.current.alreadySelecting for some reason
                    {
                        if (disableInputOnSubmit) StartCoroutine(nameof(SetInputFieldNotInteractableAtEndOfFrame));
                    }
                    else
                    {
                        if (disableInputOnSubmit) input.interactable = false;
                    }

                    // if (placeholder) placeholder.SetActive(true);
                }
            }*/
            mIgnoreNextEnter = true;
            input.text = string.Empty;
            if (!string.IsNullOrWhiteSpace(content)) OnSubmit(content);

            input.DeactivateInputField();
            if (!EventSystem.current.alreadySelecting) EventSystem.current.SetSelectedGameObject(null, null);
            // LateEndEdit?.Invoke();

            /*if (disablePlayerWhenTyping && HookPlayer.Instance)
            {
                HookPlayer.Instance.Get<PlayerCharacter>().characterLocomotion.SetIsControllable(true);
            }*/
        }

        private IEnumerator SetInputFieldNotInteractableAtEndOfFrame()
        {
            yield return WFEOF;
            input.interactable = false;
        }

        public void ClearHistory()
        {
            for (int i = 0, imax = mChatEntries.Count; i < imax; i++)
            {
                ChatEntry e = mChatEntries[i];
                mChatEntries.RemoveAt(i);
                Destroy(e.label.gameObject);
            }
        }

        /// <summary>
        /// Custom submit logic for what happens on chat input submission.
        /// </summary>
        protected virtual void OnSubmit(string text)
        {
        }

        /// <summary>
        /// Add a new chat entry.
        /// </summary>
        private GameObject InternalAdd(string text, Color color, bool tintBackground)
        {
            ChatEntry ent = new ChatEntry();
            ent.time = Time.unscaledTime;
            ent.color = color;
            mChatEntries.Add(ent);


            GameObject go = Instantiate(prefab, container.content, false) as GameObject;
            go.SetActive(true);
            ent.group = go.GetComponent<CanvasGroup>();

#if USE_TMP
            ent.label = go.GetComponentInChildren<TextMeshProUGUI>();
#else
            ent.label = go.GetComponentInChildren<Text>();
#endif
            ent.transform = go.transform;
            //ent.label.pivot = UIWidget.Pivot.BottomLeft;
            // ent.transform.localScale = new Vector3(1f, 0.001f, 1f);
            //ent.label.transform.localPosition = Vector3.zero;
            //ent.label.width = lineWidth;
            //ent.label.bitmapFont = font;
            //ent.label.fontSize = fontSize;

            //ent.label.color = ent.label.bitmapFont.premultipliedAlphaShader ? new Color(0f, 0f, 0f, 0f) : new Color(color.r, color.g, color.b, 0f);
            if (tintBackground)
            {
                go.GetComponent<Image>().color = color;
                ent.label.text = text;
            }
            else
            {
                ent.label.color = color;
                ent.label.text = text;
            }

            //else ent.label.text = "<color=#" + EncodeColor32(color) + ">" + text + "</color>";
            //ent.label.overflowMethod = UILabel.Overflow.ResizeHeight;
            ent.lines = ent.label.text.Split(SPACE_CHAR).Length;

            for (int i = mChatEntries.Count, lineOffset = 0; i > 0;)
            {
                ChatEntry e = mChatEntries[--i];

                if (i + 1 == mChatEntries.Count)
                {
                    // It's the first entry. It doesn't need to be re-positioned.
                    lineOffset += e.lines;
                }
                else
                {
                    // This is not a first entry. It should be tweened into its proper place.
                    //int pixelOffset = lineOffset * (int)e.label.rectTransform.sizeDelta.y;

                    if (lineOffset + e.lines > maxLines && maxLines > 0)
                    {
                        e.isExpired = true;
                        e.shouldBeDestroyed = true;

                        if (e.alpha == 0f)
                        {
                            mChatEntries.RemoveAt(i);
                            Destroy(e.label.gameObject);
                            continue;
                        }
                    }

                    lineOffset += e.lines;
                    //e.label.transform.DOLocalMove(new Vector3(0f, pixelOffset, 0f), 0.2f);
                    //TweenPosition.Begin(e.label.gameObject, 0.2f, new Vector3(0f, pixelOffset, 0f));
                }
            }

            if (!selected)
            {
                unSeenCount++;
                unseenMessages?.Set(unSeenCount, gameObject);
            }

            return go;
        }

        /// <summary>
        /// Update the "alpha" of each line and update the background size.
        /// </summary>
        protected virtual void Update()
        {
            if (activateOnReturnKey && (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter)))
            {
                if (!mIgnoreNextEnter)
                {
                    input.interactable = true;
                    input.Select();
                    input.ActivateInputField();

                    EventSystem current;
                    (current = EventSystem.current).SetSelectedGameObject(input.gameObject, null);
                    input.OnPointerClick(new PointerEventData(current));
                }

                mIgnoreNextEnter = false;
            }
            
            if(allowChatFading)
            {
                float uiAlpha = 0;

                if (selected)
                {
                    // Quickly fade in new entries
                    uiAlpha = Mathf.Clamp01(scrollBarCanvas.alpha + Time.unscaledDeltaTime * 5f);
                }
                else if (Time.unscaledTime - (uiTime + fadeOutStart) < backgroundFadeOutDuration)
                {
                    // Slowly fade out entries that have been visible for a while
                    uiAlpha = Mathf.Clamp01(scrollBarCanvas.alpha - Time.unscaledDeltaTime / backgroundFadeOutDuration);
                }
                else
                {
                    // Quickly fade out chat entries that should have faded by now,
                    // but likely didn't due to the input being selected.
                    uiAlpha = Mathf.Clamp01(scrollBarCanvas.alpha - Time.unscaledDeltaTime);
                }

                // originalBgColor.a = uiAlpha;
                background.color = Color.Lerp(fadedOutBgColor, originalBgColor, uiAlpha);

                scrollBarCanvas.alpha = uiAlpha;
            }

            // int height = 0;

            for (int i = 0; i < mChatEntries.Count;)
            {
                ChatEntry e = mChatEntries[i];
                float alpha = 0;

                if (e.isExpired)
                {
                    // Quickly fade out expired chat entries
                    alpha = Mathf.Clamp01(e.alpha - Time.unscaledDeltaTime);
                }
                else if (selected || Time.unscaledTime - e.time < fadeOutStart) //
                {
                    // Quickly fade in new entries
                    alpha = Mathf.Clamp01(e.alpha + Time.unscaledDeltaTime * 5f);
                }
                else if (Time.unscaledTime - (e.time + fadeOutStart) < fadeOutDuration)
                {
                    // Slowly fade out entries that have been visible for a while
                    alpha = Mathf.Clamp01(e.alpha - Time.unscaledDeltaTime / fadeOutDuration);
                }
                else
                {
                    // Quickly fade out chat entries that should have faded by now,
                    // but likely didn't due to the input being selected.
                    alpha = Mathf.Clamp01(e.alpha - Time.unscaledDeltaTime);
                }

                if (e.alpha != alpha)
                {
                    e.alpha = alpha;
                    e.group.alpha = !allowChatFading || i > mChatEntries.Count - (minVisibleLines + 1) ? 1 : e.alpha;

                    if ((int)alpha == 1)
                    {
                        // The chat entry has faded in fully
                        e.fadedIn = true;
                    }
                    else if (alpha == 0f && e.shouldBeDestroyed)
                    {
                        // This chat entry has expired and should be removed
                        mChatEntries.RemoveAt(i);
                        Destroy(e.label.gameObject);
                        continue;
                    }
                }

                // If the line is visible, it should be counted
                ++i;
            }
        }

        /// <summary>
        /// Add a new chat entry.
        /// </summary>
        protected virtual GameObject Add(string text, Color color, bool tintBackground, Player player)
        {
            return InternalAdd(text, color, tintBackground);
        }
    }
}