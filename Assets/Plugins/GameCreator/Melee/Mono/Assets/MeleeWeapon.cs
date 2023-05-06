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
            BackUpper, BackMiddle, BackLower, JuggleFront, JuggleBack, KnockDownFront, KnockDownBack

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

        // 3d model:
        public List<WeaponModel> weaponModels = new List<WeaponModel>();
        public GameObject prefab;
        public WeaponBone attachment = WeaponBone.RightHand;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;

        // audio:
        public AudioClip audioSheathe;
        public AudioClip audioDraw;
        public AudioClip audioImpactNormal;
        public AudioClip audioImpactKnockback;

        // reactions:
        public List<MeleeClip> groundHitReactionsFront = new List<MeleeClip>();
        public List<MeleeClip> groundHitReactionsBehind = new List<MeleeClip>();

        public List<MeleeClip> airborneHitReactionsFront = new List<MeleeClip>();
        public List<MeleeClip> airborneHitReactionsBehind = new List<MeleeClip>();

        public List<MeleeClip> knockbackReaction = new List<MeleeClip>();

        public List<MeleeClip> groundHitReactionsBackMiddle = new List<MeleeClip>();
        public List<MeleeClip> groundHitReactionsLeftMiddle = new List<MeleeClip>();
        public List<MeleeClip> groundHitReactionsRightMiddle = new List<MeleeClip>();

        public MeleeClip dodgeF = new MeleeClip();
        public MeleeClip dodgeFL = new MeleeClip();
        public MeleeClip dodgeFR = new MeleeClip();
        public MeleeClip dodgeB = new MeleeClip();
        public MeleeClip dodgeBL = new MeleeClip();
        public MeleeClip dodgeBR = new MeleeClip();
        public MeleeClip dodgeL = new MeleeClip();
        public MeleeClip dodgeR = new MeleeClip();

        // grabs
        public MeleeClip grabAttack;
        public MeleeClip grabReaction;
        public CharacterState grabReactionState;
        public Vector3 grabPlaceholderPosition;

        // combo system:
        public List<Combo> combos = new List<Combo>();

        // impacts:
        public GameObject prefabImpactNormal;
        public GameObject prefabImpactKnockback;


        // recovery:
        public MeleeClip recoveryStandUp;
        public MeleeClip recoveryStandDown;

        // PRIVATE PROPERTIES: --------------------------------------------------------------------

        private int prevRandomHit = -1;

        // PUBLIC METHODS: ------------------------------------------------------------------------
        
        public List<GameObject> EquipNewWeapon(CharacterAnimator character)
        {
            if (weaponModels.Count == 0) return null;
            var weaponObjectsList = (from item in weaponModels from obj in item.weaponModelDatas select obj).ToList();

            var instances = new List<GameObject>();
        
            
            //if (weaponObjectsList.Count == 0) return null;
            if (character == null) return null;

            foreach (var model in weaponObjectsList)
            {
                GameObject instance = Instantiate(model.prefabWeapon);
                instances.Add(instance);
                instance.transform.localScale = model.prefabWeapon.transform.localScale;
                
                Transform bone = null;
                switch (model.attachmentWeapon)
                {
                    case WeaponBone.Root:
                        bone = character.transform;
                        break;
        
                    case WeaponBone.Camera:
                        bone = HookCamera.Instance.transform;
                        break;
        
                    default:
                        bone = character.animator.GetBoneTransform((HumanBodyBones)model.attachmentWeapon);
                        break;
        
                }
        
                if (!bone) return null;
                instance.transform.SetParent(bone);
                instance.transform.localPosition = model.positionOffsetWeapon;
                instance.transform.localRotation = Quaternion.Euler(model.rotationOffsetWeapon);
            }
            
            return instances;
        }
        
        public GameObject EquipWeapon(CharacterAnimator character)
        {
            if (this.prefab == null) return null;
            if (character == null) return null;
            
            Transform bone = null;
            switch (this.attachment)
            {
                case WeaponBone.Root:
                    bone = character.transform;
                    break;

                case WeaponBone.Camera:
                    bone = HookCamera.Instance.transform;
                    break;

                default:
                    bone = character.animator.GetBoneTransform((HumanBodyBones)this.attachment);
                    break;

            }

            if (!bone) return null;

            GameObject instance = Instantiate(this.prefab);
            instance.transform.localScale = this.prefab.transform.localScale;

            instance.transform.SetParent(bone);

            instance.transform.localPosition = this.positionOffset;
            instance.transform.localRotation = Quaternion.Euler(this.rotationOffset);

            return instance;
        }

        public MeleeClip GetHitReaction(bool isGrounded, HitLocation location, bool isKnockback)
        {
            int index = 0;
            MeleeClip meleeClip = null;

            if (isKnockback)
            {
                index = UnityEngine.Random.Range(0, this.knockbackReaction.Count - 1);
                if (this.knockbackReaction.Count != 1 && index == this.prevRandomHit) index++;
                this.prevRandomHit = index;

                return this.knockbackReaction[index];
            }


            switch(location) {
                case HitLocation.LeftMiddle:
                    index = UnityEngine.Random.Range(0, this.groundHitReactionsLeftMiddle.Count - 1);
                    meleeClip = this.groundHitReactionsLeftMiddle[index];
                    break;

                case HitLocation.RightMiddle:
                    index = UnityEngine.Random.Range(0, this.groundHitReactionsRightMiddle.Count - 1);
                    meleeClip = this.groundHitReactionsRightMiddle[index];
                    break;

                case HitLocation.FrontUpper:
                case HitLocation.FrontMiddle:
                case HitLocation.FrontLower:
                    index = UnityEngine.Random.Range(0, this.groundHitReactionsFront.Count - 1);

                    meleeClip = this.groundHitReactionsFront[index];
                    break;

                case HitLocation.BackMiddle:
                case HitLocation.BackUpper:
                case HitLocation.BackLower:
                    index = UnityEngine.Random.Range(0, this.groundHitReactionsBehind.Count);

                    meleeClip = this.groundHitReactionsBehind[index];
                    break;
                
                default:
                    index = UnityEngine.Random.Range(0, this.groundHitReactionsBehind.Count);

                    meleeClip = this.groundHitReactionsBehind[index];
                    break;
            }

            return meleeClip;
        }
    }
}
