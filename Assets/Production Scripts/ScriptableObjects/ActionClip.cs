using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

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
            Ability,
            FlashAttack,
            GrabAttack,
            Lunge,
            Flinch
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
        
        private readonly static ClipType[] attackClipTypes = new ClipType[]
        {
            ClipType.LightAttack,
            ClipType.HeavyAttack,
            ClipType.Ability,
            ClipType.FlashAttack,
            ClipType.GrabAttack
        };

        public bool IsAttack() { return attackClipTypes.Contains(clipType); }

        public FollowUpActionClip[] followUpActionClipsToPlay = new FollowUpActionClip[0];

        [System.Serializable]
        public class FollowUpActionClip
        {
            public float normalizedTimeToPlayClip = 0.5f;
            public ActionClip actionClip;
        }

        [SerializeField] private HitReactionType hitReactionType;
        public HitReactionType GetHitReactionType() { return hitReactionType; }

        public bool shouldApplyRootMotion = true;
        public AnimationCurve rootMotionForwardMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        public AnimationCurve rootMotionSidesMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        public AnimationCurve rootMotionVerticalMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));

        public AnimationCurve attackRootMotionForwardMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        public AnimationCurve attackRootMotionSidesMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        public AnimationCurve attackRootMotionVerticalMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));

        public bool shouldFlinch;
        [SerializeField] private Vector2 flinchAmountMin = new Vector2(-10, -10);
        [SerializeField] private Vector2 flinchAmountMax = new Vector2(10, 10);

        public Vector2 GetFlinchAmount() { return new Vector2(Random.Range(flinchAmountMin.x, flinchAmountMax.x), Random.Range(flinchAmountMin.y, flinchAmountMax.y)); }

        public bool shouldPlayHitReaction = true;
        public AnimationCurve hitReactionRootMotionForwardMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        public AnimationCurve hitReactionRootMotionSidesMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        public AnimationCurve hitReactionRootMotionVerticalMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));

        [SerializeField] private AnimationCurve debugForwardMotion;
        [SerializeField] private AnimationCurve debugSidesMotion;
        [SerializeField] private AnimationCurve debugVerticalMotion;

        public float YAngleRotationOffset = 0;

        public AvatarLayer avatarLayer = AvatarLayer.FullBody;
        public float transitionTime = 0.15f;
        public float dodgeCancelTransitionTime = 0.15f;
        public float animationSpeed = 1;
        public float recoveryAnimationSpeed = 1;

        public float agentStaminaCost = 20;
        public float agentDefenseCost = 0;
        public float agentRageCost = 50;

        public Weapon.WeaponBone[] effectedWeaponBones = new Weapon.WeaponBone[0];
        public Weapon.WeaponBone[] weaponBonesToHide = new Weapon.WeaponBone[0];
        public bool mustBeAiming;

        public bool chargeAttackHasEndAnimation;
        public float chargeAttackStateLoopCount = 1;
        public bool canEnhance = true;
        public float chargeTimeDamageMultiplier = 5;
        public float enhancedChargeDamageMultiplier = 1;
        public float chargePenaltyDamage = 10;

        public const float chargePenaltyTime = 1.0f;
        public const float enhanceChargeTime = 0.20f;
        public const float chargeAttackTime = 0.001f;
        public const float cancelChargeTime = 0.000f;
        public const float chargeAttackStateAnimatorTransitionDuration = 0.25f;

        public float attackingNormalizedTime = 0.25f;
        public float recoveryNormalizedTime = 0.75f;
        public float damage = 20;
        public float healAmount = 0;
        public float staminaDamage = 0;
        public float defenseDamage = 0;
        public float healthPenaltyOnMiss = 0;
        public float staminaPenaltyOnMiss = 0;
        public float defensePenaltyOnMiss = 0;
        public float ragePenaltyOnMiss = 0;
        public int maxHitLimit = 1;
        [SerializeField] private float timeBetweenHits = 1;
        public bool isBlockable = true;
        public bool isUninterruptable;
        public bool isInvincible;
        public bool canFlashAttack;
        public bool isFollowUpAttack;
        public Ailment ailment = Ailment.None;
        public AnimationClip grabAttackClip;
        public AnimationClip grabVictimClip;
        public bool[] ailmentHitDefinition = new bool[0];

        public const float HitStopEffectDuration = 0.1f;
        public float GetTimeBetweenHits()
        {
            return timeBetweenHits + HitStopEffectDuration;
        }

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

        public bool canBeCancelledByLightAttacks;
        public bool canBeCancelledByHeavyAttacks;
        public bool canBeCancelledByAbilities;

        public Sprite abilityImageIcon;
        public float abilityCooldownTime = 5;

        public List<StatusPayload> statusesToApplyToSelfOnActivate = new List<StatusPayload>();
        public List<StatusPayload> statusesToApplyToTargetOnHit = new List<StatusPayload>();
        public List<StatusPayload> statusesToApplyToTeammateOnHit = new List<StatusPayload>();

        public List<ActionVFX> actionVFXList = new List<ActionVFX>();
        public ActionVFX previewActionVFX;
        public Vector3 previewActionVFXScale = new Vector3(1, 1, 1);

        public bool useRotationalTargetingSystem = true;
        public bool limitAttackMotionBasedOnTarget = true;
        public static readonly Vector3 boxCastOriginPositionOffset = new Vector3(0, 0.5f, 0);
        public static readonly Vector3 boxCastHalfExtents = new Vector3(2, 1, 1);
        public const float boxCastDistance = 5;
        public float maximumTargetingRotationAngle = 60;

        public const float maximumRootMotionLimitRotationAngle = 60;

        // Lunge Settings
        public const float maximumLungeAngle = 60;
        public bool canLunge;
        public float minLungeDistance = 2.5f;
        public float maxLungeDistance = 5;

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