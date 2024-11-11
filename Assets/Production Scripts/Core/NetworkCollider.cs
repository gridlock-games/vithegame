using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Core.MovementHandlers;
using Vi.ProceduralAnimations;
using Vi.Utility;
using System.Linq;

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

        private static List<int> colliderInstanceIDMap = new List<int>();

        private void Awake()
        {
            MovementHandler = GetComponentInParent<PhysicsMovementHandler>();
            CombatAgent = GetComponentInParent<CombatAgent>();
            CombatAgent.SetNetworkCollider(this);
            Colliders = GetNetworkColliders().ToArray();
            if (staticWallBody)
            {
                staticWallColliders = staticWallBody.GetComponentsInChildren<Collider>();
            }
            
            foreach (Collider col in Colliders)
            {
                col.enabled = false;
                col.hasModifiableContacts = true;

                if (staticWallBody)
                {
                    foreach (Collider staticWallCollider in staticWallColliders)
                    {
                        Physics.IgnoreCollision(col, staticWallCollider);
                        colliderInstanceIDMap.Add(staticWallCollider.GetInstanceID());
                        staticWallCollider.hasModifiableContacts = true;
                    }
                }
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

        private void OnEnable()
        {
            if (staticWallBody)
            {
                NetworkPhysicsSimulation.AddRigidbody(staticWallBody);
                PersistentLocalObjects.Singleton.StartCoroutine(RemoveParentOfStaticWallBody());
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
            if (staticWallBody)
            {
                if (!CombatAgent.IsSpawned) { return; }

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

        private void OnDisable()
        {
            if (staticWallBody)
            {
                NetworkPhysicsSimulation.RemoveRigidbody(staticWallBody);
                PersistentLocalObjects.Singleton.StartCoroutine(ReparentStaticWallBody());
                Physics.ContactModifyEvent -= Physics_ContactModifyEvent;
            }

            foreach (Collider col in Colliders)
            {
                col.enabled = false;
            }
        }

        private void Physics_ContactModifyEvent(PhysicsScene scene, Unity.Collections.NativeArray<ModifiableContactPair> pairs)
        {
            // For each contact pair, ignore the contact points that are close to origin
            foreach (var pair in pairs)
            {
                for (int i = 0; i < pair.contactCount; ++i)
                {
                    if (CombatAgent.WeaponHandler.CurrentActionClip.IsAttack())
                    {
                        if (colliderInstanceIDMap.Contains(pair.otherColliderInstanceID))
                        {
                            if (!CombatAgent.AnimationHandler.IsAtRest())
                            {
                                pair.SetDynamicFriction(i, 1);
                            }
                        }
                    }
                }
            }
        }

        private IEnumerator RemoveParentOfStaticWallBody()
        {
            yield return null;
            if (!staticWallBody) { yield break; }
            staticWallBody.transform.SetParent(null, true);
        }

        private IEnumerator ReparentStaticWallBody()
        {
            yield return null;
            if (!staticWallBody) { yield break; }
            staticWallBody.transform.SetParent(transform, true);
            staticWallBody.transform.localPosition = Vector3.zero;
            staticWallBody.transform.localRotation = Quaternion.identity;
        }

        private void OnDestroy()
        {
            if (staticWallBody)
            {
                foreach (Collider staticWallCollider in staticWallColliders)
                {
                    colliderInstanceIDMap.Remove(staticWallCollider.GetInstanceID());
                }
                Destroy(staticWallBody.gameObject);
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
                    else
                    {
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
# endif
    }
}