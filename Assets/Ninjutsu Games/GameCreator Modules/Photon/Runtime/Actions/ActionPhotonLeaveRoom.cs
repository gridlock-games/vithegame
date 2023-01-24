namespace GameCreator.Core
{
    using UnityEngine;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
    public class ActionPhotonLeaveRoom : IAction
    {
        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            return PhotonNetwork.LeaveRoom();
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Leave Room";
        private const string NODE_TITLE = "Leave Room";

        private const string DESC = "Leave the current room and return to the Master Server where you can join or create rooms";
        private const string DESC2 = "This will clean up all (network) GameObjects with a PhotonView, unless you changed autoCleanUp to false. Returns to the Master Server.\n\n" +
            "In OfflineMode, the local 'fake' room gets cleaned up and OnLeftRoom gets called immediately.\n\n" +
            "In a room with playerTTL = 0, LeaveRoom just turns a client inactive. The player stays in the room's player list " +
            "and can return later on. Setting becomeInactive to false deliberately, means to 'abandon' the room, despite the playerTTL allowing you to come back.\n\n" +
            " In a room with playerTTL == 0, become inactive has no effect (clients are removed from the room right away).";

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
