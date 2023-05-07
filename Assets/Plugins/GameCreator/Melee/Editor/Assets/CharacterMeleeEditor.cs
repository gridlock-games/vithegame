using UnityEditorInternal;

namespace GameCreator.Melee
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(CharacterMelee))]
    public class CharacterMeleeEditor : Editor
    {
        private SerializedProperty spCurrentWeapon;
        private SerializedProperty spCurrentShield;

        private SerializedProperty spMaxHealth;
        private SerializedProperty spPoiseDelay;
        private SerializedProperty spPoiseMax;
        private SerializedProperty spPoiseRecovery;

        private SerializedProperty spHitRenderer;
        private SerializedProperty spDefaultMaterials;
        private SerializedProperty spHitMaterials;
        private SerializedProperty spMeshColorResetDelay;
        

        // INITIALIZER: ---------------------------------------------------------------------------

        private void OnEnable()
        {
            this.spCurrentWeapon = this.serializedObject.FindProperty("currentWeapon");
            this.spCurrentShield = this.serializedObject.FindProperty("currentShield");

            this.spMaxHealth = this.serializedObject.FindProperty("maxHealth");
            this.spPoiseDelay = this.serializedObject.FindProperty("delayPoise");
            this.spPoiseMax = this.serializedObject.FindProperty("maxPoise");
            this.spPoiseRecovery = this.serializedObject.FindProperty("poiseRecoveryRate");
           
            this.spHitRenderer = this.serializedObject.FindProperty("hitRenderer");
            this.spDefaultMaterials = this.serializedObject.FindProperty("defaultMaterials");
            this.spHitMaterials = this.serializedObject.FindProperty("hitMaterials");
            this.spMeshColorResetDelay = this.serializedObject.FindProperty("meshColorResetDelay");
        }

        // PAINT METHODS: -------------------------------------------------------------------------

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(this.spCurrentWeapon);
            EditorGUILayout.PropertyField(this.spCurrentShield);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(this.spMaxHealth);
            EditorGUILayout.PropertyField(this.spPoiseDelay);
            EditorGUILayout.PropertyField(this.spPoiseMax);
            EditorGUILayout.PropertyField(this.spPoiseRecovery);
            
            EditorGUILayout.Space(15);
            EditorGUILayout.PropertyField(this.spHitRenderer);
            EditorGUILayout.PropertyField(this.spDefaultMaterials);
            EditorGUILayout.PropertyField(this.spHitMaterials);
            EditorGUILayout.PropertyField(this.spMeshColorResetDelay);

            this.serializedObject.ApplyModifiedProperties();
        }
    }
}
 