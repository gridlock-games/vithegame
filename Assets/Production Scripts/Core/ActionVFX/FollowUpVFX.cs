using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;

namespace Vi.Core.VFX
{
    public class FollowUpVFX : GameInteractiveActionVFX
    {
        [SerializeField] private bool shouldAffectSelf;
        [SerializeField] private bool shouldAffectTeammates;
        [SerializeField] private bool shouldAffectEnemies = true;

        protected bool ShouldAffect(HittableAgent hittableAgent)
        {
            bool shouldAffect = false;
            if (hittableAgent == GetAttacker())
            {
                if (shouldAffectSelf) { shouldAffect = true; }
            }
            else
            {
                bool canHit = PlayerDataManager.Singleton.CanHit(hittableAgent, GetAttacker());
                if (shouldAffectEnemies & canHit) { shouldAffect = true; }
                if (shouldAffectTeammates & !canHit) { shouldAffect = true; }
            }

            if (spellType == SpellType.GroundSpell)
            {
                if (hittableAgent.StatusAgent.IsImmuneToGroundSpells()) { shouldAffect = false; }
            }
            return shouldAffect;
        }
    }
}