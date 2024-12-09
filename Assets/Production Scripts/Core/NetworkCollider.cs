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

            CombatAgent.SetNetworkCollider(this);
            
            foreach (Collider col in Colliders)
            {
                col.enabled = false;
                colliderInstanceIDMap.Add(col.GetInstanceID(), this);
                col.hasModifiableContacts = true;

                foreach (Collider staticWallCollider in staticWallColliders)
                {
                    Physics.IgnoreCollision(col, staticWallCollider);
                }
            }

            foreach (Collider staticWallCollider in staticWallColliders)
            {
                staticWallColliderInstanceIDMap.Add(staticWallCollider.GetInstanceID(), this);
                staticWallCollider.hasModifiableContacts = true;
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

        private void OnReturnToPool()
        {
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
                        EvaluateContactPairAsMovementCollider(pair, i, other);
                    }
                    else if (col) // Col is a movement collider, but other is a static wall
                    {
                        if (staticWallColliderInstanceIDMap.TryGetValue(pair.otherColliderInstanceID, out other))
                        {
                            EvaluateContactPairAsStaticWallCollider(pair, i, other);
                        }
                    }
                    else if (other) // Other is a movement collider, but col is a static wall
                    {
                        if (staticWallColliderInstanceIDMap.TryGetValue(pair.colliderInstanceID, out col))
                        {
                            EvaluateContactPairAsStaticWallCollider(pair, i, col);
                        }
                    }
                }
            }
        }

        private void EvaluateContactPairAsMovementCollider(ModifiableContactPair pair, int i, NetworkCollider other)
        {
            //pair.IgnoreContact(i);
            if (ShouldApplyRecoveryDodgeLogic())
            {
                pair.IgnoreContact(i);
            }
            else if (StaticWallsEnabledForThisCollision(other))
            {
                pair.IgnoreContact(i);
            }
        }

        private void EvaluateContactPairAsStaticWallCollider(ModifiableContactPair pair, int i, NetworkCollider other)
        {
            // Phase through other players if we are dodging out of an ailment like knockdown
            if (ShouldApplyRecoveryDodgeLogic())
            {
                pair.IgnoreContact(i);
            }
            else if (StaticWallsEnabledForThisCollision(other))
            {
                if (CombatAgent.WeaponHandler.CurrentActionClip.IsAttack())
                {
                    if (PlayerDataManager.Singleton.CanHit(CombatAgent, other.CombatAgent))
                    {
                        if (!CombatAgent.AnimationHandler.IsAtRest())
                        {
                            pair.SetDynamicFriction(i, 1);
                        }
                    }
                }
            }
            else
            {
                pair.IgnoreContact(i);
            }
        }

        public bool StaticWallsEnabledForThisCollision(NetworkCollider other)
        {
            // If either player is standing still or not at rest, return true
            if (!CombatAgent.AnimationHandler.IsAtRest()) { return true; }
            if (!other.CombatAgent.AnimationHandler.IsAtRest()) { return true; }

            if (MovementHandler.LastMovementWasZero) { return true; }
            if (other.MovementHandler.LastMovementWasZero) { return true; }

            return false;
        }

        private bool ShouldApplyRecoveryDodgeLogic()
        {
            return CombatAgent.CanRecoveryDodge
                & CombatAgent.WeaponHandler.CurrentActionClip.GetClipType() == ActionClip.ClipType.Dodge
                & !CombatAgent.AnimationHandler.IsAtRest();
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

        private void FixedUpdate()
        {
            if (!staticWallBody) { return; }
            staticWallBody.MovePosition(MovementHandler.Rigidbody.position);
            staticWallBody.MoveRotation(MovementHandler.Rigidbody.rotation);
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
                        c.enabled = CombatAgent.GetAilment() != ActionClip.Ailment.Death & CombatAgent.IsSpawned;
                    }
                    else if (PlayerDataManager.Singleton.GetGameMode() != PlayerDataManager.GameMode.None)
                    {
                        c.enabled = CombatAgent.GetAilment() != ActionClip.Ailment.Death & CombatAgent.IsSpawned;
                    }
                    else // Player hub
                    {
                        c.enabled = false;
                    }
                }
            }
            lastAilmentEvaluated = CombatAgent.GetAilment();
            lastSpawnState = CombatAgent.IsSpawned;

            //foreach (Attributes attributes in PlayerDataManager.Singleton.GetActivePlayerObjects())
            //{
            //    foreach (Collider col in Colliders)
            //    {
            //        foreach (Collider otherCol in attributes.NetworkCollider.Colliders)
            //        {
            //            Physics.IgnoreCollision(col, otherCol, ShouldApplyRecoveryDodgeLogic() | StaticWallsEnabledForThisCollision(attributes.NetworkCollider));
            //        }
            //    }
            //}
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
# endif
    }
}