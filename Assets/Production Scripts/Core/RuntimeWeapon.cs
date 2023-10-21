using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    public class RuntimeWeapon : MonoBehaviour
    {
        protected Attributes parentAttributes;
        protected WeaponHandler parentWeaponHandler;

        protected void Start()
        {
            parentAttributes = GetComponentInParent<Attributes>();
            parentWeaponHandler = GetComponentInParent<WeaponHandler>();
        }

        protected ActionClip GetHitReaction(float attackVectorAngle)
        {
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