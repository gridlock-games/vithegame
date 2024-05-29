using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    public class FollowUpVFX : ActionVFX
    {
        public bool shouldAffectSelf;
        public bool shouldAffectTeammates;
        public bool shouldAffectEnemies;

        public Attributes Attacker { get; private set; }
        public ActionClip ActionClip { get; private set; }

        public void Initialize(Attributes attacker, ActionClip actionClip)
        {
            Attacker = attacker;
            ActionClip = actionClip;
        }
    }
}