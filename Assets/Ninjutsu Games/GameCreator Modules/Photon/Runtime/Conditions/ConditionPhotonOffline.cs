namespace GameCreator.Core
{
    using UnityEngine;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
	public class ConditionPhotonOffline : ICondition
	{
		public bool satisfied = true;

		// EXECUTABLE: ----------------------------------------------------------------------------

		public override bool Check(GameObject target)
		{
            return satisfied == PhotonNetwork.OfflineMode;
		}

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Conditions/";

        public static new string NAME = "Photon/Is Offline";
		private const string NODE_TITLE = "Is {0}in Offline Mode";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spSatisfied;

		// INSPECTOR METHODS: ---------------------------------------------------------------------

		public override string GetNodeTitle()
		{
			return string.Format(NODE_TITLE, satisfied ? string.Empty : "NOT ");
		}

		protected override void OnEnableEditorChild ()
		{
			spSatisfied = serializedObject.FindProperty("satisfied");
		}

		protected override void OnDisableEditorChild ()
		{
			spSatisfied = null;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(spSatisfied, new GUIContent("Is Offline"));

			serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}
