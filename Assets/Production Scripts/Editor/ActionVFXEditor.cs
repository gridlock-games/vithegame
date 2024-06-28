using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using UnityEditor;

namespace Vi.Editor
{
    [CustomEditor(typeof(ActionVFX))]
    [CanEditMultipleObjects]
    public class ActionVFXEditor : UnityEditor.Editor
    {
        private SerializedProperty spVFXPositionOffset;
        private SerializedProperty spVFXRotationOffset;

        private SerializedProperty spVFXSpawnType;
        private SerializedProperty spTransformType;

        private SerializedProperty spRaycastOffset;
        private SerializedProperty spFartherRaycastOffset;
        private SerializedProperty spRaycastMaxDistance;
        private SerializedProperty spLookRotationUpDirection;

        private SerializedProperty spOnActivateVFXSpawnNormalizedTime;

        private SerializedProperty spWeaponBone;

        protected SerializedProperty spAudioClipToPlayOnAwake;
        protected SerializedProperty spAudioClipToPlayOnDestroy;

        private SerializedProperty spVFXToPlayOnDestroy;

        protected void OnEnable()
        {
            spVFXPositionOffset = serializedObject.FindProperty("vfxPositionOffset");
            spVFXRotationOffset = serializedObject.FindProperty("vfxRotationOffset");

            spVFXSpawnType = serializedObject.FindProperty("vfxSpawnType");
            spTransformType = serializedObject.FindProperty("transformType");

            spRaycastOffset = serializedObject.FindProperty("raycastOffset");
            spFartherRaycastOffset = serializedObject.FindProperty("fartherRaycastOffset");
            spRaycastMaxDistance = serializedObject.FindProperty("raycastMaxDistance");
            spLookRotationUpDirection = serializedObject.FindProperty("lookRotationUpDirection");

            spOnActivateVFXSpawnNormalizedTime = serializedObject.FindProperty("onActivateVFXSpawnNormalizedTime");

            spWeaponBone = serializedObject.FindProperty("weaponBone");

            spAudioClipToPlayOnAwake = serializedObject.FindProperty("audioClipToPlayOnAwake");
            spAudioClipToPlayOnDestroy = serializedObject.FindProperty("audioClipToPlayOnDestroy");

            spVFXToPlayOnDestroy = serializedObject.FindProperty("VFXToPlayOnDestroy");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Action VFX", EditorStyles.whiteLargeLabel);
            EditorGUILayout.PropertyField(spVFXPositionOffset);
            EditorGUILayout.PropertyField(spVFXRotationOffset);

            EditorGUILayout.PropertyField(spVFXSpawnType);
            if ((ActionVFX.VFXSpawnType)spVFXSpawnType.enumValueIndex == ActionVFX.VFXSpawnType.OnActivate)
            {
                spOnActivateVFXSpawnNormalizedTime.floatValue = EditorGUILayout.Slider("OnActivate VFX Play Time", spOnActivateVFXSpawnNormalizedTime.floatValue, 0, 1);
            }

            EditorGUILayout.PropertyField(spTransformType);
            if ((ActionVFX.TransformType)spTransformType.enumValueIndex == ActionVFX.TransformType.ConformToGround)
            {
                EditorGUILayout.PropertyField(spRaycastOffset);
                EditorGUILayout.PropertyField(spFartherRaycastOffset);
                EditorGUILayout.PropertyField(spRaycastMaxDistance);
                EditorGUILayout.PropertyField(spLookRotationUpDirection);
            }

            if ((ActionVFX.TransformType)spTransformType.enumValueIndex == ActionVFX.TransformType.SpawnAtWeaponPoint)
            {
                EditorGUILayout.PropertyField(spWeaponBone);
            }

            EditorGUILayout.PropertyField(spAudioClipToPlayOnAwake);
            EditorGUILayout.PropertyField(spAudioClipToPlayOnDestroy);

            EditorGUILayout.PropertyField(spVFXToPlayOnDestroy);
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
    }
}