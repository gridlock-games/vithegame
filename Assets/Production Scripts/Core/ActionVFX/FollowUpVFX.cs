using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Core
{
    public class FollowUpVFX : MonoBehaviour
    {
        public bool shouldAffectSelf;
        public bool shouldAffectTeammates;
        public bool shouldAffectEnemies;

        public Attributes Attacker { get; private set; }

        public void Initialize(Attributes attacker)
        {
            Attacker = attacker;
        }
    }
}