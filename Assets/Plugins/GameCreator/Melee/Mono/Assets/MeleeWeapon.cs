using System.Linq;

namespace GameCreator.Melee
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using GameCreator.Characters;
    using GameCreator.Core;
    using GameCreator.Localization;
    using GameCreator.Core.Hooks;

    [CreateAssetMenu(fileName = "Melee Weapon", menuName = "Game Creator/Melee/Melee Weapon")]
    public class MeleeWeapon : ScriptableObject
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

        public enum HitLocation
        {
            FrontUpper, FrontMiddle, FrontLower, LeftMiddle, RightMiddle,
            BackUpper, BackMiddle, BackLower, JuggleFront, JuggleBack, KnockDownFront, KnockUpFront

        }

        public const CharacterAnimation.Layer LAYER_STANCE = CharacterAnimation.Layer.Layer1;

        // PROPERTIES: ----------------------------------------------------------------------------

        // general:
        [LocStringNoPostProcess] public LocString weaponName = new LocString("Weapon Name");
        [LocStringNoPostProcess] public LocString weaponDescription = new LocString("Weapon Description");

        public string meleeWeaponName;

        public MeleeShield defaultShield;
        public CharacterState characterState;
        public AvatarMask characterMask;
        public Sprite weaponImage;

        [Serializable]
        public class WeaponModelData
        {
            public LimbReferences skinPrefab;
            public Data[] data;

            [Serializable]
            public class Data
            {
                public GameObject prefabWeapon;
                public WeaponBone attachmentWeapon = WeaponBone.RightHand;
                public Vector3 positionOffsetWeapon;
                public Vector3 rotationOffsetWeapon;
            }
        }

        // 3d model:
        public List<WeaponModelData> weaponModelData = new List<WeaponModelData>();

        // audio:
        public AudioClip audioSheathe;
        public AudioClip audioDraw;
        public AudioClip audioImpactNormal;
        public AudioClip audioImpactKnockback;
        public AudioClip audioSwing;

        // reactions:
        public List<MeleeClip> groundHitReactionsFront = new List<MeleeClip>();
        public List<MeleeClip> groundHitReactionsBehind = new List<MeleeClip>();

        public List<MeleeClip> airborneHitReactionsFront = new List<MeleeClip>();
        public List<MeleeClip> airborneHitReactionsBehind = new List<MeleeClip>();

        public List<MeleeClip> knockbackReaction = new List<MeleeClip>();
        public List<MeleeClip> knockupReaction = new List<MeleeClip>();

        public List<MeleeClip> groundHitReactionsBackMiddle = new List<MeleeClip>();
        public List<MeleeClip> groundHitReactionsLeftMiddle = new List<MeleeClip>();
        public List<MeleeClip> groundHitReactionsRightMiddle = new List<MeleeClip>();

        public MeleeClip dodgeF;
        public MeleeClip dodgeFL;
        public MeleeClip dodgeFR;
        public MeleeClip dodgeB;
        public MeleeClip dodgeBL;
        public MeleeClip dodgeBR;
        public MeleeClip dodgeL;
        public MeleeClip dodgeR;

        // grabs
        public MeleeClip grabAttack;
        public MeleeClip grabReaction;
        public Vector3 grabPlaceholderPosition;

        // knockdown

        public MeleeClip knockbackF;
        public MeleeClip knockupF;
        public MeleeClip staggerF;

        // combo system:
        public List<Combo> combos = new List<Combo>();

        // impacts:
        public GameObject prefabImpactNormal;
        public GameObject prefabImpactHit;
        public GameObject prefabImpactKnockback;

        // recovery:
        public MeleeClip recoveryStandUp; // Facing Up
        public MeleeClip recoveryStandDown; // facing Down
        public MeleeClip recoveryStun; // Stand up from stun

        // PRIVATE PROPERTIES: --------------------------------------------------------------------

        private int prevRandomHit = -1;

        // PUBLIC METHODS: ------------------------------------------------------------------------
        public List<GameObject> EquipNewWeapon(CharacterAnimator character)
        {
            List<GameObject> instances = new List<GameObject>();

            bool broken = false;
            foreach (WeaponModelData data in weaponModelData)
            {
                if (data.skinPrefab.name == character.GetComponentInChildren<LimbReferences>().name)
                {
                    foreach (WeaponModelData.Data modelData in data.data)
                    {
                        GameObject instance = Instantiate(modelData.prefabWeapon);
                        instances.Add(instance);
                        instance.transform.localScale = modelData.prefabWeapon.transform.localScale;

                        Transform bone = null;
                        switch (modelData.attachmentWeapon)
                        {
                            case WeaponBone.Root:
                                bone = character.transform;
                                break;
                            case WeaponBone.Camera:
                                bone = HookCamera.Instance.transform;
                                break;
                            default:
                                bone = character.animator.GetBoneTransform((HumanBodyBones)modelData.attachmentWeapon);
                                break;
                        }

                        instance.transform.SetParent(bone);
                        instance.transform.localPosition = modelData.positionOffsetWeapon;
                        instance.transform.localRotation = Quaternion.Euler(modelData.rotationOffsetWeapon);
                    }
                    broken = true;
                    break;
                }
            }

            //if (!broken)
            //{
            //    Debug.LogError("Could not find a weapon model data element for this skin: " + character.GetComponentInChildren<LimbReferences>().name + " on this melee weapon: " + this);
            //}

            return instances;
        }
        
        public MeleeClip GetHitReaction(bool isGrounded, HitLocation location, bool isKnockback, bool isKnockup, bool isPulled)
        {
            int index = 0;
            MeleeClip meleeClip = null;

            if (isKnockup)
            {
                index = UnityEngine.Random.Range(0, 2);
                if (this.knockupReaction.Count != 1 && index == this.prevRandomHit) index++;
                this.prevRandomHit = index;

                switch (location)
                {
                    case HitLocation.RightMiddle:
                        return this.knockupReaction[4];

                    case HitLocation.LeftMiddle:
                        return this.knockupReaction[3];

                    case HitLocation.BackMiddle:
                        return this.knockupReaction[5];

                    default:
                        return this.knockupReaction[index];
                }
            }

            if (isKnockback)
            {
                index = UnityEngine.Random.Range(0, this.knockbackReaction.Count - 1);
                if (this.knockbackReaction.Count != 1 && index == this.prevRandomHit) index++;
                this.prevRandomHit = index;

                return this.knockbackReaction[index];
            }

            switch (location)
            {
                case HitLocation.LeftMiddle:
                    index = UnityEngine.Random.Range(0, this.groundHitReactionsLeftMiddle.Count - 1);
                    meleeClip = isPulled ? this.groundHitReactionsRightMiddle[0] : this.groundHitReactionsLeftMiddle[index];
                    break;

                case HitLocation.RightMiddle:
                    index = UnityEngine.Random.Range(0, this.groundHitReactionsRightMiddle.Count - 1);
                    meleeClip = isPulled ? this.groundHitReactionsLeftMiddle[0] : this.groundHitReactionsRightMiddle[index];
                    break;

                case HitLocation.FrontUpper:
                case HitLocation.FrontMiddle:
                case HitLocation.FrontLower:
                    index = UnityEngine.Random.Range(0, this.groundHitReactionsFront.Count - 1);
                    meleeClip = isPulled ? this.groundHitReactionsBehind[0] : this.groundHitReactionsFront[index];
                    break;

                case HitLocation.BackMiddle:
                case HitLocation.BackUpper:
                case HitLocation.BackLower:
                    index = UnityEngine.Random.Range(0, this.groundHitReactionsBehind.Count);
                    meleeClip = isPulled ? this.groundHitReactionsFront[0] : this.groundHitReactionsBehind[index];
                    break;

                default:
                    index = UnityEngine.Random.Range(0, this.groundHitReactionsBehind.Count);
                    meleeClip = isPulled ? this.groundHitReactionsFront[0] : this.groundHitReactionsBehind[index];
                    break;
            }

            meleeClip.movementMultiplier = isPulled ? 1.50f: 0.5f;
            return meleeClip;
        }
    }
}
