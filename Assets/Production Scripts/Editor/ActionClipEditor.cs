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

        private SerializedProperty spHitReactionClipType;

        private SerializedProperty spAgentStaminaCost;
        private SerializedProperty spAgentDefenseCost;
        private SerializedProperty spAgentRageCost;

        private SerializedProperty spShouldApplyRootMotion;
        private SerializedProperty spRootMotionForwardMultiplier;
        private SerializedProperty spRootMotionSidesMultiplier;
        private SerializedProperty spRootMotionVerticalMultiplier;
        private SerializedProperty spAvatarLayer;
        private SerializedProperty spTransitionTime;
        private SerializedProperty spAnimationSpeed;

        private SerializedProperty spEffectedWeaponBones;
        private SerializedProperty spMustBeAiming;
        private SerializedProperty spAttackingNormalizedTime;
        private SerializedProperty spRecoveryNormalizedTime;
        private SerializedProperty spDamage;
        private SerializedProperty spHealAmount;
        private SerializedProperty spStaminaDamage;
        private SerializedProperty spDefenseDamage;
        private SerializedProperty spMaxHitLimit;
        private SerializedProperty spTimeBetweenHits;
        private SerializedProperty spIsBlockable;
        private SerializedProperty spIsUninterruptable;
        private SerializedProperty spIsInvincible;
        private SerializedProperty spAilment;
        private SerializedProperty spAilmentDuration;
        private SerializedProperty spGrabDistance;
        private SerializedProperty spDodgeLock;

        private SerializedProperty spCanCancelLightAttacks;
        private SerializedProperty spCanCancelHeavyAttacks;
        private SerializedProperty spCanCancelAbilities;

        private SerializedProperty spCanBeCancelledByLightAttacks;
        private SerializedProperty spCanBeCancelledByHeavyAttacks;
        private SerializedProperty spCanBeCancelledByAbilities;

        private SerializedProperty spAbilityImageIcon;
        private SerializedProperty spAbilityCooldownTime;

        private SerializedProperty spStatusesToApplyToSelfOnActivate;
        private SerializedProperty spStatusesToApplyToTargetOnHit;
        private SerializedProperty spStatusesToApplyToTeammateOnHit;

        private SerializedProperty spActionVFXList;
        private SerializedProperty spPreviewActionVFX;
        private SerializedProperty spPreviewActionVFXScale;

        private SerializedProperty spShouldAimBody;
        private SerializedProperty spShouldAimOffHand;
        private SerializedProperty spAimDuringAnticipation;
        private SerializedProperty spAimDuringAttack;
        private SerializedProperty spAimDuringRecovery;
        private SerializedProperty spRequireAmmo;
        private SerializedProperty spRequiredAmmoAmount;

        private void OnEnable()
        {
            spClipType = serializedObject.FindProperty("clipType");
            spHitReactionClipType = serializedObject.FindProperty("hitReactionType");
            spShouldApplyRootMotion = serializedObject.FindProperty("shouldApplyRootMotion");
            spRootMotionForwardMultiplier = serializedObject.FindProperty("rootMotionForwardMultiplier");
            spRootMotionSidesMultiplier = serializedObject.FindProperty("rootMotionSidesMultiplier");
            spRootMotionVerticalMultiplier = serializedObject.FindProperty("rootMotionVerticalMultiplier");
            spAvatarLayer = serializedObject.FindProperty("avatarLayer");
            spTransitionTime = serializedObject.FindProperty("transitionTime");
            spAnimationSpeed = serializedObject.FindProperty("animationSpeed");

            spAgentStaminaCost = serializedObject.FindProperty("agentStaminaCost");
            spAgentDefenseCost = serializedObject.FindProperty("agentDefenseCost");
            spAgentRageCost = serializedObject.FindProperty("agentRageCost");

            spEffectedWeaponBones = serializedObject.FindProperty("effectedWeaponBones");
            spMustBeAiming = serializedObject.FindProperty("mustBeAiming");
            spAttackingNormalizedTime = serializedObject.FindProperty("attackingNormalizedTime");
            spRecoveryNormalizedTime = serializedObject.FindProperty("recoveryNormalizedTime");
            spDamage = serializedObject.FindProperty("damage");
            spHealAmount = serializedObject.FindProperty("healAmount");
            spStaminaDamage = serializedObject.FindProperty("staminaDamage");
            spDefenseDamage = serializedObject.FindProperty("defenseDamage");
            spMaxHitLimit = serializedObject.FindProperty("maxHitLimit");
            spTimeBetweenHits = serializedObject.FindProperty("timeBetweenHits");
            spIsBlockable = serializedObject.FindProperty("isBlockable");
            spIsUninterruptable = serializedObject.FindProperty("isUninterruptable");
            spIsInvincible = serializedObject.FindProperty("isInvincible");
            spAilment = serializedObject.FindProperty("ailment");
            spAilmentDuration = serializedObject.FindProperty("ailmentDuration");
            spGrabDistance = serializedObject.FindProperty("grabDistance");
            spDodgeLock = serializedObject.FindProperty("dodgeLock");
            spAbilityImageIcon = serializedObject.FindProperty("abilityImageIcon");
            spAbilityCooldownTime = serializedObject.FindProperty("abilityCooldownTime");

            spCanCancelLightAttacks = serializedObject.FindProperty("canCancelLightAttacks");
            spCanCancelHeavyAttacks = serializedObject.FindProperty("canCancelHeavyAttacks");
            spCanCancelAbilities = serializedObject.FindProperty("canCancelAbilities");

            spCanBeCancelledByLightAttacks = serializedObject.FindProperty("canBeCancelledByLightAttacks");
            spCanBeCancelledByHeavyAttacks = serializedObject.FindProperty("canBeCancelledByHeavyAttacks");
            spCanBeCancelledByAbilities = serializedObject.FindProperty("canBeCancelledByAbilities");

            spStatusesToApplyToSelfOnActivate = serializedObject.FindProperty("statusesToApplyToSelfOnActivate");
            spStatusesToApplyToTargetOnHit = serializedObject.FindProperty("statusesToApplyToTargetOnHit");
            spStatusesToApplyToTeammateOnHit = serializedObject.FindProperty("statusesToApplyToTeammateOnHit");

            spActionVFXList = serializedObject.FindProperty("actionVFXList");
            spPreviewActionVFX = serializedObject.FindProperty("previewActionVFX");
            spPreviewActionVFXScale = serializedObject.FindProperty("previewActionVFXScale");

            spShouldAimBody = serializedObject.FindProperty("shouldAimBody");
            spShouldAimOffHand = serializedObject.FindProperty("shouldAimOffHand");
            spAimDuringAnticipation = serializedObject.FindProperty("aimDuringAnticipation");
            spAimDuringAttack = serializedObject.FindProperty("aimDuringAttack");
            spAimDuringRecovery = serializedObject.FindProperty("aimDuringRecovery");
            spRequireAmmo = serializedObject.FindProperty("requireAmmo");
            spRequiredAmmoAmount = serializedObject.FindProperty("requiredAmmoAmount");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(spClipType);

            EditorGUILayout.LabelField("Root Motion Settings", EditorStyles.whiteLargeLabel);
            EditorGUILayout.PropertyField(spShouldApplyRootMotion);
            EditorGUILayout.PropertyField(spTransitionTime);
            EditorGUILayout.PropertyField(spAnimationSpeed);
            EditorGUILayout.PropertyField(spAvatarLayer);
            if (spShouldApplyRootMotion.boolValue)
            {
                EditorGUILayout.LabelField("Curves should start at 0 and end at 1", EditorStyles.whiteLabel);
                EditorGUILayout.PropertyField(spRootMotionForwardMultiplier);
                EditorGUILayout.PropertyField(spRootMotionSidesMultiplier);
                EditorGUILayout.PropertyField(spRootMotionVerticalMultiplier);
            }
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Statuses", EditorStyles.whiteLargeLabel);
            EditorGUILayout.PropertyField(spStatusesToApplyToSelfOnActivate);
            EditorGUILayout.PropertyField(spStatusesToApplyToTargetOnHit);
            EditorGUILayout.PropertyField(spStatusesToApplyToTeammateOnHit);
            EditorGUILayout.Space();
            
            if ((ActionClip.ClipType)spClipType.enumValueIndex == ActionClip.ClipType.LightAttack)
            {
                EditorGUILayout.PropertyField(spEffectedWeaponBones);
                EditorGUILayout.PropertyField(spMustBeAiming);
                EditorGUILayout.PropertyField(spDamage);
                EditorGUILayout.PropertyField(spHealAmount);
                EditorGUILayout.PropertyField(spStaminaDamage);
                EditorGUILayout.PropertyField(spDefenseDamage);
                EditorGUILayout.PropertyField(spMaxHitLimit);
                if (spMaxHitLimit.intValue > 1) { EditorGUILayout.PropertyField(spTimeBetweenHits); }
                EditorGUILayout.PropertyField(spIsBlockable);
                EditorGUILayout.PropertyField(spIsUninterruptable);
                EditorGUILayout.PropertyField(spIsInvincible);
                EditorGUILayout.PropertyField(spAilment);
                if ((ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Knockdown
                    | (ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Knockup
                    | (ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Stun)
                {
                    EditorGUILayout.PropertyField(spAilmentDuration);
                }
                else if ((ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Grab)
                {
                    EditorGUILayout.PropertyField(spAilmentDuration);
                    EditorGUILayout.PropertyField(spGrabDistance);
                }
                EditorGUILayout.PropertyField(spDodgeLock);
                EditorGUILayout.PropertyField(spActionVFXList);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Attack Phase Settings", EditorStyles.whiteLargeLabel);
                EditorGUILayout.LabelField("Normalized time is progress of an animation on a scale of 0 - 1", EditorStyles.whiteLabel);
                spAttackingNormalizedTime.floatValue = EditorGUILayout.Slider("Attacking Normalized Time", spAttackingNormalizedTime.floatValue, 0, 1);
                spRecoveryNormalizedTime.floatValue = EditorGUILayout.Slider("Recovery Normalized Time", spRecoveryNormalizedTime.floatValue, 0, 1);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Only for Shooter Weapons", EditorStyles.whiteLargeLabel);
                if (!spMustBeAiming.boolValue)
                {
                    EditorGUILayout.PropertyField(spShouldAimBody);
                    EditorGUILayout.PropertyField(spShouldAimOffHand);
                    EditorGUILayout.PropertyField(spAimDuringAnticipation);
                    EditorGUILayout.PropertyField(spAimDuringAttack);
                    EditorGUILayout.PropertyField(spAimDuringRecovery);
                }
                EditorGUILayout.PropertyField(spRequireAmmo);
                if (spRequireAmmo.boolValue)
                {
                    EditorGUILayout.PropertyField(spRequiredAmmoAmount);
                }
            }
            else if ((ActionClip.ClipType)spClipType.enumValueIndex == ActionClip.ClipType.HeavyAttack)
            {
                EditorGUILayout.PropertyField(spEffectedWeaponBones);
                EditorGUILayout.PropertyField(spMustBeAiming);
                EditorGUILayout.PropertyField(spAgentStaminaCost);
                EditorGUILayout.PropertyField(spDamage);
                EditorGUILayout.PropertyField(spHealAmount);
                EditorGUILayout.PropertyField(spStaminaDamage);
                EditorGUILayout.PropertyField(spDefenseDamage);
                EditorGUILayout.PropertyField(spMaxHitLimit);
                if (spMaxHitLimit.intValue > 1) { EditorGUILayout.PropertyField(spTimeBetweenHits); }
                EditorGUILayout.PropertyField(spIsBlockable);
                EditorGUILayout.PropertyField(spIsUninterruptable);
                EditorGUILayout.PropertyField(spIsInvincible);
                EditorGUILayout.PropertyField(spAilment);

                if ((ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Knockdown
                    | (ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Knockup
                    | (ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Stun)
                {
                    EditorGUILayout.PropertyField(spAilmentDuration);
                }
                else if ((ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Grab)
                {
                    EditorGUILayout.PropertyField(spAilmentDuration);
                    EditorGUILayout.PropertyField(spGrabDistance);
                }
                EditorGUILayout.PropertyField(spDodgeLock);
                EditorGUILayout.PropertyField(spActionVFXList);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Attack Phase Settings", EditorStyles.whiteLargeLabel);
                EditorGUILayout.LabelField("Normalized time is progress of an animation on a scale of 0 - 1", EditorStyles.whiteLabel);
                spAttackingNormalizedTime.floatValue = EditorGUILayout.Slider("Attacking Normalized Time", spAttackingNormalizedTime.floatValue, 0, 1);
                spRecoveryNormalizedTime.floatValue = EditorGUILayout.Slider("Recovery Normalized Time", spRecoveryNormalizedTime.floatValue, 0, 1);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Only for Shooter Weapons", EditorStyles.whiteLargeLabel);
                if (!spMustBeAiming.boolValue)
                {
                    EditorGUILayout.PropertyField(spShouldAimBody);
                    EditorGUILayout.PropertyField(spShouldAimOffHand);
                    EditorGUILayout.PropertyField(spAimDuringAnticipation);
                    EditorGUILayout.PropertyField(spAimDuringAttack);
                    EditorGUILayout.PropertyField(spAimDuringRecovery);
                }
                EditorGUILayout.PropertyField(spRequireAmmo);
                if (spRequireAmmo.boolValue)
                {
                    EditorGUILayout.PropertyField(spRequiredAmmoAmount);
                }
            }
            else if ((ActionClip.ClipType)spClipType.enumValueIndex == ActionClip.ClipType.FlashAttack)
            {
                EditorGUILayout.PropertyField(spEffectedWeaponBones);
                EditorGUILayout.PropertyField(spAgentStaminaCost);
                EditorGUILayout.PropertyField(spAgentDefenseCost);
                EditorGUILayout.PropertyField(spAgentRageCost);
                EditorGUILayout.PropertyField(spDamage);
                EditorGUILayout.PropertyField(spHealAmount);
                EditorGUILayout.PropertyField(spStaminaDamage);
                EditorGUILayout.PropertyField(spDefenseDamage);
                EditorGUILayout.PropertyField(spMaxHitLimit);
                if (spMaxHitLimit.intValue > 1) { EditorGUILayout.PropertyField(spTimeBetweenHits); }
                EditorGUILayout.PropertyField(spIsBlockable);
                EditorGUILayout.PropertyField(spIsUninterruptable);
                EditorGUILayout.PropertyField(spIsInvincible);
                EditorGUILayout.PropertyField(spAilment);

                if ((ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Knockdown
                    | (ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Knockup
                    | (ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Stun)
                {
                    EditorGUILayout.PropertyField(spAilmentDuration);
                }
                else if ((ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Grab)
                {
                    EditorGUILayout.PropertyField(spAilmentDuration);
                    EditorGUILayout.PropertyField(spGrabDistance);
                }
                EditorGUILayout.PropertyField(spDodgeLock);
                EditorGUILayout.PropertyField(spActionVFXList);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Attack Phase Settings", EditorStyles.whiteLargeLabel);
                EditorGUILayout.LabelField("Normalized time is progress of an animation on a scale of 0 - 1", EditorStyles.whiteLabel);
                spAttackingNormalizedTime.floatValue = EditorGUILayout.Slider("Attacking Normalized Time", spAttackingNormalizedTime.floatValue, 0, 1);
                spRecoveryNormalizedTime.floatValue = EditorGUILayout.Slider("Recovery Normalized Time", spRecoveryNormalizedTime.floatValue, 0, 1);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Only for Shooter Weapons", EditorStyles.whiteLargeLabel);
                if (!spMustBeAiming.boolValue)
                {
                    EditorGUILayout.PropertyField(spShouldAimBody);
                    EditorGUILayout.PropertyField(spShouldAimOffHand);
                    EditorGUILayout.PropertyField(spAimDuringAnticipation);
                    EditorGUILayout.PropertyField(spAimDuringAttack);
                    EditorGUILayout.PropertyField(spAimDuringRecovery);
                }
                EditorGUILayout.PropertyField(spRequireAmmo);
                if (spRequireAmmo.boolValue)
                {
                    EditorGUILayout.PropertyField(spRequiredAmmoAmount);
                }
            }
            else if ((ActionClip.ClipType)spClipType.enumValueIndex == ActionClip.ClipType.HitReaction)
            {
                EditorGUILayout.PropertyField(spHitReactionClipType);
                EditorGUILayout.PropertyField(spAilment);
            }
            else if ((ActionClip.ClipType)spClipType.enumValueIndex == ActionClip.ClipType.Ability)
            {
                EditorGUILayout.PropertyField(spAbilityImageIcon);

                EditorGUILayout.PropertyField(spEffectedWeaponBones);
                EditorGUILayout.PropertyField(spAgentStaminaCost);
                EditorGUILayout.PropertyField(spAgentDefenseCost);
                EditorGUILayout.PropertyField(spAgentRageCost);
                EditorGUILayout.PropertyField(spDamage);
                EditorGUILayout.PropertyField(spHealAmount);
                EditorGUILayout.PropertyField(spStaminaDamage);
                EditorGUILayout.PropertyField(spDefenseDamage);
                EditorGUILayout.PropertyField(spMaxHitLimit);
                if (spMaxHitLimit.intValue > 1) { EditorGUILayout.PropertyField(spTimeBetweenHits); }
                EditorGUILayout.PropertyField(spIsBlockable);
                EditorGUILayout.PropertyField(spIsUninterruptable);
                EditorGUILayout.PropertyField(spIsInvincible);
                EditorGUILayout.PropertyField(spAilment);

                if ((ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Knockdown
                    | (ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Knockup
                    | (ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Stun)
                {
                    EditorGUILayout.PropertyField(spAilmentDuration);
                }
                else if ((ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Grab)
                {
                    EditorGUILayout.PropertyField(spAilmentDuration);
                    EditorGUILayout.PropertyField(spGrabDistance);
                }
                EditorGUILayout.PropertyField(spDodgeLock);
                EditorGUILayout.PropertyField(spCanCancelLightAttacks);
                EditorGUILayout.PropertyField(spCanCancelHeavyAttacks);
                EditorGUILayout.PropertyField(spCanCancelAbilities);
                EditorGUILayout.PropertyField(spCanBeCancelledByLightAttacks);
                EditorGUILayout.PropertyField(spCanBeCancelledByHeavyAttacks);
                EditorGUILayout.PropertyField(spCanBeCancelledByAbilities);
                EditorGUILayout.PropertyField(spAbilityCooldownTime);
                EditorGUILayout.PropertyField(spActionVFXList);
                EditorGUILayout.PropertyField(spPreviewActionVFX);
                EditorGUILayout.PropertyField(spPreviewActionVFXScale);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Attack Phase Settings", EditorStyles.whiteLargeLabel);
                EditorGUILayout.LabelField("Normalized time is progress of an animation on a scale of 0 - 1", EditorStyles.whiteLabel);
                spAttackingNormalizedTime.floatValue = EditorGUILayout.Slider("Attacking Normalized Time", spAttackingNormalizedTime.floatValue, 0, 1);
                spRecoveryNormalizedTime.floatValue = EditorGUILayout.Slider("Recovery Normalized Time", spRecoveryNormalizedTime.floatValue, 0, 1);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Only for Shooter Characters", EditorStyles.whiteLargeLabel);
                if (!spMustBeAiming.boolValue)
                {
                    EditorGUILayout.PropertyField(spShouldAimBody);
                    EditorGUILayout.PropertyField(spShouldAimOffHand);
                    EditorGUILayout.PropertyField(spAimDuringAnticipation);
                    EditorGUILayout.PropertyField(spAimDuringAttack);
                    EditorGUILayout.PropertyField(spAimDuringRecovery);
                }
                EditorGUILayout.PropertyField(spRequireAmmo);
                if (spRequireAmmo.boolValue)
                {
                    EditorGUILayout.PropertyField(spRequiredAmmoAmount);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}