using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.ScriptableObjects;
using Unity.Netcode.Components;

namespace Vi.Core
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkTransform))]
    public class GameInteractiveActionVFX : ActionVFX, INetworkUpdateSystem
    {
        [SerializeField] private FollowUpVFX[] followUpVFXToPlayOnDestroy;
        [SerializeField] private bool shouldBlockProjectiles;
        [SerializeField] private bool shouldDestroyOnEnemyHit;

        public bool ShouldBlockProjectiles() { return shouldBlockProjectiles; }

        protected Attributes attacker;
        protected ActionClip attack;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer) { NetworkUpdateLoop.RegisterAllNetworkUpdates(this); }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsServer)
            {
                NetworkUpdateLoop.UnregisterAllNetworkUpdates(this);
                foreach (FollowUpVFX prefab in followUpVFXToPlayOnDestroy)
                {
                    NetworkObject netObj = Instantiate(prefab.gameObject, transform.position, transform.rotation).GetComponent<NetworkObject>();
                    netObj.Spawn(true);
                    if (netObj.TryGetComponent(out FollowUpVFX vfx)) { vfx.Initialize(attacker, attack); }
                }
            }
        }

        private bool visibilitySet;
        private bool visibilityNetworkUpdatePassed;
        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            if (visibilityNetworkUpdatePassed) { return; }

            if (updateStage == NetworkUpdateStage.EarlyUpdate)
            {
                // Don't keep setting object visibility here
                if (visibilitySet)
                {
                    StartCoroutine(DespawnVFXAfterPlaying());
                    visibilityNetworkUpdatePassed = true;
                    return;
                }

                if (!NetworkObject.IsNetworkVisibleTo(OwnerClientId)) { NetworkObject.NetworkShow(OwnerClientId); }
                foreach (PlayerDataManager.PlayerData playerData in PlayerDataManager.Singleton.GetPlayerDataListWithSpectators())
                {
                    ulong networkId = playerData.id >= 0 ? (ulong)playerData.id : 0;
                    if (networkId == 0) { continue; }
                    if (networkId == OwnerClientId) { continue; }

                    if (playerData.channel == PlayerDataManager.Singleton.GetPlayerData((int)OwnerClientId).channel)
                    {
                        if (!NetworkObject.IsNetworkVisibleTo(networkId))
                        {
                            NetworkObject.NetworkShow(networkId);
                        }
                    }
                    else
                    {
                        if (NetworkObject.IsNetworkVisibleTo(networkId))
                        {
                            NetworkObject.NetworkHide(networkId);
                        }
                    }
                }
                visibilitySet = true;
            }
        }

        

        public virtual void OnHit(Attributes attacker)
        {
            if (!IsSpawned) { return; }
            if (shouldDestroyOnEnemyHit)
            {
                if (PlayerDataManager.Singleton.CanHit(attacker, this.attacker))
                {
                    NetworkObject.Despawn(true);
                }
            }
        }
    }
}