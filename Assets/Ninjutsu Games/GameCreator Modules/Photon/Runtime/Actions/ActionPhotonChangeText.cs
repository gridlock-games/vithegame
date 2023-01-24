namespace GameCreator.Core
{
    using UnityEngine;
    using UnityEngine.UI;
    using NJG.PUN;
    using System;
    using Hashtable = ExitGames.Client.Photon.Hashtable;
    using Photon.Realtime;
    using System.Collections.Generic;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
    public class ActionPhotonChangeText : IAction
    {
        public Text text;
        [TextArea]public string content = "Player: [local:Name] Room: [RoomName]";

        private bool needsMonoUpdate;

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            UpdateText();

            return true;
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                if (needsMonoUpdate && NetworkManager.UpdateCalls.Contains(UpdateText))
                {
                    NetworkManager.UpdateCalls.Remove(UpdateText);
                }

                if (!needsMonoUpdate && NetworkManager.PhotonCalls.Contains(UpdateText))
                {
                    NetworkManager.PhotonCalls.Remove(UpdateText);
                }
            }
        }

        private void UpdateText()
        {
            if (text)
            {
                text.text = PhotonVariableParser.Parse(content, gameObject, out needsMonoUpdate);
            }

            if (needsMonoUpdate && !NetworkManager.UpdateCalls.Contains(UpdateText))
            {
                NetworkManager.UpdateCalls.Add(UpdateText);
            }

            if (!needsMonoUpdate && !NetworkManager.PhotonCalls.Contains(UpdateText))
            {
                NetworkManager.PhotonCalls.Add(UpdateText);
            }
        }


        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "UI/Photon Change Text";
        private const string NODE_TITLE = "Photon Change text of {0}";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spText;
        private SerializedProperty spContent;
        private SerializedProperty spVariable;
        private bool toggle;
        
        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
        {
            return string.Format(NODE_TITLE, text == null ? "(none)" : text.gameObject.name);
        }

        protected override void OnEnableEditorChild()
        {
            spText = serializedObject.FindProperty("text");
            spContent = serializedObject.FindProperty("content");
        }

        protected override void OnDisableEditorChild()
        {
            spText = null;
            spContent = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(spText);
            EditorGUILayout.PropertyField(spContent);

            //EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;
            toggle = EditorGUILayout.Foldout(toggle, "Reference", true);
            EditorGUI.indentLevel--;
            //EditorGUILayout.LabelField("Reference", EditorStyles.boldLabel);
            //EditorGUILayout.EndVertical();

            if (toggle)
            {
                ActionPhotonVariable.PaintDocs();
            }
            serializedObject.ApplyModifiedProperties();
        }
#endif
    }
}
