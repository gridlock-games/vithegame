using GameCreator.Melee;

namespace NJG.PUN.Melee
{

    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using GameCreator.Core;
    using UnityEditor;
    using UnityEditor.AnimatedValues;
    using UnityEditorInternal;
    using UnityEngine;
    using UnityEngine.Events;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(CharacterMelee), true)]
    public class CustomCharacterMeleeEditor : CharacterMeleeEditor
    {
        public class Section
        {

            public GUIContent name;
            public AnimBool state;

            public Section(string name, Texture2D icon, UnityAction repaint)
            {
                this.name = new GUIContent(string.Format(" {0}", name), icon);
                this.state = new AnimBool(this.GetState());
                this.state.speed = ANIM_BOOL_SPEED;
                this.state.valueChanged.AddListener(repaint);
            }

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

            private bool GetState()
            {
                string key = string.Format(KEY_STATE, this.name.text.GetHashCode());
                return EditorPrefs.GetBool(key, true);
            }
        }

        private const float ANIM_BOOL_SPEED = 3f;
        private const string KEY_STATE = "network-player-melee-section-{0}";

        private const string ICONS_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/";

        private GUIContent GUI_SYNC = new GUIContent("Sync Character Melee",
            "Enables synchronization of over the network for this Character Melee.");

        private Section section;
        private bool hasComponent;

        private CharacterMelee shooter;

        public static Texture2D GetTexture(string icon, string overridePath = "")
        {
            string path = Path.Combine(string.IsNullOrEmpty(overridePath) ? ICONS_PATH : overridePath, icon);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public override void OnInspectorGUI()
        {
            if (serializedObject == null) return;

            base.OnInspectorGUI();
            shooter = (CharacterMelee) target;

            EditorGUILayout.BeginVertical();
            GUILayout.Space(3);

            if (section == null)
            {
                section = new Section("Network Settings", GetTexture("ActionNetwork.png"), Repaint);
            }

            bool hasChanged = false;

            section.PaintSection();
            using (var group = new EditorGUILayout.FadeGroupScope(section.state.faded))
            {
                if (group.visible)
                {
                    EditorGUILayout.BeginVertical(CoreGUIStyles.GetBoxExpanded());

                    EditorGUI.BeginChangeCheck();
                    hasComponent = EditorGUILayout.Toggle(GUI_SYNC, hasComponent);
                    hasChanged = EditorGUI.EndChangeCheck();
                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();

            if (hasChanged && hasComponent)
            {
                var tmpGO = new GameObject("tempOBJ");
                var inst = tmpGO.AddComponent<CharacterMeleeNetwork>();
                MonoScript yourReplacementScript = MonoScript.FromMonoBehaviour(inst);

                SerializedObject so = new SerializedObject(shooter);
                SerializedProperty scriptProperty = so.FindProperty("m_Script");
                so.Update();
                scriptProperty.objectReferenceValue = yourReplacementScript;
                so.ApplyModifiedProperties();

                DestroyImmediate(tmpGO, true);
            }
        }
    }
}
