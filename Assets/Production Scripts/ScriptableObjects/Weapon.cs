using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Linq;

namespace Vi.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Weapon", menuName = "Production/Weapon")]
    public class Weapon : ScriptableObject
    {
        public enum WeaponClass
        {
            Brawler,
            Greatsword,
            Bow,
            Hammer,
            Lance,
            Staff
        }

        [SerializeField] private WeaponClass weaponClass;

        public WeaponClass GetWeaponClass() { return weaponClass; }

        public enum WeaponMaterial
        {
            Metal,
            Wood,
            Bone,
            Ice
        }

        [Header("Locomotion")]
        [SerializeField] private float runSpeed = 5;
        [SerializeField] private float walkSpeed = 2.5f;
        [SerializeField] private BlockingLocomotion blockingLocomotion = BlockingLocomotion.CanRun;

        public enum BlockingLocomotion
        {
            NoMovement,
            CanWalk,
            CanRun
        }

        [Header("Health")]
        [SerializeField] private float maxHP = 100;
        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100;
        [SerializeField] private float staminaRecoveryRate = 5;
        [SerializeField] private float staminaDelay = 1;
        [Header("Spirit")]
        [SerializeField] private float maxSpirit = 100;
        [Header("Rage")]
        [SerializeField] private float maxRage = 100;
        [SerializeField] private float rageRecoveryRate = 0;

        public bool IsWalking(bool isBlocking)
        {
            if (blockingLocomotion == BlockingLocomotion.CanWalk & isBlocking) { return true; }
            return false;
        }

        public float GetMovementSpeed(bool isBlocking)
        {
            switch (blockingLocomotion)
            {
                case BlockingLocomotion.NoMovement:
                    return isBlocking ? 0 : runSpeed;
                case BlockingLocomotion.CanWalk:
                    return isBlocking ? walkSpeed : runSpeed;
                case BlockingLocomotion.CanRun:
                    return runSpeed;
                default:
                    Debug.LogError("Unsure how to handle blocking locomotion type - " + blockingLocomotion);
                    break;
            }
            return runSpeed;
        }

        public float GetRunSpeed() { return runSpeed; }

        public float GetMaxHP() { return maxHP; }
        public float GetMaxStamina() { return maxStamina; }
        public float GetMaxSpirit() { return maxSpirit; }
        public float GetMaxRage() { return maxRage; }
        public float GetStaminaDelay() { return staminaDelay; }
        public float GetStaminaRecoveryRate() { return staminaRecoveryRate; }
        public float GetRageRecoveryRate() { return rageRecoveryRate; }

        [Header("Shooter Weapon Settings")]
        [SerializeField] private bool shouldUseAmmo;
        [SerializeField] private int maxAmmoCount;

        public bool ShouldUseAmmo() { return shouldUseAmmo; }
        public int GetMaxAmmoCount() { return maxAmmoCount; }

        public const float attackSoundEffectVolume = 0.25f;
        public const float projectileNearbyWhooshVolume = 1;
        public const float projectileNearbyWhooshDistanceThreshold = 5;

        [System.Serializable]
        private class AttackSoundEffect
        {
            public WeaponBone weaponBone = WeaponBone.RightHand;
            public AudioClip[] attackSoundEffects = new AudioClip[0];
        }

        [SerializeField] private List<AttackSoundEffect> attackSoundEffects = new List<AttackSoundEffect>();
        public AudioClip GetAttackSoundEffect(WeaponBone weaponBone)
        {
            AttackSoundEffect attackSoundEffect = attackSoundEffects.Find(item => item.weaponBone == weaponBone);
            if (attackSoundEffect == null) { return null; }
            return attackSoundEffect.attackSoundEffects[Random.Range(0, attackSoundEffect.attackSoundEffects.Length)];
        }

        public enum ArmorType
        {
            Cloth,
            Metal,
            Flesh
        }

        public const float hitSoundEffectVolume = 1;

        [System.Serializable]
        private class InflictHitSoundEffect
        {
            public ActionClip.Ailment ailment;
            public ArmorType armorType;
            public WeaponBone[] weaponBones = new WeaponBone[] { WeaponBone.Root };
            public AudioClip[] hitSounds = new AudioClip[0];
        }

        [SerializeField] private List<InflictHitSoundEffect> inflictHitSoundEffects = new List<InflictHitSoundEffect>();

        public AudioClip GetInflictHitSoundEffect(ArmorType armorType, WeaponBone weaponBone, ActionClip.Ailment ailment)
        {
            List<InflictHitSoundEffect> inflictHitSoundEffects;
            if (weaponBone == WeaponBone.Root)
            {
                inflictHitSoundEffects = this.inflictHitSoundEffects;
            }
            else if (this.inflictHitSoundEffects.Exists(item => item.weaponBones.Contains(weaponBone)))
            {
                inflictHitSoundEffects = this.inflictHitSoundEffects.FindAll(item => item.weaponBones.Contains(weaponBone));
            }
            else
            {
                Debug.LogWarning("Weapon bone " + weaponBone + " doesn't have inflict hit sound effects on weapon " + this);
                inflictHitSoundEffects = this.inflictHitSoundEffects;
            }

            InflictHitSoundEffect inflictHitSoundEffect = ailment == ActionClip.Ailment.None ? null : inflictHitSoundEffects.Find(item => item.ailment == ailment);
            if (inflictHitSoundEffect == null)
            {
                inflictHitSoundEffect = inflictHitSoundEffects.Find(item => item.armorType == armorType & item.ailment == ActionClip.Ailment.None);
                if (inflictHitSoundEffect == null)
                {
                    return inflictHitSoundEffects.Find(item => item.ailment == ActionClip.Ailment.None).hitSounds[Random.Range(0, inflictHitSoundEffect.hitSounds.Length)];
                }
                else // If armor effect isn't null
                {
                    return inflictHitSoundEffect.hitSounds[Random.Range(0, inflictHitSoundEffect.hitSounds.Length)];
                }
            }
            else // If ailment effect isn't null
            {
                return inflictHitSoundEffect.hitSounds[Random.Range(0, inflictHitSoundEffect.hitSounds.Length)];
            }
        }

        public GameObject hitVFXPrefab;

        [System.Serializable]
        private class BlockSoundEffect
        {
            public WeaponMaterial attackingWeaponMaterial;
            public AudioClip[] hitSounds;
        }

        [SerializeField] private List<BlockSoundEffect> blockingHitSoundEffects = new List<BlockSoundEffect>();

        public AudioClip GetBlockingHitSoundEffect(WeaponMaterial attackingWeaponMaterial)
        {
            BlockSoundEffect blockingHitSoundEffect;
            if (blockingHitSoundEffects.Exists(item => item.attackingWeaponMaterial == attackingWeaponMaterial))
            {
                blockingHitSoundEffect = blockingHitSoundEffects.Find(item => item.attackingWeaponMaterial == attackingWeaponMaterial);
            }
            else
            {
                blockingHitSoundEffect = blockingHitSoundEffects[0];
            }
            return blockingHitSoundEffect.hitSounds[Random.Range(0, blockingHitSoundEffect.hitSounds.Length)];
        }

        public GameObject blockVFXPrefab;

        public enum WeaponBone
        {
            Root = -1,
            RightHand = HumanBodyBones.RightHand,
            LeftHand = HumanBodyBones.LeftHand,
            RightArm = HumanBodyBones.RightLowerArm,
            LeftArm = HumanBodyBones.LeftLowerArm,
            RightFoot = HumanBodyBones.RightFoot,
            LeftFoot = HumanBodyBones.LeftFoot
        }

        [System.Serializable]
        public class WeaponModelData
        {
            public GameObject skinPrefab;
            public Data[] data;

            [System.Serializable]
            public class Data
            {
                public GameObject weaponPrefab;
                public WeaponBone weaponBone = WeaponBone.RightHand;
                public Vector3 weaponPositionOffset;
                public Vector3 weaponRotationOffset;
                public Vector3 stowedWeaponPositionOffset;
                public Vector3 stowedWeaponRotationOffset;
            }
        }

        [SerializeField] private List<WeaponModelData> weaponModelData = new List<WeaponModelData>();

        public List<WeaponModelData> GetWeaponModelData() { return weaponModelData; }

        public enum HitLocation
        {
            AllDirections,
            Front,
            Back,
            Left,
            Right
        }

        [System.Serializable]
        private class HitReaction
        {
            public HitLocation hitLocation = HitLocation.Front;
            public ActionClip reactionClip;
            public bool shouldAlreadyHaveAilment;
        }

        [SerializeField] private List<HitReaction> hitReactions = new List<HitReaction>();

        public ActionClip GetDeathReaction() { return hitReactions.Find(item => item.reactionClip.ailment == ActionClip.Ailment.Death).reactionClip; }

        public ActionClip GetHitReactionByDirection(HitLocation hitLocation) { return hitReactions.Find(item => item.hitLocation == hitLocation & item.reactionClip.GetHitReactionType() == ActionClip.HitReactionType.Normal).reactionClip; }

        public ActionClip GetHitReaction(ActionClip attack, float attackAngle, bool isBlocking, ActionClip.Ailment attackAilment, ActionClip.Ailment currentAilment)
        {
            HitLocation hitLocation;
            if (attackAngle <= 45.00f && attackAngle >= -45.00f)
            {
                hitLocation = HitLocation.Front;
            }
            else if (attackAngle > 45.00f && attackAngle < 135.00f)
            {
                hitLocation = HitLocation.Right;
            }
            else if (attackAngle < -45.00f && attackAngle > -135.00f)
            {
                hitLocation = HitLocation.Left;
            }
            else
            {
                hitLocation = HitLocation.Back;
            }

            HitReaction hitReaction = null;
            if (isBlocking & attack.isBlockable)
            {
                // Block the attack
                hitReaction = hitReactions.Find(item => (item.hitLocation == hitLocation | item.hitLocation == HitLocation.AllDirections) & item.reactionClip.GetHitReactionType() == ActionClip.HitReactionType.Blocking);
            }

            if (hitReaction == null) // If attack wasn't blocked
            {
                if (currentAilment != attackAilment & attackAilment != ActionClip.Ailment.None)
                {
                    // Find the start reaction for the attack's ailment
                    hitReaction = hitReactions.Find(item => (item.hitLocation == hitLocation | item.hitLocation == HitLocation.AllDirections) & item.reactionClip.ailment == attackAilment & !item.shouldAlreadyHaveAilment);

                    // Find a hit reaction for an in progress ailment
                    if (hitReaction == null)
                    {
                        hitReaction = hitReactions.Find(item => (item.hitLocation == hitLocation | item.hitLocation == HitLocation.AllDirections) & item.reactionClip.ailment == currentAilment & item.shouldAlreadyHaveAilment);
                    }

                    // Find a normal hit reaction if there isn't a special hit reaction for this ailment
                    if (hitReaction == null)
                    {
                        hitReaction = hitReactions.Find(item => (item.hitLocation == hitLocation | item.hitLocation == HitLocation.AllDirections) & item.reactionClip.GetHitReactionType() == ActionClip.HitReactionType.Normal);
                    }
                }
                else if (currentAilment != ActionClip.Ailment.None)
                {
                    // Find a hit reaction for an in progress ailment
                    hitReaction = hitReactions.Find(item => (item.hitLocation == hitLocation | item.hitLocation == HitLocation.AllDirections) & item.reactionClip.ailment == currentAilment & item.shouldAlreadyHaveAilment);

                    // If we can't find an in progress reaction, just get a normal reaction
                    if (hitReaction == null)
                    {
                        hitReaction = hitReactions.Find(item => (item.hitLocation == hitLocation | item.hitLocation == HitLocation.AllDirections) & item.reactionClip.GetHitReactionType() == ActionClip.HitReactionType.Normal);
                    }
                }
                else
                {
                    // Find a normal hit reaction
                    hitReaction = hitReactions.Find(item => (item.hitLocation == hitLocation | item.hitLocation == HitLocation.AllDirections) & item.reactionClip.GetHitReactionType() == ActionClip.HitReactionType.Normal);
                }
            }

            if (hitReaction == null)
            {
                Debug.LogError("Could not find hit reaction for location: " + hitLocation + " for weapon: " + this + " ailment: " + attackAilment + " blocking: " + isBlocking + " current ailment: " + currentAilment);
                return null;
            }

            return hitReaction.reactionClip;
        }

        [System.Serializable]
        private class FlinchReaction
        {
            public HitLocation hitLocation = HitLocation.Front;
            public ActionClip reactionClip;
        }

        [SerializeField] private List<FlinchReaction> flinchReactions = new List<FlinchReaction>();

        public ActionClip GetFlinchClip(float attackAngle)
        {
            HitLocation hitLocation;
            if (attackAngle <= 45.00f && attackAngle >= -45.00f)
            {
                hitLocation = HitLocation.Front;
            }
            else if (attackAngle > 45.00f && attackAngle < 135.00f)
            {
                hitLocation = HitLocation.Right;
            }
            else if (attackAngle < -45.00f && attackAngle > -135.00f)
            {
                hitLocation = HitLocation.Left;
            }
            else
            {
                hitLocation = HitLocation.Back;
            }
            return flinchReactions.Find(item => item.hitLocation == hitLocation).reactionClip;
        }

        public enum InputAttackType
        {
            LightAttack,
            HeavyAttack,
            Ability1,
            Ability2,
            Ability3,
            Ability4
        }

        public List<ActionClip> GetAbilities()
        {
            List<ActionClip> abilityList = new List<ActionClip>();
            abilityList.Add(ability1);
            abilityList.Add(ability2);
            abilityList.Add(ability3);
            abilityList.Add(ability4);
            return abilityList;
        }

        public ActionClip GetAbility1() { return ability1; }
        public ActionClip GetAbility2() { return ability2; }
        public ActionClip GetAbility3() { return ability3; }
        public ActionClip GetAbility4() { return ability4; }

        [SerializeField] private ActionClip ability1;
        [SerializeField] private ActionClip ability2;
        [SerializeField] private ActionClip ability3;
        [SerializeField] private ActionClip ability4;

        private float lastAbility1ActivateTime = Mathf.NegativeInfinity;
        private float lastAbility2ActivateTime = Mathf.NegativeInfinity;
        private float lastAbility3ActivateTime = Mathf.NegativeInfinity;
        private float lastAbility4ActivateTime = Mathf.NegativeInfinity;

        public void ResetAllAbilityCooldowns()
        {
            lastAbility1ActivateTime = Mathf.NegativeInfinity;
            lastAbility2ActivateTime = Mathf.NegativeInfinity;
            lastAbility3ActivateTime = Mathf.NegativeInfinity;
            lastAbility4ActivateTime = Mathf.NegativeInfinity;
        }

        public void StartAbilityCooldown(ActionClip ability)
        {
            if (ability == ability1)
            {
                lastAbility1ActivateTime = Time.time;
            }
            else if (ability == ability2)
            {
                lastAbility2ActivateTime = Time.time;
            }
            else if (ability == ability3)
            {
                lastAbility3ActivateTime = Time.time;
            }
            else if (ability == ability4)
            {
                lastAbility4ActivateTime = Time.time;
            }
            else
            {
                Debug.LogError(ability + " is not one of this weapon's abilities! " + this);
            }
        }

        public float GetAbilityCooldownProgress(ActionClip ability)
        {
            if (ability == ability1)
            {
                return Mathf.Clamp((Time.time - lastAbility1ActivateTime) / ability1.abilityCooldownTime, 0, 1);
            }
            else if (ability == ability2)
            {
                return Mathf.Clamp((Time.time - lastAbility2ActivateTime) / ability2.abilityCooldownTime, 0, 1);
            }
            else if (ability == ability3)
            {
                return Mathf.Clamp((Time.time - lastAbility3ActivateTime) / ability3.abilityCooldownTime, 0, 1);
            }
            else if (ability == ability4)
            {
                return Mathf.Clamp((Time.time - lastAbility4ActivateTime) / ability4.abilityCooldownTime, 0, 1);
            }
            else
            {
                Debug.LogError(ability + " is not one of this weapon's abilities! " + this);
                return 0;
            }
        }

        public void ReduceAbilityCooldownTime(ActionClip ability, float percent)
        {
            if (ability == ability1)
            {
                lastAbility1ActivateTime -= ability1.abilityCooldownTime * percent;
            }
            else if (ability == ability2)
            {
                lastAbility2ActivateTime -= ability2.abilityCooldownTime * percent;
            }
            else if (ability == ability3)
            {
                lastAbility3ActivateTime -= ability3.abilityCooldownTime * percent;
            }
            else if (ability == ability4)
            {
                lastAbility4ActivateTime -= ability4.abilityCooldownTime * percent;
            }
            else
            {
                Debug.LogError(ability + " is not one of this weapon's abilities! " + this);
            }
        }

        [SerializeField] private ActionClip flashAttack;

        public ActionClip GetFlashAttack() { return flashAttack; }

        public List<Attack> GetAttackList() { return attackList; }
        [SerializeField] private List<Attack> attackList = new List<Attack>();

        public enum ComboCondition
        {
            None,
            InputForward,
            InputBackwards,
            InputLeft,
            InputRight
        }

        [System.Serializable]
        public class Attack
        {
            public List<InputAttackType> inputs;
            public ComboCondition comboCondition = ComboCondition.None;
            public ActionClip attackClip;
        }

        public ActionClip GetGrabAttackClip(ActionClip attack)
        {
            GrabAttackCrosswalk crosswalk = System.Array.Find(grabAttackClipList, item => item.attack == attack);
            if (crosswalk == null) { Debug.LogError("Can't find grab attack crosswalk for " + attack); }
            return crosswalk.grabAttackClip;
        }

        [System.Serializable]
        private class GrabAttackCrosswalk
        {
            public ActionClip attack;
            public ActionClip grabAttackClip;
        }

        [SerializeField] private GrabAttackCrosswalk[] grabAttackClipList = new GrabAttackCrosswalk[0];

        public float dodgeStaminaCost { get; private set; } = 0;
        [Header("Dodge Assignments")]
        public float dodgeCooldownDuration = 5;
        [SerializeField] private ActionClip dodgeF;
        [SerializeField] private ActionClip dodgeFL;
        [SerializeField] private ActionClip dodgeFR;
        [SerializeField] private ActionClip dodgeB;
        [SerializeField] private ActionClip dodgeBL;
        [SerializeField] private ActionClip dodgeBR;
        [SerializeField] private ActionClip dodgeL;
        [SerializeField] private ActionClip dodgeR;

        private float lastDodgeActivateTime = Mathf.NegativeInfinity;

        public void StartDodgeCooldown() { lastDodgeActivateTime = Time.time; }

        public bool IsDodgeOnCooldown() { return Time.time - lastDodgeActivateTime < dodgeCooldownDuration; }

        public float GetDodgeCooldownProgress() { return Mathf.Clamp((Time.time - lastDodgeActivateTime) / dodgeCooldownDuration, 0, 1); }

        public ActionClip GetDodgeClip(float angle)
        {
            ActionClip dodgeClip;
            if (angle <= 15f && angle >= -15f)
            {
                dodgeClip = dodgeF;
            }
            else if (angle < 80f && angle > 15f)
            {
                dodgeClip = dodgeFL;
            }
            else if (angle > -80f && angle < -15f)
            {
                dodgeClip = dodgeFR;
            }
            else if (angle > 80f && angle < 100f)
            {
                dodgeClip = dodgeL;
            }
            else if (angle < -80f && angle > -100f)
            {
                dodgeClip = dodgeR;
            }
            else if (angle < -100f && angle > -170f)
            {
                dodgeClip = dodgeBR;
            }
            else if (angle > 100f && angle < 170f)
            {
                dodgeClip = dodgeBL;
            }
            else
            {
                dodgeClip = dodgeB;
            }

            return dodgeClip;
        }

        [Header("Lunge Assignments")]
        [SerializeField] private ActionClip lunge;

        public ActionClip GetLungeClip() { return lunge; }

        private Dictionary<string, ActionClip> actionClipLookup = new Dictionary<string, ActionClip>();

        private void Awake()
        {
            if (dodgeF) { actionClipLookup.TryAdd(dodgeF.name, dodgeF); }
            if (dodgeFL) { actionClipLookup.TryAdd(dodgeFL.name, dodgeFL); }
            if (dodgeFR) { actionClipLookup.TryAdd(dodgeFR.name, dodgeFR); }
            if (dodgeB) { actionClipLookup.TryAdd(dodgeB.name, dodgeB); }
            if (dodgeBL) { actionClipLookup.TryAdd(dodgeBL.name, dodgeBL); }
            if (dodgeBR) { actionClipLookup.TryAdd(dodgeBR.name, dodgeBR); }
            if (dodgeL) { actionClipLookup.TryAdd(dodgeL.name, dodgeL); }
            if (dodgeR) { actionClipLookup.TryAdd(dodgeR.name, dodgeR); }

            if (lunge) { actionClipLookup.TryAdd(lunge.name, lunge); }

            if (ability1) { actionClipLookup.TryAdd(ability1.name, ability1); }
            if (ability2) { actionClipLookup.TryAdd(ability2.name, ability2); }
            if (ability3) { actionClipLookup.TryAdd(ability3.name, ability3); }
            if (ability4) { actionClipLookup.TryAdd(ability4.name, ability4); }

            if (flashAttack) { actionClipLookup.TryAdd(flashAttack.name, flashAttack); }

            foreach (HitReaction hitReaction in hitReactions)
            {
                if (!hitReaction.reactionClip) { continue; }
                actionClipLookup.TryAdd(hitReaction.reactionClip.name, hitReaction.reactionClip);
            }

            foreach (FlinchReaction flinchReaction in flinchReactions)
            {
                if (!flinchReaction.reactionClip) { continue; }
                actionClipLookup.TryAdd(flinchReaction.reactionClip.name, flinchReaction.reactionClip);
            }

            foreach (Attack attack in attackList)
            {
                if (!attack.attackClip) { continue; }
                actionClipLookup.TryAdd(attack.attackClip.name, attack.attackClip);
            }

            foreach (GrabAttackCrosswalk grabAttackCrosswalk in grabAttackClipList)
            {
                if (grabAttackCrosswalk.attack) { actionClipLookup.TryAdd(grabAttackCrosswalk.attack.name, grabAttackCrosswalk.attack); }
                if (grabAttackCrosswalk.grabAttackClip) { actionClipLookup.TryAdd(grabAttackCrosswalk.grabAttackClip.name, grabAttackCrosswalk.grabAttackClip); }
            }
        }

        public ActionClip GetActionClipByName(string clipName)
        {
            if (actionClipLookup.ContainsKey(clipName))
            {
                return actionClipLookup[clipName];
            }
            else
            {
                if (Application.isPlaying) { Debug.LogError("Action clip Not Found: " + clipName); }
                return null;
            }
        }

        public ActionClip GetActionClipByNameUsingReflection(string clipName)
        {
            IEnumerable<FieldInfo> propertyList = typeof(Weapon).GetRuntimeFields();
            foreach (FieldInfo propertyInfo in propertyList)
            {
                if (propertyInfo.FieldType == typeof(ActionClip))
                {
                    var ActionClipObject = propertyInfo.GetValue(this);
                    ActionClip ActionClip = (ActionClip)ActionClipObject;

                    if (ActionClip)
                    {
                        if (ActionClip.name == clipName) { return ActionClip; }

                        foreach (ActionClip.FollowUpActionClip followUpClip in ActionClip.followUpActionClipsToPlay)
                        {
                            if (followUpClip.actionClip.name == clipName) { return followUpClip.actionClip; }
                        }
                    }
                }
                else if (propertyInfo.FieldType == typeof(List<ActionClip>))
                {
                    var ActionClipListObject = propertyInfo.GetValue(this);
                    List<ActionClip> ActionClipList = (List<ActionClip>)ActionClipListObject;

                    foreach (ActionClip ActionClip in ActionClipList)
                    {
                        if (ActionClip)
                        {
                            if (ActionClip.name == clipName) { return ActionClip; }

                            foreach (ActionClip.FollowUpActionClip followUpClip in ActionClip.followUpActionClipsToPlay)
                            {
                                if (followUpClip.actionClip.name == clipName) { return followUpClip.actionClip; }
                            }
                        }
                    }
                }
                else if (propertyInfo.FieldType == typeof(List<HitReaction>))
                {
                    var HitReactionListObject = propertyInfo.GetValue(this);
                    List<HitReaction> hitReactions = (List<HitReaction>)HitReactionListObject;

                    foreach (HitReaction hitReaction in hitReactions)
                    {
                        if (hitReaction.reactionClip)
                        {
                            if (hitReaction.reactionClip.name == clipName) { return hitReaction.reactionClip; }

                            foreach (ActionClip.FollowUpActionClip followUpClip in hitReaction.reactionClip.followUpActionClipsToPlay)
                            {
                                if (followUpClip.actionClip.name == clipName) { return followUpClip.actionClip; }
                            }
                        }
                    }
                }
                else if (propertyInfo.FieldType == typeof(List<FlinchReaction>))
                {
                    var FlinchReactionListObject = propertyInfo.GetValue(this);
                    List<FlinchReaction> hitReactions = (List<FlinchReaction>)FlinchReactionListObject;

                    foreach (FlinchReaction flinchReaction in hitReactions)
                    {
                        if (flinchReaction.reactionClip)
                        {
                            if (flinchReaction.reactionClip.name == clipName) { return flinchReaction.reactionClip; }

                            foreach (ActionClip.FollowUpActionClip followUpClip in flinchReaction.reactionClip.followUpActionClipsToPlay)
                            {
                                if (followUpClip.actionClip.name == clipName) { return followUpClip.actionClip; }
                            }
                        }
                    }
                }
                else if (propertyInfo.FieldType == typeof(List<Attack>))
                {
                    var AttackListObject = propertyInfo.GetValue(this);
                    List<Attack> attacks = (List<Attack>)AttackListObject;

                    foreach (Attack attack in attacks)
                    {
                        if (attack.attackClip)
                        {
                            if (attack.attackClip.name == clipName) { return attack.attackClip; }

                            foreach (ActionClip.FollowUpActionClip followUpClip in attack.attackClip.followUpActionClipsToPlay)
                            {
                                if (followUpClip.actionClip.name == clipName) { return followUpClip.actionClip; }
                            }
                        }
                    }
                }
                else if (propertyInfo.FieldType == typeof(GrabAttackCrosswalk[]))
                {
                    var AttackArrayObject = propertyInfo.GetValue(this);
                    GrabAttackCrosswalk[] attacks = (GrabAttackCrosswalk[])AttackArrayObject;

                    foreach (GrabAttackCrosswalk attack in attacks)
                    {
                        if (attack.grabAttackClip)
                        {
                            if (attack.grabAttackClip.name == clipName) { return attack.grabAttackClip; }

                            foreach (ActionClip.FollowUpActionClip followUpClip in attack.grabAttackClip.followUpActionClipsToPlay)
                            {
                                if (followUpClip.actionClip.name == clipName) { return followUpClip.actionClip; }
                            }
                        }
                    }
                }
            }

            if (Application.isPlaying) { Debug.LogError("Action clip Not Found: " + clipName); }
            return null;
        }
    }
}