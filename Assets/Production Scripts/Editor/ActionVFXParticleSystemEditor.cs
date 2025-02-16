using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Vi.Core;
using Vi.Core.VFX;

namespace Vi.Editor
{
    [CustomEditor(typeof(ActionVFXParticleSystem))]
    [CanEditMultipleObjects]
    public class ActionVFXParticleSystemEditor : FollowUpVFXEditor
    {
        private SerializedProperty spParticleSystemType;

        private SerializedProperty spShouldUseAttackerPositionForHitAngles;

        private SerializedProperty spShouldOverrideMaxHits;
        private SerializedProperty spMaxHitOverride;

        private SerializedProperty spScaleVFXBasedOnEdges;
        private SerializedProperty spBoundsPoint;
        private SerializedProperty spBoundsLocalAxis;

        private SerializedProperty spLatchToFirstTarget;
        private SerializedProperty spLatchPositionOffset;

        private new void OnEnable()
        {
            base.OnEnable();
            spParticleSystemType = serializedObject.FindProperty("particleSystemType");

            spShouldUseAttackerPositionForHitAngles = serializedObject.FindProperty("shouldUseAttackerPositionForHitAngles");

            spShouldOverrideMaxHits = serializedObject.FindProperty("shouldOverrideMaxHits");
            spMaxHitOverride = serializedObject.FindProperty("maxHitOverride");

            spScaleVFXBasedOnEdges = serializedObject.FindProperty("scaleVFXBasedOnEdges");
            spBoundsPoint = serializedObject.FindProperty("boundsPoint");
            spBoundsLocalAxis = serializedObject.FindProperty("boundsLocalAxis");

            spLatchToFirstTarget = serializedObject.FindProperty("latchToFirstTarget");
            spLatchPositionOffset = serializedObject.FindProperty("latchPositionOffset");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.LabelField("Action VFX Particle System", EditorStyles.whiteLargeLabel);

            EditorGUILayout.PropertyField(spParticleSystemType);

            EditorGUILayout.PropertyField(spShouldUseAttackerPositionForHitAngles);

            EditorGUILayout.PropertyField(spShouldOverrideMaxHits);
            if (spShouldOverrideMaxHits.boolValue) { EditorGUILayout.IntSlider(spMaxHitOverride, 1, 100); }

            EditorGUILayout.PropertyField(spScaleVFXBasedOnEdges);
            if (spScaleVFXBasedOnEdges.boolValue)
            {
                EditorGUILayout.PropertyField(spBoundsPoint);
                EditorGUILayout.PropertyField(spBoundsLocalAxis);
            }

            EditorGUILayout.PropertyField(spLatchToFirstTarget);
            if (spLatchToFirstTarget.boolValue)
            {
                EditorGUILayout.PropertyField(spLatchPositionOffset);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

