using UnityEditor;
using UnityEngine;

namespace NJG.PUN
{
    [CustomEditor(typeof(LocalVariablesNetwork))]

    public class LocalVariablesNetworkEditor : Editor
    {
        private readonly string[] exlude = new string[] { "m_Script" };
        private string INFO = "Component automatically added to handle LocalVariables.\nDon't remove it unless you are not really using it.";
        private static GUIContent content = new GUIContent("List");

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // DrawPropertiesExcluding(serializedObject, exlude);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("localVars"), content);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.HelpBox(INFO, MessageType.Warning);

            serializedObject.ApplyModifiedProperties();
        }
    }
}