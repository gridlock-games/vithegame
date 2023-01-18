namespace GameCreator.Core
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;

	[AddComponentMenu("")]
	public class ConditionRaycastHit : ICondition
	{
		public TargetGameObject CastRaycastFrom = new TargetGameObject(TargetGameObject.Target.GameObject);
		public float RaycastLength = 1f;
		public LayerMask RaycastLayer;
		Ray ray;

		public override bool Check(GameObject target)
		{
			ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			// Debug.DrawRay(Camera.main.ScreenPointToRay(Input.mousePosition), CastRaycastFrom.GetGameObject(target).transform.forward*RaycastLength,Color.green);
			RaycastHit hit = new RaycastHit();
			GameObject go;
			

			if (Physics.Raycast(ray, out hit, RaycastLength, RaycastLayer.value))
			{
				go = hit.transform.gameObject;
				return true;
			}
			else
			{
				return false;
			}
			
		}
        
		#if UNITY_EDITOR
        public static new string NAME = "Custom/Condition Raycast Hit";
		#endif
	}
}
