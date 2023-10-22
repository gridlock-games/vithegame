using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    public class RuntimeWeapon : MonoBehaviour
    {
        protected Weapon.WeaponBone weaponBone;

        public void SetWeaponBone(Weapon.WeaponBone weaponBone) { this.weaponBone = weaponBone; }

        protected Attributes parentAttributes;
        protected WeaponHandler parentWeaponHandler;

        protected void Start()
        {
            parentAttributes = transform.root.GetComponent<Attributes>();
            parentWeaponHandler = transform.root.GetComponent<WeaponHandler>();
        }

        protected ActionClip GetHitReaction(float attackVectorAngle)
        {
            if (!parentWeaponHandler) { return null; }

            Weapon.HitLocation hitLocation;
            if (attackVectorAngle <= 45.00f && attackVectorAngle >= -45.00f)
            {
                hitLocation = Weapon.HitLocation.Front;
            }
            else if (attackVectorAngle > 45.00f && attackVectorAngle < 135.00f)
            {
                hitLocation = Weapon.HitLocation.Right;
            }
            else if (attackVectorAngle < -45.00f && attackVectorAngle > -135.00f)
            {
                hitLocation = Weapon.HitLocation.Left;
            }
            else
            {
                hitLocation = Weapon.HitLocation.Back;
            }

            return parentWeaponHandler.GetWeapon().GetHitReaction(hitLocation);
        }
    }
}