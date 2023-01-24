namespace GameCreator.Core
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Events;
	using Core;
    using Variables;
    using Photon.Pun;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
	public class ActionPhotonSyncScene : IAction
	{
        public BoolProperty canSync = new BoolProperty(true);

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index, params object[] parameters)
        {
	        PhotonNetwork.AutomaticallySyncScene = canSync.GetValue(target);
            return true;
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Automatically Sync Scene";
		private const string NODE_TITLE = "Photon Automatically Sync Scene: {0}";
		private const string INFO = "Defines if all clients in a room should automatically load the same level as the Master Client.\n"+
			"When enabled, clients load the same scene that is active on the Master Client.\n"+
		"When a client joins a room, the scene gets loaded even before the callback OnJoinedRoom gets called.";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spType;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
		{
            return string.Format(NODE_TITLE, canSync);
        }    

		protected override void OnEnableEditorChild ()
		{
            spType = serializedObject.FindProperty("canSync");
        }

		protected override void OnDisableEditorChild ()
		{
			spType = null;
        }

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(spType);
			EditorGUILayout.HelpBox(INFO, MessageType.Info);

            serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}
