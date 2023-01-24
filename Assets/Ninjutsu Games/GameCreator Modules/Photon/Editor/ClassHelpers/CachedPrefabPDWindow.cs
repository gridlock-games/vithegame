namespace NJG.PUN
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using GameCreator.Core;
    using UnityEngine.Events;

    public class CachedPrefabPDWindow : PopupWindowContent
    {
        private const string TITLE_MANAGE = "Manage Cached Prefabs";
        private const string INPUTTEXT_NAME = "gamecreator-cachedprefabholder-input";
        private const float WIN_HEIGHT = 300f;
        private static DatabasePhoton DATABASE;

        private Rect windowRect = Rect.zero;
        private bool inputfieldFocus = true;
        private Vector2 scroll = Vector2.zero;
        private UnityAction<string> callback;
        private int itemsIndex = -1;

        private string searchText = "";
        private List<int> suggestions = new List<int>();

        private GUIStyle inputBGStyle;
        private GUIStyle suggestionHeaderStyle;
        private GUIStyle suggestionItemStyle;
        private GUIStyle searchFieldStyle;
        private GUIStyle searchCloseOnStyle;
        private GUIStyle searchCloseOffStyle;

        //SkillHolderPD itemHolderPropertyDrawer;

        private bool keyPressedAny = false;
        private bool keyPressedUp = false;
        private bool keyPressedDown = false;
        private bool keyPressedEnter = false;
        private bool keyFlagVerticalMoved = false;
        private Rect itemSelectedRect = Rect.zero;

        // PUBLIC METHODS: ---------------------------------------------------------------------------------------------

        public CachedPrefabPDWindow(Rect activatorRect, UnityAction<string> callback) //SkillHolderPD itemHolderPropertyDrawer
        {
            this.windowRect = new Rect(
                activatorRect.x,
                activatorRect.y + activatorRect.height,
                activatorRect.width,
                WIN_HEIGHT
            );

            this.inputfieldFocus = true;
            this.scroll = Vector2.zero;
            this.callback = callback;
            //this.itemHolderPropertyDrawer = itemHolderPropertyDrawer;

            if (DATABASE == null) DATABASE = DatabasePhoton.LoadDatabase<DatabasePhoton>();
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(this.windowRect.width, WIN_HEIGHT);
        }

        public override void OnOpen()
        {
            this.inputBGStyle = new GUIStyle(GUI.skin.FindStyle("TabWindowBackground"));
            this.suggestionHeaderStyle = new GUIStyle(GUI.skin.FindStyle("IN BigTitle"));
            this.suggestionHeaderStyle.margin = new RectOffset(0, 0, 0, 0);
            this.suggestionItemStyle = new GUIStyle(GUI.skin.FindStyle("MenuItem"));
            this.searchFieldStyle = new GUIStyle(GUI.skin.FindStyle("SearchTextField"));
            this.searchCloseOnStyle = new GUIStyle(GUI.skin.FindStyle("SearchCancelButton"));
            this.searchCloseOffStyle = new GUIStyle(GUI.skin.FindStyle("SearchCancelButtonEmpty"));

            this.inputfieldFocus = true;

            this.searchText = "";
            this.suggestions = DATABASE.GetPrefabSuggestions(this.searchText);
        }

        // GUI METHODS: ------------------------------------------------------------------------------------------------

        public override void OnGUI(Rect windowRect)
        {
            if (this.callback == null) { this.editorWindow.Close(); return; }
            //if (this.itemHolderPropertyDrawer.property == null) { this.editorWindow.Close(); return; }
            //this.itemHolderPropertyDrawer.property.serializedObject.Update();

            this.HandleKeyboardInput();

            string modSearchText = this.searchText;
            this.PaintInputfield(ref modSearchText);
            this.PaintSuggestions(ref modSearchText);

            this.searchText = modSearchText;

            //this.itemHolderPropertyDrawer.property.serializedObject.ApplyModifiedProperties();

            if (this.keyPressedEnter)
            {
                this.editorWindow.Close();
                UnityEngine.Event.current.Use();
            }

            bool repaintEvent = false;
            repaintEvent = repaintEvent || UnityEngine.Event.current.type == EventType.MouseMove;
            repaintEvent = repaintEvent || UnityEngine.Event.current.type == EventType.MouseDown;
            repaintEvent = repaintEvent || this.keyPressedAny;
            if (repaintEvent) this.editorWindow.Repaint();
        }

        // PRIVATE METHODS: --------------------------------------------------------------------------------------------

        private void HandleKeyboardInput()
        {
            this.keyPressedAny = false;
            this.keyPressedUp = false;
            this.keyPressedDown = false;
            this.keyPressedEnter = false;

            if (UnityEngine.Event.current.type != EventType.KeyDown) return;

            this.keyPressedAny = true;
            this.keyPressedUp = (UnityEngine.Event.current.keyCode == KeyCode.UpArrow);
            this.keyPressedDown = (UnityEngine.Event.current.keyCode == KeyCode.DownArrow);

            this.keyPressedEnter = (
                UnityEngine.Event.current.keyCode == KeyCode.KeypadEnter ||
                UnityEngine.Event.current.keyCode == KeyCode.Return
            );

            this.keyFlagVerticalMoved = (
                this.keyPressedUp ||
                this.keyPressedDown
            );
        }

        private void PaintInputfield(ref string modifiedText)
        {
            EditorGUILayout.BeginHorizontal(this.inputBGStyle);

            GUI.SetNextControlName(INPUTTEXT_NAME);
            modifiedText = EditorGUILayout.TextField(GUIContent.none, modifiedText, this.searchFieldStyle);


            GUIStyle style = (string.IsNullOrEmpty(this.searchText)
                ? this.searchCloseOffStyle
                : this.searchCloseOnStyle
            );

            if (this.inputfieldFocus)
            {
                EditorGUI.FocusTextInControl(INPUTTEXT_NAME);
                this.inputfieldFocus = false;
            }

            if (GUILayout.Button("", style))
            {
                modifiedText = "";
                GUIUtility.keyboardControl = 0;
                EditorGUIUtility.keyboardControl = 0;
                this.inputfieldFocus = true;
            }

            EditorGUILayout.EndHorizontal();
        }

        protected void PaintFooter()
        {
            GUILayout.BeginVertical(CoreGUIStyles.GetSearchBox());

            if (GUILayout.Button(TITLE_MANAGE))
            {
                PreferencesWindow.OpenWindowTab("Photon Network");
                this.editorWindow.Close();
            }

            GUILayout.EndVertical();
        }

        private void PaintSuggestions(ref string modifiedText)
        {
            EditorGUILayout.BeginHorizontal(this.suggestionHeaderStyle);
            EditorGUILayout.LabelField("Cached Prefabs", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            this.scroll = EditorGUILayout.BeginScrollView(this.scroll);
            if (modifiedText != this.searchText)
            {
                this.suggestions = DATABASE.GetPrefabSuggestions(modifiedText);
            }

            int suggestionCount = this.suggestions.Count;

            if (suggestionCount > 0)
            {
                for (int i = 0; i < suggestionCount; ++i)
                {
                    GameObject item = DATABASE.prefabs[this.suggestions[i]];
                    string itemName = (string.IsNullOrEmpty(item.name) ? "No-name" : item.name);
                    GUIContent itemContent = new GUIContent(itemName);
                    
                    Rect itemRect = GUILayoutUtility.GetRect(itemContent, this.suggestionItemStyle);
                    bool itemHasFocus = (i == this.itemsIndex);
                    bool mouseEnter = itemHasFocus && UnityEngine.Event.current.type == EventType.MouseDown;

                    if (UnityEngine.Event.current.type == EventType.Repaint)
                    {
                        this.suggestionItemStyle.Draw(
                            itemRect,
                            itemContent,
                            itemHasFocus,
                            itemHasFocus,
                            false,
                            false
                        );

                        //Rect colorRect = itemRect;
                        //colorRect.height = 16;
                        //colorRect.width = 16;
                        //colorRect.x += 2;
                        //colorRect.y += 2;

                        //Color tempColor = GUI.backgroundColor;
                        //GUI.backgroundColor = item.definition.color;
                        //EditorGUI.LabelField(colorRect, string.Empty, CoreGUIStyles.GetLabelTag());
                        //GUI.backgroundColor = tempColor;
                    }

                    if (this.itemsIndex == i) this.itemSelectedRect = itemRect;

                    if (itemHasFocus)
                    {
                        if (mouseEnter || this.keyPressedEnter)
                        {
                            if (this.keyPressedEnter) UnityEngine.Event.current.Use();
                            modifiedText = itemName;
                            if (this.callback != null) this.callback.Invoke(item.name);
                            //this.itemHolderPropertyDrawer.spItem.objectReferenceValue = item;
                            //this.itemHolderPropertyDrawer.spItem.stringValue = item.definition.name.content;
                            //this.itemHolderPropertyDrawer.spItem.serializedObject.ApplyModifiedProperties();
                            //this.itemHolderPropertyDrawer.spItem.serializedObject.Update();

                            this.editorWindow.Close();
                        }
                    }

                    if (UnityEngine.Event.current.type == EventType.MouseMove &&
                        GUILayoutUtility.GetLastRect().Contains(UnityEngine.Event.current.mousePosition))
                    {
                        this.itemsIndex = i;
                    }
                }

                if (this.keyPressedDown && this.itemsIndex < suggestionCount - 1)
                {
                    this.itemsIndex++;
                    UnityEngine.Event.current.Use();
                }
                else if (this.keyPressedUp && this.itemsIndex > 0)
                {
                    this.itemsIndex--;
                    UnityEngine.Event.current.Use();
                }
            }

            EditorGUILayout.EndScrollView();
            float scrollHeight = GUILayoutUtility.GetLastRect().height;

            if (UnityEngine.Event.current.type == EventType.Repaint && this.keyFlagVerticalMoved)
            {
                this.keyFlagVerticalMoved = false;
                if (this.itemSelectedRect != Rect.zero)
                {
                    bool isUpperLimit = this.scroll.y > this.itemSelectedRect.y;
                    bool isLowerLimit = (this.scroll.y + scrollHeight <
                        this.itemSelectedRect.position.y + this.itemSelectedRect.size.y
                    );

                    if (isUpperLimit)
                    {
                        this.scroll = Vector2.up * (this.itemSelectedRect.position.y);
                        this.editorWindow.Repaint();
                    }
                    else if (isLowerLimit)
                    {
                        float positionY = this.itemSelectedRect.y + this.itemSelectedRect.height - scrollHeight;
                        this.scroll = Vector2.up * positionY;
                        this.editorWindow.Repaint();
                    }
                }
            }

            PaintFooter();
        }
    }
}