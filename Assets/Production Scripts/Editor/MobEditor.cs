using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Vi.Core.CombatAgents;
using Vi.Core.VFX;
using Vi.ScriptableObjects;

namespace Vi.Editor
{
    [CustomEditor(typeof(Mob))]
    [CanEditMultipleObjects]
    public class MobEditor : UnityEditor.Editor
    {
        private SerializedProperty spMaxHP;
        private SerializedProperty spWeaponOption;
        private SerializedProperty spWhitelistedAilments;
        private SerializedProperty spArmorType;
        private SerializedProperty spRaceAndGender;

        private SerializedProperty spDeathBehavior;
        private SerializedProperty spDeathShakeDuration;

        private SerializedProperty spDeathVFX;
        private SerializedProperty spDeathVFXAttack;

        private void OnEnable()
        {
            spMaxHP = serializedObject.FindProperty("maxHP");
            spWeaponOption = serializedObject.FindProperty("weaponOption");
            spWhitelistedAilments = serializedObject.FindProperty("whitelistedAilments");
            spArmorType = serializedObject.FindProperty("armorType");
            spRaceAndGender = serializedObject.FindProperty("raceAndGender");

            spDeathBehavior = serializedObject.FindProperty("deathBehavior");
            spDeathShakeDuration = serializedObject.FindProperty("deathShakeDuration");

            spDeathVFX = serializedObject.FindProperty("deathVFX");
            spDeathVFXAttack = serializedObject.FindProperty("deathVFXAttack");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(spMaxHP);
            EditorGUILayout.PropertyField(spWeaponOption);
            EditorGUILayout.PropertyField(spWhitelistedAilments);
            EditorGUILayout.PropertyField(spArmorType);
            EditorGUILayout.PropertyField(spRaceAndGender);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(spDeathBehavior);
            switch ((Mob.DeathBehavior)spDeathBehavior.enumValueIndex)
            {
                case Mob.DeathBehavior.Ragdoll:
                    break;
                case Mob.DeathBehavior.Shake:
                    EditorGUILayout.PropertyField(spDeathShakeDuration);
                    break;
                default:
                    Debug.LogError("Unsure how to handle death behavior in editor" + (Mob.DeathBehavior)spDeathBehavior.enumValueIndex);
                    break;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(spDeathVFX);
            if (spDeathVFX.objectReferenceValue)
            {
                if (spDeathVFX.objectReferenceValue.GetType().IsSubclassOf(typeof(GameInteractiveActionVFX)) | spDeathVFX.objectReferenceValue.GetType() == typeof(GameInteractiveActionVFX))
                {
                    EditorGUILayout.PropertyField(spDeathVFXAttack);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}