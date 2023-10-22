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

        [SerializeField] private ClipType clipType;
        public ClipType GetClipType() { return clipType; }

        public Weapon.WeaponBone weaponBone;
        public float attackingNormalizedTime = 0.25f;
        public float recoveryNormalizedTime = 0.75f;
        public int maxHitLimit = 1;
    }
}