using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace Vi.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Weapon", menuName = "Production/Weapon")]
    public class Weapon : ScriptableObject
    {
        [SerializeField] private float runSpeed = 5;
        [Header("Health")]
        [SerializeField] private float maxHP = 100;
        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100;
        [SerializeField] private float staminaRecoveryRate = 5;
        [SerializeField] private float staminaDelay = 1;
        [Header("Defense")]
        [SerializeField] private float maxDefense = 100;
        [SerializeField] private float defenseRecoveryRate = 5;
        [SerializeField] private float defenseDelay = 1;
        [Header("Rage")]
        [SerializeField] private float maxRage = 100;
        [SerializeField] private float rageRecoveryRate = 0;

        public float GetRunSpeed() { return runSpeed; }
        public float GetMaxHP() { return maxHP; }
        public float GetMaxStamina() { return maxStamina; }
        public float GetMaxDefense() { return maxDefense; }
        public float GetMaxRage() { return maxRage; }
        public float GetStaminaDelay() { return staminaDelay; }
        public float GetDefenseDelay() { return defenseDelay; }
        public float GetStaminaRecoveryRate() { return staminaRecoveryRate; }
        public float GetDefenseRecoveryRate() { return defenseRecoveryRate; }
        public float GetRageRecoveryRate() { return rageRecoveryRate; }

        [Header("Shooter Weapon Settings")]
        [SerializeField] private bool shouldUseAmmo;
        [SerializeField] private int maxAmmoCount;

        public bool ShouldUseAmmo() { return shouldUseAmmo; }
        public int GetMaxAmmoCount() { return maxAmmoCount; }

        [System.Serializable]
        private class AttackSoundEffect
        {
            public WeaponBone weaponBone = WeaponBone.RightHand;
            public AudioClip attackSoundEffect;
        }

        [Header("Rest Of Settings")]
        [SerializeField] private List<AttackSoundEffect> attackSoundEffects = new List<AttackSoundEffect>();
        public AudioClip GetAttackSoundEffect(WeaponBone weaponBone) { return attackSoundEffects.Find(item => item.weaponBone == weaponBone).attackSoundEffect; }

        public AudioClip drawSoundEffect;
        public AudioClip sheatheSoundEffect;

        [Header("Recieve Hit Effects")]
        public AudioClip hitAudioClip;
        public AudioClip knockbackHitAudioClip;
        public GameObject hitVFXPrefab;

        [Header("Block Effects")]
        public AudioClip blockAudioClip;
        public GameObject blockVFXPrefab;

        public enum WeaponBone
        {
            Root = -1,
            RightHand = HumanBodyBones.RightHand,
            LeftHand = HumanBodyBones.LeftHand,
            RightArm = HumanBodyBones.RightLowerArm,
            LeftArm = HumanBodyBones.LeftLowerArm,
            RightFoot = HumanBodyBones.RightFoot,
            LeftFoot = HumanBodyBones.LeftFoot,
            Camera = 100,
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
            
            if (hitReaction == null) // If attack isn't blockable
            {
                if (currentAilment != attackAilment & attackAilment != ActionClip.Ailment.None)
                {
                    // Find the start reaction for the attack's ailment
                    hitReaction = hitReactions.Find(item => (item.hitLocation == hitLocation | item.hitLocation == HitLocation.AllDirections) & item.reactionClip.ailment == attackAilment & !item.shouldAlreadyHaveAilment);
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
                Debug.LogError("Could not find hit reaction for location: " + hitLocation + " for weapon: " + this + " ailment: " + attackAilment + " blocking: " + isBlocking);
                return null;
            }

            return hitReaction.reactionClip;
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

        [Header("Dodge Assignments")]
        public float dodgeStaminaCost = 20;
        [SerializeField] private ActionClip dodgeF;
        [SerializeField] private ActionClip dodgeFL;
        [SerializeField] private ActionClip dodgeFR;
        [SerializeField] private ActionClip dodgeB;
        [SerializeField] private ActionClip dodgeBL;
        [SerializeField] private ActionClip dodgeBR;
        [SerializeField] private ActionClip dodgeL;
        [SerializeField] private ActionClip dodgeR;

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

        public ActionClip GetActionClipByName(string clipName)
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
                        }
                    }
                }
            }

            Debug.LogError("Action clip Not Found: " + clipName);
            return null;
        }
    }
}