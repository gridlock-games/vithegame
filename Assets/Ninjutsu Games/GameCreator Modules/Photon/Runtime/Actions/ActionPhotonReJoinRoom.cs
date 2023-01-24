namespace GameCreator.Core
{
    using UnityEngine;
    using GameCreator.Variables;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
    public class ActionPhotonReJoinRoom : IAction
    {
        public StringProperty roomName = new StringProperty("Development");

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            return PhotonNetwork.RejoinRoom(roomName.GetValue(target));
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Rejoin Room";
        private const string NODE_TITLE = "ReJoin Room: {0}";
        private const string DESC = "Can be used to return to a room after a disconnect and reconnect.";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spRoomName;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
        {
            return string.Format(NODE_TITLE, this.roomName);
        }

        protected override void OnEnableEditorChild()
        {
            this.spRoomName = this.serializedObject.FindProperty("roomName");
        }

        protected override void OnDisableEditorChild()
        {
            this.spRoomName = null;
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            EditorGUILayout.PropertyField(this.spRoomName);
            EditorGUILayout.HelpBox(DESC, MessageType.None);

            this.serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}
