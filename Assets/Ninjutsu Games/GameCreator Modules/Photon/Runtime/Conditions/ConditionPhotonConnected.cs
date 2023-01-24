namespace GameCreator.Core
{
    using UnityEngine;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
	public class ConditionPhotonConnected : ICondition
	{
		public bool satisfied = true;

		// EXECUTABLE: ----------------------------------------------------------------------------

		public override bool Check(GameObject target)
		{
            return satisfied == PhotonNetwork.IsConnected;
		}

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Conditions/";

        public static new string NAME = "Photon/Is Connected";
		private const string NODE_TITLE = "Is Connected to Photon";
		private const string NODE_TITLE2 = "Is Not Connected to Photon";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spSatisfied;

		// INSPECTOR METHODS: ---------------------------------------------------------------------

		public override string GetNodeTitle()
		{
			return string.Format(this.satisfied ? NODE_TITLE : NODE_TITLE2);
		}

		protected override void OnEnableEditorChild ()
		{
			this.spSatisfied = this.serializedObject.FindProperty("satisfied");
		}

		protected override void OnDisableEditorChild ()
		{
			this.spSatisfied = null;
		}

		public override void OnInspectorGUI()
		{
			this.serializedObject.Update();

			EditorGUILayout.PropertyField(this.spSatisfied, new GUIContent("Is Connected"));

			this.serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}
