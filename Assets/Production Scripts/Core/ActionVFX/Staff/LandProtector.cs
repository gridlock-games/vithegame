using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Utility;
using Unity.Netcode;

namespace Vi.Core.VFX.Staff
{
    public class LandProtector : GameInteractiveActionVFX
    {
        [SerializeField] private AudioClip[] soundToPlayOnSpellCancel;
        [SerializeField] private PooledObject[] VFXToPlayOnSpellCancel;

        private List<SpellType> spellTypesToCancel = new List<SpellType>() { SpellType.GroundSpell };

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            foreach (Collider col in GetComponentsInChildren<Collider>())
            {
                col.enabled = IsServer;
            }

            if (IsServer)
            {
                Collider[] cols = Physics.OverlapBox(new Vector3(transform.position.x, transform.position.y + 0.8f, transform.position.z), new Vector3(5.6f, 2, 5.6f) / 2, transform.rotation);
                foreach (Collider col in cols)
                {
                    OnTriggerEnter(col);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            if (other.TryGetComponent(out GameInteractiveActionVFX gameInteractiveActionVFX))
            {
                if (!gameInteractiveActionVFX.IsSpawned) { return; }
                if (spellTypesToCancel.Contains(gameInteractiveActionVFX.GetSpellType()))
                {
                    if (PlayerDataManager.Singleton.CanHit(attacker, gameInteractiveActionVFX.GetAttacker()))
                    {
                        PlayEffects(gameInteractiveActionVFX.transform.position);
                        gameInteractiveActionVFX.NetworkObject.Despawn(true);
                    }
                }
            }
        }

        private void PlayEffects(Vector3 position)
        {
            if (!IsServer) { Debug.LogError("LandProtector.PlayEffects should only be called on the server!"); return; }

            int soundIndex = Random.Range(0, soundToPlayOnSpellCancel.Length);
            int VFXIndex = Random.Range(0, VFXToPlayOnSpellCancel.Length);
            
            AudioManager.Singleton.PlayClipAtPoint(gameObject, soundToPlayOnSpellCancel[soundIndex], position, actionVFXSoundEffectVolume);
            FasterPlayerPrefs.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(VFXToPlayOnSpellCancel[VFXIndex], transform.position, transform.rotation)));

            PlayEffectsClientRpc(position, soundIndex, VFXIndex);
        }

        [Rpc(SendTo.NotServer)]
        private void PlayEffectsClientRpc(Vector3 position, int soundIndex, int VFXIndex)
        {
            AudioManager.Singleton.PlayClipAtPoint(gameObject, soundToPlayOnSpellCancel[soundIndex], position, actionVFXSoundEffectVolume);
            FasterPlayerPrefs.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(VFXToPlayOnSpellCancel[VFXIndex], transform.position, transform.rotation)));
        }
    }
}