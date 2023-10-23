using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    public class RuntimeWeapon : MonoBehaviour
    {
        protected Dictionary<Attributes, int> hitCounter = new Dictionary<Attributes, int>();

        public void AddHit(Attributes attributes)
        {
            if (!hitCounter.ContainsKey(attributes))
            {
                hitCounter.Add(attributes, 1);
            }
            else
            {
                hitCounter[attributes] += 1;
            }
        }

        public void ResetHitCounter()
        {
            hitCounter.Clear();
        }

        protected Weapon.WeaponBone weaponBone;

        public void SetWeaponBone(Weapon.WeaponBone weaponBone) { this.weaponBone = weaponBone; }

        protected Attributes parentAttributes;
        protected WeaponHandler parentWeaponHandler;

        protected void Start()
        {
            parentAttributes = transform.root.GetComponent<Attributes>();
            parentWeaponHandler = transform.root.GetComponent<WeaponHandler>();
        }
    }
}