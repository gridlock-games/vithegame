using UnityEditor;
using UnityEngine;

namespace NJG.PUN
{
    [CustomEditor(typeof(ActionsNetwork))]
    public class ActionsNetworkEditor : Editor
    {
        // private readonly string[] exlude = new string[] { "m_Script" };
        
        private string INFO = "Component automatically added to sync Actions.\nDon't remove it unless you are not really using it.";
        private static GUIContent content = new GUIContent("List");

        public override void OnInspectorGUI()
        {
            if (serializedObject == null) return;

            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("actions"), content);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.HelpBox(INFO, MessageType.Warning);

            //EditorGUILayout.HelpBox("Instance ID: "+target.GetInstanceID(), MessageType.None);

            serializedObject.ApplyModifiedProperties();
        }

        // INITIALIZERS: -----------------------------------------------------------------------------------------------

        private void OnEnable()
        {
            //if (this.target != null) this.target.hideFlags = HideFlags.None;
            //if (this.target != null) this.target.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        }
    }
}
