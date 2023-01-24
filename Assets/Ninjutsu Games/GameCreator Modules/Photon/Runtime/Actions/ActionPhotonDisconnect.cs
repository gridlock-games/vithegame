namespace GameCreator.Core
{
    using UnityEngine;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
	public class ActionPhotonDisconnect : IAction
	{
        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            PhotonNetwork.Disconnect();
            return true;
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Photon Disconnect";
		private const string NODE_TITLE = "Photon Disconnect";
        private const string DESC = "Makes this client disconnect from the photon server, a process that leaves any room and calls OnDisconnectedFromPhoton on completion.";

        // PROPERTIES: ----------------------------------------------------------------------------


        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
		{
			return NODE_TITLE;
		}

		protected override void OnEnableEditorChild ()
		{
        }

		protected override void OnDisableEditorChild ()
		{
        }

		public override void OnInspectorGUI()
		{
			this.serializedObject.Update();

            EditorGUILayout.HelpBox(DESC, MessageType.Info);

            this.serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}
