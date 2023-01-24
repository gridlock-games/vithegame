using UnityEditor;
using UnityEngine;

namespace NJG.PUN
{
    [CustomEditor(typeof(RPCNetwork))]

    public class RPCNetworkEditor : Editor
    {
        private readonly string[] exlude = new string[] { "m_Script" };
        private string INFO = "Component automatically added to handle RPC igniters.\nDon't remove it unless you are not really using it.";
        private static GUIContent content = new GUIContent("List");

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // DrawPropertiesExcluding(serializedObject, exlude);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("igniters"), content);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.HelpBox(INFO, MessageType.Warning);

            serializedObject.ApplyModifiedProperties();
        }
    }
}