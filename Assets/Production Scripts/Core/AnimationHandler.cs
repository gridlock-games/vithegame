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
            if (Animator.IsInTransition(Animator.GetLayerIndex("Actions")))
            {
                return Animator.GetNextAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsName("Empty");
            }
            else
            {
                return Animator.GetCurrentAnimatorStateInfo(Animator.GetLayerIndex("Actions")).IsName("Empty");
            }
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
                if (lastClipPlayed.GetClipType() == ActionClip.ClipType.Dodge | (actionClip.GetClipType() != ActionClip.ClipType.HitReaction & lastClipPlayed.GetClipType() == ActionClip.ClipType.HitReaction)) { return; }

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
                    if (lastClipPlayed.GetClipType() == ActionClip.ClipType.Ability) { return; }
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

                        networkCollider.Attributes.TryAddStatus(ActionClip.Status.rooted, 0, actionClip.ailmentDuration, 0);
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

            // Set the current action clip for the weapon handler
            weaponHandler.SetActionClip(actionClip);
            UpdateAnimationLayerWeights(actionClip.avatarLayer);

            // Play the action clip based on its type
            if (actionClip.ailment != ActionClip.Ailment.Death)
            {
                if (actionClip.GetClipType() == ActionClip.ClipType.HitReaction)
                    Animator.CrossFade(actionStateName, actionClip.transitionTime, Animator.GetLayerIndex("Actions"), 0);
                else
                    Animator.CrossFade(actionStateName, actionClip.transitionTime, Animator.GetLayerIndex("Actions"));
            }

            // Invoke the PlayActionClientRpc method on the client side
            PlayActionClientRpc(actionStateName);
            // Update the lastClipType to the current action clip type
            lastClipPlayed = actionClip;
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
        private void PlayActionClientRpc(string actionStateName)
        {
            if (IsServer) { return; }

            // Retrieve the ActionClip based on the actionStateName
            ActionClip actionClip = weaponHandler.GetWeapon().GetActionClipByName(actionStateName);

            // Play the action clip on the client side based on its type
            if (actionClip.ailment != ActionClip.Ailment.Death)
            {
                if (actionClip.GetClipType() == ActionClip.ClipType.HitReaction)
                    Animator.CrossFade(actionStateName, actionClip.transitionTime, Animator.GetLayerIndex("Actions"), 0);
                else
                    Animator.CrossFade(actionStateName, actionClip.transitionTime, Animator.GetLayerIndex("Actions"));
            }

            // Set the current action clip for the weapon handler
            weaponHandler.SetActionClip(actionClip);
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

        public void ApplyCharacterMaterial(CharacterReference.CharacterMaterial characterMaterial)
        {
            if (characterMaterial == null) { return; }
            animatorReference.ApplyCharacterMaterial(characterMaterial);
        }

        public void ApplyWearableEquipment(CharacterReference.WearableEquipmentOption wearableEquipmentOption)
        {
            if (wearableEquipmentOption == null) { return; }
            animatorReference.ApplyWearableEquipment(wearableEquipmentOption);
        }

        private void ChangeSkin(int characterIndex, int skinIndex)
        {
            animatorReference = GetComponentInChildren<AnimatorReference>();
            if (animatorReference)
            {
                Destroy(animatorReference.gameObject);
            }

            CharacterReference.PlayerModelOption modelOption = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions()[characterIndex];
            GameObject modelInstance = Instantiate(modelOption.skinOptions[skinIndex], transform, false);

            Animator = modelInstance.GetComponent<Animator>();
            LimbReferences = modelInstance.GetComponent<LimbReferences>();
            animatorReference = modelInstance.GetComponent<AnimatorReference>();
        }

        private struct CharacterModelInfo : INetworkSerializable
        {
            public int characterIndex;
            public int skinIndex;

            public CharacterModelInfo(int characterIndex, int skinIndex)
            {
                this.characterIndex = characterIndex;
                this.skinIndex = skinIndex;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref characterIndex);
                serializer.SerializeValue(ref skinIndex);
            }
        }

        private NetworkVariable<CharacterModelInfo> characterModelInfo = new NetworkVariable<CharacterModelInfo>(new CharacterModelInfo(-1, -1));
        public void SetCharacter(int characterIndex, int skinIndex)
        {
            characterModelInfo.Value = new CharacterModelInfo(characterIndex, skinIndex);
            if (IsSpawned)
            {
                ChangeSkin(characterModelInfo.Value.characterIndex, characterModelInfo.Value.skinIndex);
            }
            else if (!NetworkManager.IsServer) // This code block is for preview characters
            {
                ChangeSkin(characterIndex, skinIndex);
            }
        }

        private IEnumerator ChangeCharacter()
        {
            yield return null;
            WebRequestManager.Character character = PlayerDataManager.Singleton.GetPlayerData(attributes.GetPlayerDataId()).character;
            CharacterReference characterReference = PlayerDataManager.Singleton.GetCharacterReference();
            
            // Apply materials and equipment
            CharacterReference.RaceAndGender raceAndGender = characterReference.GetPlayerModelOptions()[characterReference.GetPlayerModelOptionIndices(character.model.ToString()).Key].raceAndGender;
            List<CharacterReference.CharacterMaterial> characterMaterialOptions = characterReference.GetCharacterMaterialOptions(raceAndGender);
            ApplyCharacterMaterial(characterMaterialOptions.Find(item => item.material.name == character.bodyColor));
            ApplyCharacterMaterial(characterMaterialOptions.Find(item => item.material.name == character.eyeColor));

            List<CharacterReference.WearableEquipmentOption> equipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWearableEquipmentOptions(raceAndGender);
            CharacterReference.WearableEquipmentOption beardOption = equipmentOptions.Find(item => item.wearableEquipmentPrefab.name == character.beard);
            ApplyWearableEquipment(beardOption ?? new CharacterReference.WearableEquipmentOption(CharacterReference.EquipmentType.Beard));
            CharacterReference.WearableEquipmentOption browsOption = equipmentOptions.Find(item => item.wearableEquipmentPrefab.name == character.brows);
            ApplyWearableEquipment(browsOption ?? new CharacterReference.WearableEquipmentOption(CharacterReference.EquipmentType.Brows));
            CharacterReference.WearableEquipmentOption hairOption = equipmentOptions.Find(item => item.wearableEquipmentPrefab.name == character.hair);
            ApplyWearableEquipment(hairOption ?? new CharacterReference.WearableEquipmentOption(CharacterReference.EquipmentType.Hair));
        }

        public override void OnNetworkSpawn()
        {
            ChangeSkin(characterModelInfo.Value.characterIndex, characterModelInfo.Value.skinIndex);
            StartCoroutine(ChangeCharacter());
        }

        private void Awake()
        {
            attributes = GetComponent<Attributes>();
            weaponHandler = GetComponent<WeaponHandler>();
        }

        public Vector3 GetAimPoint() { return aimPoint.Value; }
        private NetworkVariable<Vector3> aimPoint = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private void Update()
        {
            if (!IsSpawned) { return; }
            if (!LimbReferences.aimTargetIKSolver) { return; }

            if (IsLocalPlayer)
            {
                //bool bHit = Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, 10, ~LayerMask.GetMask(new string[] { "NetworkPrediction" }), QueryTriggerInteraction.Ignore);
                //if (bHit)
                //{
                //    if (hit.transform.TryGetComponent(out NetworkCollider networkCollider))
                //    {
                //        aimPoint.Value = hit.point;
                //    }
                //    else if (hit.transform.root != transform.root)
                //    {
                //        aimPoint.Value = hit.point;
                //    }
                //    else
                //    {
                //        aimPoint.Value = Camera.main.transform.position + Camera.main.transform.rotation * LimbReferences.aimTargetIKSolver.offset;
                //    }
                //}
                //else
                //{
                //    aimPoint.Value = Camera.main.transform.position + Camera.main.transform.rotation * LimbReferences.aimTargetIKSolver.offset;
                //}
                aimPoint.Value = Camera.main.transform.position + Camera.main.transform.rotation * LimbReferences.aimTargetIKSolver.offset;
            }

            LimbReferences.aimTargetIKSolver.transform.position = aimPoint.Value;
        }
    }
}