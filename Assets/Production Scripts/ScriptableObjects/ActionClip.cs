using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using Vi.Utility;

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
            Flinch,
            Reload
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
            Grab,
            Incapacitated
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
            armorIncreaseMultiplier,
            armorReductionMultiplier,
            burning,
            poisoned,
            drain,
            movementSpeedDecrease,
            movementSpeedIncrease,
            rooted,
            silenced,
            fear,
            healing,
            immuneToGroundSpells,
            immuneToAilments,
            immuneToNegativeStatuses,
            armorRegeneration,
            staminaRegeneration,
            attackSpeedDecrease,
            attackSpeedIncrease,
            abilityCooldownDecrease,
            abilityCooldownIncrease,
            bleed
        }

        [System.Serializable]
        public struct StatusPayload : INetworkSerializable, System.IEquatable<StatusPayload>
        {
            public Status status;
            public float value;
            public bool valueIsPercentage;
            public float duration;
            public float delay;
            public bool associatedWithCurrentWeapon;

            public StatusPayload(Status status, float value, bool valueIsPercentage, float duration, float delay, bool associatedWithCurrentWeapon)
            {
                this.status = status;
                this.value = value;
                this.valueIsPercentage = valueIsPercentage;
                this.duration = duration;
                this.delay = delay;
                this.associatedWithCurrentWeapon = associatedWithCurrentWeapon;
            }

            public bool Equals(StatusPayload other) { return status == other.status; }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref status);
                serializer.SerializeValue(ref value);
                serializer.SerializeValue(ref duration);
                serializer.SerializeValue(ref delay);
                serializer.SerializeValue(ref associatedWithCurrentWeapon);
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
        public bool shouldIgnoreGravity;

        [SerializeField] private AnimationCurve rootMotionForwardMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        [SerializeField] private AnimationCurve rootMotionSidesMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        [SerializeField] private AnimationCurve rootMotionVerticalMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));

        public AnimationCurve GetRootMotionForwardMultiplier() { return rootMotionForwardMultiplier; }
        public AnimationCurve GetRootMotionSidesMultiplier() { return rootMotionSidesMultiplier; }
        public AnimationCurve GetRootMotionVerticalMultiplier() { return rootMotionVerticalMultiplier; }

        [SerializeField] private AnimationCurve attackRootMotionForwardMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        [SerializeField] private AnimationCurve attackRootMotionSidesMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        [SerializeField] private AnimationCurve attackRootMotionVerticalMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));

        public bool shouldFlinch;
        [SerializeField] private Vector2 flinchAmountMin = new Vector2(-10, -10);
        [SerializeField] private Vector2 flinchAmountMax = new Vector2(10, 10);

        public Vector2 GetFlinchAmount()
        {
            if (!IsAttack()) { Debug.LogError("You should only be getting flinch amount on an attack clip!"); return Vector2.zero; }
            return new Vector2(Random.Range(flinchAmountMin.x, flinchAmountMax.x), Random.Range(flinchAmountMin.y, flinchAmountMax.y));
        }

        public bool shouldPlayHitReaction = true;
        [SerializeField] private AnimationCurve hitReactionRootMotionForwardMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        [SerializeField] private AnimationCurve hitReactionRootMotionSidesMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        [SerializeField] private AnimationCurve hitReactionRootMotionVerticalMultiplier = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));

        public AnimationCurve GetHitReactionRootMotionForwardMultiplier() { if (GetClipType() != ClipType.HitReaction) { Debug.LogError("Should only get hit reaction mutliplier curves on hit reaction clips " + this); } return hitReactionRootMotionForwardMultiplier; }
        public AnimationCurve GetHitReactionRootMotionSidesMultiplier() { if (GetClipType() != ClipType.HitReaction) { Debug.LogError("Should only get hit reaction mutliplier curves on hit reaction clips " + this); } return hitReactionRootMotionSidesMultiplier; }
        public AnimationCurve GetHitReactionRootMotionVerticalMultiplier() { if (GetClipType() != ClipType.HitReaction) { Debug.LogError("Should only get hit reaction mutliplier curves on hit reaction clips " + this); } return hitReactionRootMotionVerticalMultiplier; }

        public void SetHitReactionRootMotionMultipliers(ActionClip attackClip)
        {
            // TODO This needs to be networked and also needs to not use a shared instance of an action clip

            //if (!attackClip.IsAttack()) { Debug.LogError("ActionClip.SetHitReactionRootMotionMultipliers should only be called using an attack clip! " + attackClip + " " + attackClip.GetClipType()); return; }
            //if (GetClipType() != ClipType.HitReaction) { Debug.LogError("ActionClip.SetHitReactionRootMotionMultipliers should only be called on a hit reaction!"); return; }

            //hitReactionRootMotionForwardMultiplier = attackClip.attackRootMotionForwardMultiplier;
            //hitReactionRootMotionSidesMultiplier = attackClip.attackRootMotionSidesMultiplier;
            //hitReactionRootMotionVerticalMultiplier = attackClip.attackRootMotionVerticalMultiplier;
        }

        public bool IsMotionPredicted(bool isAtRest)
        {
            if (!isAtRest) { return false; }
            return GetClipType() == ClipType.Dodge;
        }

        [SerializeField] private AnimationCurve debugForwardMotion;
        [SerializeField] private AnimationCurve debugSidesMotion;
        [SerializeField] private AnimationCurve debugVerticalMotion;

        public float XAngleRotationOffset = 0;
        public float YAngleRotationOffset = 0;
        public float ZAngleRotationOffset = 0;

        public AvatarLayer avatarLayer = AvatarLayer.FullBody;
        public float transitionTime = 0.15f;
        public float dodgeCancelTransitionTime = 0.15f;
        public float rootMotionTruncateOffset = 0.15f;
        public float truncatedTransitionOutTime = 0.15f;
        public float animationSpeed = 1;
        public float recoveryAnimationSpeed = 1;

        public float agentStaminaCost = 20;
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
        public ActionVFX chargeAttackChargingVFX;

        public const float chargePenaltyTime = 1.0f;
        public const float enhanceChargeTime = 0.20f;
        public const float chargeAttackTime = 0.001f;
        public const float cancelChargeTime = 0.000f;
        public const float chargeAttackStateAnimatorTransitionDuration = 0.15f;

        public float attackingNormalizedTime = 0.25f;
        public float recoveryNormalizedTime = 0.75f;
        public bool isAffectedByRage = true;
        public float damage = 20;
        public float healAmount = 0;
        public float staminaDamage = 0;
        public float healthPenaltyOnMiss = 0;
        public float staminaPenaltyOnMiss = 0;
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
        public Weapon.Vector3AnimationCurve grabAttackRootMotionData;
        public AnimationClip grabVictimClip;
        public Weapon.Vector3AnimationCurve grabVictimRootMotionData;
        public bool[] ailmentHitDefinition = new bool[0];

#if UNITY_EDITOR
        [ContextMenu("Serialize Grab Root Motion Data")]
        private void SerializeGrabRootMotionData()
        {
            if (grabAttackClip)
            {
                grabAttackRootMotionData = Weapon.GetRootMotionCurve(grabAttackClip);
            }
            
            if (grabVictimClip)
            {
                grabVictimRootMotionData = Weapon.GetRootMotionCurve(grabVictimClip);
            }
        }
#endif

        public const float HitStopEffectDuration = 0.1f;
        public float GetTimeBetweenHits(float animatorSpeed)
        {
            return animatorSpeed > 0 ? (timeBetweenHits + HitStopEffectDuration) / animatorSpeed : timeBetweenHits + HitStopEffectDuration;
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
        public float abilityBufferTime = 0;

        [System.Serializable]
        private class ActionClipSoundEffect
        {
            public CharacterReference.RaceAndGender[] raceAndGenders = new CharacterReference.RaceAndGender[] { CharacterReference.RaceAndGender.Universal };
            public AudioClip[] audioClips;
            public float normalizedPlayTime;
        }

        [SerializeField] private List<ActionClipSoundEffect> actionClipSoundEffects = new List<ActionClipSoundEffect>();

        public struct ActionSoundEffect
        {
            public int id;
            public AudioClip audioClip;
            public float normalizedPlayTime;

            public ActionSoundEffect(int id, AudioClip audioClip, float normalizedPlayTime)
            {
                this.id = id;
                this.audioClip = audioClip;
                this.normalizedPlayTime = normalizedPlayTime;
            }
        }

        public List<ActionSoundEffect> GetActionClipSoundEffects(CharacterReference.RaceAndGender raceAndGender, List<int> idsToExclude)
        {
            List<ActionClipSoundEffect> filteredEffects = actionClipSoundEffects.FindAll(item => item.raceAndGenders.Contains(raceAndGender));
            List<ActionSoundEffect> returnList = new List<ActionSoundEffect>();
            int i = -1;
            foreach (ActionClipSoundEffect actionClipSoundEffect in actionClipSoundEffects.Where(item => item.raceAndGenders.Contains(raceAndGender)))
            {
                i++;
                if (idsToExclude.Contains(i)) { continue; }

                returnList.Add(new ActionSoundEffect(i,
                    actionClipSoundEffect.audioClips[Random.Range(0, actionClipSoundEffect.audioClips.Length)],
                    actionClipSoundEffect.normalizedPlayTime));
            }
            return returnList;
        }

        public const float actionClipSoundEffectVolume = 1;

        public List<StatusPayload> statusesToApplyToSelfOnActivate = new List<StatusPayload>();
        public List<StatusPayload> statusesToApplyToTargetOnHit = new List<StatusPayload>();
        public List<StatusPayload> statusesToApplyToTeammateOnHit = new List<StatusPayload>();

        public List<ActionVFX> actionVFXList = new List<ActionVFX>();
        public PooledObject previewActionVFX;
        public Vector3 previewActionVFXPositionOffset = new Vector3(0, 0, 0);
        public Vector3 previewActionVFXRotationOffset = new Vector3(0, 0, 0);
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

        // Only for melee weapons
        public Vector3 bladeSizeMultiplier = new Vector3(1, 1, 1);

        // Only for shooter weapons
        public bool aimDuringAnticipation;
        public bool aimDuringAttack;
        public bool aimDuringRecovery;
        public bool shouldAimBody = true;
        public bool shouldAimOffHand = true;
        public bool requireAmmo;
        public int requiredAmmoAmount = 1;
        public float reloadNormalizedTime = 0.5f;

        public NetworkObject[] summonables = new NetworkObject[0];
        public float normalizedSummonTime = 0.5f;
        public Vector3 summonPositionOffset = new Vector3(0, 0, 2);
        public int summonableCount = 0;

        public const int maxLivingSummonables = 3;

        public bool IsRangedAttack()
        {
            if (!IsAttack()) { return false; }

            if (mustBeAiming) { return true; }
            if (aimDuringAnticipation) { return true; }
            if (aimDuringAttack) { return true; }
            if (aimDuringRecovery) { return true; }
            if (requireAmmo) { return true; }
            if (avatarLayer == AvatarLayer.Aiming) { return true; }

            // This would mean only VFX is used for hits
            if (maxHitLimit == 0) { return true; }
            if (effectedWeaponBones == null) { return true; }
            if (effectedWeaponBones.Length == 0) { return true; }

            return false;
        }
    }
}