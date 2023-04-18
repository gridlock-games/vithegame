using System.IO;
using GameCreator.Core;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;

namespace NJG.Graph
{
    [CustomEditor(typeof(ActionsState), true)]
    public class ActionsStateInspector : NodeInspector
    {   
        private IActionsListEditor actionEditor;
        private ActionsState state;
        private int lastCount;
        private int lastHashCode;
        private SerializedProperty spCanReset;

        public override void OnEnable()
        {
            base.OnEnable();

            state = node as ActionsState;
            spCanReset = serializedObject.FindProperty("resetState");

            actionEditor = (IActionsListEditor) CreateEditor(state.GetActions(GraphEditor.Reactions));

            if (actionEditor)
            {
                IActionsList actions = actionEditor.target as IActionsList;
                lastCount = actions.actions.Length;
                lastHashCode = actions.actions.GetHashCode();

                GraphUtility.ReplaceLocalVarTargets(actions.actions);
            }
        }

        public override void OnInspectorGUI()
        {
            if(!actionEditor) return;
            if(actionEditor.serializedObject.targetObject == null) return;
            
            state = node as ActionsState;
            serializedObject.Update();
            
            DrawDescription();

            //EditorGUI.BeginChangeCheck();
            //EditorGUILayout.PropertyField(spCanReset);
            //if(EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();

            EditorGUI.BeginChangeCheck();
            actionEditor.serializedObject.Update();
            actionEditor.OnInspectorGUI();
            IActionsList actions = actionEditor.target as IActionsList;
            bool hasChanged = lastHashCode != actions.actions.GetHashCode() ||
                              lastCount != actions.actions.Length || EditorGUI.EndChangeCheck();
            bool isInstance = GraphEditor.Controller && GraphEditor.Controller.stateMachine &&
                              GraphEditor.Controller.reactions;
            if (hasChanged)
            {
                lastCount = actions.actions.Length;
                lastHashCode = actions.actions.GetHashCode();

                BlackboardWindow.UpdateVariables();
                GraphUtility.ReplaceLocalVarTargets(actions.actions);

                //Debug.Log("Actions Changed instance: " + isInstance);

                if (isInstance)
                {
                    GraphUtility.CopyIActionList(GraphEditor.Controller.reactions.GetReaction<IActionsList>(state),
                        GraphEditor.Controller.stateMachine.sourceReactions.GetReaction<IActionsList>(state));
                }
            }
            
            if (state.IsStartNode)
            {
                EditorGUILayout.HelpBox("This node is set as Default Node, this runs as soon as the State Machine is executed.\nThere is no need to use On Start Trigger", MessageType.Info);
                // EditorGUILayout.Space();
            }

            actionEditor.serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            base.OnInspectorGUI();
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}