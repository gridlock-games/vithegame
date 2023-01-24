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
	public class ActionPhotonRoomState : IAction
	{
        public BoolProperty open = new BoolProperty(true);
        public BoolProperty visible = new BoolProperty(true);

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index, params object[] parameters)
        {
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.CurrentRoom.IsOpen = open.GetValue(target);
                PhotonNetwork.CurrentRoom.IsVisible = visible.GetValue(target);
            }
            return true;
        }
        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Room State";
		private const string NODE_TITLE = "Set room state to Open: {0} Visible: {1}";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spState;
		private SerializedProperty spValue;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
		{
            string title = string.Format(NODE_TITLE, open, visible);
            return title;
        }    

		protected override void OnEnableEditorChild ()
		{
			spValue = serializedObject.FindProperty("open");
            spState = serializedObject.FindProperty("visible");
        }

		protected override void OnDisableEditorChild ()
		{
			spValue = null;
			spState = null;
        }

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(spValue);
            EditorGUILayout.PropertyField(spState);

            serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}
