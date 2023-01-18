namespace GameCreator.Core
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;
	using GameCreator.Variables;
	using GameCreator.Core.Math;
	using GameCreator.Core.Hooks;

	[AddComponentMenu("")]
	public class ActionRaycastFromGameObject : IAction
	{
		public TargetGameObject StartRaycastFrom = new TargetGameObject(TargetGameObject.Target.Invoker);
		public NumberProperty RaycastLength = new NumberProperty(3f);
		public VariableProperty StoreHitColliderTo = new VariableProperty(Variable.VarType.GlobalVariable);
		public bool CheckForTag;
		public string TagName = "";
		
		[Space]
		public LayerMask RaycastLayer;
		Ray ray;
    	bool m_HitDetect;
			
		RaycastHit hit;

		void Update()
		{
			float xAxis = Input.GetAxis("Horizontal") * 10.0f;
			float zAxis = Input.GetAxis("Vertical") *  10.0f;
			transform.Translate(new Vector3(xAxis, 0, zAxis));
		}

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
		{
			var gameObject = StartRaycastFrom.GetGameObject(target);
			var RayDistance = RaycastLength.GetValue(target);

			ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			
    		m_HitDetect = Physics.BoxCast(gameObject.transform.position + Vector3.up, gameObject.transform.localScale, gameObject.transform.forward, out hit, gameObject.transform.rotation, 10.0f);
			
			Debug.DrawRay(gameObject.transform.position + Vector3.up, transform.forward * 10.0f);
			//Draw a cube at the maximum distance
			// Debug.DrawWireCube(transform.position + transform.forward * 10.0f, transform.localScale);

			if (m_HitDetect) 
			{
				if (CheckForTag == true)
				{
					if (hit.collider.CompareTag(this.TagName))
					{
						this.StoreHitColliderTo.Set(hit.collider.gameObject, gameObject);
				
						print(hit.transform.name);
					}
				}
				else
				{
					this.StoreHitColliderTo.Set(hit.collider.gameObject, gameObject);
				
					print(hit.transform.name);
				}
			}
			
			return true;
            
        }

		void OnDrawGizmos()
		{
			Gizmos.color = Color.red;

			//Check if there has been a hit yet
			if (m_HitDetect)
			{
				//Draw a Ray forward from GameObject toward the hit
				Gizmos.DrawRay(transform.position, transform.forward * hit.distance);
				//Draw a cube that extends to where the hit exists
				Gizmos.DrawWireCube(transform.position + transform.forward * hit.distance, transform.localScale);
			}
			//If there hasn't been a hit yet, draw the ray at the maximum distance
			else
			{
				//Draw a Ray forward from GameObject toward the maximum distance
				Gizmos.DrawRay(transform.position, transform.forward * 10.0f);
				//Draw a cube at the maximum distance
				Gizmos.DrawWireCube(transform.position + transform.forward * 10.0f, transform.localScale);
			}
		}

		#if UNITY_EDITOR
		public static new string NAME = "Custom/Raycast From GameObject";
		#endif
	}
}
