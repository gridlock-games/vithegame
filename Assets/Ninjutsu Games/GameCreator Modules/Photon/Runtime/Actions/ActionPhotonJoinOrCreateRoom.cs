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
    public class ActionPhotonJoinOrCreateRoom : IAction
    {
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

            string rname = roomName.GetValue(target);
            return PhotonNetwork.JoinOrCreateRoom(string.IsNullOrEmpty(rname) ? null : rname, roomOptions, TypedLobby.Default);
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Join Or Create Room";
        private const string NODE_TITLE = "Join Or Create Room: {0}";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spRoomName;
        private SerializedProperty spMaxPlayers;
        private SerializedProperty spPlayerTtl;
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
            spPlayerTtl = serializedObject.FindProperty("playerTTL");
            spRoomTtl = serializedObject.FindProperty("emptyRoomTTL");
            spProperties = serializedObject.FindProperty("roomProperties");
        }

        protected override void OnDisableEditorChild()
        {
            spRoomName = null;
            spMaxPlayers = null;
            spPlayerTtl = null;
            spRoomTtl = null;
            spProperties = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(spRoomName);
            
            EditorGUILayout.PropertyField(spMaxPlayers, PhotonGUIContent.MaxPlayers);
            EditorGUILayout.PropertyField(spPlayerTtl, PhotonGUIContent.PlayerTTL);
            EditorGUILayout.PropertyField(spRoomTtl, PhotonGUIContent.EmptyRoomTTL);
            EditorGUILayout.PropertyField(spProperties, PhotonGUIContent.RoomProperties);

            serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}
