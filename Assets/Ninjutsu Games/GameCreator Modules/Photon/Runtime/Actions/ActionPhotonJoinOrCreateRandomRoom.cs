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
    public class ActionPhotonJoinOrCreateRandomRoom : IAction
    {
        public PhotonRoomData expectedRoomProperties = new PhotonRoomData("Expected Room Properties");
        public IntProperty expectedMaxPlayers = new IntProperty(0);
        public MatchmakingMode matchingType = MatchmakingMode.FillRoom;
        
        public StringProperty roomName = new StringProperty();
        public IntProperty maxPlayers = new IntProperty(0);
        public IntProperty playerTTL = new IntProperty(0);
        public IntProperty emptyRoomTTL = new IntProperty(0);
        public PhotonRoomData roomProperties = new PhotonRoomData("Room Properties");

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            RoomOptions roomOptions = new RoomOptions() { };
            roomOptions.MaxPlayers = (byte)maxPlayers.GetValue(target);
            roomOptions.PublishUserId = true;
            roomOptions.PlayerTtl = playerTTL.GetValue(target);
            roomOptions.EmptyRoomTtl = emptyRoomTTL.GetValue(target);
            roomOptions.CustomRoomProperties = roomProperties.ToHashtable(target);
            roomOptions.CustomRoomPropertiesForLobby = roomProperties.GetKeys();

            return PhotonNetwork.JoinRandomOrCreateRoom(roomOptions.CustomRoomProperties, (byte)expectedMaxPlayers.GetInt(target), 
                matchingType, TypedLobby.Default, null, roomName.GetValue(target), roomOptions);
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Join Random or Create Room";
        private const string NODE_TITLE = "Join Random or Create Room";
        private const string INFO = "Attempts to join a room that matches the specified filter and creates a room if none found.";

        // PROPERTIES: ----------------------------------------------------------------------------
        
        private SerializedProperty spData;
        private SerializedProperty spExpectedPlayers;
        private SerializedProperty spMatching;
        
        private SerializedProperty spRoomName;
        private SerializedProperty spMaxPlayers;
        private SerializedProperty spPlayerTtl;
        private SerializedProperty spRoomTtl;
        private SerializedProperty spProperties;

        // private bool creationToggle;

        // INSPECTOR METHODS: ---------------------------------------------------------------------
        
        protected override void OnEnableEditorChild()
        {
            spData = serializedObject.FindProperty("expectedRoomProperties");
            spExpectedPlayers = serializedObject.FindProperty("expectedMaxPlayers");
            spMatching = serializedObject.FindProperty("matchingType");
            
            spRoomName = serializedObject.FindProperty("roomName");
            spMaxPlayers = serializedObject.FindProperty("maxPlayers");
            spPlayerTtl = serializedObject.FindProperty("playerTTL");
            spRoomTtl = serializedObject.FindProperty("emptyRoomTTL");
            spProperties = serializedObject.FindProperty("roomProperties");
        }

        protected override void OnDisableEditorChild()
        {
            spData = null;
            spExpectedPlayers = null;
            spMatching = null;
            
            spRoomName = null;
            spMaxPlayers = null;
            spPlayerTtl = null;
            spRoomTtl = null;
            spProperties = null;
        }

        public override string GetNodeTitle()
        {
            return NODE_TITLE;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(INFO, MessageType.Info);

            EditorGUILayout.PropertyField(spExpectedPlayers, PhotonGUIContent.ExpectedMaxPlayers);
            EditorGUILayout.PropertyField(spMatching, PhotonGUIContent.MatchingType);
            EditorGUILayout.PropertyField(spData, PhotonGUIContent.ExpectedRoomProperties);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            // EditorGUI.indentLevel++;
            // creationToggle = EditorGUILayout.Foldout(creationToggle, "Room Creation Settings", true);
            // if(creationToggle)
            EditorGUILayout.LabelField(" Room Creation Settings", EditorStyles.boldLabel);
            // EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            {
                EditorGUILayout.PropertyField(spRoomName);
                EditorGUILayout.PropertyField(spMaxPlayers, PhotonGUIContent.MaxPlayers);
                EditorGUILayout.PropertyField(spPlayerTtl, PhotonGUIContent.PlayerTTL);
                EditorGUILayout.PropertyField(spRoomTtl, PhotonGUIContent.EmptyRoomTTL);
                EditorGUILayout.PropertyField(spProperties, PhotonGUIContent.RoomProperties);
            }
            EditorGUILayout.EndVertical();
            // EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}
