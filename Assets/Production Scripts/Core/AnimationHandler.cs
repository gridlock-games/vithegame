using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    public class AnimationHandler : NetworkBehaviour
    {
        public bool WaitingForActionToPlay { get; private set; }

        // This method plays an action based on the provided ActionClip parameter
        public void PlayAction(ActionClip actionClip)
        {
            if (IsServer)
            {
                PlayActionOnServer(actionClip.name);
            }
            else
            {
                WaitingForActionToPlay = true;
                PlayActionServerRpc(actionClip.name);
            }
        }

        public bool IsActionClipPlaying(ActionClip actionClip)
        {
            string stateName = actionClip.GetClipType() == ActionClip.ClipType.HeavyAttack ? actionClip.name + "_Attack" : actionClip.name;
            return Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsName(stateName) | Animator.GetNextAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsName(stateName);
        }

        public float GetActionClipNormalizedTime(ActionClip actionClip)
        {
            string stateName = actionClip.GetClipType() == ActionClip.ClipType.HeavyAttack ? actionClip.name + "_Attack" : actionClip.name;
            float normalizedTime = 0;
            if (Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsName(stateName))
            {
                normalizedTime = Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions")).normalizedTime;
            }
            else if (Animator.GetNextAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsName(stateName))
            {
                normalizedTime = Animator.GetNextAnimatorStateInfo(Animator.GetLayerIndex("Actions")).normalizedTime;
            }

            float floor = Mathf.FloorToInt(normalizedTime);
            if (!Mathf.Approximately(floor, normalizedTime)) { normalizedTime -= floor; }

            return normalizedTime;
        }

        public bool IsAtRest()
        {
            return animatorReference.IsAtRest();
        }

        public bool CanAim()
        {
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

        public void CancelAllActions()
        {
            Animator.CrossFade("Empty", 0, Animator.GetLayerIndex("Actions"));
            attributes.SetInviniciblity(0);
            attributes.SetUninterruptable(0);
            attributes.ResetAilment();
            weaponHandler.GetWeapon().ResetAllAbilityCooldowns();
        }

        // Stores the type of the last action clip played
        private ActionClip lastClipPlayed;
        private const float canAttackFromDodgeNormalizedTimeThreshold = 0.55f;

        // This method plays the action on the server
        private void PlayActionOnServer(string actionStateName)
        {
            WaitingForActionToPlay = false;
            // Retrieve the appropriate ActionClip based on the provided actionStateName
            ActionClip actionClip = weaponHandler.GetWeapon().GetActionClipByName(actionStateName);

            if (!movementHandler.CanMove()) { return; }
            if (attributes.IsRooted() & actionClip.GetClipType() != ActionClip.ClipType.HitReaction) { return; }
            if (actionClip.mustBeAiming & !weaponHandler.IsAiming()) { return; }
            if (attributes.IsSilenced() & actionClip.GetClipType() == ActionClip.ClipType.Ability) { return; }

            AnimatorStateInfo currentStateInfo = Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions"));
            AnimatorStateInfo nextStateInfo = Animator.GetNextAnimatorStateInfo(Animator.GetLayerIndex("Actions"));
            if (actionClip.GetClipType() != ActionClip.ClipType.HitReaction)
            {
                if (nextStateInfo.IsName(actionStateName)) { return; }
            }

            bool shouldUseDodgeCancelTransitionTime = false;
            // If we are not at rest and the last clip was a dodge, don't play this clip
            if (!currentStateInfo.IsName("Empty") | Animator.IsInTransition(Animator.GetLayerIndex("Actions")))
            {
                if (!(actionClip.GetClipType() == ActionClip.ClipType.Dodge & currentStateInfo.IsTag("CanDodge")))
                {
                    if (actionClip.IsAttack() & IsDodging() & currentStateInfo.IsName(lastClipPlayed.name))
                    {
                        if (currentStateInfo.normalizedTime < canAttackFromDodgeNormalizedTimeThreshold) { return; }
                        shouldUseDodgeCancelTransitionTime = true;
                    }
                    else
                    {
                        if ((actionClip.GetClipType() != ActionClip.ClipType.HitReaction & lastClipPlayed.GetClipType() == ActionClip.ClipType.Dodge) | (actionClip.GetClipType() != ActionClip.ClipType.HitReaction & lastClipPlayed.GetClipType() == ActionClip.ClipType.HitReaction)) { return; }
                    }
                }

                // Dodge lock checks
                if (actionClip.GetClipType() == ActionClip.ClipType.Dodge)
                {
                    if (lastClipPlayed.dodgeLock == ActionClip.DodgeLock.EntireAnimation)
                    {
                        return;
                    }
                    else if (lastClipPlayed.dodgeLock == ActionClip.DodgeLock.Recovery)
                    {
                        if (weaponHandler.IsInRecovery) { return; }
                    }
                }
                else if (actionClip.GetClipType() == ActionClip.ClipType.Ability | actionClip.GetClipType() == ActionClip.ClipType.HeavyAttack)
                {
                    if (currentStateInfo.IsName(actionClip.name)) { return; }
                    if (!actionClip.canCancelLightAttacks)
                    {
                        if (lastClipPlayed.GetClipType() == ActionClip.ClipType.LightAttack) { return; }
                    }
                    if (!actionClip.canCancelHeavyAttacks)
                    {
                        if (lastClipPlayed.GetClipType() == ActionClip.ClipType.HeavyAttack) { return; }
                    }
                    if (!actionClip.canCancelAbilities)
                    {
                        if (lastClipPlayed.GetClipType() == ActionClip.ClipType.Ability) { return; }
                    }
                }
                else if (actionClip.GetClipType() == ActionClip.ClipType.LightAttack)
                {
                    if (currentStateInfo.IsName(actionClip.name)) { return; }
                }

                // If the last clip was a clip that can't be cancelled, don't play this clip
                if (actionClip.IsAttack() & !weaponHandler.IsInRecovery & lastClipPlayed.IsAttack())
                {
                    if (!(actionClip.GetClipType() == ActionClip.ClipType.LightAttack & lastClipPlayed.canBeCancelledByLightAttacks)
                    & !(actionClip.GetClipType() == ActionClip.ClipType.HeavyAttack & lastClipPlayed.canBeCancelledByHeavyAttacks)
                    & !(actionClip.GetClipType() == ActionClip.ClipType.Ability & lastClipPlayed.canBeCancelledByAbilities))
                    {
                        return;
                    }
                }
            }

            if (actionClip.ailment == ActionClip.Ailment.Grab)
            {
                float raycastDistance = actionClip.grabDistance;
                bool bHit = false;
                RaycastHit[] allHits = Physics.RaycastAll(transform.position + Vector3.up, transform.forward, raycastDistance, LayerMask.GetMask(new string[] { "NetworkPrediction" }), QueryTriggerInteraction.Ignore);
                Debug.DrawRay(transform.position + Vector3.up, transform.forward * raycastDistance, Color.blue, 2);
                System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));

                foreach (RaycastHit hit in allHits)
                {
                    if (hit.transform == transform) { continue; }
                    if (hit.transform.TryGetComponent(out NetworkCollider networkCollider))
                    {
                        if (networkCollider.Attributes == attributes) { return; }

                        networkCollider.Attributes.TryAddStatus(ActionClip.Status.rooted, 0, actionClip.grabDuration, 0);
                    }
                    bHit = true;
                    break;
                }

                // Make sure that there is a detected target
                if (!bHit) { return; }
            }

            // Checks if the action is not a hit reaction and prevents the animation from getting stuck
            if (actionClip.GetClipType() != ActionClip.ClipType.HitReaction)
            {
                if (nextStateInfo.IsName(actionStateName)) { return; }
            }

            // Check stamina and rage requirements and apply statuses for specific actions
            if (actionClip.GetClipType() == ActionClip.ClipType.Dodge)
            {
                if (weaponHandler.GetWeapon().dodgeStaminaCost > attributes.GetStamina()) { return; }
                attributes.AddStamina(-weaponHandler.GetWeapon().dodgeStaminaCost);
                StartCoroutine(SetInvincibleStatusOnDodge(actionStateName));
            }
            else if (actionClip.GetClipType() == ActionClip.ClipType.HeavyAttack)
            {
                if (actionClip.agentStaminaCost > attributes.GetStamina()) { return; }
                attributes.AddStamina(-actionClip.agentStaminaCost);
            }
            else if (actionClip.GetClipType() == ActionClip.ClipType.Ability)
            {
                if (weaponHandler.GetWeapon().GetAbilityCooldownProgress(actionClip) < 1) { return; }
                if (actionClip.agentStaminaCost > attributes.GetStamina()) { return; }
                if (actionClip.agentDefenseCost > attributes.GetDefense()) { return; }
                if (actionClip.agentRageCost > attributes.GetRage()) { return; }
                attributes.AddStamina(-actionClip.agentStaminaCost);
                attributes.AddDefense(-actionClip.agentDefenseCost);
                attributes.AddRage(-actionClip.agentRageCost);
            }
            else if (actionClip.GetClipType() == ActionClip.ClipType.FlashAttack)
            {
                if (actionClip.agentStaminaCost > attributes.GetStamina()) { return; }
                if (actionClip.agentDefenseCost > attributes.GetDefense()) { return; }
                if (actionClip.agentRageCost > attributes.GetRage()) { return; }
                attributes.AddStamina(-actionClip.agentStaminaCost);
                attributes.AddDefense(-actionClip.agentDefenseCost);
                attributes.AddRage(-actionClip.agentRageCost);
            }

            // Set the current action clip for the weapon handler
            weaponHandler.SetActionClip(actionClip, weaponHandler.GetWeapon().name);
            UpdateAnimationLayerWeights(actionClip.avatarLayer);

            if (playAdditionalStatesCoroutine != null) { StopCoroutine(playAdditionalStatesCoroutine); }

            // Play the action clip based on its type
            if (actionClip.ailment != ActionClip.Ailment.Death)
            {
                if (actionClip.GetClipType() == ActionClip.ClipType.HitReaction | actionClip.GetClipType() == ActionClip.ClipType.FlashAttack)
                    Animator.CrossFade(actionStateName, shouldUseDodgeCancelTransitionTime ? actionClip.dodgeCancelTransitionTime : actionClip.transitionTime, Animator.GetLayerIndex("Actions"), 0);
                else if (actionClip.GetClipType() != ActionClip.ClipType.HeavyAttack)
                    Animator.CrossFade(actionStateName, shouldUseDodgeCancelTransitionTime ? actionClip.dodgeCancelTransitionTime : actionClip.transitionTime, Animator.GetLayerIndex("Actions"));
                else // If this is a heavy attack
                    playAdditionalStatesCoroutine = StartCoroutine(PlayAdditionalStates(actionClip));
            }

            // Invoke the PlayActionClientRpc method on the client side
            PlayActionClientRpc(actionStateName, weaponHandler.GetWeapon().name);
            // Update the lastClipType to the current action clip type
            lastClipPlayed = actionClip;
        }

        private bool heavyAttackReleased;
        [ServerRpc] public void HeavyAttackReleasedServerRpc() { heavyAttackReleased = true; }
        [ServerRpc] public void HeavyAttackPressedServerRpc() { heavyAttackReleased = false; }

        public float HeavyAttackChargeTime { get; private set; }
        private Coroutine playAdditionalStatesCoroutine;
        private IEnumerator PlayAdditionalStates(ActionClip actionClip)
        {
            if (actionClip.GetClipType() != ActionClip.ClipType.HeavyAttack) { Debug.LogError("AnimationHandler.PlayAdditionalStates() should only be called for heavy attack action clips!"); yield break; }

            Animator.ResetTrigger("CancelHeavyAttackState");
            Animator.ResetTrigger("ProgressHeavyAttackState");
            Animator.SetBool("EnhanceHeavyAttack", false);
            Animator.SetBool("CancelHeavyAttack", false);
            Animator.SetBool("PlayHeavyAttackEnd", actionClip.chargeAttackHasEndAnimation);

            Animator.CrossFade(actionClip.name + "_Start", actionClip.transitionTime, Animator.GetLayerIndex("Actions"));

            float chargeTime = 0;
            while (true)
            {
                yield return null;

                if (Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsName(actionClip.name + "_Loop") | Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsName(actionClip.name + "_Enhance"))
                {
                    chargeTime += Time.deltaTime;
                    Debug.Log(chargeTime);
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
                        attributes.ProcessEnvironmentDamageWithHitReaction(-actionClip.chargePenaltyDamage, NetworkObject);
                        HeavyAttackChargeTime = 0;
                        break;
                    }

                    if (heavyAttackReleased)
                    {
                        HeavyAttackChargeTime = chargeTime;
                        EvaluateChargeAttackClientRpc(chargeTime, actionClip.name, actionClip.chargeAttackStateLoopCount);
                        if (chargeTime > ActionClip.chargeAttackTime) // Attack
                        {
                            Animator.SetTrigger("ProgressHeavyAttackState");
                            Animator.SetBool("CancelHeavyAttack", false);

                            yield return new WaitUntil(() => Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsName(actionClip.name + "_Attack"));

                            while (true)
                            {
                                yield return null;

                                if (Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsName(actionClip.name + "_Attack"))
                                {
                                    if (Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions")).normalizedTime >= actionClip.chargeAttackStateLoopCount - ActionClip.chargeAttackStateAnimatorTransitionDuration)
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

        [ClientRpc]
        private void EvaluateChargeAttackClientRpc(float chargeTime, string actionStateName, float chargeAttackStateLoopCount)
        {
            if (IsServer) { return; }

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
            yield return new WaitUntil(() => Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsName(actionStateName + "_Attack"));

            while (true)
            {
                yield return null;

                if (Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsName(actionStateName + "_Attack"))
                {
                    if (Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions")).normalizedTime >= chargeAttackStateLoopCount - ActionClip.chargeAttackStateAnimatorTransitionDuration)
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
                    Animator.SetLayerWeight(Animator.GetLayerIndex("Actions"), 1);
                    Animator.SetLayerWeight(Animator.GetLayerIndex("Aiming Actions"), 0);
                    break;
                case ActionClip.AvatarLayer.Aiming:
                    Animator.SetLayerWeight(Animator.GetLayerIndex("Actions"), 0);
                    Animator.SetLayerWeight(Animator.GetLayerIndex("Aiming Actions"), 1);
                    break;
                default:
                    Debug.LogError(avatarLayer + " has not been implemented yet!");
                    break;
            }
        }

        // Remote Procedure Call method for playing the action on the server
        [ServerRpc]
        private void PlayActionServerRpc(string actionStateName)
        {
            PlayActionOnServer(actionStateName);
            ResetActionClientRpc();
        }

        // Remote Procedure Call method for playing the action on the client
        [ClientRpc]
        private void PlayActionClientRpc(string actionStateName, string weaponName)
        {
            if (IsServer) { return; }
            StartCoroutine(PlayActionOnClient(actionStateName, weaponName));
        }

        private IEnumerator PlayActionOnClient(string actionStateName, string weaponName)
        {
            yield return new WaitUntil(() => weaponHandler.GetWeapon().name == weaponName);

            // Retrieve the ActionClip based on the actionStateName
            ActionClip actionClip = weaponHandler.GetWeapon().GetActionClipByName(actionStateName);

            if (playAdditionalStatesCoroutine != null) { StopCoroutine(playAdditionalStatesCoroutine); }

            // Play the action clip on the client side based on its type
            if (actionClip.ailment != ActionClip.Ailment.Death)
            {
                if (actionClip.GetClipType() == ActionClip.ClipType.HitReaction | actionClip.GetClipType() == ActionClip.ClipType.FlashAttack)
                    Animator.CrossFade(actionStateName, actionClip.transitionTime, Animator.GetLayerIndex("Actions"), 0);
                else if (actionClip.GetClipType() != ActionClip.ClipType.HeavyAttack)
                    Animator.CrossFade(actionStateName, actionClip.transitionTime, Animator.GetLayerIndex("Actions"));
                else // If this is a heavy attack
                    playAdditionalStatesCoroutine = StartCoroutine(PlayAdditionalStates(actionClip));
            }

            // Set the current action clip for the weapon handler
            weaponHandler.SetActionClip(actionClip, weaponHandler.GetWeapon().name);
            UpdateAnimationLayerWeights(actionClip.avatarLayer);

            // If the action clip is a dodge, start the SetInvincibleStatusOnDodge coroutine
            if (actionClip.GetClipType() == ActionClip.ClipType.Dodge) { StartCoroutine(SetInvincibleStatusOnDodge(actionStateName)); }

            lastClipPlayed = actionClip;
        }

        [ClientRpc] private void ResetActionClientRpc() { WaitingForActionToPlay = false; }

        // Coroutine for setting invincibility status during a dodge
        private IEnumerator SetInvincibleStatusOnDodge(string actionStateName)
        {
            attributes.SetInviniciblity(5);
            yield return new WaitUntil(() => Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsName(actionStateName));
            AnimatorClipInfo[] dodgeClips = Animator.GetCurrentAnimatorClipInfo(Animator.GetLayerIndex("Actions"));
            if (dodgeClips.Length > 0)
                attributes.SetInviniciblity(dodgeClips[0].clip.length * 0.35f);
            else
                attributes.SetInviniciblity(0);
        }

        public bool ShouldApplyRootMotion() { return animatorReference.ShouldApplyRootMotion(); }
        public Vector3 ApplyLocalRootMotion() { return animatorReference.ApplyLocalRootMotion(); }
        public Vector3 ApplyNetworkRootMotion() { return animatorReference.ApplyNetworkRootMotion(); }

        public Animator Animator { get; private set; }
        public LimbReferences LimbReferences { get; private set; }
        Attributes attributes;
        WeaponHandler weaponHandler;
        AnimatorReference animatorReference;
        MovementHandler movementHandler;

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

                if (shouldCreateNewSkin) { Destroy(animatorReference.gameObject); }
            }

            if (shouldCreateNewSkin)
            {
                CharacterReference.PlayerModelOption modelOption = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions()[characterIndex];
                GameObject modelInstance = Instantiate(modelOption.skinOptions[skinIndex], transform, false);

                Animator = modelInstance.GetComponent<Animator>();
                LimbReferences = modelInstance.GetComponent<LimbReferences>();
                animatorReference = modelInstance.GetComponent<AnimatorReference>();
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

        public void ChangeCharacter(WebRequestManager.Character character)
        {
            if (IsSpawned) { Debug.LogError("Calling change character after object is spawned!"); return; }
            StartCoroutine(ChangeCharacterCoroutine(character));
        }

        public override void OnNetworkSpawn()
        {
            StartCoroutine(ChangeCharacterCoroutine(PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId()).character));
        }

        private void Awake()
        {
            attributes = GetComponent<Attributes>();
            weaponHandler = GetComponent<WeaponHandler>();
            movementHandler = GetComponent<MovementHandler>();
        }

        public Vector3 GetAimPoint() { return aimPoint.Value; }
        private NetworkVariable<Vector3> aimPoint = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private NetworkVariable<float> meleeVerticalAimConstraintOffset = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        [SerializeField] private Transform cameraPivot;

        private void Update()
        {
            if (!IsSpawned) { return; }
            if (!LimbReferences.aimTargetIKSolver) { return; }

            if (IsLocalPlayer)
            {
                aimPoint.Value = Camera.main.transform.position + Camera.main.transform.rotation * LimbReferences.aimTargetIKSolver.offset;
                meleeVerticalAimConstraintOffset.Value = weaponHandler.IsInAnticipation | weaponHandler.IsAttacking ? (cameraPivot.position.y - aimPoint.Value.y) * 6 : 0;
            }

            LimbReferences.SetMeleeVerticalAimConstraintOffset(weaponHandler.IsInAnticipation | weaponHandler.IsAttacking ? meleeVerticalAimConstraintOffset.Value : 0);
            LimbReferences.aimTargetIKSolver.transform.position = aimPoint.Value;
        }
    }
}