using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreator.Melee
{
    public class ApplyStatusOnProjectileCollision : MonoBehaviour
    {
        [SerializeField] private Status[] statuses;

        public void ApplyStatus(CharacterStatusManager targetStatusCharacter)
        {
            foreach (Status status in statuses)
            {
                targetStatusCharacter.TryAddStatus(status.status, status.value, status.duration, status.delay);
            }
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