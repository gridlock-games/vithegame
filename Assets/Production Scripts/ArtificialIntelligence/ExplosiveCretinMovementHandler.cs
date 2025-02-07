using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Unity.Netcode;
using Vi.Core.VFX;
using UnityEngine.AI;
using Vi.Core.MovementHandlers;
using Vi.Utility;

namespace Vi.ArtificialIntelligence
{
    [RequireComponent(typeof(GameplayTargetFinder))]
    public class ExplosiveCretinMovementHandler : MovementHandler
    {
        private GameplayTargetFinder gameplayTargetFinder;
        private Animator animator;
        private GameInteractiveActionVFX actionVFX;
        private new void Awake()
        {
            base.Awake();
            animator = GetComponent<Animator>();
            animator.cullingMode = FasterPlayerPrefs.IsServerPlatform | NetworkManager.Singleton.IsServer ? AnimatorCullingMode.AlwaysAnimate : AnimatorCullingMode.CullCompletely;
            actionVFX = GetComponent<GameInteractiveActionVFX>();
            gameplayTargetFinder = GetComponent<GameplayTargetFinder>();
            SetDestination(transform.position);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                SetDestination(transform.position);
                CalculatePath(transform.position);
            }
        }

        private const float runAnimationTransitionSpeed = 5;
        private const float runSpeed = 4;
        private const float rotationSpeed = 120;

        private NetworkVariable<float> moveForwardTarget = new NetworkVariable<float>();
        private new void Update()
        {
            if (!IsSpawned) { return; }
            if (IsServer)
            {
                Vector3 inputDir;
                float dist = Vector3.Distance(new Vector3(Destination.x, transform.position.y, Destination.z), transform.position);
                if (dist >= stoppingDistance)
                {
                    inputDir = NextPosition - transform.position;
                    inputDir.y = 0;
                    transform.position += Time.deltaTime * runSpeed * inputDir.normalized;
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(inputDir.normalized), Time.deltaTime * rotationSpeed);
                }
                moveForwardTarget.Value = Mathf.Clamp01(dist);
            }
            animator.SetFloat("MoveForward", Mathf.MoveTowards(animator.GetFloat("MoveForward"), moveForwardTarget.Value, Time.deltaTime * runAnimationTransitionSpeed));
        }

        private const float roamRadius = 4;
        private void FixedUpdate()
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            Collider[] colliders = Physics.OverlapSphere(transform.position, roamRadius, LayerMask.GetMask(new string[] { "NetworkPrediction" }), QueryTriggerInteraction.Collide);
            System.Array.Sort(colliders, (x, y) => Vector3.Distance(x.transform.position, transform.position).CompareTo(Vector3.Distance(y.transform.position, transform.position)));
            float minDistance = 0;
            bool minDistanceInitialized = false;
            bool targetFound = false;
            for (int i = 0; i < colliders.Length; i++)
            {
                float dist = Vector3.Distance(transform.position, colliders[i].transform.position);
                if (dist > minDistance & minDistanceInitialized) { continue; }

                if (colliders[i].transform.root.TryGetComponent(out NetworkCollider networkCollider))
                {
                    if (networkCollider.CombatAgent == actionVFX.GetAttacker()) { continue; }

                    minDistance = dist;
                    minDistanceInitialized = true;

                    bool shouldAffect = PlayerDataManager.Singleton.CanHit(actionVFX.GetAttacker(), networkCollider.CombatAgent);
                    if (shouldAffect)
                    {
                        if (actionVFX.GetSpellType() == GameInteractiveActionVFX.SpellType.GroundSpell)
                        {
                            if (networkCollider.CombatAgent.StatusAgent.IsImmuneToGroundSpells()) { shouldAffect = false; }
                        }
                    }

                    if (shouldAffect) { SetDestination(networkCollider.MovementHandler.GetPosition()); targetFound = true; }
                }
            }
            if (!targetFound) { SetDestination(transform.position); }
            CalculatePath(transform.position);
        }
    }
}