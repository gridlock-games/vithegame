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
    public class ActionMeleeDrainHP : IAction
    {
        public TargetCharacter character = new TargetCharacter(TargetCharacter.Target.Player);

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            Character _character = this.character.GetCharacter(target);
            if (character == null) return true;

            CharacterMelee melee = _character.GetComponent<CharacterMelee>();

            if (melee != null)
            {
                melee.DrainHP(30.0f);
            }

            return true;
        }

#if UNITY_EDITOR

        public static new string NAME = "Melee/Drain Character HP";
        private const string NODE_TITLE = "Drain HP";

        public override string GetNodeTitle()
        {
            return string.Format(NODE_TITLE, this.character);
        }

#endif
    }
}