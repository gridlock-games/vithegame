namespace GameCreator.Core
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using GameCreator.Core;
    using NJG.PUN;
    using GameCreator.Variables;
    using Photon.Pun;
    using Photon.Realtime;

#if UNITY_EDITOR
    using UnityEditor;
    
#endif

    [AddComponentMenu("")]
    public class ConditionPhotonPlayerProperty : ICondition
    {
        public enum VariableType
        {
            Int,
            Float,
            String,
            Bool
        }

        public enum OtherComparisson
        {
            Equal,
            NotEqual
        }

        public enum NumberComparisson
        {
            Equal,
            NotEqual,
            Greater,
            GreaterOrEqual,
            Less,
            LessOrEqual,
            Remainder
        }

        public TargetPhotonPlayer target = new TargetPhotonPlayer() { target = TargetPhotonPlayer.Target.Player };
        public VariableType variable = VariableType.Int;
        public string propertyName;
        public NumberComparisson numberComparisson = NumberComparisson.Equal;
        public OtherComparisson otherComparisson = OtherComparisson.Equal;

        public IntProperty intValue = new IntProperty();
        public NumberProperty floatValue = new NumberProperty();
        public StringProperty stringValue = new StringProperty();
        public BoolProperty boolValue = new BoolProperty();

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool Check(GameObject target)
        {
            if (PhotonNetwork.InRoom)
            {
                Player player = this.target.GetPhotonPlayer(target);

                if (player == null) return false;

                if (variable == VariableType.Float)
                {
                    float value = player.GetFloat(propertyName);
                    switch (numberComparisson)
                    {
                        case NumberComparisson.Equal: return value == this.floatValue.GetValue(target);
                        case NumberComparisson.NotEqual: return value != this.floatValue.GetValue(target);
                        case NumberComparisson.Greater: return value > this.floatValue.GetValue(target);
                        case NumberComparisson.GreaterOrEqual: return value >= this.floatValue.GetValue(target);
                        case NumberComparisson.Less: return value < this.floatValue.GetValue(target);
                        case NumberComparisson.LessOrEqual: return value <= this.floatValue.GetValue(target);
                        case NumberComparisson.Remainder: return value % this.floatValue.GetValue(target) == 0f;
                    }
                }
                else if (variable == VariableType.Int)
                {
                    int value = player.GetInt(propertyName);
                    switch (numberComparisson)
                    {
                        case NumberComparisson.Equal: return value == this.intValue.GetValue(target);
                        case NumberComparisson.NotEqual: return value != this.intValue.GetValue(target);
                        case NumberComparisson.Greater: return value > this.intValue.GetValue(target);
                        case NumberComparisson.GreaterOrEqual: return value >= this.intValue.GetValue(target);
                        case NumberComparisson.Less: return value < this.intValue.GetValue(target);
                        case NumberComparisson.LessOrEqual: return value <= this.intValue.GetValue(target);
                        case NumberComparisson.Remainder: return value % this.intValue.GetValue(target) == 0f;
                    }
                }
                else if (variable == VariableType.String)
                {
                    string value = player.GetString(propertyName);
                    switch (otherComparisson)
                    {
                        case OtherComparisson.Equal: return value == this.stringValue.GetValue(target);
                        case OtherComparisson.NotEqual: return value != this.stringValue.GetValue(target);
                    }
                }
                else if (variable == VariableType.Bool)
                {
                    bool value = player.GetBool(propertyName);
                    switch (otherComparisson)
                    {
                        case OtherComparisson.Equal: return value == this.boolValue.GetValue(target);
                        case OtherComparisson.NotEqual: return value != this.boolValue.GetValue(target);
                    }
                }
            }

            return false;
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Conditions/";

        public static new string NAME = "Photon/Player Property";
        private const string NODE_TITLE = "Player Property {0} is {1} {2} {3}";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spTarget;
        private SerializedProperty spVariable;
        private SerializedProperty spNumberOperation;
        private SerializedProperty spOtherOperation;
        private SerializedProperty spPropertyName;

        private SerializedProperty spInt;
        private SerializedProperty spFloat;
        private SerializedProperty spString;
        private SerializedProperty spBool;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
        {
            string mid = "than";
            object value = null;

            switch (variable)
            {
                case VariableType.Int:
                    value = intValue;
                    break;
                case VariableType.Float:
                    value = floatValue;
                    break;
                case VariableType.String:
                    value = stringValue;
                    break;
                case VariableType.Bool:
                    value = boolValue;
                    break;
            }

            if ((variable == VariableType.String || variable == VariableType.Bool) || (variable != VariableType.String && variable != VariableType.Bool) && (numberComparisson == NumberComparisson.Equal || numberComparisson == NumberComparisson.NotEqual)) mid = "to";
            return string.Format(NODE_TITLE, propertyName, (variable == VariableType.String || variable == VariableType.Bool) ? otherComparisson.ToString() : numberComparisson.ToString(), mid, value);
        }

        protected override void OnEnableEditorChild()
        {
            this.spTarget = this.serializedObject.FindProperty("target");
            this.spVariable = this.serializedObject.FindProperty("variable");
            this.spNumberOperation = this.serializedObject.FindProperty("numberComparisson");
            this.spOtherOperation = this.serializedObject.FindProperty("otherComparisson");

            this.spPropertyName = this.serializedObject.FindProperty("propertyName");

            this.spInt = this.serializedObject.FindProperty("intValue");
            this.spFloat = this.serializedObject.FindProperty("floatValue");
            this.spString = this.serializedObject.FindProperty("stringValue");
            this.spBool = this.serializedObject.FindProperty("boolValue");
        }

        protected override void OnDisableEditorChild()
        {
            this.spTarget = null;
            this.spVariable = null;
            this.spNumberOperation = null;
            this.spOtherOperation = null;
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

            EditorGUILayout.PropertyField(this.spTarget);
            EditorGUILayout.PropertyField(this.spVariable);

            if (variable == VariableType.String || variable == VariableType.Bool)
            {
                EditorGUILayout.PropertyField(this.spOtherOperation);
            }
            else
            {
                EditorGUILayout.PropertyField(this.spNumberOperation);
            }
            
            EditorGUILayout.PropertyField(this.spPropertyName);

            switch (variable)
            {
                case VariableType.Int:
                    EditorGUILayout.PropertyField(this.spInt);
                    break;
                case VariableType.Float:
                    EditorGUILayout.PropertyField(this.spFloat);
                    break;
                case VariableType.String:
                    EditorGUILayout.PropertyField(this.spString);
                    break;
                case VariableType.Bool:
                    EditorGUILayout.PropertyField(this.spBool);
                    break;
            }

            this.serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}
