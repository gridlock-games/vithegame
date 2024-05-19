using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Vi.Core;

namespace Vi.Editor
{
    [CustomEditor(typeof(FollowUpVFX))]
    [CanEditMultipleObjects]
    public class FollowUpVFXEditor : ActionVFXEditor
    {
        private SerializedProperty spShouldAffectSelf;
        private SerializedProperty spShouldAffectTeammates;
        private SerializedProperty spShouldAffectEnemies;

        public bool shouldAffectSelf;
        public bool shouldAffectTeammates;
        public bool shouldAffectEnemies;

        private new void OnEnable()
        {
            base.OnEnable();
            spShouldAffectSelf = serializedObject.FindProperty("shouldAffectSelf");
            spShouldAffectTeammates = serializedObject.FindProperty("shouldAffectTeammates");
            spShouldAffectEnemies = serializedObject.FindProperty("shouldAffectEnemies");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(spShouldAffectSelf);
            EditorGUILayout.PropertyField(spShouldAffectTeammates);
            EditorGUILayout.PropertyField(spShouldAffectEnemies);

            EditorGUILayout.PropertyField(spAudioClipToPlayOnAwake);
            EditorGUILayout.PropertyField(spAudioClipToPlayOnDestroy);

            serializedObject.ApplyModifiedProperties();
        }
    }
}