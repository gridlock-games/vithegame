namespace NJG.PUN
{
    using GameCreator.Core;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
    public class ActionPhotonDebugPlayerProperty : IAction
    {
        public enum DebugType
        {
            Property,
            AllInformation
        }
        public TargetPhotonPlayer target = new TargetPhotonPlayer();
        public DebugType type = DebugType.Property;
        public string property = "property";

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            Photon.Realtime.Player player = this.target.GetPhotonPlayer(target);
            if(player == null)
            {
                Debug.LogWarningFormat(gameObject, "Invalid Photon Player in {0} type: {1}", target.name, this.target.target);
                return true;
            }

            if (type == DebugType.Property) Debug.LogFormat("Player {0} property: {1} = {2}", player.NickName, property, player.GetProperty(property));
            else Debug.Log($"Player: {player.ToStringFull()}");
            return true;
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public static new string NAME = "Photon/Debug Player Property";
        private const string NODE_TITLE = "Debug {0} {1}";
        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spTarget;
        private SerializedProperty spType;
        private SerializedProperty spProperty;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
        {
            return string.Format(NODE_TITLE, target, type == DebugType.Property ? "property: " + property : "All information");
        }

        protected override void OnEnableEditorChild()
        {
            spTarget = serializedObject.FindProperty("target");
            spType = serializedObject.FindProperty("type");
            spProperty = serializedObject.FindProperty("property");
        }

        protected override void OnDisableEditorChild()
        {
            spTarget = null;
            spType = null;
            spProperty = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(spTarget);
            EditorGUILayout.PropertyField(spType);
            if(type == DebugType.Property) EditorGUILayout.PropertyField(spProperty);

            serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}