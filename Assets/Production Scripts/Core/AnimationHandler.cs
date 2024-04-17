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

        public bool IsAtRest()
        {
            return animatorReference.IsAtRest();
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

            if (actionClip.GetClipType() != ActionClip.ClipType.HitReaction)
            {
                if (Animator.GetNextAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsName(actionStateName)) { return; }
            }

            // If we are not at rest and the last clip was a dodge, don't play this clip
            if (!Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsName("Empty") | Animator.IsInTransition(Animator.GetLayerIndex("Actions")))
            {
                if (!(actionClip.GetClipType() == ActionClip.ClipType.Dodge & Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsTag("CanDodge")))
                {
                    if ((actionClip.GetClipType() != ActionClip.ClipType.HitReaction & lastClipPlayed.GetClipType() == ActionClip.ClipType.Dodge) | (actionClip.GetClipType() != ActionClip.ClipType.HitReaction & lastClipPlayed.GetClipType() == ActionClip.ClipType.HitReaction)) { return; }
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
                else if (actionClip.GetClipType() == ActionClip.ClipType.Ability)
                {
                    if (Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsName(actionClip.name)) { return; }
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
                else if (actionClip.GetClipType() == ActionClip.ClipType.LightAttack | actionClip.GetClipType() == ActionClip.ClipType.HeavyAttack)
                {
                    if (Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsName(actionClip.name)) { return; }

                    // If the last clip was an ability that can't be cancelled, don't play this clip
                    if (!(actionClip.GetClipType() == ActionClip.ClipType.LightAttack & lastClipPlayed.canBeCancelledByLightAttacks)
                        & !(actionClip.GetClipType() == ActionClip.ClipType.HeavyAttack & lastClipPlayed.canBeCancelledByHeavyAttacks)
                        & !(actionClip.GetClipType() == ActionClip.ClipType.Ability & lastClipPlayed.canBeCancelledByAbilities))
                    {
                        if (lastClipPlayed.GetClipType() == ActionClip.ClipType.Ability) { return; }
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
                if (Animator.GetNextAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsName(actionStateName)) { return; }
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
            Debug.Log(actionClip.name);
            // Play the action clip based on its type
            if (actionClip.ailment != ActionClip.Ailment.Death)
            {
                if (actionClip.GetClipType() == ActionClip.ClipType.HitReaction | actionClip.GetClipType() == ActionClip.ClipType.FlashAttack)
                    Animator.CrossFade(actionStateName, actionClip.transitionTime, Animator.GetLayerIndex("Actions"), 0);
                else
                    Animator.CrossFade(actionStateName, actionClip.transitionTime, Animator.GetLayerIndex("Actions"));

                if (playAdditionalStatesCoroutine != null) { StopCoroutine(playAdditionalStatesCoroutine); }
                if (actionClip.GetClipType() == ActionClip.ClipType.HeavyAttack) { playAdditionalStatesCoroutine = StartCoroutine(PlayAdditionalStates(actionClip)); }
            }

            // Invoke the PlayActionClientRpc method on the client side
            PlayActionClientRpc(actionStateName, weaponHandler.GetWeapon().name);
            // Update the lastClipType to the current action clip type
            lastClipPlayed = actionClip;
        }

        private bool heavyAttackReleased;
        public void HeavyAttackReleased()
        {
            heavyAttackReleased = true;
        }

        public void HeavyAttackPressed()
        {
            heavyAttackReleased = false;
        }

        private Coroutine playAdditionalStatesCoroutine;
        private IEnumerator PlayAdditionalStates(ActionClip actionClip)
        {
            Animator.SetBool("CanExitAction", false);
            int nextStateNameIndex = 0;
            while (nextStateNameIndex < actionClip.additionalAnimationStates.Length)
            {
                string currentStateName = nextStateNameIndex == 0 ? actionClip.name : actionClip.additionalAnimationStates[nextStateNameIndex-1];

                // Play the 2nd animation (looping animation) right after the 1st completes
                if (nextStateNameIndex == 0)
                {
                    if (Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsName(currentStateName))
                    {
                        // If we are at the end of the animation, play the next animation
                        if (1 - Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions")).normalizedTime <= actionClip.transitionTime)
                        {
                            Animator.CrossFade(actionClip.additionalAnimationStates[nextStateNameIndex], actionClip.transitionTime, Animator.GetLayerIndex("Actions"));
                            nextStateNameIndex++;
                        }
                    }
                }
                else if (nextStateNameIndex == 1) // Play the end animation (attack animation) after the player releases the attack button
                {
                    if (Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsName(currentStateName))
                    {
                        if (heavyAttackReleased)
                        {
                            Debug.Log("heavy attack released");
                            Animator.SetBool("CanExitAction", true);
                            heavyAttackReleased = false;
                            Animator.CrossFade(actionClip.additionalAnimationStates[nextStateNameIndex], actionClip.transitionTime, Animator.GetLayerIndex("Actions"));
                            nextStateNameIndex++;
                        }
                    }
                }
                else
                {
                    Debug.LogError("Unsure how to handle state name index of " + nextStateNameIndex);
                    break;
                }

                yield return null;
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

            // Play the action clip on the client side based on its type
            if (actionClip.ailment != ActionClip.Ailment.Death)
            {
                if (actionClip.GetClipType() == ActionClip.ClipType.HitReaction | actionClip.GetClipType() == ActionClip.ClipType.FlashAttack)
                    Animator.CrossFade(actionStateName, actionClip.transitionTime, Animator.GetLayerIndex("Actions"), 0);
                else
                    Animator.CrossFade(actionStateName, actionClip.transitionTime, Animator.GetLayerIndex("Actions"));
            }

            // Set the current action clip for the weapon handler
            weaponHandler.SetActionClip(actionClip, weaponHandler.GetWeapon().name);
            UpdateAnimationLayerWeights(actionClip.avatarLayer);

            // If the action clip is a dodge, start the SetInvincibleStatusOnDodge coroutine
            if (actionClip.GetClipType() == ActionClip.ClipType.Dodge) { StartCoroutine(SetInvincibleStatusOnDodge(actionStateName)); }
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
            if (characterMaterial == null) { Debug.LogWarning("Character Material is null"); return; }
            animatorReference.ApplyCharacterMaterial(characterMaterial);
        }

        public void ApplyWearableEquipment(CharacterReference.EquipmentType equipmentType, CharacterReference.WearableEquipmentOption wearableEquipmentOption, CharacterReference.RaceAndGender raceAndGender)
        {
            if (wearableEquipmentOption == null)
            {
                Debug.LogWarning(equipmentType + " Equipment option is null");
                animatorReference.ClearWearableEquipment(equipmentType);
                return;
            }
            animatorReference.ApplyWearableEquipment(wearableEquipmentOption, raceAndGender);
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