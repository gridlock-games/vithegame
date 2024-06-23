using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Vi.ScriptableObjects;
using Vi.Utility;

namespace Vi.Core
{
    public class GameInteractiveActionVFX : ActionVFX
    {
        [SerializeField] private FollowUpVFX[] followUpVFXToPlayOnDestroy;
        [SerializeField] private bool shouldDestroyOnEnemyHit;

        protected Attributes attacker;
        protected ActionClip attack;

        private new void OnDisable()
        {
            base.OnDisable();
            foreach (FollowUpVFX prefab in followUpVFXToPlayOnDestroy)
            {
                GameObject g = ObjectPoolingManager.SpawnObject(prefab.gameObject, transform.position, transform.rotation);
                if (g.TryGetComponent(out FollowUpVFX vfx)) { vfx.Initialize(attacker, attack); }
                PersistentLocalObjects.Singleton.StartCoroutine(ObjectPoolingManager.ReturnVFXToPoolWhenFinishedPlaying(g));
            }
        }

        public virtual void OnHit(Attributes attacker)
        {
            if (shouldDestroyOnEnemyHit)
            {
                if (PlayerDataManager.Singleton.CanHit(attacker, this.attacker))
                {
                    ObjectPoolingManager.ReturnObjectToPool(gameObject);
                }
            }
        }
    }
}