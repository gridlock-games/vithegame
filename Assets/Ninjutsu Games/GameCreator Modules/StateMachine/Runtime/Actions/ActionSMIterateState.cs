#if UNITY_EDITOR
using UnityEditor;
#endif

using GameCreator.Core;
using GameCreator.Variables;
using UnityEngine;

namespace NJG.Graph
{


    [AddComponentMenu("")]
	public class ActionSMIterateState : IAction
	{
        public TargetGameObject stateMachine = new TargetGameObject(TargetGameObject.Target.Invoker);
        public NumberProperty times = new NumberProperty(2);
        public string nodeName;

        private StateMachineController sm;

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            GameObject go = null;
            if (sm == null)
            {
                go = stateMachine.GetGameObject(target);
                sm = go.GetComponentInChildren<StateMachineController>();
            }

            if (sm != null)
            {
                int total = times.GetInt(target);
                for (int i = 0; i < total; i++)
                {
                    sm.ExecuteNode(nodeName);
                }
            }
            else
            {
                Debug.LogWarning("Couldn't find State Machine on: " + go, target);
            }
            return true;
        }

#if UNITY_EDITOR
        public static new string NAME = "State Machine/Iterate State";
        private const string NODE_TITLE = "Iterate State {0} x{1} times";
        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/StateMachine/Icons/Actions/";

        public override string GetNodeTitle()
        {
            return string.Format(NODE_TITLE, string.IsNullOrEmpty(nodeName) ? "(none)" : nodeName, times);
        }
        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spStateMachine;
        private SerializedProperty spTimes;
        private SerializedProperty spNode;

        // INSPECTOR METHODS: ---------------------------------------------------------------------
        
        protected override void OnEnableEditorChild()
        {
            this.spStateMachine = serializedObject.FindProperty("stateMachine");
            this.spTimes = serializedObject.FindProperty("times");
            this.spNode = serializedObject.FindProperty("nodeName");
        }

        protected override void OnDisableEditorChild()
        {
            this.spStateMachine = null;
            this.spTimes = null;
            this.spNode = null;
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            EditorGUILayout.PropertyField(this.spStateMachine);
            EditorGUILayout.PropertyField(this.spTimes);
            EditorGUILayout.PropertyField(this.spNode);
            this.serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}
