using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;
using Vi.Core.VFX;
using UnityEngine.AI;

namespace Vi.ArtificialIntelligence
{
    public class ExplosiveCretinMovementHandler : MovementHandler
    {
        private Animator animator;
        private GameInteractiveActionVFX actionVFX;
        private new void Awake()
        {
            base.Awake();
            animator = GetComponent<Animator>();
            animator.cullingMode = WebRequestManager.IsServerBuild() | NetworkManager.Singleton.IsServer ? AnimatorCullingMode.AlwaysAnimate : AnimatorCullingMode.CullCompletely;
            actionVFX = GetComponent<GameInteractiveActionVFX>();
        }

        private const float runAnimationTransitionSpeed = 5;
        private const float runSpeed = 4;

        private NetworkVariable<float> moveForwardTarget = new NetworkVariable<float>();
        private new void Update()
        {
            if (IsServer)
            {
                Vector3 inputDir;
                if (Vector3.Distance(Destination, transform.position) < stoppingDistance)
                {
                    inputDir = Vector3.zero;
                }
                else
                {
                    inputDir = NextPosition - transform.position;
                    inputDir.y = 0;
                    transform.position += Time.deltaTime * runSpeed * inputDir.normalized;
                    transform.rotation = Quaternion.LookRotation(inputDir.normalized);
                    inputDir = transform.InverseTransformDirection(inputDir).normalized;
                }
                moveForwardTarget.Value = inputDir.z;
            }
            animator.SetFloat("MoveForward", Mathf.MoveTowards(animator.GetFloat("MoveForward"), moveForwardTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
        }

        private const float roamRadius = 10;
        private void FixedUpdate()
        {
            if (!IsSpawned) { return; }

            Collider[] colliders = Physics.OverlapSphere(transform.position, roamRadius, LayerMask.GetMask(new string[] { "NetworkPrediction" }), QueryTriggerInteraction.Collide);
            System.Array.Sort(colliders, (x, y) => Vector3.Distance(x.transform.position, transform.position).CompareTo(Vector3.Distance(y.transform.position, transform.position)));
            float minDistance = 0;
            bool minDistanceInitialized = false;
            for (int i = 0; i < colliders.Length; i++)
            {
                float dist = Vector3.Distance(transform.position, colliders[i].transform.position);
                if (dist > minDistance & minDistanceInitialized) { continue; }
                minDistance = dist;
                minDistanceInitialized = true;

                if (colliders[i].transform.root.TryGetComponent(out NetworkCollider networkCollider))
                {
                    if (networkCollider.CombatAgent == actionVFX.GetAttacker()) { continue; }

                    bool shouldAffect = PlayerDataManager.Singleton.CanHit(actionVFX.GetAttacker(), networkCollider.CombatAgent);
                    if (shouldAffect)
                    {
                        if (actionVFX.GetSpellType() == GameInteractiveActionVFX.SpellType.GroundSpell)
                        {
                            if (networkCollider.CombatAgent.StatusAgent.IsImmuneToGroundSpells()) { shouldAffect = false; }
                        }
                    }

                    if (shouldAffect) { SetDestination(networkCollider.MovementHandler.GetPosition()); }
                }
            }
            CalculatePath(transform.position, NavMesh.AllAreas);
        }
    }
}