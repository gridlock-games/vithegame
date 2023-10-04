using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;

namespace GameCreator.Melee
{
    [RequireComponent(typeof(ParticleSystemProjectile))]
    public class ApplyStatusOnProjectileCollision : MonoBehaviour
    {
        [SerializeField] private TargetType targetType;
        [SerializeField] private Status[] statuses;

        public enum TargetType
        {
            AllPlayers,
            SameTeam,
            Enemies
        }

        public void ApplyStatus(CharacterStatusManager targetStatusCharacter)
        {
            Team attackerMeleeTeam = ClientManager.Singleton.GetClient(projectile.GetAttacker().OwnerClientId).team;
            Team targetMeleeTeam = ClientManager.Singleton.GetClient(targetStatusCharacter.OwnerClientId).team;

            switch (targetType)
            {
                case TargetType.AllPlayers:
                    break;
                case TargetType.SameTeam:
                    // If the attacker's team is NOT the same as the victim's team, do not register this hit
                    if (attackerMeleeTeam != targetMeleeTeam) { return; }
                    break;
                case TargetType.Enemies:
                    if (attackerMeleeTeam != Team.Competitor | targetMeleeTeam != Team.Competitor)
                    {
                        // If the attacker's team is the same as the victim's team, do not register this hit
                        if (attackerMeleeTeam == targetMeleeTeam) { return; }
                    }
                    break;
            }

            foreach (Status status in statuses)
            {
                targetStatusCharacter.TryAddStatus(status.status, status.value, status.duration, status.delay);
            }
        }

        private ParticleSystemProjectile projectile;

        private void Start()
        {
            projectile = GetComponent<ParticleSystemProjectile>();
        }

        [System.Serializable]
        private struct Status
        {
            public CharacterStatusManager.CHARACTER_STATUS status;
            public float value;
            public float duration;
            public float delay;
        }
    }
}