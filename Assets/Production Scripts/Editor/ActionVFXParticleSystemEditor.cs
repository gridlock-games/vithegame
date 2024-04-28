using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Vi.Core;

namespace Vi.Editor
{
    [CustomEditor(typeof(ActionVFXParticleSystem))]
    [CanEditMultipleObjects]
    public class ActionVFXParticleSystemEditor : ActionVFXEditor
    {
        private SerializedProperty spShouldUseAttackerPositionForHitAngles;

        private new void OnEnable()
        {
            base.OnEnable();
            spShouldUseAttackerPositionForHitAngles = serializedObject.FindProperty("shouldUseAttackerPositionForHitAngles");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.PropertyField(spShouldUseAttackerPositionForHitAngles);
            serializedObject.ApplyModifiedProperties();
        }
    }
}

