using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Vi.ScriptableObjects
{
    [CreateAssetMenu(fileName = "SharedWeaponReferences", menuName = "Production/Shared Weapon References")]
    public class SharedWeaponReferences : ScriptableObject
    {
        public List<HitReaction> HitReactions { get { return new List<HitReaction>(hitReactions); } }

        [SerializeField] private List<HitReaction> hitReactions = new List<HitReaction>();

        [System.Serializable]
        public class HitReaction
        {
            public Weapon.HitLocation hitLocation = Weapon.HitLocation.Front;
            public ActionClip reactionClip;
            public bool shouldAlreadyHaveAilment;
        }

        public List<FlinchReaction> FlinchReactions { get { return new List<FlinchReaction>(flinchReactions); } }

        [SerializeField] private List<FlinchReaction> flinchReactions = new List<FlinchReaction>();

        [System.Serializable]
        public class FlinchReaction
        {
            public Weapon.HitLocation hitLocation = Weapon.HitLocation.Front;
            public ActionClip reactionClip;
        }
    }
}