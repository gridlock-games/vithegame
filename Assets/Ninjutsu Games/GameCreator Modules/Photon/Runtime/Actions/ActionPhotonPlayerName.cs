namespace GameCreator.Core
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using GameCreator.Core;
    using GameCreator.Variables;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
    public class ActionPhotonPlayerName : IAction
    {
        public TargetText source = new TargetText();

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index, params object[] parameters)
        {
            if(!string.IsNullOrEmpty(source.GetValue()) && PhotonNetwork.LocalPlayer != null) PhotonNetwork.LocalPlayer.NickName = source.GetValue();
            return true;
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Set Player Name";
        //private const string NODE_TITLE = "Set Player Name {0} {1}";
        private const string NODE_TITLE = "Set Player Name";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spNickname;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
        {
            //return string.Format(NODE_TITLE, this.source.target == TargetText.Target.String ? "to" : "from", this.source);
            return NODE_TITLE;
        }

        protected override void OnEnableEditorChild()
        {
            this.spNickname = this.serializedObject.FindProperty("source");
        }

        protected override void OnDisableEditorChild()
        {
            this.spNickname = null;
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            EditorGUILayout.PropertyField(this.spNickname);

            this.serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}
