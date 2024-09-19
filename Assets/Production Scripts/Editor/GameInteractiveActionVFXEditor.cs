using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Vi.Core;
using Vi.Core.VFX;

namespace Vi.Editor
{
    [CustomEditor(typeof(GameInteractiveActionVFX))]
    [CanEditMultipleObjects]
    public class GameInteractiveActionVFXEditor : ActionVFXEditor
    {
        private SerializedProperty spSpellType;
        private SerializedProperty spFollowUpVFXToPlayOnDestroy;
        private SerializedProperty spShouldBlockProjectiles;
        private SerializedProperty spShouldDestroyOnEnemyHit;
        private SerializedProperty spTeamColorParticleSystems;

        protected new void OnEnable()
        {
            base.OnEnable();
            spSpellType = serializedObject.FindProperty("spellType");
            spFollowUpVFXToPlayOnDestroy = serializedObject.FindProperty("followUpVFXToPlayOnDestroy");
            spShouldBlockProjectiles = serializedObject.FindProperty("shouldBlockProjectiles");
            spShouldDestroyOnEnemyHit = serializedObject.FindProperty("shouldDestroyOnEnemyHit");
            spTeamColorParticleSystems = serializedObject.FindProperty("teamColorParticleSystems");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.LabelField("Game Interactive Action VFX", EditorStyles.whiteLargeLabel);
            EditorGUILayout.PropertyField(spSpellType);
            EditorGUILayout.PropertyField(spFollowUpVFXToPlayOnDestroy);
            EditorGUILayout.PropertyField(spShouldBlockProjectiles);
            EditorGUILayout.PropertyField(spShouldDestroyOnEnemyHit);
            EditorGUILayout.PropertyField(spTeamColorParticleSystems);
            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
        }
    }
}