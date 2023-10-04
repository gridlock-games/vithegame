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
        public float drainDuration = 6.0f;
        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            Character _character = this.character.GetCharacter(target);
            if (character == null) return true;

            _character.GetComponent<CharacterStatusManager>().TryAddStatus(CharacterStatusManager.CHARACTER_STATUS.drain, 0.1f, drainDuration, 0);

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