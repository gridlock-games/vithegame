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
        
        private SerializedProperty spRageMax;
        private SerializedProperty spRageRecovery;

        private SerializedProperty spEventKnockedUpHitLimitReached;

        private SerializedProperty spCharacterCamera;
        private SerializedProperty spBoxCastHalfExtents;
        private SerializedProperty spBoxCastDistance;

        // INITIALIZER: ---------------------------------------------------------------------------

        private void OnEnable()
        {
            this.spCurrentWeapon = this.serializedObject.FindProperty("currentWeapon");
            this.spCurrentShield = this.serializedObject.FindProperty("currentShield");

            this.spMaxHealth = this.serializedObject.FindProperty("maxHealth");
            this.spPoiseDelay = this.serializedObject.FindProperty("delayPoise");
            this.spPoiseMax = this.serializedObject.FindProperty("maxPoise");
            this.spPoiseRecovery = this.serializedObject.FindProperty("poiseRecoveryRate");
            this.spRageMax = this.serializedObject.FindProperty("maxRage");
            this.spRageRecovery = this.serializedObject.FindProperty("rageRecoveryRate");

            
            this.spCharacterCamera = this.serializedObject.FindProperty("characterCamera");
            this.spBoxCastHalfExtents = this.serializedObject.FindProperty("boxCastHalfExtents");
            this.spBoxCastDistance = this.serializedObject.FindProperty("boxCastDistance");

            this.spEventKnockedUpHitLimitReached = this.serializedObject.FindProperty("EventKnockedUpHitLimitReached");
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
            EditorGUILayout.PropertyField(this.spRageMax);
            EditorGUILayout.PropertyField(this.spRageRecovery);

            

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(this.spCharacterCamera);
            EditorGUILayout.PropertyField(this.spBoxCastHalfExtents);
            EditorGUILayout.PropertyField(this.spBoxCastDistance);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(this.spEventKnockedUpHitLimitReached);

            this.serializedObject.ApplyModifiedProperties();
        }
    }
}
 