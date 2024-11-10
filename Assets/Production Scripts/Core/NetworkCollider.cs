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

        private void Awake()
        {
            MovementHandler = GetComponentInParent<PhysicsMovementHandler>();
            CombatAgent = GetComponentInParent<CombatAgent>();
            CombatAgent.SetNetworkCollider(this);
            Colliders = GetNetworkColliders().ToArray();

            foreach (Collider col in Colliders)
            {
                col.enabled = false;

                if (staticWallBody)
                {
                    foreach (Collider c in staticWallBody.GetComponentsInChildren<Collider>())
                    {
                        Physics.IgnoreCollision(col, c);
                    }
                }
            }
        }

        private List<Collider> GetNetworkColliders()
        {
            List<Collider> networkPredictionLayerColliders = new List<Collider>();
            Collider[] staticWallColliders = new Collider[0];
            if (staticWallBody) { staticWallBody.GetComponentsInChildren<Collider>(); }
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
            PersistentLocalObjects.Singleton.StartCoroutine(RemoveParentOfStaticWallBody());
            if (staticWallBody) { NetworkPhysicsSimulation.AddRigidbody(staticWallBody); }
        }

        private void OnDisable()
        {
            PersistentLocalObjects.Singleton.StartCoroutine(ReparentStaticWallBody());
            if (staticWallBody) { NetworkPhysicsSimulation.RemoveRigidbody(staticWallBody); }

            foreach (Collider col in Colliders)
            {
                col.enabled = false;
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
            if (staticWallBody) { Destroy(staticWallBody.gameObject); }
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