namespace NJG.PUN
{
    using GameCreator.Core;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;
    
#endif

    [AddComponentMenu("")]
    public class ActionPhotonDebugRoomProperty : IAction
    {
        public string property = "room property";

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            Debug.LogFormat("Room {0} property: {1} = {2}", PhotonNetwork.CurrentRoom.Name, property, PhotonNetwork.CurrentRoom.GetProperty(property));
            return true;
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public static new string NAME = "Photon/Debug Room Property";
        private const string NODE_TITLE = "Debug room property: {0}";
        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spProperty;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
        {
            return string.Format(NODE_TITLE, this.property);
        }

        protected override void OnEnableEditorChild()
        {
            this.spProperty = this.serializedObject.FindProperty("property");
        }

        protected override void OnDisableEditorChild()
        {
            this.spProperty = null;
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            EditorGUILayout.PropertyField(this.spProperty);

            this.serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}