using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Utility;
using Unity.Netcode;
using Vi.Core.Weapons;

namespace Vi.Core.VFX.Staff
{
    public class LandProtector : ActionVFXParticleSystem
    {
        [SerializeField] private AudioClip[] soundToPlayOnSpellCancel;
        [SerializeField] private PooledObject[] VFXToPlayOnSpellCancel;

        private List<SpellType> spellTypesToCancel = new List<SpellType>() { SpellType.NotASpell, SpellType.GroundSpell, SpellType.AerialSpell };

        protected override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);
            if (!IsSpawned) { return; }
            if (!IsServer) { return; }

            if (other.TryGetComponent(out GameInteractiveActionVFX gameInteractiveActionVFX))
            {
                if (!gameInteractiveActionVFX.IsSpawned) { return; }
                if (spellTypesToCancel.Contains(gameInteractiveActionVFX.GetSpellType()))
                {
                    if (PlayerDataManager.Singleton.CanHit(GetAttacker(), gameInteractiveActionVFX.GetAttacker()))
                    {
                        PlayEffects(gameInteractiveActionVFX.transform.position);
                        gameInteractiveActionVFX.NetworkObject.Despawn(true);
                    }
                }
            }
            else if (other.TryGetComponent(out Projectile projectile))
            {
                if (!projectile.IsSpawned) { return; }
                if (PlayerDataManager.Singleton.CanHit(GetAttacker(), projectile.GetAttacker()))
                {
                    PlayEffects(projectile.transform.position);
                    projectile.CanHitPlayers = false;
                    projectile.NetworkObject.Despawn(true);
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

        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        private void PlayEffectsClientRpc(Vector3 position, int soundIndex, int VFXIndex)
        {
            AudioManager.Singleton.PlayClipAtPoint(gameObject, soundToPlayOnSpellCancel[soundIndex], position, actionVFXSoundEffectVolume);
            FasterPlayerPrefs.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(ObjectPoolingManager.SpawnObject(VFXToPlayOnSpellCancel[VFXIndex], transform.position, transform.rotation)));
        }
    }
}