using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using System.Linq;

namespace Vi.Core
{
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

        protected Dictionary<CombatAgent, HitCounterData> hitCounter = new Dictionary<CombatAgent, HitCounterData>();

        public Dictionary<CombatAgent, HitCounterData> GetHitCounter()
        {
            Dictionary<CombatAgent, HitCounterData> hitCounter = new Dictionary<CombatAgent, HitCounterData>();
            foreach (RuntimeWeapon runtimeWeapon in associatedRuntimeWeapons)
            {
                foreach (KeyValuePair<CombatAgent, HitCounterData> kvp in runtimeWeapon.hitCounter)
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

        public void AddHit(CombatAgent combatAgent)
        {
            if (!hitCounter.ContainsKey(combatAgent))
            {
                hitCounter.Add(combatAgent, new HitCounterData(1, Time.time));
            }
            else
            {
                hitCounter[combatAgent] = new HitCounterData(hitCounter[combatAgent].hitNumber+1, Time.time);
            }
        }

        public virtual void ResetHitCounter()
        {
            hitCounter.Clear();
        }
        
        public bool CanHit(CombatAgent attributes)
        {
            Dictionary<CombatAgent, HitCounterData> hitCounter = GetHitCounter();
            if (hitCounter.ContainsKey(attributes))
            {
                if (hitCounter[attributes].hitNumber >= parentWeaponHandler.CurrentActionClip.maxHitLimit) { return false; }
                if (Time.time - hitCounter[attributes].timeOfHit < parentWeaponHandler.CurrentActionClip.GetTimeBetweenHits(parentAnimationHandler.Animator.speed)) { return false; }
            }
            return true;
        }

        public Weapon.WeaponBone WeaponBone { get; private set; }

        public void SetWeaponBone(Weapon.WeaponBone weaponBone) { this.WeaponBone = weaponBone; }

        protected CombatAgent parentCombatAgent;
        protected WeaponHandler parentWeaponHandler;
        protected AnimationHandler parentAnimationHandler;

        protected Collider[] colliders;
        private Renderer[] renderers;

        public Vector3 GetClosetPointFromAttributes(CombatAgent victim) { return victim.NetworkCollider.Colliders[0].ClosestPointOnBounds(transform.position); }

        protected void Start()
        {
            parentCombatAgent = GetComponentInParent<CombatAgent>();
            parentWeaponHandler = GetComponentInParent<WeaponHandler>();
            parentAnimationHandler = GetComponentInParent<AnimationHandler>();

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