using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Utility;

namespace Vi.Core
{
    public class GameInteractiveActionVFX : ActionVFX
    {
        [SerializeField] private FollowUpVFX[] followUpVFXToPlayOnDestroy;

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
    }
}