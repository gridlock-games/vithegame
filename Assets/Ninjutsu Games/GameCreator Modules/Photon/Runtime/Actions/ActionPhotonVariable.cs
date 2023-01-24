using GameCreator.Variables;

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
    public class ActionPhotonVariable : IAction
    {
        [VariableFilter(Variable.DataType.String)]
        public VariableProperty toVariable;

        [TextArea] public string content = "Player: [local:Name] Room: [RoomName]";

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
            toVariable?.Set(PhotonVariableParser.Parse(content, gameObject, out needsMonoUpdate));

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

        public static new string NAME = "Photon/Photon Data to Variable";
        private const string NODE_TITLE = "Photon Data to variable {0}";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spText;
        private SerializedProperty spContent;
        private SerializedProperty spVariable;
        private bool toggle;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
        {
            return string.Format(NODE_TITLE, toVariable);
        }

        protected override void OnEnableEditorChild()
        {
            spText = serializedObject.FindProperty("toVariable");
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
                PaintDocs();
            }

            serializedObject.ApplyModifiedProperties();
        }

        public static void PaintDocs()
        {
            Color tempColor = GUI.color;
            tempColor.a = 0.3f;

            EditorGUILayout.BeginVertical("ShurikenEffectBg", GUILayout.MinHeight(50));

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(2);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "TIP: For Player's related data you are allowed to use:\n1. # ID (0,1,2 etc) of the player.\n2. Use 'local' to get your player.\n3. Use 'this' to get the Player from the attached PhotonView.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Player Name", "[0:Name]", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            GUI.color = tempColor;
            EditorGUILayout.LabelField("Returns the Name of a player.");
            EditorGUILayout.LabelField("NOTE: '0' is the ID of the player.");
            GUI.color = Color.white;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Player Property", "[local:PropertyName]", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            GUI.color = tempColor;
            EditorGUILayout.LabelField("Returns the Property value of a player.");
            EditorGUILayout.LabelField("NOTE: 'local' is the current local player.");
            GUI.color = Color.white;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Player Score", "[this:Score]", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            GUI.color = tempColor;
            EditorGUILayout.LabelField("Returns the Score value of a player.");
            EditorGUILayout.LabelField("NOTE: 'this' is the owner Player of the attached PhotonView.");
            GUI.color = Color.white;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Player Ping", "[this:Ping]", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            GUI.color = tempColor;
            EditorGUILayout.LabelField("Returns the current ping of a player.");
            GUI.color = Color.white;
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Room Name", "[RoomName]", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            GUI.color = tempColor;
            EditorGUILayout.LabelField("Returns the Name of the current active room.");
            GUI.color = Color.white;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Room Property", "[Room:PropertyName]", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            GUI.color = tempColor;
            EditorGUILayout.LabelField("Returns the Property value of a room.");
            GUI.color = Color.white;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Player Count", "[PlayerCount]", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            GUI.color = tempColor;
            EditorGUILayout.LabelField("Returns the Player Count of the current room.");
            GUI.color = Color.white;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Max Players", "[MaxPlayers]", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            GUI.color = tempColor;
            EditorGUILayout.LabelField("Returns the number of Max Players of the current room.");
            GUI.color = Color.white;
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "TIP: For Time related data you are allowed to set the format after delimiter '|' like hh':'mm':'ss (Hours, Minutes, Seconds)\n\nNote that will only work when the Start Room Timer action is called",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Start Time", "[StartTime]", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            GUI.color = tempColor;
            EditorGUILayout.LabelField("Returns the start time of the room.");
            GUI.color = Color.white;
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Elapsed Time", "[ElapsedTime]", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            GUI.color = tempColor;
            EditorGUILayout.LabelField("Returns the current elapsed time of the room.");
            GUI.color = Color.white;
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Remaining Time", "[RemainingTime]", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            GUI.color = tempColor;
            EditorGUILayout.LabelField("Returns the current remaining time of the room.");
            GUI.color = Color.white;
            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
#endif
    }
}