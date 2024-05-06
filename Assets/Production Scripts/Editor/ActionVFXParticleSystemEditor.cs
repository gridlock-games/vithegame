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

        private SerializedProperty spShouldOverrideMaxHits;
        private SerializedProperty spMaxHitOverride;

        private SerializedProperty spScaleVFXBasedOnEdges;
        private SerializedProperty spBoundsPoint;
        private SerializedProperty spBoundsLocalAxis;

        private new void OnEnable()
        {
            base.OnEnable();
            spShouldUseAttackerPositionForHitAngles = serializedObject.FindProperty("shouldUseAttackerPositionForHitAngles");

            spShouldOverrideMaxHits = serializedObject.FindProperty("shouldOverrideMaxHits");
            spMaxHitOverride = serializedObject.FindProperty("maxHitOverride");

            spScaleVFXBasedOnEdges = serializedObject.FindProperty("scaleVFXBasedOnEdges");
            spBoundsPoint = serializedObject.FindProperty("boundsPoint");
            spBoundsLocalAxis = serializedObject.FindProperty("boundsLocalAxis");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.PropertyField(spShouldUseAttackerPositionForHitAngles);

            EditorGUILayout.PropertyField(spShouldOverrideMaxHits);
            if (spShouldOverrideMaxHits.boolValue) { EditorGUILayout.IntSlider(spMaxHitOverride, 1, 100); }

            EditorGUILayout.PropertyField(spScaleVFXBasedOnEdges);
            if (spScaleVFXBasedOnEdges.boolValue)
            {
                EditorGUILayout.PropertyField(spBoundsPoint);
                EditorGUILayout.PropertyField(spBoundsLocalAxis);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

