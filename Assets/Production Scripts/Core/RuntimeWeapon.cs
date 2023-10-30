using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    public class RuntimeWeapon : MonoBehaviour
    {
        protected struct HitCounterData
        {
            public int hitNumber;
            public float timeOfHit;

            public HitCounterData(int hitNumber, float timeOfHit)
            {
                this.hitNumber = hitNumber;
                this.timeOfHit = timeOfHit;
            }
        }

        protected Dictionary<Attributes, HitCounterData> hitCounter = new Dictionary<Attributes, HitCounterData>();

        public void AddHit(Attributes attributes)
        {
            if (!hitCounter.ContainsKey(attributes))
            {
                hitCounter.Add(attributes, new HitCounterData(1, Time.time));
            }
            else
            {
                hitCounter[attributes] = new HitCounterData(hitCounter[attributes].hitNumber, Time.time);
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