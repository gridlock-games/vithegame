namespace GameCreator.Melee
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;
	using GameCreator.Core;
    using GameCreator.Characters;

    [AddComponentMenu("")]
	public class ActionMeleeStopAttack : IAction
	{
		public TargetCharacter character = new TargetCharacter(TargetCharacter.Target.Player);

		public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
			Character _character = this.character.GetCharacter(target);
            CharacterAnimator animator = _character.GetCharacterAnimator();
			if (character == null) return true;

			CharacterMelee melee = _character.GetComponent<CharacterMelee>();
            if (melee != null) melee.StopAttack();
            animator.StopGesture(0f);
			melee.currentMeleeClip = null;

            return true;
        }

		#if UNITY_EDITOR

        public static new string NAME = "Melee/Input Melee Stop Attack";
		private const string NODE_TITLE = "Stop melee attack on {0}";

        public override string GetNodeTitle()
        {
			return string.Format(NODE_TITLE, this.character);
        }

        #endif
    }
}
