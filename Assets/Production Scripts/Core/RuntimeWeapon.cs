using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    public class RuntimeWeapon : MonoBehaviour
    {
        public struct HitCounterData
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

        public Dictionary<Attributes, HitCounterData> GetHitCounter() { return hitCounter; }

        public void AddHit(Attributes attributes)
        {
            if (!hitCounter.ContainsKey(attributes))
            {
                hitCounter.Add(attributes, new HitCounterData(1, Time.time));
            }
            else
            {
                hitCounter[attributes] = new HitCounterData(hitCounter[attributes].hitNumber+1, Time.time);
            }
        }

        public virtual void ResetHitCounter()
        {
            hitCounter.Clear();
        }
        
        public bool CanHit(Attributes attributes)
        {
            if (hitCounter.ContainsKey(attributes))
            {
                if (hitCounter[attributes].hitNumber >= parentWeaponHandler.CurrentActionClip.maxHitLimit) { return false; }
                if (Time.time - hitCounter[attributes].timeOfHit < parentWeaponHandler.CurrentActionClip.GetTimeBetweenHits()) { return false; }
            }
            return true;
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

        //private void OnEnable()
        //{
        //    AudioManager.Singleton.PlayClipAtPoint(parentWeaponHandler.GetWeapon().drawSoundEffect, transform.position, 0.5f);
        //}

        //private void OnDisable()
        //{
        //    AudioManager.Singleton.PlayClipAtPoint(parentWeaponHandler.GetWeapon().sheatheSoundEffect, transform.position, 0.5f);
        //}
    }
}