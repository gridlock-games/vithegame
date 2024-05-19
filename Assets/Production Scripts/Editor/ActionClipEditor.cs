using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using UnityEditor;
using System.IO;

namespace Vi.Editor
{
    [CustomEditor(typeof(ActionClip))]
    [CanEditMultipleObjects]
    public class ActionClipEditor : UnityEditor.Editor
    {
        private SerializedProperty spClipType;

        private SerializedProperty spFollowUpActionClipsToPlay;

        private SerializedProperty spHitReactionClipType;

        private SerializedProperty spAgentStaminaCost;
        private SerializedProperty spAgentRageCost;

        private SerializedProperty spShouldApplyRootMotion;
        private SerializedProperty spRootMotionForwardMultiplier;
        private SerializedProperty spRootMotionSidesMultiplier;
        private SerializedProperty spRootMotionVerticalMultiplier;

        private SerializedProperty spAttackRootMotionForwardMultiplier;
        private SerializedProperty spAttackRootMotionSidesMultiplier;
        private SerializedProperty spAttackRootMotionVerticalMultiplier;

        private SerializedProperty spShouldFlinch;
        private SerializedProperty spFlinchAmountMin;
        private SerializedProperty spFlinchAmountMax;
        private SerializedProperty spShouldPlayHitReaction;

        private SerializedProperty spDebugForwardMotion;
        private SerializedProperty spDebugSidesMotion;
        private SerializedProperty spDebugVerticalMotion;

        private SerializedProperty spAvatarLayer;
        private SerializedProperty spTransitionTime;
        private SerializedProperty spDodgeCancelTransitionTime;
        private SerializedProperty spAnimationSpeed;
        private SerializedProperty spRecoveryAnimationSpeed;

        private SerializedProperty spYAngleRotationOffset;

        private SerializedProperty spEffectedWeaponBones;
        private SerializedProperty spWeaponBonesToHide;
        private SerializedProperty spMustBeAiming;

        private SerializedProperty spChargeAttackHasEndAnimation;
        private SerializedProperty spChargeAttackStateLoopCount;
        private SerializedProperty spCanEnhance;
        private SerializedProperty spChargeTimeDamageMultiplier;
        private SerializedProperty spEnhancedChargeDamageMultiplier;
        private SerializedProperty spChargePenaltyDamage;

        private SerializedProperty spAttackingNormalizedTime;
        private SerializedProperty spRecoveryNormalizedTime;
        private SerializedProperty spIsAffectedByRage;
        private SerializedProperty spDamage;
        private SerializedProperty spHealAmount;
        private SerializedProperty spStaminaDamage;
        private SerializedProperty spHealthPenaltyOnMiss;
        private SerializedProperty spStaminaPenaltyOnMiss;
        private SerializedProperty spRagePenaltyOnMiss;
        private SerializedProperty spMaxHitLimit;
        private SerializedProperty spTimeBetweenHits;
        private SerializedProperty spIsBlockable;
        private SerializedProperty spIsUninterruptable;
        private SerializedProperty spIsInvincible;
        private SerializedProperty spCanFlashAttack;
        private SerializedProperty spIsFollowUpAttack;
        private SerializedProperty spAilment;
        private SerializedProperty spGrabAttackClip;
        private SerializedProperty spGrabVictimClip;
        private SerializedProperty spAilmentHitDefinition;
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

        private SerializedProperty spUseRotationalTargetingSystem;
        private SerializedProperty spLimitAttackMotionBasedOnTarget;
        //private SerializedProperty spBoxCastOriginPositionOffset;
        //private SerializedProperty spBoxCastHalfExtents;
        //private SerializedProperty spBoxCastDistance;
        private SerializedProperty spMaximumTargetingRotationAngle;

        private SerializedProperty spCanLunge;
        private SerializedProperty spMinLungeDistance;
        private SerializedProperty spMaxLungeDistance;

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

            spFollowUpActionClipsToPlay = serializedObject.FindProperty("followUpActionClipsToPlay");

            spHitReactionClipType = serializedObject.FindProperty("hitReactionType");

            spShouldApplyRootMotion = serializedObject.FindProperty("shouldApplyRootMotion");
            spRootMotionForwardMultiplier = serializedObject.FindProperty("rootMotionForwardMultiplier");
            spRootMotionSidesMultiplier = serializedObject.FindProperty("rootMotionSidesMultiplier");
            spRootMotionVerticalMultiplier = serializedObject.FindProperty("rootMotionVerticalMultiplier");

            spAttackRootMotionForwardMultiplier = serializedObject.FindProperty("attackRootMotionForwardMultiplier");
            spAttackRootMotionSidesMultiplier = serializedObject.FindProperty("attackRootMotionSidesMultiplier");
            spAttackRootMotionVerticalMultiplier = serializedObject.FindProperty("attackRootMotionVerticalMultiplier");

            spShouldFlinch = serializedObject.FindProperty("shouldFlinch");
            spFlinchAmountMin = serializedObject.FindProperty("flinchAmountMin");
            spFlinchAmountMax = serializedObject.FindProperty("flinchAmountMax");
            spShouldPlayHitReaction = serializedObject.FindProperty("shouldPlayHitReaction");

            spAvatarLayer = serializedObject.FindProperty("avatarLayer");
            spTransitionTime = serializedObject.FindProperty("transitionTime");
            spDodgeCancelTransitionTime = serializedObject.FindProperty("dodgeCancelTransitionTime");
            spAnimationSpeed = serializedObject.FindProperty("animationSpeed");
            spRecoveryAnimationSpeed = serializedObject.FindProperty("recoveryAnimationSpeed");
            spYAngleRotationOffset = serializedObject.FindProperty("YAngleRotationOffset");

            spAgentStaminaCost = serializedObject.FindProperty("agentStaminaCost");
            spAgentRageCost = serializedObject.FindProperty("agentRageCost");

            spEffectedWeaponBones = serializedObject.FindProperty("effectedWeaponBones");
            spWeaponBonesToHide = serializedObject.FindProperty("weaponBonesToHide");
            spMustBeAiming = serializedObject.FindProperty("mustBeAiming");

            spChargeAttackHasEndAnimation = serializedObject.FindProperty("chargeAttackHasEndAnimation");
            spChargeAttackStateLoopCount = serializedObject.FindProperty("chargeAttackStateLoopCount");
            spCanEnhance = serializedObject.FindProperty("canEnhance");
            spChargeTimeDamageMultiplier = serializedObject.FindProperty("chargeTimeDamageMultiplier");
            spEnhancedChargeDamageMultiplier = serializedObject.FindProperty("enhancedChargeDamageMultiplier");
            spChargePenaltyDamage = serializedObject.FindProperty("chargePenaltyDamage");

            spAttackingNormalizedTime = serializedObject.FindProperty("attackingNormalizedTime");
            spRecoveryNormalizedTime = serializedObject.FindProperty("recoveryNormalizedTime");
            spIsAffectedByRage = serializedObject.FindProperty("isAffectedByRage");
            spDamage = serializedObject.FindProperty("damage");
            spHealAmount = serializedObject.FindProperty("healAmount");
            spStaminaDamage = serializedObject.FindProperty("staminaDamage");
            spHealthPenaltyOnMiss = serializedObject.FindProperty("healthPenaltyOnMiss");
            spStaminaPenaltyOnMiss = serializedObject.FindProperty("staminaPenaltyOnMiss");
            spRagePenaltyOnMiss = serializedObject.FindProperty("ragePenaltyOnMiss");
            spMaxHitLimit = serializedObject.FindProperty("maxHitLimit");
            spTimeBetweenHits = serializedObject.FindProperty("timeBetweenHits");
            spIsBlockable = serializedObject.FindProperty("isBlockable");
            spIsUninterruptable = serializedObject.FindProperty("isUninterruptable");
            spIsInvincible = serializedObject.FindProperty("isInvincible");
            spCanFlashAttack = serializedObject.FindProperty("canFlashAttack");
            spIsFollowUpAttack = serializedObject.FindProperty("isFollowUpAttack");
            spAilment = serializedObject.FindProperty("ailment");
            spGrabAttackClip = serializedObject.FindProperty("grabAttackClip");
            spGrabVictimClip = serializedObject.FindProperty("grabVictimClip");
            spAilmentHitDefinition = serializedObject.FindProperty("ailmentHitDefinition");
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

            spUseRotationalTargetingSystem = serializedObject.FindProperty("useRotationalTargetingSystem");
            spLimitAttackMotionBasedOnTarget = serializedObject.FindProperty("limitAttackMotionBasedOnTarget");
            //spBoxCastOriginPositionOffset = serializedObject.FindProperty("boxCastOriginPositionOffset");
            //spBoxCastHalfExtents = serializedObject.FindProperty("boxCastHalfExtents");
            //spBoxCastDistance = serializedObject.FindProperty("boxCastDistance");
            spMaximumTargetingRotationAngle = serializedObject.FindProperty("maximumTargetingRotationAngle");

            spCanLunge = serializedObject.FindProperty("canLunge");
            spMinLungeDistance = serializedObject.FindProperty("minLungeDistance");
            spMaxLungeDistance = serializedObject.FindProperty("maxLungeDistance");

            spShouldAimBody = serializedObject.FindProperty("shouldAimBody");
            spShouldAimOffHand = serializedObject.FindProperty("shouldAimOffHand");
            spAimDuringAnticipation = serializedObject.FindProperty("aimDuringAnticipation");
            spAimDuringAttack = serializedObject.FindProperty("aimDuringAttack");
            spAimDuringRecovery = serializedObject.FindProperty("aimDuringRecovery");
            spRequireAmmo = serializedObject.FindProperty("requireAmmo");
            spRequiredAmmoAmount = serializedObject.FindProperty("requiredAmmoAmount");

            spDebugForwardMotion = serializedObject.FindProperty("debugForwardMotion");
            spDebugSidesMotion = serializedObject.FindProperty("debugSidesMotion");
            spDebugVerticalMotion = serializedObject.FindProperty("debugVerticalMotion");
        }

        private readonly List<ActionClip.ClipType> actionClipAttackTypes = new List<ActionClip.ClipType>()
        {
            ActionClip.ClipType.LightAttack,
            ActionClip.ClipType.HeavyAttack,
            ActionClip.ClipType.FlashAttack,
            ActionClip.ClipType.Ability,
            ActionClip.ClipType.GrabAttack
        };

        private Weapon weapon;
        private AnimatorOverrideController animatorOverrideController;
        private AnimationClip animationClip;
        public override void OnInspectorGUI()
        {
            if ((ActionClip.ClipType)spClipType.enumValueIndex == ActionClip.ClipType.Flinch)
            {
                EditorGUILayout.PropertyField(spClipType);
                EditorGUILayout.PropertyField(spTransitionTime);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.PropertyField(spFollowUpActionClipsToPlay);
            EditorGUILayout.PropertyField(spClipType);
            EditorGUILayout.PropertyField(spTransitionTime);
            if (actionClipAttackTypes.Contains((ActionClip.ClipType)spClipType.enumValueIndex)) { EditorGUILayout.PropertyField(spDodgeCancelTransitionTime); }
            EditorGUILayout.PropertyField(spAnimationSpeed);

            if (actionClipAttackTypes.Contains((ActionClip.ClipType)spClipType.enumValueIndex)) { EditorGUILayout.PropertyField(spRecoveryAnimationSpeed); }
            
            EditorGUILayout.PropertyField(spAvatarLayer);
            spYAngleRotationOffset.floatValue = EditorGUILayout.Slider("Y Angle Rotation Offset", spYAngleRotationOffset.floatValue, 0, 360);

            EditorGUILayout.LabelField("Root Motion Settings", EditorStyles.whiteLargeLabel);
            EditorGUILayout.PropertyField(spShouldApplyRootMotion);
            if (spShouldApplyRootMotion.boolValue)
            {
                EditorGUILayout.LabelField("Curves are MULTIPLIERS of what is baked into the animation", EditorStyles.whiteLabel);
                EditorGUILayout.PropertyField(spRootMotionForwardMultiplier);
                EditorGUILayout.PropertyField(spRootMotionSidesMultiplier);
                EditorGUILayout.PropertyField(spRootMotionVerticalMultiplier);

                Rect buttonRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.button);
                buttonRect = new Rect(
                    buttonRect.x + EditorGUIUtility.labelWidth,
                    buttonRect.y,
                    buttonRect.width - EditorGUIUtility.labelWidth,
                    buttonRect.height
                );

                if (animationClip)
                {
                    if (GUI.Button(buttonRect, "Copy Key Positions To Mutliplier Curves"))
                    {
                        AnimationCurve newAnimationCurve = spRootMotionForwardMultiplier.animationCurveValue;
                        foreach (Keyframe keyframe in spDebugForwardMotion.animationCurveValue.keys)
                        {
                            newAnimationCurve.AddKey(keyframe.time, 1);
                        }
                        spRootMotionForwardMultiplier.animationCurveValue = newAnimationCurve;

                        newAnimationCurve = spRootMotionSidesMultiplier.animationCurveValue;
                        foreach (Keyframe keyframe in spDebugSidesMotion.animationCurveValue.keys)
                        {
                            newAnimationCurve.AddKey(keyframe.time, 1);
                        }
                        spRootMotionSidesMultiplier.animationCurveValue = newAnimationCurve;

                        newAnimationCurve = spRootMotionVerticalMultiplier.animationCurveValue;
                        foreach (Keyframe keyframe in spDebugVerticalMotion.animationCurveValue.keys)
                        {
                            newAnimationCurve.AddKey(keyframe.time, 1);
                        }
                        spRootMotionVerticalMultiplier.animationCurveValue = newAnimationCurve;
                    }

                    EditorGUILayout.LabelField("Weapon Name: " + weapon.name, EditorStyles.whiteMiniLabel);
                    EditorGUILayout.LabelField("Animator Override Controller Name: " + animatorOverrideController.name, EditorStyles.whiteMiniLabel);
                    EditorGUILayout.LabelField("Animation Clip Name: " + animationClip.name, EditorStyles.whiteMiniLabel);

                    ExtractRootMotion();

                    EditorGUILayout.PropertyField(spDebugForwardMotion);
                    EditorGUILayout.PropertyField(spDebugSidesMotion);
                    EditorGUILayout.PropertyField(spDebugVerticalMotion);
                }
                else
                {
                    // Analyze root motion of original animation
                    if (GUI.Button(buttonRect, "Analyze Baked Root Motion Of Animation"))
                    {
                        (weapon, animatorOverrideController, animationClip) = FindAnimationClip();
                    }
                }
            }
            EditorGUILayout.Space();

            if (actionClipAttackTypes.Contains((ActionClip.ClipType)spClipType.enumValueIndex))
            {
                if ((ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.None)
                {
                    EditorGUILayout.PropertyField(spShouldPlayHitReaction);
                }
                else
                {
                    spShouldPlayHitReaction.boolValue = true;
                }

                if (spShouldPlayHitReaction.boolValue)
                {
                    EditorGUILayout.LabelField("These curves multiply the root motion of the hit reaction on the victim", EditorStyles.whiteLabel);
                    EditorGUILayout.PropertyField(spAttackRootMotionForwardMultiplier);
                    EditorGUILayout.PropertyField(spAttackRootMotionSidesMultiplier);
                    EditorGUILayout.PropertyField(spAttackRootMotionVerticalMultiplier);
                }
                
                EditorGUILayout.PropertyField(spShouldFlinch);
                if (spShouldFlinch.boolValue)
                {
                    EditorGUILayout.PropertyField(spFlinchAmountMin);
                    EditorGUILayout.PropertyField(spFlinchAmountMax);
                }
            }

            EditorGUILayout.LabelField("Statuses", EditorStyles.whiteLargeLabel);
            EditorGUILayout.PropertyField(spStatusesToApplyToSelfOnActivate);
            EditorGUILayout.PropertyField(spStatusesToApplyToTargetOnHit);
            EditorGUILayout.PropertyField(spStatusesToApplyToTeammateOnHit);
            EditorGUILayout.Space();
            
            if ((ActionClip.ClipType)spClipType.enumValueIndex == ActionClip.ClipType.LightAttack)
            {
                EditorGUILayout.PropertyField(spEffectedWeaponBones);
                EditorGUILayout.PropertyField(spWeaponBonesToHide);
                EditorGUILayout.PropertyField(spMustBeAiming);
                EditorGUILayout.PropertyField(spIsAffectedByRage);

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(spDamage);
                EditorGUILayout.PropertyField(spHealAmount);
                EditorGUILayout.PropertyField(spStaminaDamage);
                EditorGUILayout.PropertyField(spMaxHitLimit);
                if (spMaxHitLimit.intValue > 1) { EditorGUILayout.PropertyField(spTimeBetweenHits); }

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(spIsBlockable);
                EditorGUILayout.PropertyField(spIsUninterruptable);
                EditorGUILayout.PropertyField(spIsInvincible);
                EditorGUILayout.PropertyField(spCanFlashAttack);
                EditorGUILayout.PropertyField(spIsFollowUpAttack);

                EditorGUILayout.Space();
                if (!spMustBeAiming.boolValue)
                {
                    EditorGUILayout.PropertyField(spUseRotationalTargetingSystem);
                    EditorGUILayout.PropertyField(spLimitAttackMotionBasedOnTarget);
                    if (spUseRotationalTargetingSystem.boolValue | spLimitAttackMotionBasedOnTarget.boolValue)
                    {
                        //EditorGUILayout.PropertyField(spBoxCastOriginPositionOffset);
                        //EditorGUILayout.PropertyField(spBoxCastHalfExtents);
                        //EditorGUILayout.PropertyField(spBoxCastDistance);
                        EditorGUILayout.PropertyField(spMaximumTargetingRotationAngle);
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(spCanLunge);
                    if (spCanLunge.boolValue)
                    {
                        EditorGUILayout.PropertyField(spMinLungeDistance);
                    }
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(spAilment);
                EditorGUILayout.PropertyField(spAilmentHitDefinition);
                if ((ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Grab)
                {
                    EditorGUILayout.PropertyField(spGrabAttackClip);
                    EditorGUILayout.PropertyField(spGrabVictimClip);
                }
                EditorGUILayout.PropertyField(spDodgeLock);
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
                EditorGUILayout.PropertyField(spWeaponBonesToHide);
                EditorGUILayout.PropertyField(spMustBeAiming);
                EditorGUILayout.PropertyField(spIsAffectedByRage);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Charge Attack Settings", EditorStyles.whiteLargeLabel);
                EditorGUILayout.PropertyField(spChargeAttackHasEndAnimation);
                EditorGUILayout.PropertyField(spChargeAttackStateLoopCount);
                EditorGUILayout.PropertyField(spCanEnhance);
                EditorGUILayout.PropertyField(spChargeTimeDamageMultiplier);
                if (spCanEnhance.boolValue) { EditorGUILayout.PropertyField(spEnhancedChargeDamageMultiplier); }
                EditorGUILayout.PropertyField(spChargePenaltyDamage);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(spAgentStaminaCost);
                EditorGUILayout.PropertyField(spDamage);
                EditorGUILayout.PropertyField(spHealAmount);
                EditorGUILayout.PropertyField(spStaminaDamage);
                EditorGUILayout.PropertyField(spMaxHitLimit);
                if (spMaxHitLimit.intValue > 1) { EditorGUILayout.PropertyField(spTimeBetweenHits); }

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(spIsBlockable);
                EditorGUILayout.PropertyField(spIsUninterruptable);
                EditorGUILayout.PropertyField(spIsInvincible);
                EditorGUILayout.PropertyField(spCanFlashAttack);
                EditorGUILayout.PropertyField(spIsFollowUpAttack);

                EditorGUILayout.Space();

                if (!spMustBeAiming.boolValue)
                {
                    EditorGUILayout.PropertyField(spUseRotationalTargetingSystem);
                    EditorGUILayout.PropertyField(spLimitAttackMotionBasedOnTarget);
                    if (spUseRotationalTargetingSystem.boolValue | spLimitAttackMotionBasedOnTarget.boolValue)
                    {
                        //EditorGUILayout.PropertyField(spBoxCastOriginPositionOffset);
                        //EditorGUILayout.PropertyField(spBoxCastHalfExtents);
                        //EditorGUILayout.PropertyField(spBoxCastDistance);
                        EditorGUILayout.PropertyField(spMaximumTargetingRotationAngle);
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(spCanLunge);
                    if (spCanLunge.boolValue)
                    {
                        EditorGUILayout.PropertyField(spMinLungeDistance);
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(spAilment);
                EditorGUILayout.PropertyField(spAilmentHitDefinition);
                if ((ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Grab)
                {
                    EditorGUILayout.PropertyField(spGrabAttackClip);
                    EditorGUILayout.PropertyField(spGrabVictimClip);
                }
                EditorGUILayout.PropertyField(spDodgeLock);
                EditorGUILayout.PropertyField(spCanCancelLightAttacks);
                EditorGUILayout.PropertyField(spCanCancelHeavyAttacks);
                EditorGUILayout.PropertyField(spCanCancelAbilities);
                EditorGUILayout.PropertyField(spCanBeCancelledByLightAttacks);
                EditorGUILayout.PropertyField(spCanBeCancelledByHeavyAttacks);
                EditorGUILayout.PropertyField(spCanBeCancelledByAbilities);
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
                EditorGUILayout.PropertyField(spWeaponBonesToHide);

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(spAgentStaminaCost);
                EditorGUILayout.PropertyField(spAgentRageCost);

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(spDamage);
                EditorGUILayout.PropertyField(spHealAmount);
                EditorGUILayout.PropertyField(spStaminaDamage);

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(spHealthPenaltyOnMiss);
                EditorGUILayout.PropertyField(spStaminaPenaltyOnMiss);
                EditorGUILayout.PropertyField(spRagePenaltyOnMiss);
                EditorGUILayout.PropertyField(spMaxHitLimit);
                if (spMaxHitLimit.intValue > 1) { EditorGUILayout.PropertyField(spTimeBetweenHits); }
                EditorGUILayout.PropertyField(spIsBlockable);
                EditorGUILayout.PropertyField(spIsUninterruptable);
                EditorGUILayout.PropertyField(spIsInvincible);
                EditorGUILayout.PropertyField(spCanFlashAttack);
                EditorGUILayout.PropertyField(spIsFollowUpAttack);

                EditorGUILayout.Space();

                if (!spMustBeAiming.boolValue)
                {
                    EditorGUILayout.PropertyField(spUseRotationalTargetingSystem);
                    EditorGUILayout.PropertyField(spLimitAttackMotionBasedOnTarget);
                    if (spUseRotationalTargetingSystem.boolValue | spLimitAttackMotionBasedOnTarget.boolValue)
                    {
                        //EditorGUILayout.PropertyField(spBoxCastOriginPositionOffset);
                        //EditorGUILayout.PropertyField(spBoxCastHalfExtents);
                        //EditorGUILayout.PropertyField(spBoxCastDistance);
                        EditorGUILayout.PropertyField(spMaximumTargetingRotationAngle);
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(spCanLunge);
                    if (spCanLunge.boolValue)
                    {
                        EditorGUILayout.PropertyField(spMinLungeDistance);
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(spAilment);
                EditorGUILayout.PropertyField(spAilmentHitDefinition);
                if ((ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Grab)
                {
                    EditorGUILayout.PropertyField(spGrabAttackClip);
                    EditorGUILayout.PropertyField(spGrabVictimClip);
                }
                EditorGUILayout.PropertyField(spDodgeLock);
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
                EditorGUILayout.PropertyField(spWeaponBonesToHide);

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(spAgentStaminaCost);
                EditorGUILayout.PropertyField(spAgentRageCost);

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(spDamage);
                EditorGUILayout.PropertyField(spHealAmount);
                EditorGUILayout.PropertyField(spStaminaDamage);
                EditorGUILayout.PropertyField(spMaxHitLimit);
                if (spMaxHitLimit.intValue > 1) { EditorGUILayout.PropertyField(spTimeBetweenHits); }
                EditorGUILayout.PropertyField(spIsBlockable);
                EditorGUILayout.PropertyField(spIsUninterruptable);
                EditorGUILayout.PropertyField(spIsInvincible);
                EditorGUILayout.PropertyField(spCanFlashAttack);
                EditorGUILayout.PropertyField(spIsFollowUpAttack);

                EditorGUILayout.Space();

                if (!spMustBeAiming.boolValue)
                {
                    EditorGUILayout.PropertyField(spUseRotationalTargetingSystem);
                    EditorGUILayout.PropertyField(spLimitAttackMotionBasedOnTarget);
                    if (spUseRotationalTargetingSystem.boolValue | spLimitAttackMotionBasedOnTarget.boolValue)
                    {
                        //EditorGUILayout.PropertyField(spBoxCastOriginPositionOffset);
                        //EditorGUILayout.PropertyField(spBoxCastHalfExtents);
                        //EditorGUILayout.PropertyField(spBoxCastDistance);
                        EditorGUILayout.PropertyField(spMaximumTargetingRotationAngle);
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(spCanLunge);
                    if (spCanLunge.boolValue)
                    {
                        EditorGUILayout.PropertyField(spMinLungeDistance);
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(spAilment);
                EditorGUILayout.PropertyField(spAilmentHitDefinition);
                if ((ActionClip.Ailment)spAilment.enumValueIndex == ActionClip.Ailment.Grab)
                {
                    EditorGUILayout.PropertyField(spGrabAttackClip);
                    EditorGUILayout.PropertyField(spGrabVictimClip);
                }
                EditorGUILayout.PropertyField(spDodgeLock);
                EditorGUILayout.PropertyField(spCanCancelLightAttacks);
                EditorGUILayout.PropertyField(spCanCancelHeavyAttacks);
                EditorGUILayout.PropertyField(spCanCancelAbilities);
                EditorGUILayout.PropertyField(spCanBeCancelledByLightAttacks);
                EditorGUILayout.PropertyField(spCanBeCancelledByHeavyAttacks);
                EditorGUILayout.PropertyField(spCanBeCancelledByAbilities);
                EditorGUILayout.PropertyField(spAbilityCooldownTime);
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
            else if ((ActionClip.ClipType)spClipType.enumValueIndex == ActionClip.ClipType.GrabAttack)
            {
                EditorGUILayout.PropertyField(spEffectedWeaponBones);
                EditorGUILayout.PropertyField(spWeaponBonesToHide);

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(spDamage);
                EditorGUILayout.PropertyField(spHealAmount);
                EditorGUILayout.PropertyField(spStaminaDamage);
                EditorGUILayout.PropertyField(spMaxHitLimit);
                if (spMaxHitLimit.intValue > 1) { EditorGUILayout.PropertyField(spTimeBetweenHits); }

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(spAilment);
                EditorGUILayout.PropertyField(spAilmentHitDefinition);
                EditorGUILayout.PropertyField(spDodgeLock);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Attack Phase Settings", EditorStyles.whiteLargeLabel);
                EditorGUILayout.LabelField("Normalized time is progress of an animation on a scale of 0 - 1", EditorStyles.whiteLabel);
                spAttackingNormalizedTime.floatValue = EditorGUILayout.Slider("Attacking Normalized Time", spAttackingNormalizedTime.floatValue, 0, 1);
                spRecoveryNormalizedTime.floatValue = EditorGUILayout.Slider("Recovery Normalized Time", spRecoveryNormalizedTime.floatValue, 0, 1);
            }
            else if ((ActionClip.ClipType)spClipType.enumValueIndex == ActionClip.ClipType.Lunge)
            {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(spAgentStaminaCost);
                EditorGUILayout.PropertyField(spAgentRageCost);

                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(spMaxLungeDistance);
            }
            else if ((ActionClip.ClipType)spClipType.enumValueIndex != ActionClip.ClipType.Dodge & (ActionClip.ClipType)spClipType.enumValueIndex != ActionClip.ClipType.Flinch)
            {
                Debug.LogError("Unsure how to handle clip type " + (ActionClip.ClipType)spClipType.enumValueIndex);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Action VFX", EditorStyles.whiteLargeLabel);
            EditorGUILayout.PropertyField(spActionVFXList);
            if ((ActionClip.ClipType)spClipType.enumValueIndex == ActionClip.ClipType.Ability)
            {
                EditorGUILayout.PropertyField(spPreviewActionVFX);
                EditorGUILayout.PropertyField(spPreviewActionVFXScale);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private (Weapon, AnimatorOverrideController, AnimationClip) FindAnimationClip()
        {
            string[] filepaths = Directory.GetFiles(@"Assets/Production/Weapons", "*.asset", SearchOption.AllDirectories);
            Weapon targetWeapon = null;
            foreach (string filepath in filepaths)
            {
                Weapon weapon = AssetDatabase.LoadAssetAtPath<Weapon>(filepath);
                ActionClip thisActionClip = serializedObject.targetObject as ActionClip;
                ActionClip weaponClip = weapon.GetActionClipByNameUsingReflection(serializedObject.targetObject.name);

                if (weaponClip == thisActionClip)
                {
                    targetWeapon = weapon;
                    break;
                }
            }

            if (!targetWeapon) { Debug.LogError("Could not find target weapon for " + serializedObject.targetObject.name); return (null, null, null); }

            filepaths = Directory.GetFiles(@"Assets/Production/AnimationControllers", "*.overrideController", SearchOption.AllDirectories);
            foreach (string filepath in filepaths)
            {
                if (Path.GetFileNameWithoutExtension(filepath) != targetWeapon.name.Replace("Weapon", "")) { continue; }

                AnimatorOverrideController animatorOverrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(filepath);

                List<KeyValuePair<AnimationClip, AnimationClip>> animationOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                animatorOverrideController.GetOverrides(animationOverrides);
                foreach (var kvp in animationOverrides)
                {
                    if (kvp.Key.name == serializedObject.targetObject.name)
                    {
                        return (targetWeapon, animatorOverrideController, kvp.Value);
                    }
                }
            }
            Debug.LogError("Could not find target animation clip");
            return (null, null, null);
        }

        private void ExtractRootMotion()
        {
            if (animationClip != null)
            {
                if (animationClip.hasRootCurves)
                {
                    EditorCurveBinding[] curves = AnimationUtility.GetCurveBindings(animationClip);
                    for (int i = 0; i < curves.Length; ++i)
                    {
                        if (curves[i].propertyName == "RootT.x")
                        {
                            AnimationCurve curve = AnimationUtility.GetEditorCurve(
                                animationClip,
                                curves[i]
                            );
                            curve = ProcessRootCurve(curve);
                            spDebugSidesMotion.animationCurveValue = curve;
                        }

                        if (curves[i].propertyName == "RootT.y")
                        {
                            AnimationCurve curve = AnimationUtility.GetEditorCurve(
                                animationClip,
                                curves[i]
                            );
                            curve = ProcessRootCurve(curve);
                            spDebugForwardMotion.animationCurveValue = curve;
                        }

                        if (curves[i].propertyName == "RootT.z")
                        {
                            AnimationCurve curve = AnimationUtility.GetEditorCurve(
                                animationClip,
                                curves[i]
                            );
                            curve = ProcessRootCurve(curve);
                            spDebugVerticalMotion.animationCurveValue = curve;
                        }
                    }
                }
            }
        }

        private AnimationCurve ProcessRootCurve(AnimationCurve source)
        {
            float value = source.Evaluate(0f);
            float duration = source.keys[source.length - 1].time;
            AnimationCurve result = new AnimationCurve();

            for (int i = 0; i < source.keys.Length; ++i)
            {
                result.AddKey(new Keyframe(
                    source.keys[i].time / duration,
                    source.keys[i].value - value,
                    source.keys[i].inTangent,
                    source.keys[i].outTangent,
                    source.keys[i].inWeight,
                    source.keys[i].outWeight
                ));
            }

            return result;
        }
    }
}