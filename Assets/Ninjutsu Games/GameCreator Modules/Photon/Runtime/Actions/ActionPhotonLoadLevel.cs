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
	public class ActionPhotonLoadLevel : IAction
	{
		public enum RoomType
		{
			Name,
			Index
		}

		public RoomType loadBy = RoomType.Index;
        public IntProperty roomId = new IntProperty(0);
        public StringProperty roomName = new StringProperty();

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index, params object[] parameters)
        {
	        switch (loadBy)
	        {
		        case RoomType.Index:             
			        PhotonNetwork.LoadLevel(roomId.GetInt(target));
			        break;
		        case RoomType.Name:             
			        PhotonNetwork.LoadLevel(roomName.GetValue(target));
			        break;
	        }
            return true;
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Photon Load Level";
		private const string NODE_TITLE = "Photon Load Level: {0}";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spType;
		private SerializedProperty spId;
		private SerializedProperty spName;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
		{
            return string.Format(NODE_TITLE, loadBy == RoomType.Index ? roomId.ToString() : roomName.ToString());
        }    

		protected override void OnEnableEditorChild ()
		{
			spId = serializedObject.FindProperty("roomId");
            spType = serializedObject.FindProperty("loadBy");
            spName = serializedObject.FindProperty("roomName");
        }

		protected override void OnDisableEditorChild ()
		{
			spId = null;
			spType = null;
			spName = null;
        }

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(spType);

			if(loadBy == RoomType.Index) EditorGUILayout.PropertyField(spId);
			else EditorGUILayout.PropertyField(spName);

            serializedObject.ApplyModifiedProperties();
		}

		#endif
	}
}
