namespace GameCreator.Core
{
    using UnityEngine;
    using Variables;
    using NJG.PUN;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
	public class ActionPhotonStartTimer : IAction
	{
		public bool limitTime;
        public IntProperty duration = new IntProperty();

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (duration.GetValue(target) > 0) PhotonNetwork.CurrentRoom.SetDurationTime(duration.GetValue(target));
                NetworkManager.SetStartTime();
            }
            return true;
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Start Room Timer";
		private const string NODE_TITLE = "Start Room Timer with limit from {0}";
		// private const string NODE_TITLE_LIMIT = "with a limit of {0}(s)";
		// private const string NODE_TITLE_NOLIMIT = "with no limit";
		private const string TIME = "with no limit";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spDuration;

		// INSPECTOR METHODS: ---------------------------------------------------------------------

		public override string GetNodeTitle()
		{
			return string.Format(NODE_TITLE, duration);
		}

		protected override void OnEnableEditorChild ()
		{
			spDuration = serializedObject.FindProperty("duration");
		}

		protected override void OnDisableEditorChild ()
		{
			spDuration = null;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

            EditorGUILayout.PropertyField(spDuration, new GUIContent("Time Limit"));
            if (duration.value < 0) duration.value = 0;

            if (duration.GetValue(null) > 0) EditorGUILayout.HelpBox("(seconds)", MessageType.None, false);
            else EditorGUILayout.HelpBox("(unlimited)", MessageType.None, false);

            EditorGUILayout.HelpBox("Only the Master Client can execute this.", MessageType.Warning, false);

            serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}
