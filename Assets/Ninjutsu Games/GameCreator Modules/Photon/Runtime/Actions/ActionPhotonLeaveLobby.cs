using Photon.Realtime;

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
	public class ActionPhotonLeaveLobby : IAction
	{
        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index, params object[] parameters)
        {
	        PhotonNetwork.LeaveLobby();
            return true;
        }
        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public new static string NAME = "Photon/Leave Lobby";
		private const string NODE_TITLE = "Leave Lobby";
		
		private const string DESC = "Leave a lobby to stop getting updates about available rooms.";

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
			serializedObject.Update();

			EditorGUILayout.HelpBox(DESC, MessageType.Info);

            serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}
