using GameCreator.Variables;
using NJG.PUN;
using Photon.Pun.UtilityScripts;
using UnityEngine;

namespace GameCreator.Core
{
#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
    public class ActionPhotonPlayerNumber : IAction
    {
        public TargetPhotonPlayer target = new TargetPhotonPlayer { target = TargetPhotonPlayer.Target.Player };
        public IntProperty number = new IntProperty();

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index, params object[] parameters)
        {
            this.target.GetPhotonPlayer(target).SetPlayerNumber(number.GetInt(target));
            return true;
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Set Player Number";
        private const string NODE_TITLE = "Set Player Name to {0}";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spTarget;
        private SerializedProperty spNumber;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
        {
            return string.Format(NODE_TITLE, number);
            //return string.Format(NODE_TITLE, this.source.target == TargetText.Target.String ? "to" : "from", this.source);
            // return NODE_TITLE;
        }

        protected override void OnEnableEditorChild()
        {
            spTarget = serializedObject.FindProperty("target");
            spNumber = serializedObject.FindProperty("number");
        }

        protected override void OnDisableEditorChild()
        {
            spTarget = null;
            spNumber = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(spTarget);
            EditorGUILayout.PropertyField(spNumber);

            serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}
