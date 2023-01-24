namespace GameCreator.Core
{
    using UnityEngine;
    using NJG.PUN;
    using GameCreator.Variables;
    using Photon.Pun;
    using Photon.Realtime;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
    public class ActionPhotonRoomProperty : IAction
    {
        public enum VariableType
        {
            Int,
            Float,
            String,
            Bool
        }
        public enum Operation
        {
            Set,
            Add
        }
        public enum Permission
        {
            MasterClient,
            Anyone
        }
        public VariableType variableType = VariableType.Int;
        public Operation operation = Operation.Add;
        public Permission permission = Permission.Anyone;
        public StringProperty propertyName = new StringProperty();
        public IntProperty intValue = new IntProperty();
        public NumberProperty floatValue = new NumberProperty();
        public StringProperty stringValue = new StringProperty();
        public BoolProperty boolValue = new BoolProperty();
        public bool webForward = true;

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            if (PhotonNetwork.InRoom)
            {
                Room room = PhotonNetwork.CurrentRoom;

                if (permission == Permission.MasterClient && !PhotonNetwork.IsMasterClient) return true;

                switch (variableType)
                {
                    case VariableType.Int:
                        if (operation == Operation.Set) room.SetInt(propertyName.GetValue(target), intValue.GetValue(target), webForward);
                        else room.AddInt(propertyName.GetValue(target), intValue.GetValue(target), webForward);
                        break;
                    case VariableType.Float:
                        if (operation == Operation.Set) room.SetFloat(propertyName.GetValue(target), floatValue.GetValue(target), webForward);
                        else room.AddFloat(propertyName.GetValue(target), floatValue.GetValue(target), webForward);
                        break;
                    case VariableType.String:
                        room.SetString(propertyName.GetValue(target), stringValue.GetValue(target), webForward);
                        break;
                    case VariableType.Bool:
                        room.SetBool(propertyName.GetValue(target), boolValue.GetValue(target), webForward);
                        break;
                }
            }
            else
            {
                Debug.LogWarning("You need to be inside a room in order to change a Room property.", gameObject);
            }
            return true;
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Room Property";
        private const string NODE_TITLE = "Room Property {0} {1} to {2}";

        private readonly static GUIContent GUI_PERMISSION = new GUIContent("Write Permission", "Determines who is allowed to modify this room property\n" +
            "\n\n - Master Client: Only the Master Client is allowed to modify this property." +
            "\n\n - Anyone: Anyone is allowed to modify this property.");

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spVariable;
        private SerializedProperty spOperation;
        private SerializedProperty spPermission;
        private SerializedProperty spPropertyName;

        private SerializedProperty spInt;
        private SerializedProperty spFloat;
        private SerializedProperty spString;
        private SerializedProperty spBool;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
        {
            object value = null;
            switch (variableType)
            {
                case VariableType.Int: value = intValue; break;
                case VariableType.Float: value = floatValue; break;
                case VariableType.String: value = stringValue; break;
                case VariableType.Bool: value = boolValue; break;
            }

            return string.Format(NODE_TITLE, operation, propertyName, value);
        }

        protected override void OnEnableEditorChild()
        {
            this.spVariable = this.serializedObject.FindProperty("variableType");
            this.spOperation = this.serializedObject.FindProperty("operation");
            this.spPermission = this.serializedObject.FindProperty("permission");
            this.spPropertyName = this.serializedObject.FindProperty("propertyName");

            this.spInt = this.serializedObject.FindProperty("intValue");
            this.spFloat = this.serializedObject.FindProperty("floatValue");
            this.spString = this.serializedObject.FindProperty("stringValue");
            this.spBool = this.serializedObject.FindProperty("boolValue");
        }

        protected override void OnDisableEditorChild()
        {
            this.spVariable = null;
            this.spOperation = null;
            this.spPermission = null;
            this.spPropertyName = null;

            this.spInt = null;
            this.spFloat = null;
            this.spString = null;
            this.spBool = null;
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            /*if (!PhotonNetwork.inRoom)
            {
                EditorGUILayout.HelpBox("This only works while connected with Photon and inside a room.", MessageType.Warning);
            }*/

            EditorGUILayout.PropertyField(this.spPropertyName);
            EditorGUILayout.PropertyField(this.spVariable);
            EditorGUI.indentLevel++;
            switch (variableType)
            {
                case VariableType.Int:
                    EditorGUILayout.PropertyField(this.spOperation);
                    EditorGUILayout.PropertyField(this.spInt);
                    break;
                case VariableType.Float:
                    EditorGUILayout.PropertyField(this.spOperation);
                    EditorGUILayout.PropertyField(this.spFloat);
                    break;
                case VariableType.String:
                    EditorGUILayout.PropertyField(this.spString);
                    break;
                case VariableType.Bool:
                    EditorGUILayout.PropertyField(this.spBool);
                    break;
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(this.spPermission, GUI_PERMISSION);
            this.serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}
