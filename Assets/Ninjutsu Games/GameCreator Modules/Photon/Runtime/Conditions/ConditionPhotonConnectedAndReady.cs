namespace GameCreator.Core
{
    using UnityEngine;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
	public class ConditionPhotonConnectedAndReady : ICondition
	{
		public bool satisfied = true;

		// EXECUTABLE: ----------------------------------------------------------------------------

		public override bool Check(GameObject target)
		{
			return satisfied == PhotonNetwork.IsConnectedAndReady;
		}

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Conditions/";

        public static new string NAME = "Photon/Is Connected and Ready";
		private const string NODE_TITLE = "Is Connected and Ready";
		private const string NODE_TITLE2 = "Is Not Connected and Ready";

		private static GUIContent GUI_INFO = new GUIContent("A refined version of connected which is true only if your connection to the server is ready to accept operations like join, leave, etc.");

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

			EditorGUILayout.HelpBox(GUI_INFO);

			this.serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}
