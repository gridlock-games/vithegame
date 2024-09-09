using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Utility;

namespace Vi.Core
{
    [RequireComponent(typeof(PooledObject))]
    public class RuntimeWeapon : MonoBehaviour
    {
        [SerializeField] private Weapon.WeaponMaterial weaponMaterial;

        public Weapon.WeaponMaterial GetWeaponMaterial() { return weaponMaterial; }

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

        protected Dictionary<IHittable, HitCounterData> hitCounter = new Dictionary<IHittable, HitCounterData>();

        public Dictionary<IHittable, HitCounterData> GetHitCounter()
        {
            Dictionary<IHittable, HitCounterData> hitCounter = new Dictionary<IHittable, HitCounterData>();
            foreach (RuntimeWeapon runtimeWeapon in associatedRuntimeWeapons)
            {
                foreach (KeyValuePair<IHittable, HitCounterData> kvp in runtimeWeapon.hitCounter)
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

        public void AddHit(IHittable hittable)
        {
            if (!hitCounter.ContainsKey(hittable))
            {
                hitCounter.Add(hittable, new HitCounterData(1, Time.time));
            }
            else
            {
                hitCounter[hittable] = new HitCounterData(hitCounter[hittable].hitNumber+1, Time.time);
            }
        }

        public virtual void ResetHitCounter()
        {
            hitCounter.Clear();
        }
        
        public bool CanHit(IHittable hittable)
        {
            Dictionary<IHittable, HitCounterData> hitCounter = GetHitCounter();
            if (hitCounter.ContainsKey(hittable))
            {
                if (hitCounter[hittable].hitNumber >= parentCombatAgent.WeaponHandler.CurrentActionClip.maxHitLimit) { return false; }
                if (Time.time - hitCounter[hittable].timeOfHit < parentCombatAgent.WeaponHandler.CurrentActionClip.GetTimeBetweenHits(parentCombatAgent.AnimationHandler.Animator.speed)) { return false; }
            }
            return true;
        }

        public Weapon.WeaponBone WeaponBone { get; private set; }

        public void SetWeaponBone(Weapon.WeaponBone weaponBone) { WeaponBone = weaponBone; }

        protected CombatAgent parentCombatAgent;

        protected Collider[] colliders;
        private Renderer[] renderers;

        public Vector3 GetClosetPointFromAttributes(CombatAgent victim) { return victim.NetworkCollider.Colliders[0].ClosestPointOnBounds(transform.position); }

        protected void OnEnable()
        {
            parentCombatAgent = GetComponentInParent<CombatAgent>();
            if (!parentCombatAgent) { return; }

            colliders = GetComponentsInChildren<Collider>(true);
            foreach (Collider col in colliders)
            {
                col.enabled = parentCombatAgent.IsServer;
            }

            renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer is SkinnedMeshRenderer smr)
                {
                    smr.updateWhenOffscreen = parentCombatAgent.IsServer;
                }
            }
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

        protected void OnDisable()
        {
            isStowed = false;
        }
    }
}