using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    public class FollowUpVFX : GameInteractiveActionVFX
    {
        public bool shouldAffectSelf;
        public bool shouldAffectTeammates;
        public bool shouldAffectEnemies;
    }
}