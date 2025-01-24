using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Linq;
using Vi.Utility;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

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

        public BlockingLocomotion GetBlockingLocomotion() { return blockingLocomotion; }

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

        public float GetMovementSpeed(bool isBlocking, bool isAiming)
        {
            if (isBlocking)
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
            }

            if (isAiming) { return walkSpeed; }

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
        [SerializeField] private ActionClip reloadClip;

        public ActionClip GetReloadClip() { return reloadClip; }

        public bool ShouldUseAmmo() { return shouldUseAmmo; }
        public int GetMaxAmmoCount() { return maxAmmoCount; }

        public const float attackSoundEffectVolume = 0.25f;
        public const float projectileNearbyWhooshVolume = 0.75f;
        public const float projectileNearbyWhooshDistanceThreshold = 5;

        public const float attackSoundEffectMaxDistance = 50;

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
                    InflictHitSoundEffect soundEffect = inflictHitSoundEffects.Find(item => item.ailment == ActionClip.Ailment.None);
                    if (soundEffect == null)
                    {
                        Debug.LogError(this + " doesn't have any inflict hit sound effects with ailment none!");
                        return null;
                    }
                    else if (soundEffect.hitSounds == null)
                    {
                        Debug.LogError("No Inflict Sound effect for " + this + " for the following params: " + armorType + " " + weaponBone + " " + ailment);
                        return null;
                    }
                    else if (soundEffect.hitSounds.Length == 0)
                    {
                        Debug.LogError("No Inflict Sound effect for " + this + " for the following params: " + armorType + " " + weaponBone + " " + ailment);
                        return null;
                    }
                    else
                    {
                        return soundEffect.hitSounds[Random.Range(0, soundEffect.hitSounds.Length)];
                    }
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

        public PooledObject hitVFXPrefab;

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

        public PooledObject blockVFXPrefab;

        [SerializeField] private List<AudioClip> reloadSoundEffects = new List<AudioClip>();

        public const float reloadSoundEffectVolume = 0.1f;
        public AudioClip GetReloadSoundEffect()
        {
            if (reloadSoundEffects.Count == 0) { return null; }
            return reloadSoundEffects[Random.Range(0, reloadSoundEffects.Count)];
        }

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
                public StowedWeaponParentType stowedParentType = StowedWeaponParentType.Back_M;
                public Vector3 stowedWeaponPositionOffset;
                public Vector3 stowedWeaponRotationOffset;
                public PersistentNonWeaponData[] persistentNonWeaponPrefabs = new PersistentNonWeaponData[0];
            }

            [System.Serializable]
            public struct PersistentNonWeaponData
            {
                public StowedWeaponParentType parentType;
                public PooledObject prefab;
            }
        }

        public enum StowedWeaponParentType
        {
            Hip_L,
            Hip_R,
            LegPlate_L,
            LegPlate_R,
            Back_2HL,
            Back_Bow,
            Back_L,
            Back_M,
            Back_R,
            Back_Quiver,

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

        [SerializeField] private SharedWeaponReferences sharedWeaponReferences;

        public ActionClip GetDeathReaction() { return CreateCopyOfActionClip(sharedWeaponReferences.HitReactions.Find(item => item.reactionClip.ailment == ActionClip.Ailment.Death).reactionClip); }

        public ActionClip GetIncapacitatedReaction() { return CreateCopyOfActionClip(sharedWeaponReferences.HitReactions.Find(item => item.reactionClip.ailment == ActionClip.Ailment.Incapacitated).reactionClip); }

        public ActionClip GetHitReactionByDirection(HitLocation hitLocation) { return CreateCopyOfActionClip(sharedWeaponReferences.HitReactions.Find(item => item.hitLocation == hitLocation & item.reactionClip.GetHitReactionType() == ActionClip.HitReactionType.Normal).reactionClip); }

        private ActionClip CreateCopyOfActionClip(ActionClip actionClip)
        {
            ActionClip instance = Instantiate(actionClip);
            instance.name = actionClip.name;
            return instance;
        }

        public ActionClip GetHitReaction(ActionClip attack, float attackAngle, bool isBlocking, ActionClip.Ailment attackAilment, ActionClip.Ailment currentAilment, bool applyAilmentRegardless)
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

            SharedWeaponReferences.HitReaction hitReaction = null;
            if (isBlocking & attack.isBlockable)
            {
                // Block the attack
                hitReaction = sharedWeaponReferences.HitReactions.Find(item => (item.hitLocation == hitLocation | item.hitLocation == HitLocation.AllDirections) & item.reactionClip.GetHitReactionType() == ActionClip.HitReactionType.Blocking);
            }

            if (hitReaction == null) // If attack wasn't blocked
            {
                if (currentAilment != attackAilment & attackAilment != ActionClip.Ailment.None)
                {
                    // Find the start reaction for the attack's ailment
                    hitReaction = sharedWeaponReferences.HitReactions.Find(item => (item.hitLocation == hitLocation | item.hitLocation == HitLocation.AllDirections) & item.reactionClip.ailment == attackAilment & !item.shouldAlreadyHaveAilment);

                    // Find a hit reaction for an in progress ailment
                    if (hitReaction == null)
                    {
                        hitReaction = sharedWeaponReferences.HitReactions.Find(item => (item.hitLocation == hitLocation | item.hitLocation == HitLocation.AllDirections) & item.reactionClip.ailment == currentAilment & item.shouldAlreadyHaveAilment);
                    }

                    // Find a normal hit reaction if there isn't a special hit reaction for this ailment
                    if (hitReaction == null)
                    {
                        hitReaction = sharedWeaponReferences.HitReactions.Find(item => (item.hitLocation == hitLocation | item.hitLocation == HitLocation.AllDirections) & item.reactionClip.GetHitReactionType() == ActionClip.HitReactionType.Normal);
                    }
                }
                else if (currentAilment != ActionClip.Ailment.None)
                {
                    // Find a hit reaction for an in progress ailment
                    hitReaction = sharedWeaponReferences.HitReactions.Find(item => (item.hitLocation == hitLocation | item.hitLocation == HitLocation.AllDirections) & item.reactionClip.ailment == currentAilment & item.shouldAlreadyHaveAilment);

                    // If we can't find an in progress reaction, just get a normal reaction
                    if (hitReaction == null)
                    {
                        if (applyAilmentRegardless)
                        {
                            hitReaction = sharedWeaponReferences.HitReactions.Find(item => (item.hitLocation == hitLocation | item.hitLocation == HitLocation.AllDirections) & item.reactionClip.ailment == attackAilment & !item.shouldAlreadyHaveAilment);
                        }
                        else
                        {
                            hitReaction = sharedWeaponReferences.HitReactions.Find(item => (item.hitLocation == hitLocation | item.hitLocation == HitLocation.AllDirections) & item.reactionClip.GetHitReactionType() == ActionClip.HitReactionType.Normal);
                        }
                    }
                }
                else
                {
                    // Find a normal hit reaction
                    hitReaction = sharedWeaponReferences.HitReactions.Find(item => (item.hitLocation == hitLocation | item.hitLocation == HitLocation.AllDirections) & item.reactionClip.GetHitReactionType() == ActionClip.HitReactionType.Normal);
                }
            }

            if (hitReaction == null)
            {
                Debug.LogError("Could not find hit reaction for location: " + hitLocation + " for weapon: " + this + " ailment: " + attackAilment + " blocking: " + isBlocking + " current ailment: " + currentAilment);
                return null;
            }

            return CreateCopyOfActionClip(hitReaction.reactionClip);
        }

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
            return sharedWeaponReferences.FlinchReactions.Find(item => item.hitLocation == hitLocation).reactionClip;
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

        [SerializeField] private int maxAbility1Count = 1;
        [SerializeField] private int maxAbility2Count = 1;
        [SerializeField] private int maxAbility3Count = 1;
        [SerializeField] private int maxAbility4Count = 1;

        public int GetMaxAbilityStacks(ActionClip ability)
        {
            if (ability == ability1)
            {
                return maxAbility1Count;
            }
            else if (ability == ability2)
            {
                return maxAbility2Count;
            }
            else if (ability == ability3)
            {
                return maxAbility3Count;
            }
            else if (ability == ability4)
            {
                return maxAbility4Count;
            }
            else
            {
                Debug.LogError(ability + " is not one of this weapon's abilities! " + this);
                return 0;
            }
        }

        private float[] lastAbility1ActivateTimes;
        private float[] lastAbility2ActivateTimes;
        private float[] lastAbility3ActivateTimes;
        private float[] lastAbility4ActivateTimes;

        // For buffering stacks
        private float lastAbility1ActivateTime = Mathf.NegativeInfinity;
        private float lastAbility2ActivateTime = Mathf.NegativeInfinity;
        private float lastAbility3ActivateTime = Mathf.NegativeInfinity;
        private float lastAbility4ActivateTime = Mathf.NegativeInfinity;

        public void ResetAllAbilityCooldowns()
        {
            lastAbility1ActivateTimes = new float[maxAbility1Count];
            for (int i = 0; i < lastAbility1ActivateTimes.Length; i++)
            {
                lastAbility1ActivateTimes[i] = Mathf.NegativeInfinity;
            }
            lastAbility1ActivateTime = Mathf.NegativeInfinity;

            lastAbility2ActivateTimes = new float[maxAbility2Count];
            for (int i = 0; i < lastAbility2ActivateTimes.Length; i++)
            {
                lastAbility2ActivateTimes[i] = Mathf.NegativeInfinity;
            }
            lastAbility2ActivateTime = Mathf.NegativeInfinity;

            lastAbility3ActivateTimes = new float[maxAbility3Count];
            for (int i = 0; i < lastAbility3ActivateTimes.Length; i++)
            {
                lastAbility3ActivateTimes[i] = Mathf.NegativeInfinity;
            }
            lastAbility3ActivateTime = Mathf.NegativeInfinity;

            lastAbility4ActivateTimes = new float[maxAbility4Count];
            for (int i = 0; i < lastAbility4ActivateTimes.Length; i++)
            {
                lastAbility4ActivateTimes[i] = Mathf.NegativeInfinity;
            }
            lastAbility4ActivateTime = Mathf.NegativeInfinity;
        }

        public void StartAbilityCooldown(ActionClip ability)
        {
            if (ability == ability1)
            {
                float min = lastAbility1ActivateTimes.Min();
                int index = System.Array.FindIndex(lastAbility1ActivateTimes, item => Mathf.Approximately(item, min) | item == min);
                if (index == -1)
                {
                    Debug.LogWarning("No index for min ability activate time!");
                }
                else
                {
                    lastAbility1ActivateTimes[index] = Time.time;
                    lastAbility1ActivateTime = Time.time;
                }
            }
            else if (ability == ability2)
            {
                float min = lastAbility2ActivateTimes.Min();
                int index = System.Array.FindIndex(lastAbility2ActivateTimes, item => Mathf.Approximately(item, min) | item == min);
                if (index == -1)
                {
                    Debug.LogWarning("No index for min ability activate time!");
                }
                else
                {
                    lastAbility2ActivateTimes[index] = Time.time;
                    lastAbility2ActivateTime = Time.time;
                }
            }
            else if (ability == ability3)
            {
                float min = lastAbility3ActivateTimes.Min();
                int index = System.Array.FindIndex(lastAbility3ActivateTimes, item => Mathf.Approximately(item, min) | item == min);
                if (index == -1)
                {
                    Debug.LogWarning("No index for min ability activate time!");
                }
                else
                {
                    lastAbility3ActivateTimes[index] = Time.time;
                    lastAbility3ActivateTime = Time.time;
                }
            }
            else if (ability == ability4)
            {
                float min = lastAbility4ActivateTimes.Min();
                int index = System.Array.FindIndex(lastAbility4ActivateTimes, item => Mathf.Approximately(item, min) | item == min);
                if (index == -1)
                {
                    Debug.LogWarning("No index for min ability activate time!");
                }
                else
                {
                    lastAbility4ActivateTimes[index] = Time.time;
                    lastAbility4ActivateTime = Time.time;
                }
            }
            else
            {
                Debug.LogError(ability + " is not one of this weapon's abilities! " + this);
            }
        }

        public int GetNumberOfAbilitiesOffCooldown(ActionClip ability)
        {
            if (ability == ability1)
            {
                int numAbilitiesOfCooldown = 0;
                foreach (float lastAbilityActivateTime in lastAbility1ActivateTimes)
                {
                    if (Time.time - lastAbilityActivateTime >= GetAbilityCooldownDuration(ability))
                    {
                        numAbilitiesOfCooldown++;
                    }
                }
                return numAbilitiesOfCooldown;
            }
            else if (ability == ability2)
            {
                int numAbilitiesOfCooldown = 0;
                foreach (float lastAbilityActivateTime in lastAbility2ActivateTimes)
                {
                    if (Time.time - lastAbilityActivateTime >= GetAbilityCooldownDuration(ability))
                    {
                        numAbilitiesOfCooldown++;
                    }
                }
                return numAbilitiesOfCooldown;
            }
            else if (ability == ability3)
            {
                int numAbilitiesOfCooldown = 0;
                foreach (float lastAbilityActivateTime in lastAbility3ActivateTimes)
                {
                    if (Time.time - lastAbilityActivateTime >= GetAbilityCooldownDuration(ability))
                    {
                        numAbilitiesOfCooldown++;
                    }
                }
                return numAbilitiesOfCooldown;
            }
            else if (ability == ability4)
            {
                int numAbilitiesOfCooldown = 0;
                foreach (float lastAbilityActivateTime in lastAbility4ActivateTimes)
                {
                    if (Time.time - lastAbilityActivateTime >= GetAbilityCooldownDuration(ability))
                    {
                        numAbilitiesOfCooldown++;
                    }
                }
                return numAbilitiesOfCooldown;
            }
            else
            {
                Debug.LogError(ability + " is not one of this weapon's abilities! " + this);
            }
            return 0;
        }

        private float GetLastAbilityActivateTimeForBuffering(ActionClip ability)
        {
            if (ability == ability1)
            {
                return lastAbility1ActivateTime;
            }
            else if (ability == ability2)
            {
                return lastAbility2ActivateTime;
            }
            else if (ability == ability3)
            {
                return lastAbility3ActivateTime;
            }
            else if (ability == ability4)
            {
                return lastAbility4ActivateTime;
            }
            else
            {
                Debug.LogError(ability + " is not one of this weapon's abilities! " + this);
                return 0;
            }
        }

        private float GetLastAbilityActivateTime(ActionClip ability)
        {
            if (ability == ability1)
            {
                if (GetNumberOfAbilitiesOffCooldown(ability) == lastAbility1ActivateTimes.Length)
                {
                    return lastAbility1ActivateTimes.Max();
                }
                else
                {
                    return lastAbility1ActivateTimes.Min();
                }
            }
            else if (ability == ability2)
            {
                if (GetNumberOfAbilitiesOffCooldown(ability) == lastAbility2ActivateTimes.Length)
                {
                    return lastAbility2ActivateTimes.Max();
                }
                else
                {
                    return lastAbility2ActivateTimes.Min();
                }
            }
            else if (ability == ability3)
            {
                if (GetNumberOfAbilitiesOffCooldown(ability) == lastAbility3ActivateTimes.Length)
                {
                    return lastAbility3ActivateTimes.Max();
                }
                else
                {
                    return lastAbility3ActivateTimes.Min();
                }
            }
            else if (ability == ability4)
            {
                if (GetNumberOfAbilitiesOffCooldown(ability) == lastAbility4ActivateTimes.Length)
                {
                    return lastAbility4ActivateTimes.Max();
                }
                else
                {
                    return lastAbility4ActivateTimes.Min();
                }
            }
            else
            {
                Debug.LogError(ability + " is not one of this weapon's abilities! " + this);
                return 0;
            }
        }

        private float GetAbilityBufferDuration(ActionClip ability)
        {
            return ability.abilityBufferTime;
        }

        public float GetAbilityBufferTimeLeft(ActionClip ability)
        {
            float abilityBufferDuration = GetAbilityBufferDuration(ability);
            return Mathf.Max(0, abilityBufferDuration - (Time.time - GetLastAbilityActivateTimeForBuffering(ability)));
        }

        public float GetAbilityBufferProgress(ActionClip ability)
        {
            float abilityBufferDuration = GetAbilityBufferDuration(ability);
            if (Mathf.Approximately(abilityBufferDuration, 0)) { return 1; }
            return Mathf.Clamp((Time.time - GetLastAbilityActivateTimeForBuffering(ability)) / abilityBufferDuration, 0, 1);
        }

        public float GetAbilityCooldownTimeLeft(ActionClip ability)
        {
            float abilityCooldownDuration = GetAbilityCooldownDuration(ability);
            return Mathf.Max(0, abilityCooldownDuration - (Time.time - GetLastAbilityActivateTime(ability)));
        }

        public float GetAbilityCooldownProgress(ActionClip ability)
        {
            float abilityCooldownDuration = GetAbilityCooldownDuration(ability);
            if (Mathf.Approximately(abilityCooldownDuration, 0)) { return 1; }
            return Mathf.Clamp((Time.time - GetLastAbilityActivateTime(ability)) / abilityCooldownDuration, 0, 1);
        }

        public float AbilityCooldownMultiplier { get; set; } = 1;
        private float GetAbilityCooldownDuration(ActionClip ability)
        {
            if (ability == ability1)
            {
                return Mathf.Max(0, ability1.abilityCooldownTime - ability1CooldownOffset) * AbilityCooldownMultiplier;
            }
            else if (ability == ability2)
            {
                return Mathf.Max(0, ability2.abilityCooldownTime - ability2CooldownOffset) * AbilityCooldownMultiplier;
            }
            else if (ability == ability3)
            {
                return Mathf.Max(0, ability3.abilityCooldownTime - ability3CooldownOffset) * AbilityCooldownMultiplier;
            }
            else if (ability == ability4)
            {
                return Mathf.Max(0, ability4.abilityCooldownTime - ability4CooldownOffset) * AbilityCooldownMultiplier;
            }
            else
            {
                Debug.LogError(ability + " is not one of this weapon's abilities! " + this);
                return 0;
            }
        }

        private float ability1CooldownOffset;
        private float ability2CooldownOffset;
        private float ability3CooldownOffset;
        private float ability4CooldownOffset;

        public void PermanentlyReduceAbilityCooldownTime(ActionClip ability, float percent)
        {
            if (ability == ability1)
            {
                ability1CooldownOffset = ability1.abilityCooldownTime * percent;
            }
            else if (ability == ability2)
            {
                ability2CooldownOffset = ability2.abilityCooldownTime * percent;
            }
            else if (ability == ability3)
            {
                ability3CooldownOffset = ability3.abilityCooldownTime * percent;
            }
            else if (ability == ability4)
            {
                ability4CooldownOffset = ability4.abilityCooldownTime * percent;
            }
            else
            {
                Debug.LogError(ability + " is not one of this weapon's abilities! " + this);
            }
            ReduceAbilityCooldownTime(ability, percent);
        }

        public float GetAbilityOffsetDifference(ActionClip ability)
        {
            if (ability == ability1)
            {
                return ability1.abilityCooldownTime - ability1CooldownOffset;
            }
            else if (ability == ability2)
            {
                return ability2.abilityCooldownTime - ability2CooldownOffset;
            }
            else if (ability == ability3)
            {
                return ability3.abilityCooldownTime - ability3CooldownOffset;
            }
            else if (ability == ability4)
            {
                return ability4.abilityCooldownTime - ability4CooldownOffset;
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
                for (int i = 0; i < lastAbility1ActivateTimes.Length; i++)
                {
                    lastAbility1ActivateTimes[i] -= ability1.abilityCooldownTime * percent;
                }
            }
            else if (ability == ability2)
            {
                for (int i = 0; i < lastAbility2ActivateTimes.Length; i++)
                {
                    lastAbility2ActivateTimes[i] -= ability1.abilityCooldownTime * percent;
                }
            }
            else if (ability == ability3)
            {
                for (int i = 0; i < lastAbility3ActivateTimes.Length; i++)
                {
                    lastAbility3ActivateTimes[i] -= ability1.abilityCooldownTime * percent;
                }
            }
            else if (ability == ability4)
            {
                for (int i = 0; i < lastAbility4ActivateTimes.Length; i++)
                {
                    lastAbility4ActivateTimes[i] -= ability1.abilityCooldownTime * percent;
                }
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

        public float dodgeStaminaCost
        {
            get
            {
                if (GetNumberOfDodgesOffCooldown() == lastDodgeActivateTimes.Length)
                {
                    return 10;
                }
                else
                {
                    return 20;
                }
            }
        }

        public void ResetDodgeCooldowns()
        {
            lastDodgeActivateTimes = new float[maxDodgeCount];
            for (int i = 0; i < lastDodgeActivateTimes.Length; i++)
            {
                lastDodgeActivateTimes[i] = Mathf.NegativeInfinity;
            }
        }

        private float dodgeCooldownDuration = 5;

        [Header("Dodge Assignments")]
        [SerializeField] private ActionClip dodgeF;
        [SerializeField] private ActionClip dodgeFL;
        [SerializeField] private ActionClip dodgeFR;
        [SerializeField] private ActionClip dodgeB;
        [SerializeField] private ActionClip dodgeBL;
        [SerializeField] private ActionClip dodgeBR;
        [SerializeField] private ActionClip dodgeL;
        [SerializeField] private ActionClip dodgeR;

        private const int maxDodgeCount = 2;
        private float[] lastDodgeActivateTimes;

        public void StartDodgeCooldown()
        {
            float min = lastDodgeActivateTimes.Min();
            int index = System.Array.FindIndex(lastDodgeActivateTimes, item => Mathf.Approximately(item, min) | item == min);
            if (index == -1)
            {
                Debug.LogWarning("No index for min dodge activate time!");
            }
            else
            {
                lastDodgeActivateTimes[index] = Time.time;
            }
        }

        public bool IsDodgeOnCooldown()
        {
            float timeToConsider = lastDodgeActivateTimes.Min();
            return Time.time - timeToConsider < dodgeCooldownDuration;
        }

        public float GetDodgeCooldownProgress()
        {
            List<float> normalizedCooldownTimes = new List<float>();
            foreach (float lastDodgeActivateTime in lastDodgeActivateTimes)
            {
                normalizedCooldownTimes.Add(Mathf.Clamp((Time.time - lastDodgeActivateTime) / dodgeCooldownDuration, 0, 1));
            }

            if (GetNumberOfDodgesOffCooldown() == lastDodgeActivateTimes.Length)
            {
                return normalizedCooldownTimes.Max();
            }
            else
            {
                return normalizedCooldownTimes.Min();
            }
        }

        public int GetNumberOfDodgesOffCooldown()
        {
            int numDodgesOfCooldown = 0;
            foreach (float lastDodgeActivateTime in lastDodgeActivateTimes)
            {
                if (Time.time - lastDodgeActivateTime >= dodgeCooldownDuration)
                {
                    numDodgesOfCooldown++;
                }
            }
            return numDodgesOfCooldown;
        }

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
        [SerializeField] private ActionClip invinicbleLunge;
        [SerializeField] private ActionClip uninterruptableLunge;

        public ActionClip GetLungeClip(bool isUninterruptable, bool isInvincible)
        {
            if (isInvincible)
            {
                return invinicbleLunge;
            }
            else if (isUninterruptable)
            {
                return uninterruptableLunge;
            }
            else
            {
                return lunge;
            }
        }

        private Dictionary<string, ActionClip> actionClipLookup = new Dictionary<string, ActionClip>();
        private Dictionary<string, AnimationClip> animationClipLookup = new Dictionary<string, AnimationClip>();
        private Dictionary<string, Vector3AnimationCurve> rootMotionLookup = new Dictionary<string, Vector3AnimationCurve>();
        private void Awake()
        {
            actionClipLookup = GetActionClipLookup();
            animationClipLookup = animationClipLookupKeys.Zip(animationClipLookupValues, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);
            rootMotionLookup = animationClipLookupKeys.Zip(animationRootMotion, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);

            ResetDodgeCooldowns();

            ResetAllAbilityCooldowns();
        }

        private Dictionary<string, ActionClip> GetActionClipLookup()
        {
            Dictionary<string, ActionClip> actionClipLookup = new Dictionary<string, ActionClip>();

            if (reloadClip) { actionClipLookup.TryAdd(reloadClip.name, reloadClip); }

            if (dodgeF) { actionClipLookup.TryAdd(dodgeF.name, dodgeF); }
            if (dodgeFL) { actionClipLookup.TryAdd(dodgeFL.name, dodgeFL); }
            if (dodgeFR) { actionClipLookup.TryAdd(dodgeFR.name, dodgeFR); }
            if (dodgeB) { actionClipLookup.TryAdd(dodgeB.name, dodgeB); }
            if (dodgeBL) { actionClipLookup.TryAdd(dodgeBL.name, dodgeBL); }
            if (dodgeBR) { actionClipLookup.TryAdd(dodgeBR.name, dodgeBR); }
            if (dodgeL) { actionClipLookup.TryAdd(dodgeL.name, dodgeL); }
            if (dodgeR) { actionClipLookup.TryAdd(dodgeR.name, dodgeR); }

            if (lunge) { actionClipLookup.TryAdd(lunge.name, lunge); }
            if (invinicbleLunge) { actionClipLookup.TryAdd(invinicbleLunge.name, invinicbleLunge); }
            if (uninterruptableLunge) { actionClipLookup.TryAdd(uninterruptableLunge.name, uninterruptableLunge); }

            if (ability1) { actionClipLookup.TryAdd(ability1.name, ability1); }
            if (ability2) { actionClipLookup.TryAdd(ability2.name, ability2); }
            if (ability3) { actionClipLookup.TryAdd(ability3.name, ability3); }
            if (ability4) { actionClipLookup.TryAdd(ability4.name, ability4); }

            if (flashAttack) { actionClipLookup.TryAdd(flashAttack.name, flashAttack); }

            if (sharedWeaponReferences)
            {
                foreach (SharedWeaponReferences.HitReaction hitReaction in sharedWeaponReferences.HitReactions)
                {
                    if (!hitReaction.reactionClip) { continue; }
                    actionClipLookup.TryAdd(hitReaction.reactionClip.name, hitReaction.reactionClip);
                }

                foreach (SharedWeaponReferences.FlinchReaction flinchReaction in sharedWeaponReferences.FlinchReactions)
                {
                    if (!flinchReaction.reactionClip) { continue; }
                    actionClipLookup.TryAdd(flinchReaction.reactionClip.name, flinchReaction.reactionClip);
                }
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

            return actionClipLookup;
        }

        public ActionClip GetActionClipByName(string clipName)
        {
            if (actionClipLookup.ContainsKey(clipName))
            {
                return actionClipLookup[clipName];
            }
            else
            {
                if (Application.isPlaying)
                {
                    Debug.LogError("Action clip Not Found: " + clipName + " weapon name: " + name);
                    Debug.LogError("Action Clip Lookup Dictionary Count: " + actionClipLookup.Count);
                }
                return null;
            }
        }

        public AnimationClip GetAnimationClip(string animationStateNameWithoutLayer)
        {
            if (animationClipLookup.ContainsKey(animationStateNameWithoutLayer))
            {
                return animationClipLookup[animationStateNameWithoutLayer];
            }
            else
            {
                Debug.LogError("Couldn't find an animation clip with state name: " + animationStateNameWithoutLayer);
                return null;
            }
        }

        public IEnumerable<ActionClip> GetAllActionClips()
        {
            return GetActionClipLookup().Values;
        }

        [System.Serializable]
        public class PreviewActionClip
        {
            public float normalizedTimeToPlayNext;
            public ActionClip actionClip;
        }

        public List<PreviewActionClip> PreviewCombo { get { return previewCombo; } }
        [SerializeField] private List<PreviewActionClip> previewCombo = new List<PreviewActionClip>();

        public float GetMVPAnimationSpeed() { return MVPAnimationSpeed; }
        [SerializeField] private float MVPAnimationSpeed = 1;

        [Header("DO NOT MODIFY, USE THE CONTEXT MENU")]
        [SerializeField] private List<string> animationClipLookupKeys = new List<string>();
        [SerializeField] private List<AnimationClip> animationClipLookupValues = new List<AnimationClip>();
        [SerializeField] private List<Vector3AnimationCurve> animationRootMotion = new List<Vector3AnimationCurve>();

        [System.Serializable]
        public class Vector3AnimationCurve
        {
            public AnimationCurve curveX;
            public AnimationCurve curveY;
            public AnimationCurve curveZ;

            public Vector3AnimationCurve(AnimationCurve curveX, AnimationCurve curveY, AnimationCurve curveZ)
            {
                this.curveX = curveX;
                this.curveY = curveY;
                this.curveZ = curveZ;
            }

            public int Length
            {
                get
                {
                    return Mathf.Max(curveX.length, curveY.length, curveZ.length);
                }
            }

            public Vector3 Evaluate(float t)
            {
                return new Vector3(curveX.Evaluate(t), curveY.Evaluate(t), curveZ.Evaluate(t));
            }

            public Vector3 EvaluateNormalized(float t)
            {
                return new Vector3(curveX.EvaluateNormalizedTime(t), curveY.EvaluateNormalizedTime(t), curveZ.EvaluateNormalizedTime(t));
            }

            public float GetMaxCurveTime(string stateNameForDebugging)
            {
                if (curveX.length == 0 | curveY.length == 0 | curveZ.length == 0) { Debug.LogWarning("Curve doesn't have keys! " + stateNameForDebugging + " " + this); return 0; }

                return Mathf.Max(curveX[curveX.length - 1].time,
                    curveY[curveY.length - 1].time,
                    curveZ[curveZ.length - 1].time);
            }
        }

        public Vector3 GetRootMotion(string stateName, float normalizedTime)
        {
            if (string.IsNullOrWhiteSpace(stateName)) { return Vector3.zero; }

            if (rootMotionLookup.ContainsKey(stateName))
            {
                return rootMotionLookup[stateName].EvaluateNormalized(normalizedTime);
            }
            else
            {
                if (Application.isPlaying)
                {
                    Debug.LogError("Action Clip Not Found: " + stateName + " weapon name: " + name);
                    Debug.LogError("Root Motion Lookup Dictionary Count: " + rootMotionLookup.Count);
                }
                return Vector3.zero;
            }
        }

        public float GetMaxRootMotionTime(string stateName)
        {
            if (string.IsNullOrWhiteSpace(stateName)) { return 0; }

            if (rootMotionLookup.ContainsKey(stateName))
            {
                return rootMotionLookup[stateName].GetMaxCurveTime(stateName);
            }
            else
            {
                if (Application.isPlaying)
                {
                    Debug.LogError("Root Motion State Not Found: " + stateName + " weapon name: " + name);
                    Debug.LogError("Root Motion Lookup Dictionary Count: " + rootMotionLookup.Count);
                }
                return default;
            }
        }

        public void OverrideRootMotionCurvesAtRuntime(string stateName, Vector3AnimationCurve vector3AnimationCurve)
        {
            if (!Application.isPlaying) { Debug.LogError("You shouldn't be setting root motion curves at edit time"); return; }
            if (rootMotionLookup.ContainsKey(stateName))
            {
                rootMotionLookup[stateName] = vector3AnimationCurve;
            }
            else
            {
                Debug.LogError("Can't find state name to override " + stateName + " " + this);
            }
        }

#if UNITY_EDITOR
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
                else if (propertyInfo.FieldType == typeof(List<SharedWeaponReferences.HitReaction>))
                {
                    var HitReactionListObject = propertyInfo.GetValue(this);
                    List<SharedWeaponReferences.HitReaction> hitReactions = (List<SharedWeaponReferences.HitReaction>)HitReactionListObject;

                    foreach (SharedWeaponReferences.HitReaction hitReaction in hitReactions)
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
                else if (propertyInfo.FieldType == typeof(List<SharedWeaponReferences.FlinchReaction>))
                {
                    var FlinchReactionListObject = propertyInfo.GetValue(this);
                    List<SharedWeaponReferences.FlinchReaction> hitReactions = (List<SharedWeaponReferences.FlinchReaction>)FlinchReactionListObject;

                    foreach (SharedWeaponReferences.FlinchReaction flinchReaction in hitReactions)
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

        [ContextMenu("Find Animations")]
        public void FindAnimations()
        {
            CharacterReference characterReference = (CharacterReference)Selection.activeObject;
            foreach (CharacterReference.WeaponOption weaponOption in characterReference.GetWeaponOptions())
            {
                if (weaponOption.weapon == this)
                {
                    animationClipLookupKeys.Clear();
                    animationClipLookupValues.Clear();
                    animationRootMotion.Clear();
                    string path = AssetDatabase.GetAssetPath(weaponOption.animationController.runtimeAnimatorController);

                    List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                    weaponOption.animationController.GetOverrides(overrides);

                    AnimatorController animatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                    foreach (AnimatorControllerLayer layer in animatorController.layers)
                    {
                        if (layer.name != "Actions") { continue; }
                        foreach (ChildAnimatorState state in layer.stateMachine.states)
                        {
                            if (state.state.motion is AnimationClip animationClip)
                            {
                                bool overrideFound = false;
                                foreach (KeyValuePair<AnimationClip, AnimationClip> ovride in overrides)
                                {
                                    if (ovride.Key == animationClip)
                                    {
                                        if (!ovride.Value) { continue; }
                                        overrideFound = true;
                                        animationClipLookupKeys.Add(state.state.name);
                                        animationClipLookupValues.Add(ovride.Value);
                                        animationRootMotion.Add(GetRootMotionCurve(ovride.Value));
                                        break;
                                    }
                                }

                                if (!overrideFound)
                                {
                                    animationClipLookupKeys.Add(state.state.name);
                                    animationClipLookupValues.Add(animationClip);
                                    animationRootMotion.Add(GetRootMotionCurve(animationClip));
                                }
                            }
                        }
                    }
                    break;
                }
            }
            EditorUtility.SetDirty(this);
        }

        public static Vector3AnimationCurve GetRootMotionCurve(AnimationClip clip)
        {
            ModelImporter modelImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip)) as ModelImporter;
            if (modelImporter == null) { return default; }

            float rotationOffset = 0;
            if (modelImporter.clipAnimations != null)
            {
                foreach (ModelImporterClipAnimation modelImporterClipAnimation in modelImporter.clipAnimations)
                {
                    if (clip.name == modelImporterClipAnimation.name)
                    {
                        rotationOffset = modelImporterClipAnimation.rotationOffset;
                    }
                }
            }

            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);

            Dictionary<string, AnimationCurve> curveDictionary = new Dictionary<string, AnimationCurve>()
            {
                { "RootT.x", null },
                { "RootT.y", null },
                { "RootT.z", null },
                { "RootQ.x", null },
                { "RootQ.y", null },
                { "RootQ.z", null }
            };

            foreach (EditorCurveBinding curveBinding in curveBindings)
            {
                if (curveDictionary.ContainsKey(curveBinding.propertyName))
                {
                    curveDictionary[curveBinding.propertyName] = AnimationUtility.GetEditorCurve(clip, curveBinding);
                }
            }

            AnimationCurve curveX = curveDictionary["RootT.x"];
            AnimationCurve curveY = curveDictionary["RootT.y"];
            AnimationCurve curveZ = curveDictionary["RootT.z"];
            RotateAnimationCurves(ref curveX, ref curveY, ref curveZ, Quaternion.Euler(0, -rotationOffset, 0),
                curveDictionary["RootQ.x"], curveDictionary["RootQ.y"], curveDictionary["RootQ.z"]);

            return new Vector3AnimationCurve(curveX, curveY, curveZ);
        }

        private static void RotateAnimationCurves(ref AnimationCurve curveX, ref AnimationCurve curveY, ref AnimationCurve curveZ, Quaternion rotation,
            AnimationCurve rotX, AnimationCurve rotY, AnimationCurve rotZ)
        {
            // Collect all unique times from the three curves
            HashSet<float> timeSet = new HashSet<float>();
            foreach (var key in curveX.keys) timeSet.Add(key.time);
            foreach (var key in curveY.keys) timeSet.Add(key.time);
            foreach (var key in curveZ.keys) timeSet.Add(key.time);

            // Sort the times
            List<float> times = new List<float>(timeSet);
            times.Sort();

            // Create new AnimationCurves for rotated data
            AnimationCurve rotatedCurveX = new AnimationCurve();
            AnimationCurve rotatedCurveY = new AnimationCurve();
            AnimationCurve rotatedCurveZ = new AnimationCurve();

            // Rotate each keyframe at each unique time
            foreach (float time in times)
            {
                // Sample the original curves at the current time
                float originalX = curveX.Evaluate(time);
                float originalY = curveY.Evaluate(time);
                float originalZ = curveZ.Evaluate(time);

                Vector3 originalPosition = new Vector3(originalX, originalY, originalZ);
                //originalPosition = Quaternion.Euler(rotX.Evaluate(time), rotY.Evaluate(time), rotZ.Evaluate(time)) * originalPosition;

                // Apply rotation
                Vector3 rotatedPosition = rotation * originalPosition;

                // Add new keyframes with rotated values
                rotatedCurveX.AddKey(new Keyframe(time, rotatedPosition.x));
                rotatedCurveY.AddKey(new Keyframe(time, rotatedPosition.y));
                rotatedCurveZ.AddKey(new Keyframe(time, rotatedPosition.z));
            }

            // Replace original curves with rotated ones
            curveX = rotatedCurveX;
            curveY = rotatedCurveY;
            curveZ = rotatedCurveZ;
        }
#endif
    }
}