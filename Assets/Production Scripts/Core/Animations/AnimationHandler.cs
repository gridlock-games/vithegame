using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Core.CombatAgents;
using Vi.ProceduralAnimations;
using Vi.Utility;

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
                AddActionToServerQueue(actionClip.name, isFollowUpClip);
            }
            else if (IsOwner)
            {
                CanPlayActionClipResult canPlayActionClipResult = CanPlayActionClip(actionClip, isFollowUpClip);
                if (!canPlayActionClipResult.canPlay) { return; }
                WaitingForActionClipToPlay = true;
                PlayActionServerRpc(actionClip.name, isFollowUpClip);
            }
            else
            {
                Debug.LogError("You should not be calling AnimationHandler.PlayAction() when we aren't the owner or the server " + actionClip);
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
            animationStateName = (actionClip.GetClipType() == ActionClip.ClipType.Flinch ? flinchLayerName : actionsLayerName) + "." + animationStateName;
            return animationStateName;
        }

        private string GetActionClipAnimationStateNameWithoutLayer(ActionClip actionClip)
        {
            if (!actionClip) { Debug.LogError("Calling GetActionClipAnimationStateNameWithoutLayer with a null action clip! " + name); return ""; }
            string animationStateName = actionClip.name;
            if (actionClip.GetClipType() == ActionClip.ClipType.GrabAttack) { animationStateName = "GrabAttack"; }
            if (actionClip.GetClipType() == ActionClip.ClipType.HeavyAttack) { animationStateName = actionClip.name + "_Attack"; }
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

            float floor = Mathf.FloorToInt(normalizedTime);
            if (!Mathf.Approximately(floor, normalizedTime)) { normalizedTime -= floor; }

            return normalizedTime;
        }

        public bool IsAtRest()
        {
            if (!animatorReference) { return true; }
            return animatorReference.IsAtRest();
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
            return Animator.IsInTransition(Animator.GetLayerIndex("Aiming")) | !Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Aiming")).IsName("Empty");
        }

        public bool IsReloading()
        {
            return Animator.GetBool("Reloading") | Animator.IsInTransition(Animator.GetLayerIndex("Reload")) | Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Reload")).IsName("Reload");
        }

        public bool IsFinishingReload()
        {
            return Animator.IsInTransition(Animator.GetLayerIndex("Reload")) & !Animator.GetNextAnimatorStateInfo(Animator.GetLayerIndex("Reload")).IsName("Reload");
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

        public void OnRevive()
        {
            Animator.Play("Empty", actionsLayerIndex);
            Animator.Play("Empty", flinchLayerIndex);
        }

        public void CancelAllActions(float transitionTime)
        {
            if (!IsServer) { Debug.LogError("AnimationHandler.CancelAllActions() should only be called on the server!"); return; }

            if (playAdditionalClipsCoroutine != null) { StopCoroutine(playAdditionalClipsCoroutine); }
            if (heavyAttackCoroutine != null) { StopCoroutine(heavyAttackCoroutine); }

            if (waitForLungeThenPlayAttackCorountine != null) { StopCoroutine(waitForLungeThenPlayAttackCorountine); }

            if (evaluateGrabAttackHitsCoroutine != null) { StopCoroutine(evaluateGrabAttackHitsCoroutine); }

            Animator.CrossFadeInFixedTime("Empty", transitionTime, actionsLayerIndex);
            Animator.CrossFadeInFixedTime("Empty", transitionTime, flinchLayerIndex);
            combatAgent.SetInviniciblity(0);
            combatAgent.SetUninterruptable(0);
            combatAgent.ResetAilment();
            combatAgent.StatusAgent.RemoveAllStatuses();
            combatAgent.WeaponHandler.GetWeapon().ResetAllAbilityCooldowns();
            
            CancelAllActionsClientRpc(transitionTime);
        }

        [Rpc(SendTo.NotServer)]
        private void CancelAllActionsClientRpc(float transitionTime)
        {
            if (playAdditionalClipsCoroutine != null) { StopCoroutine(playAdditionalClipsCoroutine); }
            if (heavyAttackCoroutine != null) { StopCoroutine(heavyAttackCoroutine); }

            if (waitForLungeThenPlayAttackCorountine != null) { StopCoroutine(waitForLungeThenPlayAttackCorountine); }

            if (evaluateGrabAttackHitsCoroutine != null) { StopCoroutine(evaluateGrabAttackHitsCoroutine); }

            Animator.CrossFadeInFixedTime("Empty", transitionTime, actionsLayerIndex);
            Animator.CrossFadeInFixedTime("Empty", transitionTime, flinchLayerIndex);
            combatAgent.WeaponHandler.GetWeapon().ResetAllAbilityCooldowns();
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

        private struct CanPlayActionClipResult
        {
            public bool canPlay;
            public bool shouldUseDodgeCancelTransitionTime;

            public CanPlayActionClipResult(bool canPlay, bool shouldUseDodgeCancelTransitionTime)
            {
                this.canPlay = canPlay;
                this.shouldUseDodgeCancelTransitionTime = shouldUseDodgeCancelTransitionTime;
            }
        }

        RaycastHit[] allHits = new RaycastHit[10];
        private CanPlayActionClipResult CanPlayActionClip(ActionClip actionClip, bool isFollowUpClip)
        {
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
                if (combatAgent.IsGrabbed() & actionClip.ailment != ActionClip.Ailment.Grab) { return default; }
            }

            if (!isFollowUpClip & IsServer)
            {
                if (actionClip.IsAttack())
                {
                    if (actionClip.canLunge)
                    {
                        if (!IsLunging())
                        {
                            ActionClip lungeClip = Instantiate(combatAgent.WeaponHandler.GetWeapon().GetLungeClip());
                            lungeClip.name = lungeClip.name.Replace("(Clone)", "");
                            lungeClip.isInvincible = actionClip.isInvincible;
                            lungeClip.isUninterruptable = actionClip.isUninterruptable;

                            if (AreActionClipRequirementsMet(lungeClip) & AreActionClipRequirementsMet(actionClip))
                            {
                                // Lunge mechanic
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
                                        if (PlayerDataManager.Singleton.CanHit(combatAgent, networkCollider.CombatAgent) & !networkCollider.CombatAgent.IsInvincible())
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
                        return default;
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

        [Rpc(SendTo.Server)] private void PlayActionServerRpc(string actionClipName, bool isFollowUpClip) { AddActionToServerQueue(actionClipName, isFollowUpClip); }

        [Rpc(SendTo.Owner)] private void ResetWaitingForActionToPlayClientRpc() { WaitingForActionClipToPlay = false; }

        private Queue<(string, bool)> serverActionQueue = new Queue<(string, bool)>();
        private void AddActionToServerQueue(string actionClipName, bool isFollowUpClip)
        {
            serverActionQueue.Enqueue((actionClipName, isFollowUpClip));
        }

        private bool PlayActionOnServer(string actionClipName, bool isFollowUpClip)
        {
            // Retrieve the appropriate ActionClip based on the provided actionStateName
            ActionClip actionClip = combatAgent.WeaponHandler.GetWeapon().GetActionClipByName(actionClipName);

            CanPlayActionClipResult canPlayActionClipResult = CanPlayActionClip(actionClip, isFollowUpClip);
            if (!canPlayActionClipResult.canPlay) { ResetWaitingForActionToPlayClientRpc(); return false; }

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
                SetInvincibleStatusOnDodge(actionClipName);
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
                    combatAgent.WeaponHandler.AnimatorOverrideControllerInstance["GrabReaction"] = combatAgent.GetGrabReactionClip();
                }
                else
                {
                    combatAgent.WeaponHandler.AnimatorOverrideControllerInstance["GrabAttack"] = actionClip.grabAttackClip;
                }
            }

            if (actionClip.GetClipType() == ActionClip.ClipType.GrabAttack) { evaluateGrabAttackHitsCoroutine = StartCoroutine(EvaluateGrabAttackHits(actionClip)); }

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
                    Animator.CrossFadeInFixedTime(animationStateName, transitionTime, actionsLayerIndex);
                    break;
                case ActionClip.ClipType.HeavyAttack:
                    heavyAttackCoroutine = StartCoroutine(PlayHeavyAttack(actionClip));
                    break;
                case ActionClip.ClipType.HitReaction:
                    Animator.CrossFadeInFixedTime(animationStateName, transitionTime, actionsLayerIndex, 0);
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

            combatAgent.MovementHandler.OnServerActionClipPlayed();

            // Invoke the PlayActionClientRpc method on the client side
            PlayActionClientRpc(actionClipName, combatAgent.WeaponHandler.GetWeapon().name.Replace("(Clone)", ""), transitionTime);
            // Update the lastClipType to the current action clip type
            if (actionClip.GetClipType() != ActionClip.ClipType.Flinch) { SetLastActionClip(actionClip); }
            return true;
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
                    RuntimeWeapon runtimeWeapon = combatAgent.WeaponHandler.GetWeaponInstances()[grabAttackClip.effectedWeaponBones[weaponBoneIndex]];

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
                    return combatAgent.IsRaging() & actionClip.isAffectedByRage ? combatAgent.WeaponHandler.GetWeapon().dodgeStaminaCost * CombatAgent.ragingStaminaCostMultiplier : combatAgent.WeaponHandler.GetWeapon().dodgeStaminaCost;
                case ActionClip.ClipType.LightAttack:
                case ActionClip.ClipType.HeavyAttack:
                case ActionClip.ClipType.Ability:
                case ActionClip.ClipType.FlashAttack:
                case ActionClip.ClipType.Lunge:
                    return combatAgent.IsRaging() & actionClip.isAffectedByRage ? actionClip.agentStaminaCost * CombatAgent.ragingStaminaCostMultiplier : actionClip.agentStaminaCost;
                case ActionClip.ClipType.HitReaction:
                case ActionClip.ClipType.Flinch:
                case ActionClip.ClipType.GrabAttack:
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
                        EvaluateChargeAttackClientRpc(chargeTime, animationStateName, actionClip.chargeAttackStateLoopCount);
                        if (chargeTime > ActionClip.chargeAttackTime) // Attack
                        {
                            Animator.SetTrigger("ProgressHeavyAttackState");
                            Animator.SetBool("CancelHeavyAttack", false);

                            yield return new WaitUntil(() => animatorReference.CurrentActionsAnimatorStateInfo.IsName(animationStateName + "_Attack"));

                            while (true)
                            {
                                yield return null;

                                if (animatorReference.CurrentActionsAnimatorStateInfo.IsName(animationStateName + "_Attack"))
                                {
                                    if (animatorReference.CurrentActionsAnimatorStateInfo.normalizedTime >= actionClip.chargeAttackStateLoopCount - ActionClip.chargeAttackStateAnimatorTransitionDuration)
                                    {
                                        Animator.SetTrigger("ProgressHeavyAttackState");
                                        break;
                                    }
                                }
                            }
                        }
                        else if (chargeTime > ActionClip.cancelChargeTime) // Play Cancel Anim
                        {
                            Animator.SetTrigger("ProgressHeavyAttackState");
                            Animator.SetBool("CancelHeavyAttack", true);
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
        private void EvaluateChargeAttackClientRpc(float chargeTime, string actionStateName, float chargeAttackStateLoopCount)
        {
            if (heavyAttackCoroutine != null) { StopCoroutine(heavyAttackCoroutine); }
            if (chargeTime > ActionClip.chargeAttackTime) // Attack
            {
                Animator.SetTrigger("ProgressHeavyAttackState");
                Animator.SetBool("CancelHeavyAttack", false);

                StartCoroutine(PlayChargeAttackOnClient(actionStateName, chargeAttackStateLoopCount));
            }
            else if (chargeTime > ActionClip.cancelChargeTime) // Play Cancel Anim
            {
                Animator.SetTrigger("ProgressHeavyAttackState");
                Animator.SetBool("CancelHeavyAttack", true);
            }
            else // Return straight to idle
            {
                Animator.SetTrigger("CancelHeavyAttackState");
            }
        }

        private IEnumerator PlayChargeAttackOnClient(string actionStateName, float chargeAttackStateLoopCount)
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
                        break;
                    }
                }
            }
        }

        private void UpdateAnimationLayerWeights(ActionClip.AvatarLayer avatarLayer)
        {
            switch (avatarLayer)
            {
                case ActionClip.AvatarLayer.FullBody:
                    Animator.SetLayerWeight(actionsLayerIndex, 1);
                    Animator.SetLayerWeight(Animator.GetLayerIndex("Aiming Actions"), 0);
                    break;
                case ActionClip.AvatarLayer.Aiming:
                    Animator.SetLayerWeight(actionsLayerIndex, 0);
                    Animator.SetLayerWeight(Animator.GetLayerIndex("Aiming Actions"), 1);
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
            StartCoroutine(PlayActionOnClient(actionClipName, weaponName, transitionTime));
            WaitingForActionClipToPlay = false;
        }

        private IEnumerator PlayActionOnClient(string actionClipName, string weaponName, float transitionTime)
        {
            // Retrieve the ActionClip based on the actionStateName
            ActionClip actionClip = combatAgent.WeaponHandler.GetWeapon().GetActionClipByName(actionClipName);
            if (actionClip.IsAttack())
            {
                if (combatAgent.WeaponHandler.GetWeapon().name != weaponName)
                {
                    yield return new WaitUntil(() => combatAgent.WeaponHandler.GetWeapon().name.Replace("(Clone)", "") == weaponName.Replace("(Clone)", ""));
                }
            }

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
                    combatAgent.WeaponHandler.AnimatorOverrideControllerInstance["GrabReaction"] = combatAgent.GetGrabReactionClip();
                }
                else
                {
                    combatAgent.WeaponHandler.AnimatorOverrideControllerInstance["GrabAttack"] = actionClip.grabAttackClip;
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
                    Animator.CrossFadeInFixedTime(animationStateName, transitionTime, actionsLayerIndex);
                    break;
                case ActionClip.ClipType.HeavyAttack:
                    heavyAttackCoroutine = StartCoroutine(PlayHeavyAttack(actionClip));
                    break;
                case ActionClip.ClipType.HitReaction:
                    Animator.CrossFadeInFixedTime(animationStateName, transitionTime, actionsLayerIndex, 0);
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

            if (lastClipPlayed.GetClipType() != ActionClip.ClipType.Flinch) { SetLastActionClip(actionClip); }
        }

        // Coroutine for setting invincibility status during a dodge
        private void SetInvincibleStatusOnDodge(string actionStateName)
        {
            combatAgent.SetInviniciblity(combatAgent.WeaponHandler.AnimatorOverrideControllerInstance[actionStateName].length * 0.35f);
        }

        public bool ShouldApplyRootMotion() { return animatorReference.ShouldApplyRootMotion(); }
        public Vector3 ApplyRootMotion() { return animatorReference.ApplyRootMotion(); }

        private void SetLastActionClip(ActionClip actionClip)
        {
            lastClipPlayed = actionClip;
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

        AnimatorReference animatorReference;
        private IEnumerator ChangeCharacterCoroutine(WebRequestManager.Character character)
        {
            KeyValuePair<int, int> kvp = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptionIndices(character.model.ToString());
            int characterIndex = kvp.Key;
            int skinIndex = kvp.Value;

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
                if (characterIndex == -1) { Debug.LogWarning("Character Index is -1!"); yield break; }
                CharacterReference.PlayerModelOption modelOption = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions()[characterIndex];
                GameObject modelInstance = Instantiate(modelOption.skinOptions[skinIndex], transform, false);

                Animator = modelInstance.GetComponent<Animator>();
                actionsLayerIndex = Animator.GetLayerIndex(actionsLayerName);
                flinchLayerIndex = Animator.GetLayerIndex(flinchLayerName);

                LimbReferences = modelInstance.GetComponent<LimbReferences>();
                animatorReference = modelInstance.GetComponent<AnimatorReference>();

                SetRagdollActive(false);
            }

            yield return null;
            CharacterReference characterReference = PlayerDataManager.Singleton.GetCharacterReference();

            // Apply materials and equipment
            CharacterReference.RaceAndGender raceAndGender = characterReference.GetPlayerModelOptions()[characterReference.GetPlayerModelOptionIndices(character.model.ToString()).Key].raceAndGender;
            List<CharacterReference.CharacterMaterial> characterMaterialOptions = characterReference.GetCharacterMaterialOptions(raceAndGender);
            ApplyCharacterMaterial(characterMaterialOptions.Find(item => item.material.name == character.bodyColor));
            ApplyCharacterMaterial(characterMaterialOptions.Find(item => item.material.name == character.eyeColor));

            List<CharacterReference.WearableEquipmentOption> equipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterEquipmentOptions(raceAndGender);
            CharacterReference.WearableEquipmentOption beardOption = equipmentOptions.Find(item => item.GetModel(raceAndGender, characterReference.GetEmptyWearableEquipment()).name == character.beard);
            ApplyWearableEquipment(CharacterReference.EquipmentType.Beard, beardOption ?? new CharacterReference.WearableEquipmentOption(CharacterReference.EquipmentType.Beard), raceAndGender);
            CharacterReference.WearableEquipmentOption browsOption = equipmentOptions.Find(item => item.GetModel(raceAndGender, characterReference.GetEmptyWearableEquipment()).name == character.brows);
            ApplyWearableEquipment(CharacterReference.EquipmentType.Brows, browsOption ?? new CharacterReference.WearableEquipmentOption(CharacterReference.EquipmentType.Brows), raceAndGender);
            CharacterReference.WearableEquipmentOption hairOption = equipmentOptions.Find(item => item.GetModel(raceAndGender, characterReference.GetEmptyWearableEquipment()).name == character.hair);
            ApplyWearableEquipment(CharacterReference.EquipmentType.Hair, hairOption ?? new CharacterReference.WearableEquipmentOption(CharacterReference.EquipmentType.Hair), raceAndGender);
        }

        private void OnDisable()
        {
            if (Animator)
            {
                if (Animator.transform != transform)
                {
                    if (Animator.TryGetComponent(out PooledObject pooledObject))
                    {
                        ObjectPoolingManager.ReturnObjectToPool(pooledObject);
                        Animator = null;
                        LimbReferences = null;
                        animatorReference = null;
                    }
                }
            }
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
        }

        private const string actionsLayerName = "Actions";
        private const string flinchLayerName = "Flinch";

        private int actionsLayerIndex;
        private int flinchLayerIndex;

        CombatAgent combatAgent;
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
                actionsLayerIndex = Animator.GetLayerIndex(actionsLayerName);
                flinchLayerIndex = Animator.GetLayerIndex(flinchLayerName);
            }
        }

        private void OnEnable()
        {
            SetRagdollActive(false);
        }

        public Vector3 GetAimPoint() { return aimPoint.Value; }
        private NetworkVariable<Vector3> aimPoint = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public Vector3 GetCameraPivotPoint() { return NetworkObject.IsPlayerObject ? transform.position + cameraPivotLocalPosition : transform.position + transform.up * 0.5f; }
        public Vector3 GetCameraForwardDirection() { return NetworkObject.IsPlayerObject ? cameraForwardDir.Value : transform.forward; }
        private NetworkVariable<Vector3> cameraForwardDir = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private readonly Vector3 cameraPivotLocalPosition = new Vector3(0.34f, 1.73f, 0);

        private Camera mainCamera;
        private void FindMainCamera()
        {
            if (mainCamera) { return; }
            mainCamera = Camera.main;
        }

        private void Update()
        {
            RefreshAimPoint();
            if (serverActionQueue.TryDequeue(out (string, bool) result)) { PlayActionOnServer(result.Item1, result.Item2); }
        }

        private void RefreshAimPoint()
        {
            FindMainCamera();

            if (!IsSpawned) { return; }
            if (!LimbReferences.aimTargetIKSolver) { return; }

            if (IsOwner)
            {
                if (NetworkObject.IsPlayerObject)
                {
                    aimPoint.Value = mainCamera.transform.position + mainCamera.transform.rotation * LimbReferences.aimTargetIKSolver.offset;
                    cameraForwardDir.Value = mainCamera.transform.forward;
                }
                else
                {
                    aimPoint.Value = GetCameraPivotPoint() + transform.rotation * LimbReferences.aimTargetIKSolver.offset;
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
    }
}