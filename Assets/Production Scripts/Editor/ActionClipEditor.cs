using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using UnityEditor;

namespace Vi.Editor
{
    [CustomEditor(typeof(ActionClip))]
    [CanEditMultipleObjects]
    public class ActionClipEditor : UnityEditor.Editor
    {
        private SerializedProperty spClipType;

        private SerializedProperty spNextAttackCanBePlayedTime;

        private void OnEnable()
        {
            spClipType = serializedObject.FindProperty("clipType");

            spNextAttackCanBePlayedTime = serializedObject.FindProperty("nextAttackCanBePlayedTime");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(spClipType);

            if ((ActionClip.ClipType)spClipType.enumValueIndex == ActionClip.ClipType.LightAttack)
            {
                EditorGUILayout.PropertyField(spNextAttackCanBePlayedTime);
            }
            else if ((ActionClip.ClipType)spClipType.enumValueIndex == ActionClip.ClipType.HeavyAttack)
            {
                EditorGUILayout.PropertyField(spNextAttackCanBePlayedTime);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}