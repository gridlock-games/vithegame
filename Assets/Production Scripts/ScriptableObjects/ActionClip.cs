using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.ScriptableObjects
{
    [CreateAssetMenu(fileName = "ActionClip", menuName = "Production/ActionClip")]
    public class ActionClip : ScriptableObject
    {
        public enum ClipType
        {
            Dodge,
            LightAttack,
            HeavyAttack,
            HitReaction,
            Ability
        }

        public enum HitReactionType
        {
            Normal,
            Blocking,
        }

        public enum Ailment
        {
            None,
            Knockdown,
            Knockup,
            Stun,
            Stagger,
            Pull
        }

        public enum Status
        {
            damageMultiplier,
            damageReductionMultiplier,
            damageReceivedMultiplier,
            healingMultiplier,
            defenseIncreaseMultiplier,
            defenseReductionMultiplier,
            burning,
            poisoned,
            drain,
            movementSpeedDecrease,
            movementSpeedIncrease,
            rooted,
            silenced,
            fear,
            healing
        }

        [SerializeField] private ClipType clipType;
        public ClipType GetClipType() { return clipType; }

        [SerializeField] private HitReactionType hitReactionType;
        public HitReactionType GetHitReactionType() { return hitReactionType; }

        public float rootMotionMulitplier = 1;

        public float agentStaminaCost = 20;
        public float agentDefenseCost = 0;
        public float agentRageCost = 50;

        public Weapon.WeaponBone weaponBone;
        public float attackingNormalizedTime = 0.25f;
        public float recoveryNormalizedTime = 0.75f;
        public float damage = 20;
        public float staminaDamage = 0;
        public float defenseDamage = 0;
        public int maxHitLimit = 1;
        public float timeBetweenHits = 1;
        public bool isBlockable = true;
        public Ailment ailment = Ailment.None;
        public float ailmentDuration = 2;

        public enum DodgeLock
        {
            None,
            EntireAnimation,
            Recovery
        }

        public DodgeLock dodgeLock = DodgeLock.None;

        public bool canCancelLightAttacks;
        public bool canCancelHeavyAttacks;
        public bool canCancelAbilities;

        public Sprite abilityImageIcon;
        public float abilityCooldownTime = 5;
    }
}