using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

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
            Pull,
            Death,
            Grab
        }

        public enum AvatarLayer
        {
            FullBody,
            Aiming
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

        [System.Serializable]
        public struct StatusPayload : INetworkSerializable, System.IEquatable<StatusPayload>
        {
            public Status status;
            public float value;
            public float duration;
            public float delay;

            public StatusPayload(Status status, float value, float duration, float delay)
            {
                this.status = status;
                this.value = value;
                this.duration = duration;
                this.delay = delay;
            }

            public bool Equals(StatusPayload other) { return status == other.status; }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref status);
                serializer.SerializeValue(ref value);
                serializer.SerializeValue(ref duration);
                serializer.SerializeValue(ref delay);
            }
        }

        [SerializeField] private ClipType clipType;
        public ClipType GetClipType() { return clipType; }

        [SerializeField] private HitReactionType hitReactionType;
        public HitReactionType GetHitReactionType() { return hitReactionType; }

        public bool shouldApplyRootMotion = true;
        public AnimationCurve rootMotionForwardMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        public AnimationCurve rootMotionSidesMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        public AnimationCurve rootMotionVerticalMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));

        public AvatarLayer avatarLayer = AvatarLayer.FullBody;
        public float transitionTime = 0.15f;
        public float animationSpeed = 1;

        public float agentStaminaCost = 20;
        public float agentDefenseCost = 0;
        public float agentRageCost = 50;

        public Weapon.WeaponBone[] effectedWeaponBones;
        public bool mustBeAiming;
        public float attackingNormalizedTime = 0.25f;
        public float recoveryNormalizedTime = 0.75f;
        public float damage = 20;
        public float healAmount = 0;
        public float staminaDamage = 0;
        public float defenseDamage = 0;
        public int maxHitLimit = 1;
        public float timeBetweenHits = 1;
        public bool isBlockable = true;
        public bool isUninterruptable;
        public bool isInvincible;
        public Ailment ailment = Ailment.None;
        public float ailmentDuration = 2;
        public float grabDistance = 3;

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

        public List<StatusPayload> statusesToApplyToSelfOnActivate = new List<StatusPayload>();
        public List<StatusPayload> statusesToApplyToTargetOnHit = new List<StatusPayload>();
        public List<StatusPayload> statusesToApplyToTeammateOnHit = new List<StatusPayload>();

        public List<ActionVFX> actionVFXList = new List<ActionVFX>();
        public ActionVFX previewActionVFX;
        public Vector3 previewActionVFXScale = new Vector3(1, 1, 1);

        // Only for shooter characters
        public bool aimDuringAnticipation;
        public bool aimDuringAttack;
        public bool aimDuringRecovery;
        public bool shouldAimBody = true;
        public bool shouldAimOffHand = true;
        public bool requireAmmo;
        public int requiredAmmoAmount = 1;
    }
}