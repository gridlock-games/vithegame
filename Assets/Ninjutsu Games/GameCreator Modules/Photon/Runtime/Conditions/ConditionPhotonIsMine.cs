namespace GameCreator.Core
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using GameCreator.Core;
    using Photon.Pun;
    using NJG.PUN;

#if UNITY_EDITOR
    using UnityEditor;    
#endif

    [AddComponentMenu("")]
    public class ConditionPhotonIsMine : ICondition
    {
        public TargetPhotonView target = new TargetPhotonView();
        public bool satisfied = true;
        
        private PhotonView photonView;

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool Check(GameObject target)
        {
            bool isMine = false;

            if (!PhotonNetwork.InRoom)
            {
                return true;
            }

            photonView = this.target.GetView(target);

            if (photonView)
            {
                isMine = photonView.IsMine;
            }

            //Debug.LogWarningFormat("IsMine view: {0}", photonView);

            if(!photonView)
            {
#if UNITY_EDITOR
                Debug.LogErrorFormat("The object {0} doesn't have PhotonView component. Type: {1}", target, this.target.target, gameObject);
#endif
                return false;
            }
            //Debug.Log("ConditionPhotonIsMine Check photonView " + photonView+" / "+ target, target);
            return isMine == this.satisfied;
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Conditions/";

        public static new string NAME = "Photon/Is Mine";
        private const string NODE_TITLE = "Is {0} Mine";
        private const string NODE_TITLE2 = "Is {0} Not Mine";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spTarget;        
        private SerializedProperty spSatisfied;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
        {
            /*string targetName = this.target.ToString();
            if(this.target.target == TargetGameObject.Target.GameObject 
                && (this.target.gameObject.optionIndex == Variables.BaseProperty<GameObject>.OPTION.UseLocalVariable || (this.target.gameObject.optionIndex == Variables.BaseProperty<GameObject>.OPTION.UseGlobalVariable)))
            {
                targetName = this.target.gameObject.optionIndex == Variables.BaseProperty<GameObject>.OPTION.UseLocalVariable ? this.target.gameObject.local.name : this.target.gameObject.global.name;
            }*/
            return string.Format(this.satisfied ? NODE_TITLE : NODE_TITLE2, this.target.ToString());
        }

        protected override void OnEnableEditorChild()
        {
            this.spTarget = this.serializedObject.FindProperty("target");
            this.spSatisfied = this.serializedObject.FindProperty("satisfied");
        }

        protected override void OnDisableEditorChild()
        {
            this.spTarget = null;
            this.spSatisfied = null;
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            EditorGUILayout.PropertyField(this.spTarget);
            EditorGUILayout.PropertyField(this.spSatisfied, new GUIContent("Is Mine"));

            this.serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}
