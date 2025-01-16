using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Core.MovementHandlers;
using Vi.ProceduralAnimations;
using Vi.Utility;
using System.Linq;
using Vi.Core.CombatAgents;

namespace Vi.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class NetworkCollider : MonoBehaviour
    {
        [SerializeField] private PhysicsMaterial physicMaterial;
        [SerializeField] private Rigidbody staticWallBody;

        public CombatAgent CombatAgent { get; private set; }
        public PhysicsMovementHandler MovementHandler { get; private set; }
        public Collider[] Colliders { get; private set; }

        private Collider[] staticWallColliders = new Collider[0];

        private static Dictionary<int, NetworkCollider> colliderInstanceIDMap = new Dictionary<int, NetworkCollider>();
        private static Dictionary<int, NetworkCollider> staticWallColliderInstanceIDMap = new Dictionary<int, NetworkCollider>();

        private PooledObject parentPooledObject;

        // This is for code executed not on the main thread
        private bool hasStaticWallBody;
        private bool forceUseStaticWallCollisions;

        private float[] originalRadiuses = new float[0];
        private float[] originalStaticRadiuses = new float[0];

        private void Awake()
        {
            MovementHandler = GetComponentInParent<PhysicsMovementHandler>();
            CombatAgent = MovementHandler.GetComponent<CombatAgent>();
            parentPooledObject = MovementHandler.GetComponent<PooledObject>();

            parentPooledObject.OnSpawnFromPool += OnSpawnFromPool;
            parentPooledObject.OnReturnToPool += OnReturnToPool;

            Colliders = GetNetworkColliders().ToArray();
            if (staticWallBody)
            {
                staticWallColliders = staticWallBody.GetComponentsInChildren<Collider>();
            }

            hasStaticWallBody = staticWallBody;

            CombatAgent.SetNetworkCollider(this);

            originalRadiuses = new float[Colliders.Length];

            int i = 0;
            foreach (Collider col in Colliders)
            {
                col.enabled = false;
                colliderInstanceIDMap.Add(col.GetInstanceID(), this);
                col.hasModifiableContacts = true;

                foreach (Collider staticWallCollider in staticWallColliders)
                {
                    Physics.IgnoreCollision(col, staticWallCollider);
                }

                if (col is CapsuleCollider capsuleCollider)
                {
                    originalRadiuses[i] = capsuleCollider.radius;
                }
                else
                {
                    originalRadiuses[i] = 0;
                }

                i++;
            }

            originalStaticRadiuses = new float[staticWallColliders.Length];

            i = 0;
            foreach (Collider staticWallCollider in staticWallColliders)
            {
                staticWallColliderInstanceIDMap.Add(staticWallCollider.GetInstanceID(), this);
                staticWallCollider.hasModifiableContacts = true;

                if (staticWallCollider is CapsuleCollider capsuleCollider)
                {
                    originalStaticRadiuses[i] = capsuleCollider.radius;
                }
                else
                {
                    originalStaticRadiuses[i] = 0;
                }

                i++;
            }
        }

        private List<Collider> GetNetworkColliders()
        {
            List<Collider> networkPredictionLayerColliders = new List<Collider>();
            foreach (Collider col in GetComponentsInChildren<Collider>())
            {
                if (col.gameObject.layer == LayerMask.NameToLayer("NetworkPrediction"))
                {
                    networkPredictionLayerColliders.Add(col);
                }
            }
            return networkPredictionLayerColliders;
        }

        private void OnSpawnFromPool()
        {
            if (staticWallBody)
            {
                NetworkPhysicsSimulation.AddRigidbody(staticWallBody);
                staticWallBody.position = MovementHandler.Rigidbody.position;
                staticWallBody.rotation = MovementHandler.Rigidbody.rotation;
                staticWallBody.transform.SetParent(null, true);
                Physics.ContactModifyEvent += Physics_ContactModifyEvent;
                foreach (Collider col in staticWallColliders)
                {
                    col.enabled = false;
                }
                OnNetworkSpawn();
            }
        }

        public void OnNetworkSpawn()
        {
            if (!CombatAgent.IsSpawned) { return; }
            if (staticWallBody)
            {
                foreach (Collider col in staticWallColliders)
                {
                    // Disable colliders on player hub
                    if (NetSceneManager.Singleton.IsSceneGroupLoaded("Tutorial Room") | NetSceneManager.Singleton.IsSceneGroupLoaded("Training Room"))
                    {
                        col.enabled = true;
                    }
                    else
                    {
                        col.enabled = PlayerDataManager.Singleton.GetGameMode() != PlayerDataManager.GameMode.None;
                    }
                }
            }
        }

        public void OnNetworkDespawn() { }

        public Vector3 GetClosestPoint(Vector3 sourcePosition)
        {
            float minDist = 0;
            Vector3 destinationPoint = sourcePosition;
            for (int i = 0; i < Colliders.Length; i++)
            {
                Vector3 closestPoint = Colliders[i].ClosestPoint(sourcePosition);
                float dist = Vector3.Distance(sourcePosition, closestPoint);
                if (dist < minDist | i == 0)
                {
                    minDist = dist;
                    destinationPoint = closestPoint;
                }
            }
            return destinationPoint;
        }

        private void OnReturnToPool()
        {
            forceUseStaticWallCollisions = default;
            if (staticWallBody)
            {
                NetworkPhysicsSimulation.RemoveRigidbody(staticWallBody);
                staticWallBody.transform.SetParent(transform, true);
                staticWallBody.transform.localPosition = Vector3.zero;
                staticWallBody.transform.localRotation = Quaternion.identity;
                Physics.ContactModifyEvent -= Physics_ContactModifyEvent;
            }
        }

        private void Physics_ContactModifyEvent(PhysicsScene scene, Unity.Collections.NativeArray<ModifiableContactPair> pairs)
        {
            foreach (ModifiableContactPair pair in pairs)
            {
                for (int i = 0; i < pair.contactCount; ++i)
                {
                    colliderInstanceIDMap.TryGetValue(pair.colliderInstanceID, out NetworkCollider col);
                    colliderInstanceIDMap.TryGetValue(pair.otherColliderInstanceID, out NetworkCollider other);

                    // Both colliders are movement colliders
                    if (col & other)
                    {
                        EvaluateContactPairAsMovementCollider(col, other, pair, i);
                    }
                    else if (col) // Col is a movement collider, but other is a static wall
                    {
                        if (staticWallColliderInstanceIDMap.TryGetValue(pair.otherColliderInstanceID, out other))
                        {
                            EvaluateContactPairAsStaticWallCollider(col, other, pair, i);
                        }
                    }
                    else if (other) // Other is a movement collider, but col is a static wall
                    {
                        if (staticWallColliderInstanceIDMap.TryGetValue(pair.colliderInstanceID, out col))
                        {
                            EvaluateContactPairAsStaticWallCollider(col, other, pair, i);
                        }
                    }
                }
            }
        }

        private static void EvaluateContactPairAsMovementCollider(NetworkCollider col, NetworkCollider other, ModifiableContactPair pair, int i)
        {
            //if (pair.GetNormal(i).y > 0.7f)
            //{
            //    pair.IgnoreContact(i);
            //    return;
            //}

            if (col.CombatAgent.AnimationHandler.IsDodging() | other.CombatAgent.AnimationHandler.IsDodging())
            {
                pair.IgnoreContact(i);
            }
            else if (StaticWallsEnabledForThisCollision(col, other))
            {
                pair.IgnoreContact(i);
            }
            else if (PlayerDataManager.Singleton.CanHit(col.CombatAgent, other.CombatAgent))
            {
                if (col.CombatAgent.WeaponHandler.CurrentActionClip.IsAttack() & !col.CombatAgent.AnimationHandler.IsAtRest())
                {
                    pair.SetDynamicFriction(i, 1);
                }
                else if (other.CombatAgent.WeaponHandler.CurrentActionClip.IsAttack() & !other.CombatAgent.AnimationHandler.IsAtRest())
                {
                    pair.SetDynamicFriction(i, 1);
                }
            }
        }
        
        private static void EvaluateContactPairAsStaticWallCollider(NetworkCollider col, NetworkCollider other, ModifiableContactPair pair, int i)
        {
            //if (pair.GetNormal(i).y > 0.7f)
            //{
            //    pair.IgnoreContact(i);
            //    return;
            //}

            if (col.CombatAgent.AnimationHandler.IsDodging() | other.CombatAgent.AnimationHandler.IsDodging())
            {
                pair.IgnoreContact(i);
            }
            else if ((CombatAgent.IgnorePlayerCollisionsDuringAilment(col.CombatAgent.GetAilment()) & !col.CombatAgent.ResetColliderRadiusPredicted)
                | (CombatAgent.IgnorePlayerCollisionsDuringAilment(other.CombatAgent.GetAilment()) & !other.CombatAgent.ResetColliderRadiusPredicted))
            {
                pair.IgnoreContact(i);
            }
            else if (StaticWallsEnabledForThisCollision(col, other))
            {
                if (PlayerDataManager.Singleton.CanHit(col.CombatAgent, other.CombatAgent))
                {
                    if (col.CombatAgent.WeaponHandler.CurrentActionClip.IsAttack() & !col.CombatAgent.AnimationHandler.IsAtRest())
                    {
                        pair.SetDynamicFriction(i, 1);
                    }
                    else if (other.CombatAgent.WeaponHandler.CurrentActionClip.IsAttack() & !other.CombatAgent.AnimationHandler.IsAtRest())
                    {
                        pair.SetDynamicFriction(i, 1);
                    }
                }
            }
            else
            {
                pair.IgnoreContact(i);
            }
        }

        public static bool StaticWallsEnabledForThisCollision(NetworkCollider col, NetworkCollider other)
        {
            if (col.forceUseStaticWallCollisions) { return true; }
            if (other.forceUseStaticWallCollisions) { return true; }

            if (!col.hasStaticWallBody) { return false; }
            if (!other.hasStaticWallBody) { return false; }

            // If player is playing an attack, use static walls for collisions between the target and the attacker
            // Also use static wall collisions when one player is playing a hit reaction
            if (!col.CombatAgent.AnimationHandler.IsAtRest())
            {
                if (col.CombatAgent.WeaponHandler.CurrentActionClip.IsAttack())
                {
                    return true;
                }
                else if (col.CombatAgent.WeaponHandler.CurrentActionClip.GetClipType() == ActionClip.ClipType.HitReaction)
                {
                    return true;
                }
            }

            if (!other.CombatAgent.AnimationHandler.IsAtRest())
            {
                if (other.CombatAgent.WeaponHandler.CurrentActionClip.IsAttack())
                {
                    return true;
                }
                else if (other.CombatAgent.WeaponHandler.CurrentActionClip.GetClipType() == ActionClip.ClipType.HitReaction)
                {
                    return true;
                }
            }

            // If either player is standing still, use static wall collisions
            if (col.MovementHandler.LastMovementWasZero) { return true; }
            if (other.MovementHandler.LastMovementWasZero) { return true; }

            return false;
        }

        private void OnDestroy()
        {
            foreach (Collider c in Colliders)
            {
                colliderInstanceIDMap.Remove(c.GetInstanceID());
            }

            if (staticWallBody)
            {
                foreach (Collider staticWallCollider in staticWallColliders)
                {
                    staticWallColliderInstanceIDMap.Remove(staticWallCollider.GetInstanceID());
                }
                Destroy(staticWallBody.gameObject);
            }
        }

        public void SetOrientation(Vector3 position)
        {
            if (staticWallBody)
            {
                staticWallBody.position = position;
            }
        }

        [SerializeField] private float radiusMultiplier = 1;

        private float lastRadiusMultiplier = 1;
        private void FixedUpdate()
        {
            if (!staticWallBody) { return; }
            staticWallBody.MovePosition(MovementHandler.Rigidbody.position);
            staticWallBody.MoveRotation(MovementHandler.Rigidbody.rotation);

            if (CombatAgent.ResetColliderRadiusPredicted)
            {
                float t = (Time.fixedTime - CombatAgent.lastRecoveryFixedTime) / CombatAgent.recoveryTimeInvincibilityBuffer;
                t = Mathf.Clamp01(t);
                radiusMultiplier = Mathf.Lerp(radiusMultiplier, 1, t);
            }
            else if (CombatAgent.IgnorePlayerCollisionsDuringAilment(CombatAgent.GetAilment()))
            {
                radiusMultiplier = 0.01f;
            }
            else
            {
                float t = (Time.fixedTime - CombatAgent.lastRecoveryFixedTime) / CombatAgent.recoveryTimeInvincibilityBuffer;
                t = Mathf.Clamp01(t);
                radiusMultiplier = Mathf.Lerp(radiusMultiplier, 1, t);
            }

            if (!Mathf.Approximately(lastRadiusMultiplier, radiusMultiplier))
            {
                int i = 0;
                foreach (Collider col in Colliders)
                {
                    if (col is CapsuleCollider capsuleCollider)
                    {
                        capsuleCollider.radius = originalRadiuses[i] * radiusMultiplier;
                    }
                    i++;
                }

                i = 0;
                foreach (Collider staticWallCollider in staticWallColliders)
                {
                    if (staticWallCollider is CapsuleCollider capsuleCollider)
                    {
                        capsuleCollider.radius = originalStaticRadiuses[i] * radiusMultiplier;
                    }
                    i++;
                }
            }
            lastRadiusMultiplier = radiusMultiplier;
        }

        private ActionClip.Ailment lastAilmentEvaluated = ActionClip.Ailment.None;
        private bool lastSpawnState;
        private void Update()
        {
            if (CombatAgent.GetAilment() != lastAilmentEvaluated | lastSpawnState != CombatAgent.IsSpawned)
            {
                foreach (Collider c in Colliders)
                {
                    c.enabled = CombatAgent.GetAilment() != ActionClip.Ailment.Death & CombatAgent.IsSpawned;
                }

                foreach (Collider c in staticWallColliders)
                {
                    if (NetSceneManager.Singleton.IsSceneGroupLoaded("Tutorial Room") | NetSceneManager.Singleton.IsSceneGroupLoaded("Training Room"))
                    {
                        forceUseStaticWallCollisions = false;
                        c.enabled = CombatAgent.GetAilment() != ActionClip.Ailment.Death & CombatAgent.IsSpawned;
                    }
                    else if (PlayerDataManager.Singleton.GetGameMode() != PlayerDataManager.GameMode.None)
                    {
                        forceUseStaticWallCollisions = false;
                        c.enabled = CombatAgent.GetAilment() != ActionClip.Ailment.Death & CombatAgent.IsSpawned;
                    }
                    else // Player hub
                    {
                        forceUseStaticWallCollisions = true;
                        c.enabled = false;
                    }
                }
            }
            lastAilmentEvaluated = CombatAgent.GetAilment();
            lastSpawnState = CombatAgent.IsSpawned;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!MovementHandler) { return; }
            if (collision.transform.root == transform.root) { return; }
            MovementHandler.ReceiveOnCollisionEnterMessage(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (!MovementHandler) { return; }
            if (collision.transform.root == transform.root) { return; }
            MovementHandler.ReceiveOnCollisionStayMessage(collision);
        }

        private void OnCollisionExit(Collision collision)
        {
            if (!MovementHandler) { return; }
            if (collision.transform.root == transform.root) { return; }
            MovementHandler.ReceiveOnCollisionExitMessage(collision);
        }

#if UNITY_EDITOR
        [ContextMenu("Assign Physic Material To Colliders")]
        private void AssignPhysicMaterialToColliders()
        {
            foreach (Collider col in GetNetworkColliders())
            {
                if (col.gameObject.layer == LayerMask.NameToLayer("NetworkPrediction"))
                {
                    col.material = physicMaterial;
                    UnityEditor.EditorUtility.SetDirty(col);
                }
            }
        }

        [ContextMenu("Generate Animation Colliders")]
        private void GenerateAnimationColliders()
        {
            foreach (Collider animCol in transform.root.GetComponentInChildren<Animator>().GetComponentsInChildren<Collider>())
            {
                GameObject g = new GameObject(animCol.name);
                g.layer = LayerMask.NameToLayer("NetworkPrediction");
                g.transform.SetParent(transform, true);
                g.transform.position = animCol.transform.position;
                g.transform.rotation = animCol.transform.rotation;
                g.transform.localScale = Vector3.one;

                FollowTarget followTarget = g.AddComponent<FollowTarget>();
                followTarget.target = animCol.transform;

                if (animCol is BoxCollider animBoxCollider)
                {
                    var boxCollider = g.AddComponent<BoxCollider>();
                    boxCollider.center = animBoxCollider.center;
                    boxCollider.size = animBoxCollider.size;
                }
                else if (animCol is CapsuleCollider animCapsuleCollider)
                {
                    var capsuleCollider = g.AddComponent<CapsuleCollider>();
                    capsuleCollider.center = animCapsuleCollider.center;
                    capsuleCollider.radius = animCapsuleCollider.radius;
                    capsuleCollider.height = animCapsuleCollider.height;
                    capsuleCollider.direction = animCapsuleCollider.direction;
                }
                else if (animCol is SphereCollider animSphereCollider)
                {
                    var sphereCollider = g.AddComponent<SphereCollider>();
                    sphereCollider.center = animSphereCollider.center;
                    sphereCollider.radius = animSphereCollider.radius;
                }
                else
                {
                    DestroyImmediate(g);
                    Debug.LogError("Unsure how to handle anim collider " + animCol);
                }
            }

            AssignPhysicMaterialToColliders();
        }

        [ContextMenu("Generate Static Wall Colliders")]
        private void GenerateStaticWallColliders()
        {
            if (staticWallBody) { Debug.LogWarning("We already have a static wall body!"); return; }
            if (GetNetworkColliders().Count == 0) { Debug.LogWarning("No network colliders to base off of!"); return; }

            if (TryGetComponent(out Rigidbody rb))
            {
                GameObject g = Instantiate(rb.gameObject, rb.transform.parent);
                g.name = "StaticWallCollider";

                DestroyImmediate(g.GetComponent<NetworkCollider>());

                foreach (Transform t in g.GetComponentsInChildren<Transform>())
                {
                    if (t.TryGetComponent(out Collider col))
                    {
                        if (col.isTrigger)
                        {
                            DestroyImmediate(t.gameObject);
                            continue;
                        }
                    }

                    t.gameObject.layer = LayerMask.NameToLayer("AgentCollider");

                    if (!t.TryGetComponent(out Rigidbody staticRb))
                    {
                        staticRb = t.gameObject.AddComponent<Rigidbody>();
                    }

                    staticRb.useGravity = false;
                    staticRb.isKinematic = true;
                    staticRb.constraints = RigidbodyConstraints.None;
                }

                staticWallBody = g.GetComponent<Rigidbody>();
            }
            else
            {
                Debug.LogWarning("No rigidbody to base off of!");
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }
# endif
    }
}