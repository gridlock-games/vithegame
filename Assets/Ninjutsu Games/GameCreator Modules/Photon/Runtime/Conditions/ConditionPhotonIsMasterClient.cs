namespace GameCreator.Core
{
    using UnityEngine;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
    public class ConditionPhotonIsMasterClient : ICondition
    {
        public bool satisfied = true;        

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool Check(GameObject target)
        {
            return PhotonNetwork.IsMasterClient == this.satisfied;
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Conditions/";

        public static new string NAME = "Photon/Is Master Client";
        private const string NODE_TITLE = "Is Master Client";
        private const string NODE_TITLE2 = "Is Not Master Client";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spSatisfied;        

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
        {
            return this.satisfied ? NODE_TITLE : NODE_TITLE2;
        }

        protected override void OnEnableEditorChild()
        {
            this.spSatisfied = this.serializedObject.FindProperty("satisfied");
        }

        protected override void OnDisableEditorChild()
        {
            this.spSatisfied = null;
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            EditorGUILayout.PropertyField(this.spSatisfied, new GUIContent("Is Master Client"));

            this.serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}
