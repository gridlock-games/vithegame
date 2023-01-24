using GameCreator.Variables;
using NJG.PUN;
using Photon.Realtime;

namespace GameCreator.Core
{
    using UnityEngine;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
    public class ActionPhotonJoinRandomRoom : IAction
    {
        public PhotonRoomData expectedRoomProperties = new PhotonRoomData("Expected Room Properties");
        public IntProperty expectedMaxPlayers = new IntProperty(0);
        public MatchmakingMode matchingType = MatchmakingMode.FillRoom;

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            return PhotonNetwork.JoinRandomRoom(expectedRoomProperties.ToHashtable(target), (byte)expectedMaxPlayers.GetInt(target), matchingType, 
                TypedLobby.Default, null);
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Join Random Room";
        private const string NODE_TITLE = "Join Random Room";
        private const string INFO = "Joins any available room of the currently used lobby and fails if none is available.";

        // PROPERTIES: ----------------------------------------------------------------------------
        
        private SerializedProperty spData;
        private SerializedProperty spExpectedPlayers;
        private SerializedProperty spMatching;

        // INSPECTOR METHODS: ---------------------------------------------------------------------
        
        protected override void OnEnableEditorChild()
        {
            spData = serializedObject.FindProperty("expectedRoomProperties");
            spExpectedPlayers = serializedObject.FindProperty("expectedMaxPlayers");
            spMatching = serializedObject.FindProperty("matchingType");
        }
        
        protected override void OnDisableEditorChild()
        {
            spData = null;
            spExpectedPlayers = null;
            spMatching = null;
        }

        public override string GetNodeTitle()
        {
            return NODE_TITLE;
        }
        
        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            EditorGUILayout.HelpBox(INFO, MessageType.Info);
            
            EditorGUILayout.PropertyField(spExpectedPlayers, PhotonGUIContent.ExpectedMaxPlayers);
            EditorGUILayout.PropertyField(spMatching, PhotonGUIContent.MatchingType);
            EditorGUILayout.PropertyField(spData, PhotonGUIContent.ExpectedRoomProperties);

            this.serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}
