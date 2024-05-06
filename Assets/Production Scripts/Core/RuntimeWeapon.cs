using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using System.Linq;

namespace Vi.Core
{
    public class RuntimeWeapon : MonoBehaviour
    {
        private List<RuntimeWeapon> associatedRuntimeWeapons = new List<RuntimeWeapon>();
        public void SetAssociatedRuntimeWeapons(List<RuntimeWeapon> runtimeWeapons)
        {
            associatedRuntimeWeapons = runtimeWeapons;
        }

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

        public Dictionary<Attributes, HitCounterData> GetHitCounter()
        {
            Dictionary<Attributes, HitCounterData> hitCounter = new Dictionary<Attributes, HitCounterData>();
            foreach (RuntimeWeapon runtimeWeapon in associatedRuntimeWeapons)
            {
                foreach (KeyValuePair<Attributes, HitCounterData> kvp in runtimeWeapon.hitCounter)
                {
                    if (hitCounter.ContainsKey(kvp.Key))
                    {
                        HitCounterData newData = hitCounter[kvp.Key];
                        if (kvp.Value.timeOfHit > newData.timeOfHit) { newData.timeOfHit = kvp.Value.timeOfHit; }
                        newData.hitNumber += kvp.Value.hitNumber;
                        hitCounter[kvp.Key] = newData;
                    }
                    else
                    {
                        hitCounter.Add(kvp.Key, kvp.Value);
                    }
                }
            }
            return hitCounter;
        }

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
            Dictionary<Attributes, HitCounterData> hitCounter = GetHitCounter();
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

        private Collider[] colliders;
        private Renderer[] renderers;

        protected void Start()
        {
            parentAttributes = transform.root.GetComponent<Attributes>();
            parentWeaponHandler = transform.root.GetComponent<WeaponHandler>();

            colliders = GetComponentsInChildren<Collider>(true);
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        private bool lastIsActiveCall = true;
        public void SetActive(bool isActive)
        {
            if (isActive == lastIsActiveCall) { return; }

            foreach (Collider collider in colliders)
            {
                collider.enabled = isActive;
            }

            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = isActive;
            }

            lastIsActiveCall = isActive;
        }

        protected bool isStowed;
        public void SetIsStowed(bool isStowed)
        {
            this.isStowed = isStowed;
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