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

        private SerializedProperty spAttackingNormalizedTime;
        private SerializedProperty spRecoveryNormalizedTime;

        private SerializedProperty spAnimationSpeed;

        private void OnEnable()
        {
            spClipType = serializedObject.FindProperty("clipType");

            spAttackingNormalizedTime = serializedObject.FindProperty("attackingNormalizedTime");
            spRecoveryNormalizedTime = serializedObject.FindProperty("recoveryNormalizedTime");

            spAnimationSpeed = serializedObject.FindProperty("animationSpeed");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(spClipType);

            if ((ActionClip.ClipType)spClipType.enumValueIndex == ActionClip.ClipType.LightAttack)
            {
                EditorGUILayout.LabelField("Attack Phase Settings", EditorStyles.whiteLargeLabel);
                EditorGUILayout.LabelField("Normalized time is progress of an animation on a scale of 0 - 1", EditorStyles.whiteLabel);
                spAttackingNormalizedTime.floatValue = EditorGUILayout.Slider("Attacking Normalized Time", spAttackingNormalizedTime.floatValue, 0, 1);
                spRecoveryNormalizedTime.floatValue = EditorGUILayout.Slider("Recovery Normalized Time", spRecoveryNormalizedTime.floatValue, 0, 1);
                EditorGUILayout.Space();
            }
            else if ((ActionClip.ClipType)spClipType.enumValueIndex == ActionClip.ClipType.HeavyAttack)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Attack Phase Settings. Normalized time is progress of an animation on a scale of 0 - 1", EditorStyles.whiteLargeLabel);
                spAttackingNormalizedTime.floatValue = EditorGUILayout.Slider("Attacking Normalized Time", spAttackingNormalizedTime.floatValue, 0, 1);
                spRecoveryNormalizedTime.floatValue = EditorGUILayout.Slider("Recovery Normalized Time", spRecoveryNormalizedTime.floatValue, 0, 1);
                EditorGUILayout.Space();
            }

            EditorGUILayout.LabelField("Animation Settings", EditorStyles.whiteLargeLabel);
            spAnimationSpeed.floatValue = EditorGUILayout.Slider("Animation Speed", spAnimationSpeed.floatValue, 0, 1);

            serializedObject.ApplyModifiedProperties();
        }
    }
}