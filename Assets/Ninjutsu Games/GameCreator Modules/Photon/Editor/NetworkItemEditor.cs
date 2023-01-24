namespace NJG.PUN
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;
    using UnityEditor;
    using UnityEditorInternal;
    using GameCreator.Core;
    using UnityEditor.AnimatedValues;
    using UnityEngine.Events;

    [CustomEditor(typeof(NetworkItem))]
    public class TriggerEditor : Editor
    {
        public class Section
        {
            private const string ICONS_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/";
            private const string KEY_STATE = "network-item-section-{0}";

            private const float ANIM_BOOL_SPEED = 3.0f;

            // PROPERTIES: ------------------------------------------------------------------------

            public GUIContent name;
            public AnimBool state;

            // INITIALIZERS: ----------------------------------------------------------------------

            public Section(string name, string icon, UnityAction repaint, string overridePath = "")
            {
                this.name = new GUIContent(
                    string.Format(" {0}", name),
                    this.GetTexture(icon, overridePath)
                );

                this.state = new AnimBool(this.GetState());
                this.state.speed = ANIM_BOOL_SPEED;
                this.state.valueChanged.AddListener(repaint);
            }

            // PUBLIC METHODS: --------------------------------------------------------------------

            public void PaintSection()
            {
                GUIStyle buttonStyle = (this.state.target
                    ? CoreGUIStyles.GetToggleButtonNormalOn()
                    : CoreGUIStyles.GetToggleButtonNormalOff()
                );

                if (GUILayout.Button(this.name, buttonStyle))
                {
                    this.state.target = !this.state.target;
                    string key = string.Format(KEY_STATE, this.name.text.GetHashCode());
                    EditorPrefs.SetBool(key, this.state.target);
                }
            }

            // PRIVATE METHODS: -------------------------------------------------------------------

            private bool GetState()
            {
                string key = string.Format(KEY_STATE, this.name.text.GetHashCode());
                return EditorPrefs.GetBool(key, true);
            }

            private Texture2D GetTexture(string icon, string overridePath = "")
            {
                string path = Path.Combine(string.IsNullOrEmpty(overridePath) ? ICONS_PATH : overridePath, icon);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }

        private const string HEADER_EVENTS = "Events";
        private const string HEADER_ACTIONS = "Actions";

        //private static readonly GUIContent GUICONTENT_EVENTS = new GUIContent("Events");
        //private static readonly GUIContent GUICONTENT_ACTIONS = new GUIContent("Actions");
        //private const string MSG_CONDIT = "These Conditions must check before picking up the Item";
        private const string MSG_ACTIONS_MINE = "Actions executed if the local Player picked this up.";
        private const string MSG_ACTIONS_OTHERS = "Actions executed if somebody else picked this up.";
        private const string MSG_ACTIONS_EVERY = "Actions will executed if anyone picked this up.";

        private const string MSG_REQUIRE_HAVE_COLLIDER = "This type of Trigger requires a Collider. Select one from below";

        private const BindingFlags BINDING_FLAGS = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        private const float DOTTED_LINES_SIZE = 2.0f;
        private const string KEY_IGNITER_INDEX_PREF = "gamecreator-igniters-index";

        private static readonly Type[] COLLIDER_TYPES = new Type[]
        {
            typeof(SphereCollider),
            typeof(BoxCollider),
            typeof(CapsuleCollider),
            typeof(MeshCollider)
        };

        private class IgniterCache
        {
            public GUIContent name;
            public string comment;
            public bool requiresCollider;
            public SerializedObject serializedObject;

            public IgniterCache(UnityEngine.Object reference)
            {
                if (reference == null)
                {
                    this.name = new GUIContent("Undefined");
                    this.requiresCollider = false;
                    this.serializedObject = null;
                    return;
                }

                string igniterName = (string)reference.GetType().GetField("NAME", BINDING_FLAGS).GetValue(null);
                string iconPath = (string)reference.GetType().GetField("ICON_PATH", BINDING_FLAGS).GetValue(null);

                if (!string.IsNullOrEmpty(igniterName))
                {
                    string[] igniterNameSplit = igniterName.Split(new char[] { '/' });
                    igniterName = igniterNameSplit[igniterNameSplit.Length - 1];
                }

                Texture2D igniterIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(iconPath, igniterName + ".png"));
                if (igniterIcon == null) igniterIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath + "Default.png");
                if (igniterIcon == null) igniterIcon = EditorGUIUtility.FindTexture("GameObject Icon");

                this.name = new GUIContent(" " + igniterName, igniterIcon);
                this.comment = (string)reference.GetType().GetField("COMMENT", BINDING_FLAGS).GetValue(null);
                this.requiresCollider = (bool)reference.GetType().GetField("REQUIRES_COLLIDER", BINDING_FLAGS).GetValue(null);
                this.serializedObject = new SerializedObject(reference);
            }
        }

        private static string[] IGNITERS_PLATFORM_NAMES = new string[0];

        private static readonly GUIContent[] TAB_NAMES = new GUIContent[]
        {
            new GUIContent("By Anyone"),
            new GUIContent("By Me"),
            new GUIContent("By Other")
        };

        private const string KEY_STATE = "network-item-tab-{0}";

        // PROPERTIES: -------------------------------------------------------------------------------------------------

        private NetworkItem trigger;
        private Section sectionTrigger;
        //private EditorGUIUtils.Section sectionParameters;
        private Section sectionStartActions;
        private Section sectionEvent;
        private int ignitersIndex = 0;
        private SerializedProperty spIgnitersKeys;
        private SerializedProperty spIgnitersValues;
        private IgniterCache[] ignitersCache;
        private bool updateIgnitersPlatforms = false;
        private Rect selectIgniterButtonRect = Rect.zero;

        private SerializedProperty spRespawn;

        private SerializedProperty spTrigger;
        private SerializedProperty spTriggerKeyCode;

        private SerializedProperty spCollisionPlayer;
        private SerializedProperty spCollisionObject;

        private bool foldoutAdvancedSettings = false;
        private SerializedProperty spMinDistance;
        private SerializedProperty spMinDistanceToPlayer;
        private IActionsListEditor editorActions;
        private IActionsListEditor editorStartActions;
        private IActionsListEditor editorMineActions;
        private IActionsListEditor editorOthersActions;
        private IActionsListEditor editorEveryoneActions;
        private int tabIndex;
        private int prevTabIndex;

        // INITIALIZERS: -----------------------------------------------------------------------------------------------

        private void OnEnable()
        {
            if (serializedObject == null) return;

            
            this.trigger = (NetworkItem)target;

            this.tabIndex = this.prevTabIndex = EditorPrefs.GetInt(string.Format(KEY_STATE, target.GetHashCode()));

            this.sectionTrigger = new Section(
                "Pickup Trigger", "Trigger icon.png", this.Repaint,
                "Assets/Gizmos/GameCreator/Core");

            //this.sectionParameters = new EditorGUIUtils.Section("Parameters", "List.png", this.Repaint);
            this.sectionStartActions = new Section(
                "On Start", "On Start.png", this.Repaint,
                "Assets/Plugins/GameCreator/Extra/Icons/Igniters");

            this.sectionEvent = new Section(
                "On Picked Up", "Pickup.png", this.Repaint);

            this.spRespawn = serializedObject.FindProperty("secondsBeforeRespawn");

            /*if(trigger.actions.Count == 0)
            {
                trigger.actions.Add(new Actions() { actionsList = trigger.onPickUpActions });
            }*/

            SerializedProperty spIgniters = serializedObject.FindProperty("igniters");
            this.spIgnitersKeys = spIgniters.FindPropertyRelative("keys");
            this.spIgnitersValues = spIgniters.FindPropertyRelative("values");

            if (this.spIgnitersKeys.arraySize == 0)
            {
                Igniter igniter = this.trigger.gameObject.AddComponent<IgniterTriggerEnter>();
                igniter.Setup(this.trigger);
                igniter.enabled = false;

                this.spIgnitersKeys.InsertArrayElementAtIndex(0);
                this.spIgnitersValues.InsertArrayElementAtIndex(0);

                this.spIgnitersKeys.GetArrayElementAtIndex(0).intValue = Trigger.ALL_PLATFORMS_KEY;
                this.spIgnitersValues.GetArrayElementAtIndex(0).objectReferenceValue = igniter;

                this.serializedObject.ApplyModifiedProperties();
                this.serializedObject.Update();
            }

            this.UpdateIgnitersPlatforms();

            this.ignitersIndex = EditorPrefs.GetInt(KEY_IGNITER_INDEX_PREF, 0);
            if (this.ignitersIndex >= this.spIgnitersKeys.arraySize)
            {
                this.ignitersIndex = this.spIgnitersKeys.arraySize - 1;
                EditorPrefs.SetInt(KEY_IGNITER_INDEX_PREF, this.ignitersIndex);
            }

            this.spMinDistance = serializedObject.FindProperty("minDistance");
            this.spMinDistanceToPlayer = serializedObject.FindProperty("minDistanceToPlayer");
        }

        // INSPECTOR: --------------------------------------------------------------------------------------------------

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (this.updateIgnitersPlatforms)
            {
                this.UpdateIgnitersPlatforms();
                this.updateIgnitersPlatforms = false;
            }
            
            this.PaintParameters();
            this.PaintStartActions();
            this.PaintActions();            

            this.DoLayoutConfigurationOptions();

            EditorGUILayout.Space();

            //EditorGUILayout.Space();
            //this.DoLayoutListOptions(this.eventsList, "Event");
            //this.eventsList.DoLayoutList();

            //EditorGUILayout.Space();
            //this.DoLayoutListOptions(this.actionsList, "Actions");
            //this.actionsList.DoLayoutList();



            serializedObject.ApplyModifiedProperties();
        }

        private void PaintParameters()
        {
            /*this.sectionParameters.PaintSection();
            using (var group = new EditorGUILayout.FadeGroupScope(this.sectionParameters.state.faded))
            {
                if (group.visible)
                {*/
            //EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(this.spRespawn);

            if (trigger.secondsBeforeRespawn > 0)
            {
                EditorGUILayout.HelpBox(string.Format("Time of Respawn: {0} Pickup Is Mine: {1}", trigger.timeUntilRespawn, trigger.PickupIsMine), MessageType.None, true);
            }
            else
            {
                EditorGUILayout.HelpBox("No Respawn", MessageType.None, true);
            }
            //EditorGUILayout.Space();
            //EditorGUILayout.EndVertical();
            //}
            //}
        }

        private void PaintStartActions()
        {
            this.sectionStartActions.PaintSection();
            using (var group = new EditorGUILayout.FadeGroupScope(this.sectionStartActions.state.faded))
            {
                if (group.visible)
                {
                    EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());

                    if (this.editorStartActions == null)
                    {
                        if (this.trigger.onStartActions == null)
                        {
                            SerializedProperty spOnComplete = this.serializedObject.FindProperty("onStartActions");
                            spOnComplete.objectReferenceValue = this.trigger.gameObject.AddComponent<IActionsList>();
                            serializedObject.ApplyModifiedProperties();
                            serializedObject.Update();
                        }

                        this.editorStartActions = (IActionsListEditor)Editor.CreateEditor(this.trigger.onStartActions);
                    }

                    //EditorGUILayout.HelpBox(MSG_ACTIONS, MessageType.Info);
                    this.editorStartActions.OnInspectorGUI();

                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void PaintActions()
        {
            this.sectionEvent.PaintSection();
            using (var group = new EditorGUILayout.FadeGroupScope(this.sectionEvent.state.faded))
            {
                if (group.visible)
                {
                    EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());
                    this.tabIndex = GUILayout.Toolbar(this.tabIndex, TAB_NAMES);
                    if(this.tabIndex != this.prevTabIndex)
                    {
                        EditorPrefs.SetInt(string.Format(KEY_STATE, target.GetHashCode()), this.tabIndex);
                        this.prevTabIndex = this.tabIndex;
                    }

                    //EditorGUILayout.Space();
                    
                    switch (this.tabIndex)
                    {
                        case 0: this.PaintEveryoneActions(); break;
                        case 1: this.PaintMineActions(); break;
                        case 2: this.PaintOthersActions(); break;
                    }
                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void PaintMineActions()
        {
            if (this.editorMineActions == null)
            {
                if (this.trigger.onPickUpMine == null)
                {
                    SerializedProperty spOnComplete = this.serializedObject.FindProperty("onPickUpMine");
                    spOnComplete.objectReferenceValue = this.trigger.gameObject.AddComponent<IActionsList>();
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Update();
                }

                this.editorMineActions = (IActionsListEditor)Editor.CreateEditor(this.trigger.onPickUpMine);
            }

            EditorGUILayout.HelpBox(MSG_ACTIONS_MINE, MessageType.Info);
            this.editorMineActions.OnInspectorGUI();
        }

        private void PaintOthersActions()
        {
            if (this.editorOthersActions == null)
            {
                if (this.trigger.onPickUpOthers == null)
                {
                    SerializedProperty spOnComplete = this.serializedObject.FindProperty("onPickUpOthers");
                    spOnComplete.objectReferenceValue = this.trigger.gameObject.AddComponent<IActionsList>();
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Update();
                }

                this.editorOthersActions = (IActionsListEditor)Editor.CreateEditor(this.trigger.onPickUpOthers);
            }

            EditorGUILayout.HelpBox(MSG_ACTIONS_OTHERS, MessageType.Info);
            this.editorOthersActions.OnInspectorGUI();
        }

        private void PaintEveryoneActions()
        {
            if (this.editorEveryoneActions == null)
            {
                if (this.trigger.onPickUpEveryone == null)
                {
                    SerializedProperty spOnComplete = this.serializedObject.FindProperty("onPickUpEveryone");
                    spOnComplete.objectReferenceValue = this.trigger.gameObject.AddComponent<IActionsList>();
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Update();
                }

                this.editorEveryoneActions = (IActionsListEditor)Editor.CreateEditor(this.trigger.onPickUpEveryone);
            }

            EditorGUILayout.HelpBox(MSG_ACTIONS_EVERY, MessageType.Info);
            this.editorEveryoneActions.OnInspectorGUI();
        }

        private void PaintAdvancedSettings()
        {
            GUIStyle style = (this.foldoutAdvancedSettings
                ? CoreGUIStyles.GetToggleButtonNormalOn()
                : CoreGUIStyles.GetToggleButtonNormalOff()
            );

            EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());
            if (GUILayout.Button("Advanced Options", style))
            {
                this.foldoutAdvancedSettings = !this.foldoutAdvancedSettings;
            }

            if (this.foldoutAdvancedSettings)
            {
                EditorGUILayout.PropertyField(this.spMinDistance);
                EditorGUI.BeginDisabledGroup(!this.spMinDistance.boolValue);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(this.spMinDistanceToPlayer);
                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndVertical();

            if (this.trigger.gameObject.GetComponent<Hotspot>() == null)
            {
                if (GUILayout.Button("Create Hotspot"))
                {
                    Undo.AddComponent<Hotspot>(this.trigger.gameObject);
                }
            }
        }

        private void DoLayoutConfigurationOptions()
        {
            this.sectionTrigger.PaintSection();
            using (var group = new EditorGUILayout.FadeGroupScope(this.sectionTrigger.state.faded))
            {
                if (group.visible)
                {
                    EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());
                    
                    int removeIndex = -1;

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();

                    int ignitersIndex = GUILayout.Toolbar(this.ignitersIndex, IGNITERS_PLATFORM_NAMES);
                    if (ignitersIndex != this.ignitersIndex)
                    {
                        this.ignitersIndex = ignitersIndex;
                        EditorPrefs.SetInt(KEY_IGNITER_INDEX_PREF, this.ignitersIndex);
                    }

                    if (GUILayout.Button("+", CoreGUIStyles.GetButtonLeft(), GUILayout.Width(30f)))
                    {
                        this.SelectPlatformMenu();
                    }

                    EditorGUI.BeginDisabledGroup(this.ignitersIndex == 0);
                    if (GUILayout.Button("-", CoreGUIStyles.GetButtonRight(), GUILayout.Width(30f)))
                    {
                        removeIndex = this.ignitersIndex;
                    }

                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.PrefixLabel(this.ignitersCache[this.ignitersIndex].name, EditorStyles.miniBoldLabel);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Change Trigger", GUILayout.Width(SelectTypePanel.WINDOW_WIDTH)))
                    {
                        SelectTypePanel selectTypePanel = new SelectTypePanel(this.SelectNewIgniter, "Triggers", typeof(Igniter));
                        PopupWindow.Show(this.selectIgniterButtonRect, selectTypePanel);
                    }

                    if (UnityEngine.Event.current.type == EventType.Repaint)
                    {
                        this.selectIgniterButtonRect = GUILayoutUtility.GetLastRect();
                    }

                    EditorGUILayout.EndHorizontal();

                    if (this.ignitersCache[this.ignitersIndex].serializedObject != null)
                    {
                        string comment = this.ignitersCache[this.ignitersIndex].comment;
                        if (!string.IsNullOrEmpty(comment)) EditorGUILayout.HelpBox(comment, MessageType.Info);

                        Igniter.PaintEditor(this.ignitersCache[this.ignitersIndex].serializedObject);
                    }

                    if (this.ignitersCache[this.ignitersIndex].requiresCollider)
                    {
                        Collider collider = this.trigger.GetComponent<Collider>();
                        if (!collider) this.PaintNoCollider();
                    }

                    EditorGUILayout.EndVertical();

                    if (removeIndex > 0)
                    {
                        UnityEngine.Object obj = this.spIgnitersValues.GetArrayElementAtIndex(removeIndex).objectReferenceValue;
                        this.spIgnitersValues.GetArrayElementAtIndex(removeIndex).objectReferenceValue = null;

                        this.spIgnitersKeys.DeleteArrayElementAtIndex(removeIndex);
                        this.spIgnitersValues.DeleteArrayElementAtIndex(removeIndex);

                        if (obj != null) DestroyImmediate(obj, true);

                        this.serializedObject.ApplyModifiedProperties();
                        this.serializedObject.Update();

                        this.updateIgnitersPlatforms = true;
                        if (this.ignitersIndex >= this.spIgnitersKeys.arraySize)
                            this.ignitersIndex = this.spIgnitersKeys.arraySize - 1;
                    }

                    EditorGUILayout.Space();
                    this.PaintAdvancedSettings();
                    GUILayout.Space(2);

                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void SelectPlatformCallback(object data)
        {
            if (this.trigger.igniters.ContainsKey((int)data)) return;

            int index = this.spIgnitersKeys.arraySize;
            this.spIgnitersKeys.InsertArrayElementAtIndex(index);
            this.spIgnitersValues.InsertArrayElementAtIndex(index);

            this.spIgnitersKeys.GetArrayElementAtIndex(index).intValue = (int)data;

            Igniter igniter = this.trigger.gameObject.AddComponent<IgniterTriggerEnter>();
            igniter.Setup(this.trigger);
            igniter.enabled = false;

            this.spIgnitersValues.GetArrayElementAtIndex(index).objectReferenceValue = igniter;

            this.ignitersIndex = index;
            EditorPrefs.SetInt(KEY_IGNITER_INDEX_PREF, this.ignitersIndex);

            this.serializedObject.ApplyModifiedProperties();
            this.serializedObject.Update();

            this.updateIgnitersPlatforms = true;
        }

        private void SelectPlatformMenu()
        {
            GenericMenu menu = new GenericMenu();

            foreach (Trigger.Platforms platform in Enum.GetValues(typeof(Trigger.Platforms)))
            {
                bool disabled = this.trigger.igniters.ContainsKey((int)platform);
                menu.AddItem(new GUIContent(platform.ToString()), disabled, this.SelectPlatformCallback, (int)platform);
            }

            menu.ShowAsContext();
        }

        /*private void DoLayoutListOptions(ReorderableList list, string itemType)
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Create " + itemType, CoreGUIStyles.GetButtonLeft()))
            {
                if (PrefabUtility.GetPrefabType(target) == PrefabType.Prefab)
                {
                    int itemIndex = list.count;
                    list.serializedProperty.InsertArrayElementAtIndex(itemIndex);
                    SerializedProperty spNewItem = list.serializedProperty.GetArrayElementAtIndex(itemIndex);
                    list.index = itemIndex;

                    if (itemType == "Event")
                    {
                        spNewItem.objectReferenceValue = CreatePrefabObject.AddGameObjectToPrefab<GameCreator.Core.Event>(
                            PrefabUtility.FindPrefabRoot(this.trigger.gameObject),
                            itemType
                        );
                    }
                    else if (itemType == "Actions")
                    {
                        spNewItem.objectReferenceValue = CreatePrefabObject.AddGameObjectToPrefab<Actions>(
                            PrefabUtility.FindPrefabRoot(this.trigger.gameObject),
                            itemType
                        );
                    }

                    this.serializedObject.ApplyModifiedProperties();
                    this.serializedObject.Update();
                }
                else
                {
                    this.CreateTriggerOption(list, itemType);
                }
            }

            if (GUILayout.Button("+", CoreGUIStyles.GetButtonMid(), GUILayout.MaxWidth(30f)))
            {
                int insertIndex = list.count;
                list.serializedProperty.InsertArrayElementAtIndex(insertIndex);
                list.serializedProperty.GetArrayElementAtIndex(insertIndex).objectReferenceValue = null;
            }

            if (GUILayout.Button("-", CoreGUIStyles.GetButtonRight(), GUILayout.MaxWidth(30f)))
            {
                bool indexInRange = (list.index >= 0 && list.index < list.count);
                if (indexInRange)
                {
                    SerializedProperty spItem = list.serializedProperty.GetArrayElementAtIndex(list.index);
                    if (spItem.objectReferenceValue != null)
                    {
                        if (PrefabUtility.GetPrefabType(target) == PrefabType.Prefab)
                        {
                            CreatePrefabObject.RemoveGameObjectFromPrefab(
                                PrefabUtility.FindPrefabRoot(this.trigger.gameObject),
                                ((MonoBehaviour)spItem.objectReferenceValue).gameObject
                            );

                            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this.trigger.gameObject));
                        }
                        else
                        {
                            Undo.DestroyObjectImmediate(((MonoBehaviour)spItem.objectReferenceValue).gameObject);
                        }
                    }

                    list.serializedProperty.RemoveFromObjectArrayAt(list.index);
                }

                if (list.index < 0) list.index = 0;
                if (list.index >= list.count) list.index = list.count - 1;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void CreateTriggerOption(ReorderableList list, string itemType)
        {
            int itemIndex = list.count;
            list.serializedProperty.InsertArrayElementAtIndex(itemIndex);
            SerializedProperty spNewItem = list.serializedProperty.GetArrayElementAtIndex(itemIndex);
            list.index = itemIndex;

            GameObject asset = CreateSceneObject.Create(itemType, false);
            if (itemType == "Event") spNewItem.objectReferenceValue = asset.AddComponent<GameCreator.Core.Event>();
            else if (itemType == "Actions") spNewItem.objectReferenceValue = asset.AddComponent<Actions>();
        }
        
        private void DrawEventHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, HEADER_EVENTS);
        }

        private void DrawEventElement(Rect rect, int index, bool active, bool focused)
        {
            Rect propRect = this.GetCenteredRect(rect, EditorGUIUtility.singleLineHeight);
            SerializedProperty property = this.spEvents.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(propRect, property, GUICONTENT_EVENTS);
        }

        private void DrawActionsHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, HEADER_ACTIONS);
        }

        private void DrawActionsElement(Rect rect, int index, bool active, bool focused)
        {
            Rect propRect = this.GetCenteredRect(rect, EditorGUIUtility.singleLineHeight);
            SerializedProperty property = this.spActions.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(propRect, property, GUICONTENT_ACTIONS);
        }*/

        private void PaintNoCollider()
        {
            EditorGUILayout.HelpBox(MSG_REQUIRE_HAVE_COLLIDER, MessageType.Error);

            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < COLLIDER_TYPES.Length; ++i)
            {
                GUIStyle style = CoreGUIStyles.GetButtonMid();
                if (i == 0) style = CoreGUIStyles.GetButtonLeft();
                else if (i >= COLLIDER_TYPES.Length - 1) style = CoreGUIStyles.GetButtonRight();

                if (GUILayout.Button(COLLIDER_TYPES[i].Name, style))
                {
                    Undo.AddComponent(this.trigger.gameObject, COLLIDER_TYPES[i]);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        // SCENE METHODS: ----------------------------------------------------------------------------------------------

        /*private void OnSceneGUI()
        {
            Color eventColor = Color.green;
            Color actionsColor = Color.cyan;

            if (this.trigger.events != null)
            {
                for (int i = 0; i < this.trigger.events.Count; ++i)
                {
                    if (this.trigger.events[i] == null) continue;
                    this.PaintLine(this.trigger.transform, this.trigger.events[i].transform, eventColor);
                }
            }

            if (this.trigger.actions != null)
            {
                for (int i = 0; i < this.trigger.actions.Count; ++i)
                {
                    if (this.trigger.actions[i] == null) continue;
                    this.PaintLine(this.trigger.transform, this.trigger.actions[i].transform, actionsColor);
                }
            }
        }*/

        // PRIVATE METHODS: --------------------------------------------------------------------------------------------

        private Rect GetCenteredRect(Rect rect, float height)
        {
            return new Rect(
                rect.x,
                rect.y + (rect.height - height) / 2.0f,
                rect.width,
                height
            );
        }

        private void UpdateIgnitersPlatforms()
        {
            int numKeys = this.spIgnitersKeys.arraySize;

            this.ignitersCache = new IgniterCache[numKeys];
            IGNITERS_PLATFORM_NAMES = new string[numKeys];

            for (int i = 0; i < numKeys; ++i)
            {
                if (i == 0) IGNITERS_PLATFORM_NAMES[0] = "Any Platform";
                else
                {
                    int key = this.spIgnitersKeys.GetArrayElementAtIndex(i).intValue;
                    IGNITERS_PLATFORM_NAMES[i] = ((Trigger.Platforms)key).ToString();
                }

                UnityEngine.Object reference = this.spIgnitersValues.GetArrayElementAtIndex(i).objectReferenceValue;
                this.ignitersCache[i] = new IgniterCache(reference);
            }
        }

        private void SelectNewIgniter(Type igniterType)
        {
            SerializedProperty property = this.spIgnitersValues.GetArrayElementAtIndex(this.ignitersIndex);
            if (property.objectReferenceValue != null)
            {
                DestroyImmediate(property.objectReferenceValue, true);
                property.objectReferenceValue = null;
            }

            Igniter igniter = (Igniter)this.trigger.gameObject.AddComponent(igniterType);
            igniter.Setup(this.trigger);
            igniter.enabled = false;

            property.objectReferenceValue = igniter;
            this.ignitersCache[this.ignitersIndex] = new IgniterCache(igniter);

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }

        private void PaintLine(Transform transform1, Transform transform2, Color color)
        {
            Handles.color = color;
            Handles.DrawDottedLine(
                transform1.position,
                transform2.position,
                DOTTED_LINES_SIZE
            );
        }

        // HIERARCHY CONTEXT MENU: -------------------------------------------------------------------------------------

        [MenuItem("GameObject/Game Creator/Network Item", false, 0)]
        public static void CreateTrigger()
        {
            GameObject trigger = CreateSceneObject.Create("Item");
            SphereCollider collider = trigger.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            trigger.AddComponent<NetworkItem>();
        }
    }
}