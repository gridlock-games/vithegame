using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Core.CombatAgents;
using Vi.ProceduralAnimations;
using Vi.Utility;
using Vi.Core.MeshSlicing;
using System.Linq;
using Vi.Core.Weapons;
using System.Security.Cryptography.X509Certificates;

namespace Vi.Core
{
    [DisallowMultipleComponent]
    public class AnimationHandler : NetworkBehaviour
    {
        public bool WaitingForActionClipToPlay { get; private set; }

        public void PlayAction(ActionClip actionClip, bool isFollowUpClip = false)
        {
            if (!actionClip) { Debug.LogError("Trying to play a null action clip! " + name); return; }

            if (IsServer)
            {
                AddActionToServerQueue(actionClip.name, isFollowUpClip, false);
                WaitingForActionClipToPlay = true;
            }
            else if (IsOwner)
            {
                CanPlayActionClipResult canPlayActionClipResult = CanPlayActionClip(actionClip, isFollowUpClip);
                if (!canPlayActionClipResult.canPlay) { return; }
                PlayActionServerRpc(actionClip.name, isFollowUpClip);
                WaitingForActionClipToPlay = true;
            }
            else
            {
                Debug.LogError("You should not be calling AnimationHandler.PlayAction() when we aren't the owner or the server " + actionClip + " " + name);
            }
        }

        public float GetTotalActionClipLengthInSeconds(ActionClip actionClip)
        {
            if (!actionClip) { Debug.LogError("Calling GetTotalActionClipLengthInSeconds with a null action clip! " + name); return 2; }

            List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            combatAgent.WeaponHandler.AnimatorOverrideControllerInstance.GetOverrides(overrides);
            string stateName = GetActionClipAnimationStateNameWithoutLayer(actionClip);
            foreach (KeyValuePair<AnimationClip, AnimationClip> @override in overrides)
            {
                if (!@override.Key | !@override.Value) { continue; }
                if (@override.Key.name == stateName)
                {
                    return @override.Value.length + actionClip.transitionTime;
                }
            }

            AnimationClip clip = combatAgent.WeaponHandler.GetWeapon().GetAnimationClip(stateName);
            if (clip) { return clip.length; }
            Debug.LogError("Couldn't find an animation clip for action clip " + actionClip.name + " with weapon " + combatAgent.WeaponHandler.GetWeapon());
            return 2;
        }

        public void AddClothCapsuleCollider(MagicaCloth2.MagicaCapsuleCollider magicaCapsuleCollider)
        {
            if (animatorReference)
            {
                foreach (KeyValuePair<CharacterReference.EquipmentType, WearableEquipment> kvp in animatorReference.WearableEquipmentInstances)
                {
                    if (kvp.Value)
                    {
                        foreach (MagicaCloth2.MagicaCloth cloth in kvp.Value.ClothInstances)
                        {
                            if (!cloth.SerializeData.colliderCollisionConstraint.colliderList.Contains(magicaCapsuleCollider))
                            {
                                cloth.SerializeData.colliderCollisionConstraint.colliderList.Add(magicaCapsuleCollider);
                                cloth.SetParameterChange();
                            }
                        }
                    }
                }
            }
        }

        public void RemoveClothCapsuleCollider(MagicaCloth2.MagicaCapsuleCollider magicaCapsuleCollider)
        {
            if (animatorReference)
            {
                foreach (KeyValuePair<CharacterReference.EquipmentType, WearableEquipment> kvp in animatorReference.WearableEquipmentInstances)
                {
                    if (kvp.Value)
                    {
                        foreach (MagicaCloth2.MagicaCloth cloth in kvp.Value.ClothInstances)
                        {
                            cloth.SerializeData.colliderCollisionConstraint.colliderList.Remove(magicaCapsuleCollider);
                            cloth.SetParameterChange();
                        }
                    }
                }
            }
        }

        public bool IsActionClipPlaying(ActionClip actionClip)
        {
            string animationStateName = GetActionClipAnimationStateName(actionClip);
            if (actionClip.GetClipType() == ActionClip.ClipType.Flinch)
            {
                return animatorReference.CurrentFlinchAnimatorStateInfo.IsName(animationStateName) | animatorReference.NextFlinchAnimatorStateInfo.IsName(animationStateName);
            }
            else
            {
                return animatorReference.CurrentActionsAnimatorStateInfo.IsName(animationStateName) | animatorReference.NextActionsAnimatorStateInfo.IsName(animationStateName);
            }
        }

        public bool IsActionClipPlayingInCurrentState(ActionClip actionClip)
        {
            if (!actionClip) { Debug.LogError("Calling IsActionClipPlayingInCurrentState with a null action clip! " + name); return false; }
            string animationStateName = GetActionClipAnimationStateName(actionClip);
            if (actionClip.GetClipType() == ActionClip.ClipType.Flinch)
            {
                return animatorReference.CurrentFlinchAnimatorStateInfo.IsName(animationStateName);
            }
            else
            {
                return animatorReference.CurrentActionsAnimatorStateInfo.IsName(animationStateName);
            }
        }

        private string GetActionClipAnimationStateName(ActionClip actionClip)
        {
            if (!actionClip) { Debug.LogError("Calling GetActionClipAnimationStateName with a null action clip! " + name); return ""; }
            string animationStateName = actionClip.name;
            if (actionClip.GetClipType() == ActionClip.ClipType.GrabAttack) { animationStateName = "GrabAttack"; }
            if (actionClip.GetClipType() == ActionClip.ClipType.HeavyAttack) { animationStateName = actionClip.name + "_Attack"; }
            if (actionClip.GetClipType() == ActionClip.ClipType.Lunge) { animationStateName = "LungeF"; }
            animationStateName = (actionClip.GetClipType() == ActionClip.ClipType.Flinch ? flinchLayerName : actionsLayerName) + "." + animationStateName;
            return animationStateName;
        }

        private string GetActionClipAnimationStateNameWithoutLayer(ActionClip actionClip)
        {
            if (!actionClip) { Debug.LogError("Calling GetActionClipAnimationStateNameWithoutLayer with a null action clip! " + name); return ""; }
            string animationStateName = actionClip.name;
            if (actionClip.GetClipType() == ActionClip.ClipType.GrabAttack) { animationStateName = "GrabAttack"; }
            if (actionClip.GetClipType() == ActionClip.ClipType.HeavyAttack) { animationStateName = actionClip.name + "_Attack"; }
            if (actionClip.GetClipType() == ActionClip.ClipType.Lunge) { animationStateName = "LungeF"; }
            return animationStateName;
        }

        public float GetActionClipNormalizedTime(ActionClip actionClip)
        {
            if (!actionClip) { Debug.LogError("Calling GetActionClipNormalizedTime with a null action clip! " + name); return 0; }
            string stateName = GetActionClipAnimationStateName(actionClip);
            float normalizedTime = 0;
            if (animatorReference.CurrentActionsAnimatorStateInfo.IsName(stateName))
            {
                normalizedTime = animatorReference.CurrentActionsAnimatorStateInfo.normalizedTime;
            }
            else if (animatorReference.NextActionsAnimatorStateInfo.IsName(stateName))
            {
                normalizedTime = animatorReference.NextActionsAnimatorStateInfo.normalizedTime;
            }
            return normalizedTime;
        }

        public bool IsAtRest()
        {
            if (!animatorReference) { return true; }
            return animatorReference.IsAtRest;
        }

        public bool IsAtRestIgnoringTransition()
        {
            if (!animatorReference) { return true; }
            return animatorReference.IsAtRestIgnoringTransition();
        }

        public bool CanAim()
        {
            if (!lastClipPlayed) { return true; }
            if (IsAtRest())
            {
                return true;
            }
            else
            {
                return !(lastClipPlayed.GetClipType() == ActionClip.ClipType.Dodge | lastClipPlayed.GetClipType() == ActionClip.ClipType.HitReaction);
            }
        }

        public bool IsAiming()
        {
            if (!Animator) { return false; }
            return Animator.IsInTransition(Animator.GetLayerIndex("Aiming")) | !Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Aiming")).IsName("Empty");
        }

        public bool IsReloading()
        {
            if (!lastClipPlayed) { return false; }
            if (lastClipPlayed.GetClipType() != ActionClip.ClipType.Reload) { return false; }
            return !IsAtRest();
        }

        public bool IsDodging()
        {
            if (!lastClipPlayed) { return false; }
            if (lastClipPlayed.GetClipType() != ActionClip.ClipType.Dodge) { return false; }
            return !IsAtRest();
        }

        public bool IsLunging()
        {
            if (!lastClipPlayed) { return false; }
            if (lastClipPlayed.GetClipType() != ActionClip.ClipType.Lunge) { return false; }
            return !IsAtRest();
        }

        public bool IsPlayingBlockingHitReaction()
        {
            if (!lastClipPlayed) { return false; }
            if (lastClipPlayed.GetHitReactionType() != ActionClip.HitReactionType.Blocking) { return false; }
            return !IsAtRest();
        }

        public const float flinchingMovementSpeedMultiplier = 0.8f;
        public bool IsFlinching()
        {
            return !animatorReference.CurrentFlinchAnimatorStateInfo.IsName("Empty");
        }

        public bool IsGrabAttacking()
        {
            if (!lastClipPlayed) { return false; }
            if (lastClipPlayed.GetClipType() != ActionClip.ClipType.GrabAttack) { return false; }
            return !IsAtRestIgnoringTransition();
        }

        public bool IsCharging()
        {
            if (!lastClipPlayed) { return false; }
            if (lastClipPlayed.GetClipType() != ActionClip.ClipType.HeavyAttack) { return false; }
            string stateName = GetActionClipAnimationStateName(lastClipPlayed);
            return animatorReference.CurrentActionsAnimatorStateInfo.IsName(stateName + "_Loop")
                | animatorReference.CurrentActionsAnimatorStateInfo.IsName(stateName + "_Enhance")
                | animatorReference.CurrentActionsAnimatorStateInfo.IsName(stateName + "_Start");
        }

        public void OnDeath()
        {
            if (playAdditionalClipsCoroutine != null) { StopCoroutine(playAdditionalClipsCoroutine); }
            if (heavyAttackCoroutine != null) { StopCoroutine(heavyAttackCoroutine); }

            if (waitForLungeThenPlayAttackCorountine != null) { StopCoroutine(waitForLungeThenPlayAttackCorountine); }

            combatAgent.SetInviniciblity(0);
            combatAgent.SetUninterruptable(0);
            if (IsServer) { combatAgent.StatusAgent.RemoveAllStatuses(); }
        }

        public void HideRenderers()
        {
            StartCoroutine(HideRenderersAfterDuration());
        }

        private const float deadRendererDisplayTime = 5;
        private IEnumerator HideRenderersAfterDuration()
        {
            yield return new WaitForSeconds(deadRendererDisplayTime);
            if (combatAgent.GetAilment() != ActionClip.Ailment.Death) { yield break; }
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in animatorReference.SkinnedMeshRenderers)
            {
                skinnedMeshRenderer.forceRenderingOff = true;
                foreach (var kvp in animatorReference.WearableEquipmentInstances)
                {
                    if (kvp.Value)
                    {
                        foreach (SkinnedMeshRenderer smr in kvp.Value.GetRenderList())
                        {
                            smr.enabled = false;
                        }
                    }
                }
            }
            yield return new WaitUntil(() => combatAgent.GetAilment() != ActionClip.Ailment.Death);
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in animatorReference.SkinnedMeshRenderers)
            {
                skinnedMeshRenderer.forceRenderingOff = false;
                foreach (var kvp in animatorReference.WearableEquipmentInstances)
                {
                    if (kvp.Value)
                    {
                        foreach (SkinnedMeshRenderer smr in kvp.Value.GetRenderList())
                        {
                            smr.enabled = true;
                        }
                    }
                }
            }
        }

        public void OnRevive()
        {
            Animator.Play("Empty", actionsLayerIndex);
            Animator.Play("Empty", flinchLayerIndex);
        }

        public void CancelAllActions(float transitionTime, bool resetGameplayVariables)
        {
            if (!IsServer) { Debug.LogError("AnimationHandler.CancelAllActions() should only be called on the server!"); return; }

            if (playAdditionalClipsCoroutine != null) { StopCoroutine(playAdditionalClipsCoroutine); }
            if (heavyAttackCoroutine != null) { StopCoroutine(heavyAttackCoroutine); }

            if (waitForLungeThenPlayAttackCorountine != null) { StopCoroutine(waitForLungeThenPlayAttackCorountine); }

            if (evaluateGrabAttackHitsCoroutine != null) { StopCoroutine(evaluateGrabAttackHitsCoroutine); }

            Animator.CrossFadeInFixedTime("Empty", transitionTime, actionsLayerIndex);
            Animator.CrossFadeInFixedTime("Empty", transitionTime, flinchLayerIndex);

            if (resetGameplayVariables)
            {
                combatAgent.SetInviniciblity(0);
                combatAgent.SetUninterruptable(0);
                combatAgent.ResetAilment();
                combatAgent.StatusAgent.RemoveAllStatuses();
                combatAgent.WeaponHandler.GetWeapon().ResetAllAbilityCooldowns();
            }
            
            CancelAllActionsClientRpc(transitionTime, resetGameplayVariables);
        }

        [Rpc(SendTo.NotServer)]
        private void CancelAllActionsClientRpc(float transitionTime, bool resetGameplayVariables)
        {
            if (playAdditionalClipsCoroutine != null) { StopCoroutine(playAdditionalClipsCoroutine); }
            if (heavyAttackCoroutine != null) { StopCoroutine(heavyAttackCoroutine); }

            if (waitForLungeThenPlayAttackCorountine != null) { StopCoroutine(waitForLungeThenPlayAttackCorountine); }

            if (evaluateGrabAttackHitsCoroutine != null) { StopCoroutine(evaluateGrabAttackHitsCoroutine); }

            if (resetGameplayVariables)
            {
                Animator.CrossFadeInFixedTime("Empty", transitionTime, actionsLayerIndex);
                Animator.CrossFadeInFixedTime("Empty", transitionTime, flinchLayerIndex);
                combatAgent.WeaponHandler.GetWeapon().ResetAllAbilityCooldowns();
            }
        }

        // Stores the type of the last action clip played
        private ActionClip lastClipPlayed;
        private const float canAttackFromDodgeNormalizedTimeThreshold = 0.55f;
        private const float canAttackFromBlockingHitReactionNormalizedTimeThreshold = 0.15f;

        private readonly static List<ActionClip.ClipType> clipTypesToCheckForCancellation = new List<ActionClip.ClipType>()
        {
            ActionClip.ClipType.LightAttack,
            ActionClip.ClipType.HeavyAttack,
            ActionClip.ClipType.Ability
        };

        public struct CanPlayActionClipResult
        {
            public bool canPlay;
            public bool shouldUseDodgeCancelTransitionTime;

            public CanPlayActionClipResult(bool canPlay, bool shouldUseDodgeCancelTransitionTime)
            {
                this.canPlay = canPlay;
                this.shouldUseDodgeCancelTransitionTime = shouldUseDodgeCancelTransitionTime;
            }

            public static implicit operator bool(CanPlayActionClipResult result) => result.canPlay;
        }

        RaycastHit[] allHits = new RaycastHit[10];
        public CanPlayActionClipResult CanPlayActionClip(ActionClip actionClip, bool isFollowUpClip)
        {
            // Validate input history for light attacks so that players can't cheat their light attack combos
            if (actionClip.GetClipType() == ActionClip.ClipType.LightAttack)
            {
                if (actionClip != combatAgent.WeaponHandler.SelectAttack(Weapon.InputAttackType.LightAttack, combatAgent.WeaponHandler.GetInputHistory())) { return default; }
            }

            if (actionClip.summonableCount > 0)
            {
                if (combatAgent.GetSlaves().Count(item => item.GetAilment() != ActionClip.Ailment.Death) >= ActionClip.maxLivingSummonables)
                {
                    return default;
                }
            }

            string animationStateName = GetActionClipAnimationStateName(actionClip);

            if (!combatAgent.MovementHandler.CanMove()) { return default; }
            if ((combatAgent.StatusAgent.IsRooted()) & actionClip.GetClipType() != ActionClip.ClipType.HitReaction & actionClip.GetClipType() != ActionClip.ClipType.Flinch) { return default; }
            if (actionClip.mustBeAiming & !combatAgent.WeaponHandler.IsAiming()) { return default; }
            if (combatAgent.StatusAgent.IsSilenced() & actionClip.GetClipType() == ActionClip.ClipType.Ability) { return default; }

            if (actionClip.GetClipType() == ActionClip.ClipType.Dodge)
            {
                if (combatAgent.WeaponHandler.GetWeapon().IsDodgeOnCooldown()) { return default; }
            }
            else if (actionClip.GetClipType() == ActionClip.ClipType.Ability)
            {
                if (combatAgent.WeaponHandler.GetWeapon().GetAbilityCooldownProgress(actionClip) < 1) { return default; }
            }

            // Don't allow any clips to be played unless it's a hit reaction if we are in the middle of the grab ailment
            if (actionClip.GetClipType() != ActionClip.ClipType.HitReaction & actionClip.GetClipType() != ActionClip.ClipType.Flinch)
            {
                if (combatAgent.IsGrabbed & actionClip.ailment != ActionClip.Ailment.Grab) { return default; }
            }

            if (!isFollowUpClip & IsServer)
            {
                if (actionClip.IsAttack())
                {
                    if (actionClip.canLunge)
                    {
                        if (!IsLunging())
                        {
                            ActionClip lungeClip = combatAgent.WeaponHandler.GetWeapon().GetLungeClip(actionClip.isUninterruptable, actionClip.isInvincible);

                            if (AreActionClipRequirementsMet(lungeClip) & AreActionClipRequirementsMet(actionClip))
                            {
#if UNITY_EDITOR
                                DebugExtensions.DrawBoxCastBox(transform.position + ActionClip.boxCastOriginPositionOffset, ActionClip.boxCastHalfExtents, transform.forward, transform.rotation, ActionClip.boxCastDistance, Color.red, 1);
#endif
                                int allHitsCount = Physics.BoxCastNonAlloc(transform.position + ActionClip.boxCastOriginPositionOffset,
                                    ActionClip.boxCastHalfExtents, transform.forward.normalized, allHits, transform.rotation,
                                    ActionClip.boxCastDistance, LayerMask.GetMask("NetworkPrediction"), QueryTriggerInteraction.Ignore);

                                List<(NetworkCollider, float, RaycastHit)> angleList = new List<(NetworkCollider, float, RaycastHit)>();
                                for (int i = 0; i < allHitsCount; i++)
                                {
                                    if (allHits[i].transform.root.TryGetComponent(out NetworkCollider networkCollider))
                                    {
                                        if (PlayerDataManager.Singleton.CanHit(combatAgent, networkCollider.CombatAgent) & !networkCollider.CombatAgent.IsInvincible)
                                        {
                                            Quaternion targetRot = Quaternion.LookRotation(networkCollider.transform.position - transform.position, Vector3.up);
                                            angleList.Add((networkCollider,
                                                Mathf.Abs(targetRot.eulerAngles.y - transform.rotation.eulerAngles.y),
                                                allHits[i]));
                                        }
                                    }
                                }

                                angleList.Sort((x, y) => x.Item2.CompareTo(y.Item2));
                                foreach ((NetworkCollider networkCollider, float angle, RaycastHit hit) in angleList)
                                {
                                    Quaternion targetRot = Quaternion.LookRotation(networkCollider.transform.position - transform.position, Vector3.up);
                                    float dist = Vector3.Distance(networkCollider.transform.position, transform.position);
                                    if (angle < ActionClip.maximumLungeAngle & dist >= actionClip.minLungeDistance & dist < lungeClip.maxLungeDistance)
                                    {
                                        PlayAction(lungeClip);
                                        waitForLungeThenPlayAttackCorountine = StartCoroutine(WaitForLungeThenPlayAttack(actionClip));
                                        return default;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (actionClip.GetClipType() != ActionClip.ClipType.Lunge)
            {
                if (waitForLungeThenPlayAttackCorountine != null) { StopCoroutine(waitForLungeThenPlayAttackCorountine); }
            }

            // If we are transitioning to the same state as this actionclip
            if (actionClip.GetClipType() != ActionClip.ClipType.HitReaction & actionClip.GetClipType() != ActionClip.ClipType.Flinch)
            {
                if (animatorReference.NextActionsAnimatorStateInfo.IsName(animationStateName)) { return default; }
            }

            bool isInTransition = Animator.IsInTransition(actionsLayerIndex);
            bool shouldUseDodgeCancelTransitionTime = false;
            // If we are not at rest
            if (!animatorReference.CurrentActionsAnimatorStateInfo.IsName("Empty") | isInTransition)
            {
                string lastClipPlayedAnimationStateName = GetActionClipAnimationStateName(lastClipPlayed);

                bool shouldEvaluatePreviousState = true;
                switch (actionClip.GetClipType())
                {
                    case ActionClip.ClipType.Dodge:
                        // If the clip we are trying to play is a dodge, and we cannot dodge out of the current state, don't play this
                        if (animatorReference.CurrentActionsAnimatorStateInfo.IsName(lastClipPlayedAnimationStateName))
                        {
                            // Dodge lock checks
                            if (actionClip.GetClipType() == ActionClip.ClipType.Dodge)
                            {
                                if (lastClipPlayed.dodgeLock == ActionClip.DodgeLock.EntireAnimation)
                                {
                                    return default;
                                }
                                else if (lastClipPlayed.dodgeLock == ActionClip.DodgeLock.Recovery)
                                {
                                    if (combatAgent.WeaponHandler.IsInRecovery) { return default; }
                                }
                            }
                        }
                        else if (animatorReference.CurrentActionsAnimatorStateInfo.IsTag("CanDodge") | animatorReference.NextActionsAnimatorStateInfo.IsTag("CanDodge"))
                        {
                            if (animatorReference.CurrentActionsAnimatorStateInfo.IsTag("CanDodge"))
                            {
                                // If we are in transition and not returning to the empty state
                                if (isInTransition & !animatorReference.NextActionsAnimatorStateInfo.IsName("Empty")) { return default; }
                            }

                            shouldEvaluatePreviousState = false;
                        }
                        else
                        {
                            return default;
                        }
                        break;
                    case ActionClip.ClipType.LightAttack:
                    case ActionClip.ClipType.HeavyAttack:
                    case ActionClip.ClipType.Ability:
                    case ActionClip.ClipType.FlashAttack:
                    case ActionClip.ClipType.Reload:
                        if (animatorReference.CurrentActionsAnimatorStateInfo.IsName(lastClipPlayedAnimationStateName))
                        {
                            if (IsDodging())
                            {
                                // Check the dodge time threshold, this allows dodges to be interrupted faster by attacks
                                if (animatorReference.CurrentActionsAnimatorStateInfo.normalizedTime < canAttackFromDodgeNormalizedTimeThreshold) { return default; }
                                shouldUseDodgeCancelTransitionTime = true;
                                shouldEvaluatePreviousState = false;
                            }
                            else if (IsPlayingBlockingHitReaction())
                            {
                                // Check the blocking hit reaction time threshold, this allows us to counter from blocking
                                if (animatorReference.CurrentActionsAnimatorStateInfo.normalizedTime < canAttackFromBlockingHitReactionNormalizedTimeThreshold) { return default; }
                                shouldEvaluatePreviousState = false;
                            }
                            else if (lastClipPlayed.GetClipType() == ActionClip.ClipType.HitReaction)
                            {
                                return default;
                            }
                        }
                        break;
                    case ActionClip.ClipType.HitReaction:
                        // If we are transitioning to the last played clip, and the last played clip is a hit reaction that shouldn't be interrupted
                        if (lastClipPlayed.GetClipType() == ActionClip.ClipType.HitReaction)
                        {
                            if (lastClipPlayed.ailment == ActionClip.Ailment.Knockdown)
                            {
                                if (animatorReference.NextActionsAnimatorStateInfo.IsName(lastClipPlayedAnimationStateName)) { return default; }
                            }
                        }
                        break;
                    case ActionClip.ClipType.Lunge:
                    case ActionClip.ClipType.GrabAttack:
                    case ActionClip.ClipType.Flinch:
                        break;
                    default:
                        Debug.LogError("Unsure how to handle clip type " + actionClip.GetClipType());
                        break;
                }

                if (shouldEvaluatePreviousState)
                {
                    // If we are in the middle of dodging, or playing a hit reaction, don't play this clip unless it's a hit reaction
                    if (actionClip.GetClipType() != ActionClip.ClipType.HitReaction & actionClip.GetClipType() != ActionClip.ClipType.Flinch)
                    {
                        if (lastClipPlayed.GetClipType() == ActionClip.ClipType.Dodge) { return default; }
                        if (lastClipPlayed.GetClipType() == ActionClip.ClipType.HitReaction) { return default; }
                    }
                }

                if (actionClip.GetClipType() == ActionClip.ClipType.Ability | actionClip.GetClipType() == ActionClip.ClipType.HeavyAttack)
                {
                    if (animatorReference.CurrentActionsAnimatorStateInfo.IsName(animationStateName)) { return default; }
                    if (!actionClip.canCancelLightAttacks)
                    {
                        if (lastClipPlayed.GetClipType() == ActionClip.ClipType.LightAttack) { return default; }
                    }
                    if (!actionClip.canCancelHeavyAttacks)
                    {
                        if (lastClipPlayed.GetClipType() == ActionClip.ClipType.HeavyAttack) { return default; }
                    }
                    if (!actionClip.canCancelAbilities)
                    {
                        if (lastClipPlayed.GetClipType() == ActionClip.ClipType.Ability) { return default; }
                    }
                }
                else if (actionClip.GetClipType() == ActionClip.ClipType.LightAttack)
                {
                    if (animatorReference.CurrentActionsAnimatorStateInfo.IsName(animationStateName)) { return default; }
                }

                // If the last clip was a clip that can't be cancelled, don't play this clip
                if (clipTypesToCheckForCancellation.Contains(actionClip.GetClipType()) & !combatAgent.WeaponHandler.IsInRecovery & clipTypesToCheckForCancellation.Contains(lastClipPlayed.GetClipType()))
                {
                    if (!(actionClip.GetClipType() == ActionClip.ClipType.LightAttack & lastClipPlayed.canBeCancelledByLightAttacks)
                    & !(actionClip.GetClipType() == ActionClip.ClipType.HeavyAttack & lastClipPlayed.canBeCancelledByHeavyAttacks)
                    & !(actionClip.GetClipType() == ActionClip.ClipType.Ability & lastClipPlayed.canBeCancelledByAbilities))
                    {
                        if (NetworkObject.IsPlayerObject) { return default; }
                    }
                }
            }

            // Checks if the action is not a hit reaction and prevents the animation from getting stuck
            if (actionClip.GetClipType() != ActionClip.ClipType.HitReaction & actionClip.GetClipType() != ActionClip.ClipType.Flinch)
            {
                if (animatorReference.NextActionsAnimatorStateInfo.IsName(animationStateName)) { return default; }
            }

            if (!AreActionClipRequirementsMet(actionClip)) { return default; }

            return new CanPlayActionClipResult(true, shouldUseDodgeCancelTransitionTime);
        }

        [Rpc(SendTo.Server)] private void PlayActionServerRpc(string actionClipName, bool isFollowUpClip) { AddActionToServerQueue(actionClipName, isFollowUpClip, true); }

        [Rpc(SendTo.Owner)] private void ResetWaitingForActionToPlayClientRpc() { WaitingForActionClipToPlay = false; }

        private Queue<(string, bool, bool)> serverActionQueue = new Queue<(string, bool, bool)>();
        private void AddActionToServerQueue(string actionClipName, bool isFollowUpClip, bool wasCalledFromServerRpc)
        {
            serverActionQueue.Enqueue((actionClipName, isFollowUpClip, wasCalledFromServerRpc));
        }

        private bool PlayActionOnServer(string actionClipName, bool isFollowUpClip, bool wasCalledFromServerRpc)
        {
            if (!IsServer) { Debug.LogError("AnimationHandler.PlayActionOnServer() should only be called on the server! " + actionClipName); return false; }

            // Retrieve the appropriate ActionClip based on the provided actionStateName
            ActionClip actionClip = combatAgent.WeaponHandler.GetWeapon().GetActionClipByName(actionClipName);
            if (!actionClip) { return false; }

            CanPlayActionClipResult canPlayActionClipResult = CanPlayActionClip(actionClip, isFollowUpClip);
            if (!canPlayActionClipResult.canPlay)
            {
                WaitingForActionClipToPlay = false;
                if (wasCalledFromServerRpc) { ResetWaitingForActionToPlayClientRpc(); }
                return false;
            }

            // Check stamina and rage requirements
            if (ShouldApplyStaminaCost(actionClip))
            {
                float staminaCost = -GetStaminaCostOfClip(actionClip);
                if (staminaCost != 0) { combatAgent.AddStamina(staminaCost); }
            }
            if (ShouldApplyRageCost(actionClip))
            {
                float rageCost = -GetRageCostOfClip(actionClip);
                if (rageCost != 0) { combatAgent.AddRage(rageCost); }
            }

            // At this point we are going to play the action clip
            // Set the current action clip for the weapon handler
            combatAgent.WeaponHandler.SetActionClip(actionClip, combatAgent.WeaponHandler.GetWeapon().name);
            UpdateAnimationLayerWeights(actionClip.avatarLayer);

            if (actionClip.GetClipType() == ActionClip.ClipType.Dodge)
            {
                SetInvincibleStatusOnDodge(actionClip);
            }

            if (actionClip.GetClipType() != ActionClip.ClipType.Flinch)
            {
                if (heavyAttackCoroutine != null)
                {
                    StopCoroutine(heavyAttackCoroutine);
                    Animator.CrossFadeInFixedTime("Empty", 0, actionsLayerIndex);
                }
            }

            if (evaluateGrabAttackHitsCoroutine != null) { StopCoroutine(evaluateGrabAttackHitsCoroutine); }

            if (actionClip.ailment == ActionClip.Ailment.Grab)
            {
                if (actionClip.GetClipType() == ActionClip.ClipType.HitReaction)
                {
                    ActionClip grabAttackClip = combatAgent.GetGrabReactionClip();
                    if (!grabAttackClip.grabVictimClip) { Debug.LogError("No Grab Victim Clip Found!"); }
                    combatAgent.WeaponHandler.AnimatorOverrideControllerInstance["GrabReaction"] = grabAttackClip.grabVictimClip;
                    combatAgent.WeaponHandler.GetWeapon().OverrideRootMotionCurvesAtRuntime("GrabReaction",
                        grabAttackClip.grabVictimRootMotionData);
                }
                else
                {
                    combatAgent.WeaponHandler.AnimatorOverrideControllerInstance["GrabAttack"] = actionClip.grabAttackClip;
                    combatAgent.WeaponHandler.GetWeapon().OverrideRootMotionCurvesAtRuntime("GrabAttack",
                        actionClip.grabAttackRootMotionData);
                }
            }

            if (actionClip.GetClipType() == ActionClip.ClipType.GrabAttack) { evaluateGrabAttackHitsCoroutine = StartCoroutine(EvaluateGrabAttackHits(actionClip)); }

            combatAgent.MovementHandler.OnServerActionClipPlayed();

            string animationStateName = GetActionClipAnimationStateName(actionClip);
            float transitionTime = canPlayActionClipResult.shouldUseDodgeCancelTransitionTime ? actionClip.dodgeCancelTransitionTime : actionClip.transitionTime;
            // Play the action clip based on its type
            switch (actionClip.GetClipType())
            {
                case ActionClip.ClipType.Dodge:
                case ActionClip.ClipType.LightAttack:
                case ActionClip.ClipType.Ability:
                case ActionClip.ClipType.GrabAttack:
                case ActionClip.ClipType.Lunge:
                case ActionClip.ClipType.Reload:
                    Animator.CrossFadeInFixedTime(animationStateName, transitionTime, actionsLayerIndex);
                    break;
                case ActionClip.ClipType.HeavyAttack:
                    heavyAttackCoroutine = StartCoroutine(PlayHeavyAttack(actionClip));
                    break;
                case ActionClip.ClipType.HitReaction:
                    if (actionClip.ailment != ActionClip.Ailment.Death) { Animator.CrossFadeInFixedTime(animationStateName, transitionTime, actionsLayerIndex, 0); }
                    break;
                case ActionClip.ClipType.FlashAttack:
                    Animator.CrossFadeInFixedTime(animationStateName, transitionTime, actionsLayerIndex, 0);
                    break;
                case ActionClip.ClipType.Flinch:
                    Animator.CrossFadeInFixedTime(animationStateName, transitionTime, flinchLayerIndex, 0);
                    break;
                default:
                    Debug.LogError("Unsure how to play animation state for clip type: " + actionClip.GetClipType());
                    break;
            }
            if (!isFollowUpClip)
            {
                if (playAdditionalClipsCoroutine != null) { StopCoroutine(playAdditionalClipsCoroutine); }
                playAdditionalClipsCoroutine = StartCoroutine(PlayAdditionalClips(actionClip));
            }

            // Invoke the PlayActionClientRpc method on the client side
            PlayActionClientRpc(actionClipName, combatAgent.WeaponHandler.GetWeapon().name.Replace("(Clone)", ""), transitionTime);
            StartCoroutine(ResetWaitingForActionClipToPlayAfterOneFrame());
            // Update the lastClipType to the current action clip type
            if (actionClip.GetClipType() != ActionClip.ClipType.Flinch) { SetLastActionClip(actionClip); }
            return true;
        }

        private IEnumerator ResetWaitingForActionClipToPlayAfterOneFrame()
        {
            yield return null;
            WaitingForActionClipToPlay = false;
        }

        private Coroutine evaluateGrabAttackHitsCoroutine;
        private IEnumerator EvaluateGrabAttackHits(ActionClip grabAttackClip)
        {
            if (!grabAttackClip) { Debug.LogError("Calling EvaluateGrabAttackHits with a null action clip! " + name); yield break; }
            if (grabAttackClip.GetClipType() != ActionClip.ClipType.GrabAttack) { Debug.LogError("AnimationHandler.EvaluateGrabAttackHits() should only be called with a grab attack action clip!"); yield break; }

            // Wait until grab attack is playing fully
            yield return new WaitUntil(() => combatAgent.WeaponHandler.CurrentActionClip == grabAttackClip);
            yield return new WaitUntil(() => IsActionClipPlayingInCurrentState(grabAttackClip));

            // Wait for a grab victim to be assigned
            yield return new WaitUntil(() => combatAgent.GetGrabVictim());
            int successfulHits = 0;
            CombatAgent grabVictim = combatAgent.GetGrabVictim();
            int weaponBoneIndex = -1;
            while (true)
            {
                // If the grab attack is done playing, stop evaluating hits
                if (!IsActionClipPlaying(grabAttackClip)) { break; }
                // If the grab victim disconnects, stop evaluating hits
                if (!grabVictim) { break; }

                // If we are attacking, evaluate a hit
                if (combatAgent.WeaponHandler.IsAttacking)
                {
                    weaponBoneIndex = weaponBoneIndex + 1 == grabAttackClip.effectedWeaponBones.Length ? 0 : weaponBoneIndex + 1;
                    RuntimeWeapon runtimeWeapon = combatAgent.WeaponHandler.WeaponInstances[grabAttackClip.effectedWeaponBones[weaponBoneIndex]];

                    bool hitSucesss = grabVictim.ProcessMeleeHit(combatAgent, grabAttackClip, runtimeWeapon,
                        runtimeWeapon.GetClosetPointFromAttributes(grabVictim), combatAgent.transform.position);

                    if (hitSucesss)
                    {
                        successfulHits++;
                    }
                }

                if (successfulHits >= grabAttackClip.maxHitLimit) { break; }
                yield return new WaitForSeconds(grabAttackClip.GetTimeBetweenHits(Animator.speed));
            }
        }

        public bool AreActionClipRequirementsMet(ActionClip actionClip)
        {
            if (!actionClip) { Debug.LogError("Calling AreActionClipRequirementsMet with a null action clip! " + name); return false; }
            if (ShouldApplyStaminaCost(actionClip))
            {
                float staminaCost = GetStaminaCostOfClip(actionClip);
                if (staminaCost > combatAgent.GetStamina()) { return false; }
            }

            if (ShouldApplyRageCost(actionClip))
            {
                float rageCost = GetRageCostOfClip(actionClip);
                if (rageCost > combatAgent.GetRage()) { return false; }
            }

            return true;
        }

        private static readonly List<ActionClip.ClipType> staminaCostActionClipTypes = new List<ActionClip.ClipType>()
        {
            ActionClip.ClipType.Ability,
            ActionClip.ClipType.Dodge,
            ActionClip.ClipType.FlashAttack,
            ActionClip.ClipType.HeavyAttack,
            ActionClip.ClipType.Lunge
        };

        private bool ShouldApplyStaminaCost(ActionClip actionClip) { return staminaCostActionClipTypes.Contains(actionClip.GetClipType()); }

        private float GetStaminaCostOfClip(ActionClip actionClip)
        {
            switch (actionClip.GetClipType())
            {
                case ActionClip.ClipType.Dodge:
                    return combatAgent.IsRaging & actionClip.isAffectedByRage ? combatAgent.WeaponHandler.GetWeapon().dodgeStaminaCost * CombatAgent.ragingStaminaCostMultiplier : combatAgent.WeaponHandler.GetWeapon().dodgeStaminaCost;
                case ActionClip.ClipType.LightAttack:
                case ActionClip.ClipType.HeavyAttack:
                case ActionClip.ClipType.Ability:
                case ActionClip.ClipType.FlashAttack:
                case ActionClip.ClipType.Lunge:
                    return combatAgent.IsRaging & actionClip.isAffectedByRage ? actionClip.agentStaminaCost * CombatAgent.ragingStaminaCostMultiplier : actionClip.agentStaminaCost;
                case ActionClip.ClipType.HitReaction:
                case ActionClip.ClipType.Flinch:
                case ActionClip.ClipType.GrabAttack:
                case ActionClip.ClipType.Reload:
                    return -1;
                default:
                    Debug.LogError("Unsure how to calculate stamina cost of clip type " + actionClip.GetClipType());
                    break;
            }
            return -1;
        }

        private static readonly List<ActionClip.ClipType> rageCostActionClipTypes = new List<ActionClip.ClipType>()
        {
            ActionClip.ClipType.Ability,
            ActionClip.ClipType.FlashAttack,
            ActionClip.ClipType.Lunge
        };

        private bool ShouldApplyRageCost(ActionClip actionClip) { return rageCostActionClipTypes.Contains(actionClip.GetClipType()); }

        private float GetRageCostOfClip(ActionClip actionClip)
        {
            switch (actionClip.GetClipType())
            {
                case ActionClip.ClipType.FlashAttack:
                case ActionClip.ClipType.Lunge:
                case ActionClip.ClipType.Ability:
                    return actionClip.agentRageCost;
                case ActionClip.ClipType.LightAttack:
                case ActionClip.ClipType.HeavyAttack:
                case ActionClip.ClipType.GrabAttack:
                case ActionClip.ClipType.Dodge:
                case ActionClip.ClipType.HitReaction:
                case ActionClip.ClipType.Flinch:
                case ActionClip.ClipType.Reload:
                    return -1;
                default:
                    Debug.LogError("Unsure how to calculate stamina cost of clip type " + actionClip.GetClipType());
                    break;
            }
            return -1;
        }

        private Coroutine waitForLungeThenPlayAttackCorountine;
        private IEnumerator WaitForLungeThenPlayAttack(ActionClip attack)
        {
            if (!attack.IsAttack()) { Debug.LogError("Action Clip " + attack + " is not an attack clip!"); yield break; }
            yield return new WaitUntil(() => IsLunging());
            yield return new WaitUntil(() => !IsLunging());
            PlayAction(attack, true);
        }

        private Coroutine playAdditionalClipsCoroutine;
        private IEnumerator PlayAdditionalClips(ActionClip actionClip)
        {
            for (int i = 0; i < actionClip.followUpActionClipsToPlay.Length; i++)
            {
                if (i == 0)
                {
                    yield return new WaitUntil(() => IsActionClipPlayingInCurrentState(actionClip));
                    yield return new WaitUntil(() => animatorReference.CurrentActionsAnimatorStateInfo.normalizedTime >= actionClip.followUpActionClipsToPlay[i].normalizedTimeToPlayClip);
                    yield return new WaitForFixedUpdate();
                }
                else
                {
                    yield return new WaitUntil(() => IsActionClipPlayingInCurrentState(actionClip.followUpActionClipsToPlay[i - 1].actionClip));
                    yield return new WaitUntil(() => animatorReference.CurrentActionsAnimatorStateInfo.normalizedTime >= actionClip.followUpActionClipsToPlay[i].normalizedTimeToPlayClip);
                    yield return new WaitForFixedUpdate();
                }
                PlayAction(actionClip.followUpActionClipsToPlay[i].actionClip, true);
            }
        }


        private NetworkVariable<bool> heavyAttackPressed = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public void SetHeavyAttackPressedState(bool isPressed)
        {
            if (!IsOwner) { Debug.LogError("AnimationHandler.HeavyAttackPressed should only be called on the owner instance!"); return; }
            heavyAttackPressed.Value = isPressed;
        }

        public float HeavyAttackChargeTime { get; private set; }
        private Coroutine heavyAttackCoroutine;
        
        private enum HeavyAttackAnimationPhase
        {
            Start,
            Loop,
            Enhance,
            Cancel,
            Attack,
            AttackEnd
        }

        private HeavyAttackAnimationPhase heavyAttackAnimationPhase;

        private IEnumerator PlayHeavyAttack(ActionClip actionClip)
        {
            if (actionClip.GetClipType() != ActionClip.ClipType.HeavyAttack) { Debug.LogError("AnimationHandler.PlayHeavyAttack() should only be called for heavy attack action clips!"); yield break; }

            Animator.ResetTrigger("CancelHeavyAttackState");
            Animator.ResetTrigger("ProgressHeavyAttackState");
            Animator.SetBool("EnhanceHeavyAttack", false);
            Animator.SetBool("CancelHeavyAttack", false);
            Animator.SetBool("PlayHeavyAttackEnd", actionClip.chargeAttackHasEndAnimation);

            string animationStateName = GetActionClipAnimationStateName(actionClip).Replace("_Attack", "");
            Animator.CrossFadeInFixedTime(animationStateName + "_Start", actionClip.transitionTime, actionsLayerIndex);
            heavyAttackAnimationPhase = HeavyAttackAnimationPhase.Start;
            ResetRootMotionTime();

            bool heavyAttackWasPressedInThisCoroutine = heavyAttackPressed.Value;

            float chargeTime = 0;
            while (true)
            {
                yield return null;

                heavyAttackWasPressedInThisCoroutine = heavyAttackPressed.Value | heavyAttackWasPressedInThisCoroutine;

                if (animatorReference.CurrentActionsAnimatorStateInfo.IsName(animationStateName + "_Loop") | animatorReference.CurrentActionsAnimatorStateInfo.IsName(animationStateName + "_Enhance"))
                {
                    chargeTime += Time.deltaTime * Animator.speed;
                }

                if (actionClip.canEnhance)
                {
                    if (chargeTime > ActionClip.enhanceChargeTime) // Enhance
                    {
                        Animator.SetBool("EnhanceHeavyAttack", true);
                        heavyAttackAnimationPhase = HeavyAttackAnimationPhase.Enhance;
                        rootMotionTime = Mathf.Infinity;
                    }
                }

                if (IsServer)
                {
                    if (chargeTime > ActionClip.chargePenaltyTime)
                    {
                        combatAgent.ProcessEnvironmentDamageWithHitReaction(-actionClip.chargePenaltyDamage, NetworkObject);
                        HeavyAttackChargeTime = 0;
                        break;
                    }

                    if (!heavyAttackPressed.Value & heavyAttackWasPressedInThisCoroutine)
                    {
                        HeavyAttackChargeTime = chargeTime;
                        EvaluateChargeAttackClientRpc(chargeTime, animationStateName, actionClip.chargeAttackStateLoopCount, actionClip.chargeAttackHasEndAnimation);
                        if (chargeTime > ActionClip.chargeAttackTime) // Attack
                        {
                            Animator.SetTrigger("ProgressHeavyAttackState");
                            Animator.SetBool("CancelHeavyAttack", false);
                            heavyAttackAnimationPhase = HeavyAttackAnimationPhase.Attack;
                            ResetRootMotionTime();

                            yield return new WaitUntil(() => animatorReference.CurrentActionsAnimatorStateInfo.IsName(animationStateName + "_Attack"));

                            while (true)
                            {
                                yield return null;

                                if (animatorReference.CurrentActionsAnimatorStateInfo.IsName(animationStateName + "_Attack"))
                                {
                                    if (animatorReference.CurrentActionsAnimatorStateInfo.normalizedTime >= actionClip.chargeAttackStateLoopCount - ActionClip.chargeAttackStateAnimatorTransitionDuration)
                                    {
                                        Animator.SetTrigger("ProgressHeavyAttackState");
                                        if (actionClip.chargeAttackHasEndAnimation)
                                        {
                                            heavyAttackAnimationPhase = HeavyAttackAnimationPhase.AttackEnd;
                                            ResetRootMotionTime();
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                        else if (chargeTime > ActionClip.cancelChargeTime) // Play Cancel Anim
                        {
                            Animator.SetTrigger("ProgressHeavyAttackState");
                            Animator.SetBool("CancelHeavyAttack", true);
                            heavyAttackAnimationPhase = HeavyAttackAnimationPhase.Cancel;
                            ResetRootMotionTime();
                        }
                        else // Return straight to idle
                        {
                            Animator.SetTrigger("CancelHeavyAttackState");
                        }
                        break;
                    }
                }
            }
        }

        [Rpc(SendTo.NotServer)]
        private void EvaluateChargeAttackClientRpc(float chargeTime, string actionStateName, float chargeAttackStateLoopCount, bool hasEndAnim)
        {
            if (heavyAttackCoroutine != null) { StopCoroutine(heavyAttackCoroutine); }
            if (chargeTime > ActionClip.chargeAttackTime) // Attack
            {
                Animator.SetTrigger("ProgressHeavyAttackState");
                Animator.SetBool("CancelHeavyAttack", false);
                heavyAttackAnimationPhase = HeavyAttackAnimationPhase.Attack;
                ResetRootMotionTime();
                StartCoroutine(PlayChargeAttackOnClient(actionStateName, chargeAttackStateLoopCount, hasEndAnim));
            }
            else if (chargeTime > ActionClip.cancelChargeTime) // Play Cancel Anim
            {
                Animator.SetTrigger("ProgressHeavyAttackState");
                Animator.SetBool("CancelHeavyAttack", true);
                heavyAttackAnimationPhase = HeavyAttackAnimationPhase.Cancel;
                ResetRootMotionTime();
            }
            else // Return straight to idle
            {
                Animator.SetTrigger("CancelHeavyAttackState");
            }
        }

        private IEnumerator PlayChargeAttackOnClient(string actionStateName, float chargeAttackStateLoopCount, bool hasEndAnim)
        {
            yield return new WaitUntil(() => animatorReference.CurrentActionsAnimatorStateInfo.IsName(actionStateName + "_Attack"));

            while (true)
            {
                yield return null;

                if (animatorReference.CurrentActionsAnimatorStateInfo.IsName(actionStateName + "_Attack"))
                {
                    if (animatorReference.CurrentActionsAnimatorStateInfo.normalizedTime >= chargeAttackStateLoopCount - ActionClip.chargeAttackStateAnimatorTransitionDuration)
                    {
                        Animator.SetTrigger("ProgressHeavyAttackState");
                        if (hasEndAnim)
                        {
                            heavyAttackAnimationPhase = HeavyAttackAnimationPhase.AttackEnd;
                            ResetRootMotionTime();
                        }
                        break;
                    }
                }
            }
        }

        private const float animationLayerWeightSpeed = 6;
        private void UpdateAnimationLayerWeights(ActionClip.AvatarLayer avatarLayer)
        {
            switch (avatarLayer)
            {
                case ActionClip.AvatarLayer.FullBody:
                    Animator.SetLayerWeight(actionsLayerIndex, Mathf.Lerp(Animator.GetLayerWeight(actionsLayerIndex), 1, Time.deltaTime * animationLayerWeightSpeed));
                    Animator.SetLayerWeight(Animator.GetLayerIndex("Aiming Actions"),
                        Mathf.Lerp(Animator.GetLayerWeight(Animator.GetLayerIndex("Aiming Actions")), 0, Time.deltaTime * animationLayerWeightSpeed));
                    break;
                case ActionClip.AvatarLayer.Aiming:
                    Animator.SetLayerWeight(actionsLayerIndex, Mathf.Lerp(Animator.GetLayerWeight(actionsLayerIndex), 0, Time.deltaTime * animationLayerWeightSpeed));
                    Animator.SetLayerWeight(Animator.GetLayerIndex("Aiming Actions"),
                        Mathf.Lerp(Animator.GetLayerWeight(Animator.GetLayerIndex("Aiming Actions")), 1, Time.deltaTime * animationLayerWeightSpeed));
                    break;
                default:
                    Debug.LogError(avatarLayer + " has not been implemented yet!");
                    break;
            }
        }

        // Remote Procedure Call method for playing the action on the client
        [Rpc(SendTo.NotServer)]
        private void PlayActionClientRpc(string actionClipName, string weaponName, float transitionTime)
        {
            if (playActionOnClientCoroutine != null) { StopCoroutine(playActionOnClientCoroutine); }
            playActionOnClientCoroutine = StartCoroutine(PlayActionOnClient(actionClipName, weaponName, transitionTime));
            WaitingForActionClipToPlay = false;
        }

        private Coroutine playActionOnClientCoroutine;
        private IEnumerator PlayActionOnClient(string actionClipName, string weaponName, float transitionTime)
        {
            // Retrieve the ActionClip based on the actionStateName
            if (combatAgent.WeaponHandler.GetWeapon().name.Replace("(Clone)", "") != weaponName.Replace("(Clone)", ""))
            {
                yield return new WaitUntil(() => combatAgent.WeaponHandler.GetWeapon().name.Replace("(Clone)", "") == weaponName.Replace("(Clone)", ""));
            }
            ActionClip actionClip = combatAgent.WeaponHandler.GetWeapon().GetActionClipByName(actionClipName);

            if (actionClip.GetClipType() != ActionClip.ClipType.Flinch)
            {
                if (heavyAttackCoroutine != null)
                {
                    StopCoroutine(heavyAttackCoroutine);
                    Animator.CrossFadeInFixedTime("Empty", 0, actionsLayerIndex);
                }
            }

            string animationStateName = GetActionClipAnimationStateName(actionClip);

            if (actionClip.ailment == ActionClip.Ailment.Grab)
            {
                if (actionClip.GetClipType() == ActionClip.ClipType.HitReaction)
                {
                    yield return new WaitUntil(() => combatAgent.GetGrabAssailant());
                    ActionClip grabAttackClip = combatAgent.GetGrabReactionClip();
                    if (!grabAttackClip) { Debug.LogError("No grab attack clip found!"); }
                    if (!grabAttackClip.grabVictimClip) { Debug.LogError("No Grab Victim Animation Found!"); }
                    combatAgent.WeaponHandler.AnimatorOverrideControllerInstance["GrabReaction"] = grabAttackClip.grabVictimClip;
                    combatAgent.WeaponHandler.GetWeapon().OverrideRootMotionCurvesAtRuntime("GrabReaction",
                        grabAttackClip.grabVictimRootMotionData);
                }
                else
                {
                    combatAgent.WeaponHandler.AnimatorOverrideControllerInstance["GrabAttack"] = actionClip.grabAttackClip;
                    combatAgent.WeaponHandler.GetWeapon().OverrideRootMotionCurvesAtRuntime("GrabAttack",
                        actionClip.grabAttackRootMotionData);
                }
            }

            // Play the action clip based on its type
            switch (actionClip.GetClipType())
            {
                case ActionClip.ClipType.Dodge:
                case ActionClip.ClipType.LightAttack:
                case ActionClip.ClipType.Ability:
                case ActionClip.ClipType.GrabAttack:
                case ActionClip.ClipType.Lunge:
                case ActionClip.ClipType.Reload:
                    Animator.CrossFadeInFixedTime(animationStateName, transitionTime, actionsLayerIndex);
                    break;
                case ActionClip.ClipType.HeavyAttack:
                    heavyAttackCoroutine = StartCoroutine(PlayHeavyAttack(actionClip));
                    break;
                case ActionClip.ClipType.HitReaction:
                    if (actionClip.ailment != ActionClip.Ailment.Death) { Animator.CrossFadeInFixedTime(animationStateName, transitionTime, actionsLayerIndex, 0); }
                    break;
                case ActionClip.ClipType.FlashAttack:
                    Animator.CrossFadeInFixedTime(animationStateName, transitionTime, actionsLayerIndex, 0);
                    break;
                case ActionClip.ClipType.Flinch:
                    Animator.CrossFadeInFixedTime(animationStateName, transitionTime, flinchLayerIndex, 0);
                    break;
                default:
                    Debug.LogError("Unsure how to play animation state for clip type: " + actionClip.GetClipType());
                    break;
            }

            // Set the current action clip for the weapon handler
            combatAgent.WeaponHandler.SetActionClip(actionClip, combatAgent.WeaponHandler.GetWeapon().name);
            UpdateAnimationLayerWeights(actionClip.avatarLayer);

            if (actionClip.GetClipType() != ActionClip.ClipType.Flinch) { SetLastActionClip(actionClip); }
        }

        // Coroutine for setting invincibility status during a dodge
        private void SetInvincibleStatusOnDodge(ActionClip actionClip)
        {
            combatAgent.SetInviniciblity(GetTotalActionClipLengthInSeconds(actionClip) * 0.35f);
        }

        private string GetHeavyAttackStateName(string baseStateName)
        {
            string stateName = baseStateName;
            switch (heavyAttackAnimationPhase)
            {
                case HeavyAttackAnimationPhase.Start:
                    stateName += "_Start";
                    break;
                case HeavyAttackAnimationPhase.Loop:
                    stateName += "_Loop";
                    break;
                case HeavyAttackAnimationPhase.Cancel:
                    stateName += "_Cancel";
                    break;
                case HeavyAttackAnimationPhase.Enhance:
                    stateName += "_Enhance";
                    break;
                case HeavyAttackAnimationPhase.Attack:
                    stateName += "_Attack";
                    break;
                case HeavyAttackAnimationPhase.AttackEnd:
                    stateName += "_AttackEnd";
                    break;
                default:
                    Debug.LogError("Unsure how to handle heavy attack animation phase " + heavyAttackAnimationPhase);
                    break;
            }
            return stateName;
        }

        public float TotalRootMotionTime { get { return totalRootMotionTime; } }
        private float rootMotionTime;
        private float totalRootMotionTime;

        private void ResetRootMotionTime()
        {
            rootMotionTime = 0;
            totalRootMotionTime = 0;
            combatAgent.MovementHandler.OnRootMotionTimeReset();
        }

        public bool ShouldApplyRootMotion()
        {
            if (NetworkObject.IsPlayerObject & (IsOwner | IsServer))
            {
                if (combatAgent.WeaponHandler.CurrentActionClip.ailment == ActionClip.Ailment.Death) { return false; }
                string stateName = GetActionClipAnimationStateNameWithoutLayer(combatAgent.WeaponHandler.CurrentActionClip);
                if (combatAgent.WeaponHandler.CurrentActionClip.GetClipType() == ActionClip.ClipType.HeavyAttack)
                {
                    stateName = GetHeavyAttackStateName(stateName.Replace("_Attack", ""));
                }
                float maxRootMotionTime = combatAgent.WeaponHandler.GetWeapon().GetMaxRootMotionTime(stateName);
                if (heavyAttackAnimationPhase == HeavyAttackAnimationPhase.Attack)
                {
                    maxRootMotionTime *= combatAgent.WeaponHandler.CurrentActionClip.chargeAttackStateLoopCount;
                }
                return totalRootMotionTime <= maxRootMotionTime - (combatAgent.WeaponHandler.CurrentActionClip.transitionTime);
            }
            else
            {
                return animatorReference.ShouldApplyRootMotion();
            }
        }

        private float GetNormalizedRootMotionTime()
        {
            string stateName = GetActionClipAnimationStateNameWithoutLayer(combatAgent.WeaponHandler.CurrentActionClip);

            float transitionTime = combatAgent.WeaponHandler.CurrentActionClip.transitionTime;
            if (combatAgent.WeaponHandler.CurrentActionClip.GetClipType() == ActionClip.ClipType.HeavyAttack)
            {
                stateName = GetHeavyAttackStateName(stateName.Replace("_Attack", ""));
            }

            float maxRootMotionTime = combatAgent.WeaponHandler.GetWeapon().GetMaxRootMotionTime(stateName);
            return StringUtility.NormalizeValue(rootMotionTime, 0, maxRootMotionTime - transitionTime);
        }

        public Vector3 ApplyRootMotion()
        {
            if (NetworkObject.IsPlayerObject)
            {
                if (ShouldApplyRootMotion())
                {
                    string stateName = GetActionClipAnimationStateNameWithoutLayer(combatAgent.WeaponHandler.CurrentActionClip);
                    if (combatAgent.WeaponHandler.CurrentActionClip.GetClipType() == ActionClip.ClipType.HeavyAttack) { stateName = GetHeavyAttackStateName(stateName.Replace("_Attack", "")); }

                    float prevNormalizedTime = GetNormalizedRootMotionTime();
                    Vector3 prev = combatAgent.WeaponHandler.GetWeapon().GetRootMotion(stateName, prevNormalizedTime);

                    float animationSpeed = combatAgent.WeaponHandler.CurrentActionClip.animationSpeed;
                    if (combatAgent.WeaponHandler.CurrentActionClip.IsAttack())
                    {
                        if (prevNormalizedTime >= combatAgent.WeaponHandler.CurrentActionClip.recoveryNormalizedTime)
                        {
                            animationSpeed = combatAgent.WeaponHandler.CurrentActionClip.recoveryAnimationSpeed;
                        }
                    }
                    
                    rootMotionTime += Time.fixedDeltaTime * animationSpeed;
                    totalRootMotionTime += Time.fixedDeltaTime * animationSpeed;

                    bool shouldApplyMultiplierCurves = true;
                    float newNormalizedTime = GetNormalizedRootMotionTime();

                    if (combatAgent.WeaponHandler.CurrentActionClip.GetClipType() == ActionClip.ClipType.HeavyAttack)
                    {
                        shouldApplyMultiplierCurves = heavyAttackAnimationPhase == HeavyAttackAnimationPhase.Attack;
                        if (heavyAttackAnimationPhase == HeavyAttackAnimationPhase.Attack)
                        {
                            if (newNormalizedTime >= 1)
                            {
                                rootMotionTime = 0;
                                prevNormalizedTime = GetNormalizedRootMotionTime();
                                prev = combatAgent.WeaponHandler.GetWeapon().GetRootMotion(stateName, prevNormalizedTime);
                                rootMotionTime += Time.fixedDeltaTime * animationSpeed;
                                newNormalizedTime = GetNormalizedRootMotionTime();
                            }
                        }
                    }

                    Vector3 delta = combatAgent.WeaponHandler.GetWeapon().GetRootMotion(stateName, newNormalizedTime) - prev;
                    delta = animatorReference.ProcessMotionData(delta, newNormalizedTime, shouldApplyMultiplierCurves);

                    // Account for animation transition
                    if (combatAgent.WeaponHandler.CurrentActionClip.GetClipType() == ActionClip.ClipType.HeavyAttack)
                    {
                        if (heavyAttackAnimationPhase == HeavyAttackAnimationPhase.Attack)
                        {
                            float maxRootMotionTime = combatAgent.WeaponHandler.GetWeapon().GetMaxRootMotionTime(stateName);
                            maxRootMotionTime *= combatAgent.WeaponHandler.CurrentActionClip.chargeAttackStateLoopCount;

                            float transitionTime = combatAgent.WeaponHandler.CurrentActionClip.transitionTime;
                            
                            // Don't move if we're in a transition
                            if (totalRootMotionTime < transitionTime)
                            {
                                delta = Vector3.zero;
                            }
                            else if (maxRootMotionTime - totalRootMotionTime < transitionTime & !combatAgent.WeaponHandler.CurrentActionClip.chargeAttackHasEndAnimation)
                            {
                                delta = Vector3.zero;
                            }
                        }
                    }
                    return delta;
                }
                else
                {
                    return Vector3.zero;
                }
            }
            else
            {
                return animatorReference.ApplyRootMotion();
            }
        }

        private void SetLastActionClip(ActionClip actionClip)
        {
            lastClipPlayed = actionClip;
            if (actionClip.ailment != ActionClip.Ailment.Death) { ResetRootMotionTime(); }
        }

        public Animator Animator { get; private set; }
        public LimbReferences LimbReferences { get; private set; }

        public void ApplyCharacterMaterial(CharacterReference.CharacterMaterial characterMaterial)
        {
            if (characterMaterial == null) { return; }
            animatorReference.ApplyCharacterMaterial(characterMaterial);
        }

        public void ApplyWearableEquipment(CharacterReference.EquipmentType equipmentType, CharacterReference.WearableEquipmentOption wearableEquipmentOption, CharacterReference.RaceAndGender raceAndGender)
        {
            if (wearableEquipmentOption == null)
            {
                animatorReference.ClearWearableEquipment(equipmentType);
            }
            else
            {
                animatorReference.ApplyWearableEquipment(wearableEquipmentOption, raceAndGender);
            }
        }

        public Weapon.ArmorType GetArmorType() { return animatorReference.GetArmorType(); }

        public AnimatorReference.WorldSpaceLabelTransformInfo GetWorldSpaceLabelTransformInfo()
        {
            if (!animatorReference) { return AnimatorReference.WorldSpaceLabelTransformInfo.GetDefaultWorldSpaceLabelTransformInfo(); }
            return animatorReference.GetWorldSpaceLabelTransformInfo();
        }

        AnimatorReference animatorReference;
        private IEnumerator ChangeCharacterCoroutine(WebRequestManager.Character character)
        {
            bool shouldCreateNewSkin = true;
            animatorReference = GetComponentInChildren<AnimatorReference>();
            if (animatorReference)
            {
                shouldCreateNewSkin = animatorReference.name.Replace("(Clone)", "") != character.model;

                if (shouldCreateNewSkin)
                {
                    if (animatorReference.TryGetComponent(out PooledObject pooledObject))
                    {
                        ObjectPoolingManager.ReturnObjectToPool(pooledObject);
                    }
                    else
                    {
                        Destroy(animatorReference.gameObject);
                    }
                }
            }

            if (shouldCreateNewSkin)
            {
                var playerModelOption = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterModel(character.raceAndGender);
                if (playerModelOption == null) { Debug.LogError("Could not find character model for race and gender " + character.raceAndGender); yield break; }

                GameObject modelInstance;
                if (playerModelOption.model.TryGetComponent(out PooledObject pooledObject))
                {
                    modelInstance = ObjectPoolingManager.SpawnObject(playerModelOption.model.GetComponent<PooledObject>(), transform).gameObject;
                }
                else
                {
                    modelInstance = Instantiate(playerModelOption.model, transform, false);
                }

                Animator = modelInstance.GetComponent<Animator>();
                actionsLayerIndex = Animator.GetLayerIndex(actionsLayerName);
                flinchLayerIndex = Animator.GetLayerIndex(flinchLayerName);

                LimbReferences = modelInstance.GetComponent<LimbReferences>();
                animatorReference = modelInstance.GetComponent<AnimatorReference>();

                explodableMeshes = GetComponentsInChildren<ExplodableMesh>();

                SetRagdollActive(false);
            }

            CharacterReference characterReference = PlayerDataManager.Singleton.GetCharacterReference();

            // Apply materials
            List<CharacterReference.CharacterMaterial> characterMaterialOptions = characterReference.GetCharacterMaterialOptions(character.raceAndGender);
            ApplyCharacterMaterial(characterMaterialOptions.Find(item => item.material.name == character.bodyColor));
            ApplyCharacterMaterial(characterMaterialOptions.Find(item => item.material.name == character.eyeColor));

            yield return null;

            // Apply equipment
            List<CharacterReference.WearableEquipmentOption> equipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterEquipmentOptions(character.raceAndGender);
            CharacterReference.WearableEquipmentOption beardOption = equipmentOptions.Find(item => item.GetModel(character.raceAndGender, characterReference.EmptyWearableEquipment).name == character.beard);
            ApplyWearableEquipment(CharacterReference.EquipmentType.Beard, beardOption ?? new CharacterReference.WearableEquipmentOption(CharacterReference.EquipmentType.Beard), character.raceAndGender);
            CharacterReference.WearableEquipmentOption browsOption = equipmentOptions.Find(item => item.GetModel(character.raceAndGender, characterReference.EmptyWearableEquipment).name == character.brows);
            ApplyWearableEquipment(CharacterReference.EquipmentType.Brows, browsOption ?? new CharacterReference.WearableEquipmentOption(CharacterReference.EquipmentType.Brows), character.raceAndGender);
            CharacterReference.WearableEquipmentOption hairOption = equipmentOptions.Find(item => item.GetModel(character.raceAndGender, characterReference.EmptyWearableEquipment).name == character.hair);
            ApplyWearableEquipment(CharacterReference.EquipmentType.Hair, hairOption ?? new CharacterReference.WearableEquipmentOption(CharacterReference.EquipmentType.Hair), character.raceAndGender);
        }

        private void OnReturnToPool()
        {
            if (animatorReference)
            {
                foreach (SkinnedMeshRenderer skinnedMeshRenderer in animatorReference.SkinnedMeshRenderers)
                {
                    skinnedMeshRenderer.forceRenderingOff = false;
                }

                if (animatorReference.TryGetComponent(out PooledObject pooledObject))
                {
                    if (pooledObject.IsSpawned)
                    {
                        ObjectPoolingManager.ReturnObjectToPool(pooledObject);
                        Animator = null;
                        LimbReferences = null;
                        animatorReference = null;
                        explodableMeshes = null;
                    }
                }
            }

            WaitingForActionClipToPlay = false;

            foreach (PooledObject sliceInstance in sliceInstances)
            {
                ObjectPoolingManager.ReturnObjectToPool(sliceInstance);
            }
            sliceInstances.Clear();
        }

        private new void OnDestroy()
        {
            base.OnDestroy();
            foreach (PooledObject sliceInstance in sliceInstances)
            {
                ObjectPoolingManager.ReturnObjectToPool(sliceInstance);
            }
            sliceInstances.Clear();
        }

        public void ChangeCharacter(WebRequestManager.Character character)
        {
            if (IsSpawned) { Debug.LogError("Calling change character after object is spawned!"); return; }
            StartCoroutine(ChangeCharacterCoroutine(character));
        }

        public override void OnNetworkSpawn()
        {
            if (combatAgent is Attributes attributes)
            {
                StartCoroutine(ChangeCharacterCoroutine(attributes.CachedPlayerData.character));
            }

            if (animatorReference)
            {
                animatorReference.OnNetworkSpawn();
            }
        }

        private const string actionsLayerName = "Actions";
        private const string flinchLayerName = "Flinch";

        private int actionsLayerIndex;
        private int flinchLayerIndex;

        private CombatAgent combatAgent;

        private ExplodableMesh[] explodableMeshes;

        private void Awake()
        {
            lastClipPlayed = ScriptableObject.CreateInstance<ActionClip>();
            combatAgent = GetComponent<CombatAgent>();

            AnimatorReference animatorReference = GetComponentInChildren<AnimatorReference>();
            if (animatorReference)
            {
                this.animatorReference = animatorReference;
                LimbReferences = animatorReference.GetComponent<LimbReferences>();
                Animator = animatorReference.GetComponent<Animator>();
                explodableMeshes = GetComponentsInChildren<ExplodableMesh>();
                actionsLayerIndex = Animator.GetLayerIndex(actionsLayerName);
                flinchLayerIndex = Animator.GetLayerIndex(flinchLayerName);
            }
            GetComponent<PooledObject>().OnReturnToPool += OnReturnToPool;
        }

        private void OnEnable()
        {
            SetRagdollActive(false);
        }

        private void OnDisable()
        {
            UseGenericAimPoint = false;
            ResetRootMotionTime();
        }

        public Vector3 GetAimPoint() { return aimPoint.Value; }
        private NetworkVariable<Vector3> aimPoint = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public Vector3 GetCameraPivotPoint() { return NetworkObject.IsPlayerObject ? transform.position + cameraPivotLocalPosition : transform.position + transform.up * 0.5f; }
        public Vector3 GetCameraForwardDirection() { return NetworkObject.IsPlayerObject ? cameraForwardDir.Value : transform.forward; }
        private NetworkVariable<Vector3> cameraForwardDir = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private static readonly Vector3 cameraPivotLocalPosition = new Vector3(0.34f, 1.73f, 0);

        private Camera mainCamera;
        private void FindMainCamera()
        {
            if (mainCamera)
            {
                if (mainCamera.gameObject.CompareTag("MainCamera"))
                {
                    return;
                }
            }
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (IsAtRest())
            {
                UpdateAnimationLayerWeights(ActionClip.AvatarLayer.FullBody);
            }
            else
            {
                UpdateAnimationLayerWeights(lastClipPlayed.avatarLayer);
            }
            RefreshAimPoint();
        }

        public void ProcessNextActionClip()
        {
            if (serverActionQueue.TryDequeue(out (string, bool, bool) result)) { PlayActionOnServer(result.Item1, result.Item2, result.Item3); }
        }

        public bool UseGenericAimPoint { get; set; }
        private static readonly Vector3 aimTargetOffset = new Vector3(0, 0, 10);
        private void RefreshAimPoint()
        {
            FindMainCamera();

            if (!IsSpawned) { return; }
            if (!LimbReferences.aimTargetIKSolver) { return; }

            if (IsOwner)
            {
                if (NetworkObject.IsPlayerObject & mainCamera & !UseGenericAimPoint)
                {
                    aimPoint.Value = mainCamera.transform.position + mainCamera.transform.rotation * aimTargetOffset;
                    cameraForwardDir.Value = mainCamera.transform.forward;
                }
                else
                {
                    aimPoint.Value = GetCameraPivotPoint() + transform.rotation * aimTargetOffset;
                }
            }

            LimbReferences.SetMeleeVerticalAimConstraintOffset(combatAgent.WeaponHandler.IsInAnticipation | combatAgent.WeaponHandler.IsAttacking ? (GetCameraPivotPoint().y - aimPoint.Value.y) * 6 : 0);
            LimbReferences.aimTargetIKSolver.transform.position = aimPoint.Value;
        }

        public void SetRagdollActive(bool isActive)
        {
            if (!animatorReference) { return; }
            animatorReference.SetRagdollActive(isActive);
        }

        private Coroutine explosionCoroutine;
        public void Explode(float explosionDelay)
        {
            if (explosionCoroutine != null) { RemoveExplosion(); }
            explosionCoroutine = StartCoroutine(ExplosionDelay(explosionDelay));
        }

        private List<PooledObject> sliceInstances = new List<PooledObject>();
        private IEnumerator ExplosionDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (combatAgent.GetAilment() != ActionClip.Ailment.Death) { yield break; }
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in animatorReference.SkinnedMeshRenderers)
            {
                skinnedMeshRenderer.forceRenderingOff = true;
            }

            foreach (ExplodableMesh explodableMesh in explodableMeshes)
            {
                sliceInstances.AddRange(explodableMesh.Explode());
            }

            yield return new WaitForSeconds(deadRendererDisplayTime);

            foreach (PooledObject sliceInstance in sliceInstances)
            {
                ObjectPoolingManager.ReturnObjectToPool(sliceInstance);
            }
            sliceInstances.Clear();
        }

        public void RemoveExplosion()
        {
            if (explosionCoroutine != null) { StopCoroutine(explosionCoroutine); }
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in animatorReference.SkinnedMeshRenderers)
            {
                skinnedMeshRenderer.forceRenderingOff = false;
            }

            foreach (PooledObject sliceInstance in sliceInstances)
            {
                ObjectPoolingManager.ReturnObjectToPool(sliceInstance);
            }
            sliceInstances.Clear();
        }
    }
}