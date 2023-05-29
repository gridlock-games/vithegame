namespace GameCreator.Melee
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;
	using GameCreator.Core;
    using GameCreator.Characters;
	using Unity.Netcode;

    [AddComponentMenu("")]
	public class ActionMeleeAttack : IAction
	{
		public TargetCharacter character = new TargetCharacter(TargetCharacter.Target.Player);
		public CharacterMelee.ActionKey key = CharacterMelee.ActionKey.A;

		[ServerRpc]
		void MeleeServerRpc(Vector3 targetPosition, Quaternion targetRotation, string targetName)
		{
			GameObject target = new GameObject(targetName);
			target.transform.position = targetPosition;
			target.transform.rotation = targetRotation;

			Character _character = this.character.GetCharacter(target);
			if (character == null) { Destroy(target); return; }
			if (_character.disableActions.Value) { Destroy(target); return; }

			CharacterMelee melee = _character.GetComponent<CharacterMelee>();
			if (melee == null) { Destroy(target); return; }

			MeleeClientRpc(targetPosition, targetRotation, targetName);
			if (!IsHost) InstantExecuteLocally(target);
			Destroy(target);
		}

		[ClientRpc]
		void MeleeClientRpc(Vector3 targetPosition, Quaternion targetRotation, string targetName)
        {
			GameObject target = new GameObject(targetName);
			target.transform.position = targetPosition;
			target.transform.rotation = targetRotation;
			InstantExecuteLocally(target);
			Destroy(target);
		}

		public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
			if (IsOwner) { MeleeServerRpc(target.transform.position, target.transform.rotation, target.name); }
			return false;
        }

		public bool InstantExecuteLocally(GameObject target)
        {
			Character _character = this.character.GetCharacter(target);
			if (character == null) return true;

			CharacterMelee melee = _character.GetComponent<CharacterMelee>();
			if (melee == null) return true;

			melee.Execute(this.key);
			return true;
        }

		#if UNITY_EDITOR

        public static new string NAME = "Melee/Input Melee Attack";
		private const string NODE_TITLE = "Input Melee {0} on {1}";

        public override string GetNodeTitle()
        {
			return string.Format(NODE_TITLE, this.key, this.character);
        }

        #endif
    }
}
