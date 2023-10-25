using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace Vi.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Weapon", menuName = "Production/Weapon")]
    public class Weapon : ScriptableObject
    {
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

            // Reset combo system
            lightAttackIndex = 0;
            heavyAttackIndex = 0;

            HitReaction hitReaction = null;
            if (isBlocking & attack.isBlockable)
            {
                // Block the attack
                hitReaction = hitReactions.Find(item => (item.hitLocation == hitLocation | item.hitLocation == HitLocation.AllDirections) & item.reactionClip.GetHitReactionType() == ActionClip.HitReactionType.Blocking);
            }
            else if (currentAilment != attackAilment & attackAilment != ActionClip.Ailment.None)
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

        [SerializeField] private ActionClip ability1;
        [SerializeField] private ActionClip ability2;
        [SerializeField] private ActionClip ability3;
        [SerializeField] private ActionClip ability4;

        public ActionClip GetAttack(InputAttackType inputAttackType, Animator animator)
        {
            if (inputAttackType == InputAttackType.LightAttack)
            {
                return GetLightAttack(animator);
            }
            else if (inputAttackType == InputAttackType.HeavyAttack)
            {
                return GetHeavyAttack(animator);
            }
            else if (inputAttackType == InputAttackType.Ability1)
            {
                return ability1;
            }
            else if (inputAttackType == InputAttackType.Ability2)
            {
                return ability2;
            }
            else if (inputAttackType == InputAttackType.Ability3)
            {
                return ability3;
            }
            else if (inputAttackType == InputAttackType.Ability4)
            {
                return ability4;
            }
            else
            {
                Debug.LogError("Trying to get an attack for an inputAttackType that hasn't been implemented! " + inputAttackType);
            }
            return null;
        }

        [SerializeField] private List<ActionClip> lightAttacks = new List<ActionClip>();
        private int lightAttackIndex;
        private ActionClip GetLightAttack(Animator animator)
        {
            if (animator.IsInTransition(animator.GetLayerIndex("Actions")))
            {
                return null;
            }
            else if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty"))
            {
                if (lightAttackIndex < lightAttacks.Count)
                {
                    if (animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).normalizedTime >= lightAttacks[lightAttackIndex].recoveryNormalizedTime)
                    {
                        lightAttackIndex++;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                lightAttackIndex = 0;
            }

            if (lightAttackIndex >= lightAttacks.Count) { return null; }

            ActionClip actionClip = lightAttacks[lightAttackIndex];
            if (actionClip == null) { Debug.LogError("No action clip found for index: " + lightAttackIndex + " on weapon: " + this); }

            return actionClip;
        }

        [SerializeField] private List<ActionClip> heavyAttacks = new List<ActionClip>();
        private int heavyAttackIndex;
        private ActionClip GetHeavyAttack(Animator animator)
        {
            if (animator.IsInTransition(animator.GetLayerIndex("Actions")))
            {
                return null;
            }
            else if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty"))
            {
                if (heavyAttackIndex < heavyAttacks.Count)
                {
                    if (animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).normalizedTime >= heavyAttacks[heavyAttackIndex].recoveryNormalizedTime)
                    {
                        heavyAttackIndex++;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                heavyAttackIndex = 0;
            }

            if (heavyAttackIndex >= heavyAttacks.Count) { return null; }

            ActionClip actionClip = heavyAttacks[heavyAttackIndex];
            if (actionClip == null) { Debug.LogError("No action clip found for index: " + heavyAttackIndex + " on weapon: " + this); }

            return actionClip;
        }

        [Header("Dodge Assignments")]
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

            // Reset combo system
            lightAttackIndex = 0;
            heavyAttackIndex = 0;

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
            }

            Debug.LogError("Melee clip Not Found: " + clipName);
            return null;
        }
    }
}