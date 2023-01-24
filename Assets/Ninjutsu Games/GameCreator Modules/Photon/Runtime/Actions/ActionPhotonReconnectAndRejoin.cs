namespace GameCreator.Core
{
    using UnityEngine;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
    public class ActionPhotonReconnectAndRejoin : IAction
    {
        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            return PhotonNetwork.ReconnectAndRejoin();
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Reconnect And Rejoin";
        private const string NODE_TITLE = "Reconnect And Rejoin";

        private const string DESC = "When the client lost connection during gameplay, this method attempts to reconnect and rejoin the room.";

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

            this.serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}
