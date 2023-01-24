using NJG.PUN;

namespace GameCreator.Core
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using Core;
    using Characters;
    using Variables;
    using Photon.Realtime;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
    public class ActionPhotonCreateRoom : IAction
    {
        public StringProperty roomName = new StringProperty("Development");
        public IntProperty maxPlayers = new IntProperty(0);
        public IntProperty playerTTL = new IntProperty(0);
        public IntProperty emptyRoomTTL = new IntProperty(0);
        public PhotonRoomData roomProperties = new PhotonRoomData("Room Properties");

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            RoomOptions roomOptions = new RoomOptions() { };
            roomOptions.PublishUserId = true;
            roomOptions.MaxPlayers = (byte)((float)maxPlayers.GetValue(target));
            roomOptions.PlayerTtl = playerTTL.GetValue(target);
            roomOptions.EmptyRoomTtl = emptyRoomTTL.GetValue(target);
            roomOptions.CustomRoomProperties = roomProperties.ToHashtable(target);
            roomOptions.CustomRoomPropertiesForLobby = roomProperties.GetKeys();
            return PhotonNetwork.CreateRoom(string.IsNullOrEmpty(roomName.GetValue(target)) ? null : roomName.GetValue(target), roomOptions);
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Create Room";
        private const string NODE_TITLE = "Create Room: {0}";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spRoomName;
        private SerializedProperty spMaxPlayers;
        private SerializedProperty spPlayerTTL;
        private SerializedProperty spRoomTtl;
        private SerializedProperty spProperties;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
        {
            return string.Format(NODE_TITLE, roomName);
        }

        protected override void OnEnableEditorChild()
        {
            spRoomName = serializedObject.FindProperty("roomName");
            spMaxPlayers = serializedObject.FindProperty("maxPlayers");
            spPlayerTTL = serializedObject.FindProperty("playerTTL");
            spRoomTtl = serializedObject.FindProperty("emptyRoomTTL");
            spProperties = serializedObject.FindProperty("roomProperties");
        }

        protected override void OnDisableEditorChild()
        {
            spRoomName = null;
            spMaxPlayers = null;
            spPlayerTTL = null;
            spRoomTtl = null;
            spProperties = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(spRoomName);
            
            EditorGUILayout.PropertyField(spMaxPlayers, PhotonGUIContent.MaxPlayers);
            EditorGUILayout.PropertyField(spPlayerTTL, PhotonGUIContent.PlayerTTL);
            EditorGUILayout.PropertyField(spRoomTtl, PhotonGUIContent.EmptyRoomTTL);
            EditorGUILayout.PropertyField(spProperties, PhotonGUIContent.RoomProperties);

            serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}
