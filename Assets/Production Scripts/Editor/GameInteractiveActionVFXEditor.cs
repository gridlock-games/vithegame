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

        protected new void OnEnable()
        {
            base.OnEnable();
            spFollowUpVFXToPlayOnDestroy = serializedObject.FindProperty("followUpVFXToPlayOnDestroy");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.PropertyField(spFollowUpVFXToPlayOnDestroy);
            serializedObject.ApplyModifiedProperties();
        }
    }
}