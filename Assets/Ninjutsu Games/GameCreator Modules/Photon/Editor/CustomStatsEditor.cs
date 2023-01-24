#if PHOTON_STATS
using GameCreator.Core;
using GameCreator.Stats;
using NJG;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;

namespace NJG.PUN
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Stats))]
    public class CustomStatsEditor : StatsEditor
    {
        public class Section
        {
            private const string ICONS_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/";
            private const string KEY_STATE = "network-stats-section-{0}";

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
                return EditorPrefs.GetBool(key, false);
            }

            private Texture2D GetTexture(string icon, string overridePath = "")
            {
                string path = Path.Combine(string.IsNullOrEmpty(overridePath) ? ICONS_PATH : overridePath, icon);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }

        private GUIContent GUI_SYNC = new GUIContent("Sync Stats", "Syncronize Stats values over the network.");

        private bool hasComponent = false;
        private bool initialized = false;
        private StatsNetwork statsNetwork;        
        private Stats stats;
        private Section section;

        private StatsAsset statsAsset2;
        private AttrsAsset attrsAsset2;

        private int statsAssetHash2 = 0;
        private int attrsAssetHash2 = 0;
        private SerializedProperty spStats;
        private SerializedProperty spAttributes;
        private SerializedObject serializedStatObject;

        private void OnDisable()
        {
            initialized = false;
        }

        private void ManualOnEnable()
        {
            stats = target as Stats;

            statsNetwork = stats.GetComponent<StatsNetwork>();
            hasComponent = statsNetwork != null;

            if (hasComponent)
            {
                statsAsset2 = DatabaseStatsEditor.GetStatsAsset();
                attrsAsset2 = DatabaseStatsEditor.GetAttrsAsset();

                serializedStatObject = new SerializedObject(statsNetwork);

                this.spStats = serializedStatObject.FindProperty("networkStats");
                this.spAttributes = serializedStatObject.FindProperty("networkAttributes");

                SyncStats();
                SyncAttributes();
            }
        }

        private void SyncAttributes()
        {
            if (statsNetwork == null)
            {
                return;
            }

            int referencesSize = this.spAttributes.arraySize;
            List<StatsNetwork.NetworkStat> addList = new List<StatsNetwork.NetworkStat>();
            List<int> removeList = new List<int>();

            int attrbsSize = this.attrsAsset2.attributes.Length;
            for (int i = 0; i < attrbsSize; ++i)
            {
                string statUniqueID = this.attrsAsset2.attributes[i].uniqueID;
                string uniqueName = this.attrsAsset2.attributes[i].GetNodeTitle();
                addList.Add(new StatsNetwork.NetworkStat() { name = uniqueName, id = statUniqueID, sync = true });
            }

            for (int i = 0; i < statsNetwork.networkAttributes.Length; ++i)
            {
                string refName = statsNetwork.networkAttributes[i].id;
                bool refType = statsNetwork.networkAttributes[i].sync;

                int addListIndex = addList.FindIndex(item => item.id == refName);
                if (addListIndex >= 0)
                {
                    if (refType != addList[addListIndex].sync)
                    {
                        bool type = addList[addListIndex].sync;
                        this.spAttributes.serializedObject.Update();
                        this.spAttributes.GetArrayElementAtIndex(i).FindPropertyRelative("sync").boolValue = type;
                        this.spAttributes.serializedObject.Update();
                        this.spAttributes.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }

                    addList.RemoveAt(addListIndex);
                }
                else
                {
                    removeList.Add(i);
                }
            }

            for (int i = removeList.Count - 1; i >= 0; --i)
            {
                this.spAttributes.RemoveFromObjectArrayAt(removeList[i]);
            }

            int addListCount = addList.Count;
            for (int i = 0; i < addListCount; ++i)
            {
                ArrayUtility.Add<StatsNetwork.NetworkStat>(ref this.statsNetwork.networkAttributes, addList[i]);

                this.spAttributes.serializedObject.ApplyModifiedProperties();
                this.spAttributes.serializedObject.Update();
            }
        }

        private void SyncStats()
        {
            if (statsNetwork == null)
            {
                return;
            }
            int referencesSize = this.spStats.arraySize;
            List<StatsNetwork.NetworkStat> addList = new List<StatsNetwork.NetworkStat>();
            List<int> removeList = new List<int>();

            int attrbsSize = this.statsAsset2.stats.Length;
            for (int i = 0; i < attrbsSize; ++i)
            {
                string statUniqueID = this.statsAsset2.stats[i].uniqueID;
                string uniqueName = this.statsAsset2.stats[i].GetNodeTitle();
                addList.Add(new StatsNetwork.NetworkStat() { name = uniqueName, id = statUniqueID, sync = true });
            }

            for (int i = 0; i < statsNetwork.networkStats.Length; ++i)
            {
                string refName = statsNetwork.networkStats[i].id;
                bool refType = statsNetwork.networkStats[i].sync;

                int addListIndex = addList.FindIndex(item => item.id == refName);
                if (addListIndex >= 0)
                {
                    if (refType != addList[addListIndex].sync)
                    {
                        bool type = addList[addListIndex].sync;
                        this.spStats.serializedObject.Update();
                        this.spStats.GetArrayElementAtIndex(i).FindPropertyRelative("sync").boolValue = type;
                        this.spStats.serializedObject.Update();
                        this.spStats.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }

                    addList.RemoveAt(addListIndex);
                }
                else
                {
                    removeList.Add(i);
                }
            }

            for (int i = removeList.Count - 1; i >= 0; --i)
            {
                this.spStats.RemoveFromObjectArrayAt(removeList[i]);
            }

            int addListCount = addList.Count;
            for (int i = 0; i < addListCount; ++i)
            {
                ArrayUtility.Add<StatsNetwork.NetworkStat>(ref this.statsNetwork.networkStats, addList[i]);

                this.spStats.serializedObject.ApplyModifiedProperties();
                this.spStats.serializedObject.Update();
            }
        }

        public override void OnInspectorGUI()
        {
            if (serializedObject == null) return;

            base.OnInspectorGUI();
            stats = target as Stats;

            bool statsChanged = this.statsAsset2 == null ? false : this.statsAssetHash2 != this.statsAsset2.GetHashCode();
            bool attrsChanged = this.attrsAsset2 == null ? false : this.attrsAssetHash2 != this.attrsAsset2.GetHashCode();

            if (!initialized || statsChanged || attrsChanged)
            {
                this.ManualOnEnable();
                initialized = true;
                serializedObject.ApplyModifiedProperties();
            }

            if (this.section == null)
            {
                this.section = new Section("Network Settings", "ActionNetwork.png", this.Repaint);
            }

            bool hasChanged = false;

            this.section.PaintSection();
            using (var group = new EditorGUILayout.FadeGroupScope(this.section.state.faded))
            {
                if (group.visible)
                {
                    EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());

                    EditorGUI.BeginChangeCheck();
                    hasComponent = EditorGUILayout.Toggle(GUI_SYNC, hasComponent);
                    hasChanged = EditorGUI.EndChangeCheck();

                    if (statsNetwork != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        PaintAttributes();
                        EditorGUILayout.Space();
                        PaintStats();
                        if (EditorGUI.EndChangeCheck())
                        {
                            serializedStatObject.ApplyModifiedPropertiesWithoutUndo();
                        }
                    }
                    
                    EditorGUILayout.EndVertical();
                }
            }

            if (hasChanged)
            {
                if (statsNetwork != null)
                {
                    DestroyImmediate(statsNetwork, true);
                    statsNetwork = null;
                    initialized = false;
                }
                else
                {
                    statsNetwork = stats.gameObject.GetComponent<StatsNetwork>() ?? stats.gameObject.AddComponent<StatsNetwork>();
                    statsNetwork.SetupPhotonView();

                    serializedStatObject = new UnityEditor.SerializedObject(statsNetwork);

                    serializedStatObject.ApplyModifiedProperties();
                    serializedStatObject.Update();

                    hasComponent = true;
                }
                hasChanged = false;
            }
            EditorGUILayout.Space();
        }

        private void PaintAttributes()
        {
            if (statsNetwork == null || this.spAttributes == null) return;

            EditorGUILayout.LabelField("Attributes", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            int statsSize = this.spAttributes.arraySize;
            for (int i = 0; i < statsSize; ++i)
            {
                EditorGUILayout.BeginHorizontal();
                var element = this.spAttributes.GetArrayElementAtIndex(i);

                EditorGUI.BeginChangeCheck();
                element.FindPropertyRelative("sync").serializedObject.Update();
                element.FindPropertyRelative("sync").boolValue = EditorGUILayout.ToggleLeft(new GUIContent("  Sync " + element.FindPropertyRelative("name").stringValue), element.FindPropertyRelative("sync").boolValue);

                if (EditorGUI.EndChangeCheck())
                {
                    element.FindPropertyRelative("sync").serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        private void PaintStats()
        {
            if (statsNetwork == null || this.spStats == null) return;

            EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            int statsSize = this.spStats.arraySize;
            for (int i = 0; i < statsSize; ++i)
            {
                EditorGUILayout.BeginHorizontal();
                var element = this.spStats.GetArrayElementAtIndex(i);

                EditorGUI.BeginChangeCheck();
                element.FindPropertyRelative("sync").serializedObject.Update();
                element.FindPropertyRelative("sync").boolValue = EditorGUILayout.ToggleLeft(new GUIContent("  Sync " + element.FindPropertyRelative("name").stringValue), element.FindPropertyRelative("sync").boolValue);

                if (EditorGUI.EndChangeCheck())
                {
                    element.FindPropertyRelative("sync").serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUI.indentLevel--;
        }
    }

    
}
#endif