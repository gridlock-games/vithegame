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
            HitReaction
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

        [SerializeField] private ClipType clipType;
        public ClipType GetClipType() { return clipType; }

        [SerializeField] private HitReactionType hitReactionType;
        public HitReactionType GetHitReactionType() { return hitReactionType; }

        public float agentStaminaDamage = 20;

        public Weapon.WeaponBone weaponBone;
        public float attackingNormalizedTime = 0.25f;
        public float recoveryNormalizedTime = 0.75f;
        public float damage = 20;
        public float staminaDamage = 0;
        public float defenseDamage = 0;
        public int maxHitLimit = 1;
        public Ailment ailment = Ailment.None;
    }
}