using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
            Front,
            Back,
            Left,
            Right
        }

        [System.Serializable]
        private class HitReaction
        {
            public HitLocation hitLocation;
            public ActionClip reactionClip;
        }

        [SerializeField] private List<HitReaction> hitReactions = new List<HitReaction>();

        public ActionClip GetHitReaction(HitLocation hitLocation)
        {
            HitReaction hitReaction = hitReactions.Find(item => item.hitLocation == hitLocation);
            if (hitReaction == null)
            {
                Debug.LogError("Could not find hit reaction for location: " + hitLocation + " for weapon: " + this);
                return null;
            }
            return hitReaction.reactionClip;
        }

        public enum InputAttackType
        {
            LightAttack,
            HeavyAttack
        }

        [SerializeField] private List<ActionClip> lightAttacks = new List<ActionClip>();
        private int lightAttackIndex;
        public ActionClip GetLightAttack(InputAttackType inputAttackType, Animator animator)
        {
            if (animator.IsInTransition(animator.GetLayerIndex("Actions")))
            {
                return null;
            }
            else if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty"))
            {
                if (lightAttackIndex < lightAttacks.Count)
                {
                    if (animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).normalizedTime >= lightAttacks[lightAttackIndex].nextAttackCanBePlayedTime)
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
            if (actionClip == null) { Debug.LogError("No action clip found for " + inputAttackType + " on weapon: " + this); }

            return actionClip;
        }

        [SerializeField] private List<ActionClip> heavyAttacks = new List<ActionClip>();
        private int heavyAttackIndex;
        public ActionClip GetHeavyAttack(InputAttackType inputAttackType, Animator animator)
        {
            if (animator.IsInTransition(animator.GetLayerIndex("Actions")))
            {
                return null;
            }
            else if (!animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).IsName("Empty"))
            {
                if (heavyAttackIndex < heavyAttacks.Count)
                {
                    if (animator.GetCurrentAnimatorStateInfo(animator.GetLayerIndex("Actions")).normalizedTime >= heavyAttacks[heavyAttackIndex].nextAttackCanBePlayedTime)
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
            if (actionClip == null) { Debug.LogError("No action clip found for " + inputAttackType + " on weapon: " + this); }

            return actionClip;
        }
    }
}