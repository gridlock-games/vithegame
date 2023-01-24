using GameCreator.Core;
using GameCreator.Melee;
using UnityEditor;
using UnityEngine;

namespace NJG.PUN.Melee
{
    [CustomEditor(typeof(CharacterMeleeNetwork), true)]
    public class CharacterMeleeNetworkEditor : Editor
    {
        private static readonly GUIContent GUI_DEBUG = new GUIContent("Debug", "Show debug messages of this shooter in the console log.");
        private GUIContent GUI_SYNC = new GUIContent("Sync Character Melee", "Enables synchronization of over the network for this Character Melee.");
        private CustomCharacterMeleeEditor.Section section;
        private bool hasComponent = true;

        public SerializedProperty spDebug;
        private SerializedProperty spCurrentWeapon;
        private SerializedProperty spCurrentShield;

        private SerializedProperty spPoiseDelay;
        private SerializedProperty spPoiseMax;
        private SerializedProperty spPoiseRecovery;

        // INITIALIZER: ---------------------------------------------------------------------------

        private void OnEnable()
        {
            hasComponent = true;

            spDebug = serializedObject.FindProperty("debug");
            spCurrentWeapon = serializedObject.FindProperty("currentWeapon");
            spCurrentShield = serializedObject.FindProperty("currentShield");

            spPoiseDelay = serializedObject.FindProperty("delayPoise");
            spPoiseMax = serializedObject.FindProperty("maxPoise");
            spPoiseRecovery = serializedObject.FindProperty("poiseRecoveryRate");
        }

        // PAINT METHODS: -------------------------------------------------------------------------

        public override void OnInspectorGUI()
        {
            if(serializedObject == null) return;
            if(serializedObject.targetObject == null) return;
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(spCurrentWeapon);
            EditorGUILayout.PropertyField(spCurrentShield);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(spPoiseDelay);
            EditorGUILayout.PropertyField(spPoiseMax);
            EditorGUILayout.PropertyField(spPoiseRecovery);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(spDebug, GUI_DEBUG);

            if (PaintNetworkSettings())
            {
                return;
            }

             if(serializedObject != null) serializedObject.ApplyModifiedProperties();
        }
        
        private bool PaintNetworkSettings()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Space(3);

            if (section == null)
            {
                section = new CustomCharacterMeleeEditor.Section("Network Settings", CustomCharacterMeleeEditor.GetTexture("ActionNetwork.png"), Repaint);
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

            if (hasChanged && !hasComponent)
            {
                var tmpGO = new GameObject("tempOBJ");
                var inst = tmpGO.AddComponent<CharacterMelee>();
                MonoScript yourReplacementScript = MonoScript.FromMonoBehaviour(inst);
            
                SerializedObject so = new SerializedObject(target);
                SerializedProperty scriptProperty = so.FindProperty("m_Script");
                so.Update();
                scriptProperty.objectReferenceValue = yourReplacementScript;
                so.ApplyModifiedProperties();
            
                DestroyImmediate(tmpGO, true);
                return true;
            }

            return false;
        }
    }
}