namespace GameCreator.Core
{
    using GameCreator.Variables;
    using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;

	[AddComponentMenu("")]
	public class ActionDistanceBetween : IAction
	{
		public TargetGameObject objectOne;
		public TargetGameObject objectTwo;
		public VariableProperty storeInstance = new VariableProperty();

		public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
			GameObject objUno = this.objectOne.GetGameObject(target);
			GameObject objDos = this.objectTwo.GetGameObject(target);

			if(objUno == null || objDos == null) {
				return false;
			}

			float distance = Vector3.Distance(objUno.transform.position, objDos.transform.position);
			this.storeInstance.Set(distance, target);
			return true;
        }

		#if UNITY_EDITOR
        public static new string NAME = "Custom/DistanceBetween";
		#endif
	}
}
