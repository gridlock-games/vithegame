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

        protected bool ShouldAffect(CombatAgent combatAgent)
        {
            if (!IsSpawned) { return false; }
            if (!IsServer) { Debug.LogError("FollowUpVFX.ShouldAffect() should only be called on the server!"); return false; }

            bool shouldAffect = false;
            if (combatAgent == GetAttacker())
            {
                if (shouldAffectSelf) { shouldAffect = true; }
            }
            else
            {
                bool canHit = PlayerDataManager.Singleton.CanHit(combatAgent, GetAttacker());
                if (shouldAffectEnemies & canHit) { shouldAffect = true; }
                if (shouldAffectTeammates & !canHit) { shouldAffect = true; }
            }

            if (spellType == SpellType.GroundSpell)
            {
                if (combatAgent.StatusAgent.IsImmuneToGroundSpells()) { shouldAffect = false; }
            }
            return shouldAffect;
        }
    }
}