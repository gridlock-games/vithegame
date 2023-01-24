namespace GameCreator.Core
{
    using UnityEngine;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
    public class ActionPhotonReconnect : IAction
    {
        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            return PhotonNetwork.Reconnect();
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Reconnect";
        private const string NODE_TITLE = "Reconnect";

        private const string DESC = "Can be used to reconnect to the master server after a disconnect.";

        private const string DESC2 = "After losing connection, you can use this to connect a client to the region Master Server again." +
            "\nCache the room name you're in and use ReJoinRoom(roomname) to return to a game." +
            "\nCommon use case: Press the Lock Button on a iOS device and you get disconnected immediately.";

        // PROPERTIES: ----------------------------------------------------------------------------


        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
        {
            return NODE_TITLE;
        }

        protected override void OnEnableEditorChild()
        {
        }

        protected override void OnDisableEditorChild()
        {
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            EditorGUILayout.HelpBox(DESC, MessageType.Info);
            //EditorGUILayout.HelpBox(DESC2, MessageType.None);

            this.serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}
