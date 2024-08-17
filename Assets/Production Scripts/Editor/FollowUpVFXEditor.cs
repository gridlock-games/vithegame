using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Vi.Core;
using Vi.Core.VFX;

namespace Vi.Editor
{
    [CustomEditor(typeof(FollowUpVFX))]
    [CanEditMultipleObjects]
    public class FollowUpVFXEditor : GameInteractiveActionVFXEditor
    {
        private SerializedProperty spShouldAffectSelf;
        private SerializedProperty spShouldAffectTeammates;
        private SerializedProperty spShouldAffectEnemies;

        protected new void OnEnable()
        {
            base.OnEnable();
            spShouldAffectSelf = serializedObject.FindProperty("shouldAffectSelf");
            spShouldAffectTeammates = serializedObject.FindProperty("shouldAffectTeammates");
            spShouldAffectEnemies = serializedObject.FindProperty("shouldAffectEnemies");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.LabelField("Follow Up VFX", EditorStyles.whiteLargeLabel);

            EditorGUILayout.PropertyField(spShouldAffectSelf);
            EditorGUILayout.PropertyField(spShouldAffectTeammates);
            EditorGUILayout.PropertyField(spShouldAffectEnemies);
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
    }
}