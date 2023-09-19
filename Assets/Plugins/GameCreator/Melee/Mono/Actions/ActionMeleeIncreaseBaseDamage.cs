namespace GameCreator.Melee
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using GameCreator.Core;
    using GameCreator.Characters;
    using GameCreator.Variables;

    [AddComponentMenu("")]
    public class ActionMeleeIncreaseBaseDamage : IAction
    {
        public TargetCharacter character = new TargetCharacter(TargetCharacter.Target.Player);
        public float baseDamageMultiplier = 1.0f;
        public float durationMultiplier = 1.0f;
        
        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            Character _character = this.character.GetCharacter(target);
            if (character == null) return true;

            CharacterMelee melee = _character.GetComponent<CharacterMelee>();

            if (melee != null)
            {
                melee.damageMultiplierDuration(durationMultiplier, baseDamageMultiplier);
            }

            return true;
        }

#if UNITY_EDITOR

        public static new string NAME = "Melee/Increase Base Damage";
        private const string NODE_TITLE = "Increase Base Damage";

        public override string GetNodeTitle()
        {
            return string.Format(NODE_TITLE, this.character);
        }

#endif
    }
}