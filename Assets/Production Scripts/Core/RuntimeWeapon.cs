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

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider>(true);

            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.gameObject.layer != LayerMask.NameToLayer("NetworkPrediction"))
                {
                    Debug.LogError(this + " runtime weapons should be in the network prediction layer!");
                }
            }
        }

        [SerializeField] private PooledObject dropWeaponPrefab;
        private PooledObject dropWeaponInstance;

        private ActionClip.Ailment lastAilment = ActionClip.Ailment.None;
        protected virtual void Update()
        {
            if (!parentCombatAgent) { return; }

            if (parentCombatAgent.GetAilment() != lastAilment)
            {
                if (parentCombatAgent.GetAilment() == ActionClip.Ailment.Death)
                {
                    if (dropWeaponPrefab)
                    {
                        dropWeaponInstance = ObjectPoolingManager.SpawnObject(dropWeaponPrefab, transform.position, transform.rotation);
                        if (dropWeaponInstance.TryGetComponent(out Rigidbody rb))
                        {
                            rb.interpolation = parentCombatAgent.IsClient ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
                            rb.collisionDetectionMode = parentCombatAgent.IsServer ? CollisionDetectionMode.Continuous : CollisionDetectionMode.Discrete;
                            NetworkPhysicsSimulation.AddRigidbody(rb);
                        }
                        else
                        {
                            Debug.LogError(dropWeaponInstance + " doesn't have a rigidbody!");
                        }

                        foreach (Renderer renderer in renderers)
                        {
                            renderer.forceRenderingOff = true;
                        }
                    }
                }
                else // Alive
                {
                    if (dropWeaponInstance)
                    {
                        if (dropWeaponInstance.TryGetComponent(out Rigidbody rb))
                        {
                            NetworkPhysicsSimulation.RemoveRigidbody(rb);
                        }
                        else
                        {
                            Debug.LogError(dropWeaponInstance + " doesn't have a rigidbody!");
                        }
                        ObjectPoolingManager.ReturnObjectToPool(ref dropWeaponInstance);
                    }

                    foreach (Renderer renderer in renderers)
                    {
                        renderer.forceRenderingOff = false;
                    }
                }
            }
            lastAilment = parentCombatAgent.GetAilment();
        }

        protected void OnEnable()
        {
            parentCombatAgent = transform.root.GetComponent<CombatAgent>();
            if (!parentCombatAgent) { return; }

            foreach (Collider c in colliders)
            {
                c.enabled = parentCombatAgent.IsServer;
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer is SkinnedMeshRenderer smr)
                {
                    smr.updateWhenOffscreen = parentCombatAgent.IsServer;
                }
                renderer.forceRenderingOff = true;
                renderer.gameObject.layer = LayerMask.NameToLayer(parentCombatAgent.IsSpawned ? "NetworkPrediction" : "Preview");
            }
            StartCoroutine(EnableRenderersAfterOneFrame());
        }

        private IEnumerator EnableRenderersAfterOneFrame()
        {
            yield return null;
            foreach (Renderer renderer in renderers)
            {
                renderer.forceRenderingOff = false;
            }
        }

        protected void OnDisable()
        {
            parentCombatAgent = null;
            isStowed = false;
            associatedRuntimeWeapons.Clear();
            hitCounter.Clear();

            if (dropWeaponInstance)
            {
                if (dropWeaponInstance.TryGetComponent(out Rigidbody rb))
                {
                    NetworkPhysicsSimulation.RemoveRigidbody(rb);
                }
                else
                {
                    Debug.LogError(dropWeaponInstance + " doesn't have a rigidbody!");
                }
                ObjectPoolingManager.ReturnObjectToPool(ref dropWeaponInstance);
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

#if UNITY_EDITOR
        [ContextMenu("Generate Drop Weapon Prefab Variant")]
        public void CreateDropWeaponPrefabVariant()
        {
            if (!GetComponentInChildren<Renderer>()) { return; }

            string variantAssetPath = UnityEditor.AssetDatabase.GetAssetPath(gameObject).Replace(".prefab", "") + "_dropped.prefab";
            if (System.IO.File.Exists(variantAssetPath)) { return; }

            GameObject objSource = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(gameObject);
            foreach (Component component in objSource.GetComponentsInChildren<Component>())
            {
                if (component is not Transform
                    & component is not Renderer)
                {
                    DestroyImmediate(component);
                }
            }

            Renderer renderer = objSource.GetComponentInChildren<Renderer>();
            if (!renderer) { return; }
            objSource.AddComponent<Rigidbody>();
            objSource.AddComponent<PooledObject>();
            BoxCollider boxCollider = objSource.AddComponent<BoxCollider>();
            boxCollider.center = renderer.localBounds.center;
            boxCollider.size = renderer.localBounds.size;

            foreach (Transform child in objSource.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = LayerMask.NameToLayer("Character");
            }

            dropWeaponPrefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(objSource, variantAssetPath).GetComponent<PooledObject>();

            DestroyImmediate(objSource);

            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}