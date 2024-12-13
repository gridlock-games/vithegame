using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Vi.Core.CombatAgents;
using Vi.Core.DynamicEnvironmentElements;
using Vi.Utility;

namespace Vi.Core.GameModeManagers
{
    public class EssenceWarViEssence : GameItem
    {
        [SerializeField] private AudioClip spawnSound;
        [SerializeField] private AudioClip activateSound;

        private EssenceWarManager essenceWarManager;

        public void Initialize(EssenceWarManager essenceWarManager)
        {
            this.essenceWarManager = essenceWarManager;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            AudioManager.Singleton.PlayClipOnTransform(transform, spawnSound, true, gameItemVolume);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            PlayActivateSound();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (networkCollider.CombatAgent is Attributes attributes)
                {
                    essenceWarManager.OnViEssenceActivation(attributes);
                    NetworkObject.Despawn(true);
                }
            }
        }

        private void PlayActivateSound()
        {
            AudioManager.Singleton.PlayClipAtPoint(null, activateSound, transform.position, gameItemVolume);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, 0.25f);
        }
    }
}