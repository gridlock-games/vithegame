namespace GameCreator.Core
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;
	using GameCreator.Core;

	#if UNITY_EDITOR
	using UnityEditor;
	#endif

	[AddComponentMenu("")]
	public class ActionWobbler : IAction
	{
        public TargetGameObject target = new TargetGameObject();
        public Vector2 wobbleDirection = Vector2.up;
        public float speed = 1;
        public float randomStartAngle;  // So they don't all wobble in the same sequence

        private Vector3 originalPosition;
        private Transform tr;

        // EXECUTABLE: ----------------------------------------------------------------------------

        private void Awake()
        {
            enabled = false;
        }

        private void Start()
        {
            originalPosition = transform.position;
            randomStartAngle = Random.value * 100;
        }

        private void Update()
        {
            if (tr != null)
            {
                var t = Time.time * Mathf.PI + randomStartAngle;
                t *= speed;
                var offset = Mathf.Sin(t) * wobbleDirection;
                tr.position = originalPosition + new Vector3(offset.x, offset.y, 0);
            }
        }

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            enabled = true;
            var go = this.target.GetGameObject(target);
            if (go != null) tr = go.transform;
            return true;
		}

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "NJG/Object Wobbler";
		private const string NODE_TITLE = "Wobble {0}";

		// PROPERTIES: ----------------------------------------------------------------------------

		private SerializedProperty spTarget;
		private SerializedProperty spDirection;
		private SerializedProperty spSpeed;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
		{
			return string.Format(NODE_TITLE, target.ToString());
		}

		protected override void OnEnableEditorChild ()
		{
			this.spTarget = this.serializedObject.FindProperty("target");
			this.spDirection = this.serializedObject.FindProperty("wobbleDirection");
			this.spSpeed = this.serializedObject.FindProperty("speed");
        }

		protected override void OnDisableEditorChild ()
		{
			this.spTarget = null;
			this.spDirection = null;
			this.spSpeed = null;
        }

		public override void OnInspectorGUI()
		{
			this.serializedObject.Update();

			EditorGUILayout.PropertyField(this.spTarget);
			EditorGUILayout.PropertyField(this.spDirection);
			EditorGUILayout.PropertyField(this.spSpeed);

            this.serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}
