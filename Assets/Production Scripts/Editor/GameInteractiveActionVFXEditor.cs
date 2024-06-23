using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Vi.Core;

namespace Vi.Editor
{
    [CustomEditor(typeof(GameInteractiveActionVFX))]
    [CanEditMultipleObjects]
    public class GameInteractiveActionVFXEditor : ActionVFXEditor
    {
        private SerializedProperty spFollowUpVFXToPlayOnDestroy;
        private SerializedProperty spShouldDestroyOnEnemyHit;

        protected new void OnEnable()
        {
            base.OnEnable();
            spFollowUpVFXToPlayOnDestroy = serializedObject.FindProperty("followUpVFXToPlayOnDestroy");
            spShouldDestroyOnEnemyHit = serializedObject.FindProperty("shouldDestroyOnEnemyHit");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.LabelField("Game Interactive Action VFX", EditorStyles.whiteLargeLabel);
            EditorGUILayout.PropertyField(spFollowUpVFXToPlayOnDestroy);
            EditorGUILayout.PropertyField(spShouldDestroyOnEnemyHit);
            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
        }
    }
}