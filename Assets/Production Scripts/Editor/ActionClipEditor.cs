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

        private SerializedProperty spAgentStaminaCost;
        private SerializedProperty spAgentDefenseCost;
        private SerializedProperty spAgentRageCost;

        private SerializedProperty spRootMotionMulitplier;

        private SerializedProperty spWeaponBone;
        private SerializedProperty spAttackingNormalizedTime;
        private SerializedProperty spRecoveryNormalizedTime;
        private SerializedProperty spDamage;
        private SerializedProperty spStaminaDamage;
        private SerializedProperty spDefenseDamage;
        private SerializedProperty spMaxHitLimit;
        private SerializedProperty spIsBlockable;
        private SerializedProperty spAilment;
        private SerializedProperty spAilmentDuration;

        private SerializedProperty spHitReactionClipType;

        private void OnEnable()
        {
            spClipType = serializedObject.FindProperty("clipType");
            spRootMotionMulitplier = serializedObject.FindProperty("rootMotionMulitplier");

            spAgentStaminaCost = serializedObject.FindProperty("agentStaminaCost");
            spAgentDefenseCost = serializedObject.FindProperty("agentDefenseCost");
            spAgentRageCost = serializedObject.FindProperty("agentRageCost");

            spWeaponBone = serializedObject.FindProperty("weaponBone");
            spAttackingNormalizedTime = serializedObject.FindProperty("attackingNormalizedTime");
            spRecoveryNormalizedTime = serializedObject.FindProperty("recoveryNormalizedTime");
            spDamage = serializedObject.FindProperty("damage");
            spStaminaDamage = serializedObject.FindProperty("staminaDamage");
            spDefenseDamage = serializedObject.FindProperty("defenseDamage");
            spMaxHitLimit = serializedObject.FindProperty("maxHitLimit");
            spIsBlockable = serializedObject.FindProperty("isBlockable");
            spAilment = serializedObject.FindProperty("ailment");
            spAilmentDuration = serializedObject.FindProperty("ailmentDuration");

            spHitReactionClipType = serializedObject.FindProperty("hitReactionType");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(spClipType);
            EditorGUILayout.PropertyField(spRootMotionMulitplier);

            if ((ActionClip.ClipType)spClipType.enumValueIndex == ActionClip.ClipType.Dodge)
            {
                EditorGUILayout.PropertyField(spAgentStaminaCost);
            }
            else if ((ActionClip.ClipType)spClipType.enumValueIndex == ActionClip.ClipType.LightAttack)
            {
                EditorGUILayout.PropertyField(spWeaponBone);
                EditorGUILayout.PropertyField(spDamage);
                EditorGUILayout.PropertyField(spStaminaDamage);
                EditorGUILayout.PropertyField(spDefenseDamage);
                EditorGUILayout.PropertyField(spMaxHitLimit);
                EditorGUILayout.PropertyField(spIsBlockable);
                EditorGUILayout.PropertyField(spAilment);
                if ((ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Knockdown
                    | (ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Knockup
                    | (ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Stun)
                {
                    EditorGUILayout.PropertyField(spAilmentDuration);
                }

                EditorGUILayout.LabelField("Attack Phase Settings", EditorStyles.whiteLargeLabel);
                EditorGUILayout.LabelField("Normalized time is progress of an animation on a scale of 0 - 1", EditorStyles.whiteLabel);
                spAttackingNormalizedTime.floatValue = EditorGUILayout.Slider("Attacking Normalized Time", spAttackingNormalizedTime.floatValue, 0, 1);
                spRecoveryNormalizedTime.floatValue = EditorGUILayout.Slider("Recovery Normalized Time", spRecoveryNormalizedTime.floatValue, 0, 1);
            }
            else if ((ActionClip.ClipType)spClipType.enumValueIndex == ActionClip.ClipType.HeavyAttack)
            {
                EditorGUILayout.PropertyField(spWeaponBone);
                EditorGUILayout.PropertyField(spAgentStaminaCost);
                EditorGUILayout.PropertyField(spDamage);
                EditorGUILayout.PropertyField(spStaminaDamage);
                EditorGUILayout.PropertyField(spDefenseDamage);
                EditorGUILayout.PropertyField(spMaxHitLimit);
                EditorGUILayout.PropertyField(spIsBlockable);
                EditorGUILayout.PropertyField(spAilment);

                if ((ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Knockdown
                    | (ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Knockup
                    | (ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Stun)
                {
                    EditorGUILayout.PropertyField(spAilmentDuration);
                }
                    
                EditorGUILayout.LabelField("Attack Phase Settings", EditorStyles.whiteLargeLabel);
                EditorGUILayout.LabelField("Normalized time is progress of an animation on a scale of 0 - 1", EditorStyles.whiteLabel);
                spAttackingNormalizedTime.floatValue = EditorGUILayout.Slider("Attacking Normalized Time", spAttackingNormalizedTime.floatValue, 0, 1);
                spRecoveryNormalizedTime.floatValue = EditorGUILayout.Slider("Recovery Normalized Time", spRecoveryNormalizedTime.floatValue, 0, 1);
            }
            else if ((ActionClip.ClipType)spClipType.enumValueIndex == ActionClip.ClipType.HitReaction)
            {
                EditorGUILayout.PropertyField(spHitReactionClipType);
                EditorGUILayout.PropertyField(spAilment);
            }
            else if ((ActionClip.ClipType)spClipType.enumValueIndex == ActionClip.ClipType.Ability)
            {
                EditorGUILayout.PropertyField(spWeaponBone);
                EditorGUILayout.PropertyField(spAgentStaminaCost);
                EditorGUILayout.PropertyField(spAgentDefenseCost);
                EditorGUILayout.PropertyField(spAgentRageCost);
                EditorGUILayout.PropertyField(spDamage);
                EditorGUILayout.PropertyField(spStaminaDamage);
                EditorGUILayout.PropertyField(spDefenseDamage);
                EditorGUILayout.PropertyField(spMaxHitLimit);
                EditorGUILayout.PropertyField(spIsBlockable);
                EditorGUILayout.PropertyField(spAilment);

                if ((ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Knockdown
                    | (ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Knockup
                    | (ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Stun)
                {
                    EditorGUILayout.PropertyField(spAilmentDuration);
                }

                EditorGUILayout.LabelField("Attack Phase Settings", EditorStyles.whiteLargeLabel);
                EditorGUILayout.LabelField("Normalized time is progress of an animation on a scale of 0 - 1", EditorStyles.whiteLabel);
                spAttackingNormalizedTime.floatValue = EditorGUILayout.Slider("Attacking Normalized Time", spAttackingNormalizedTime.floatValue, 0, 1);
                spRecoveryNormalizedTime.floatValue = EditorGUILayout.Slider("Recovery Normalized Time", spRecoveryNormalizedTime.floatValue, 0, 1);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}