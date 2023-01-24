using System;
using System.Collections.Generic;
using System.Linq;
using GameCreator.Core;
using NJG.PUN;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NJG.PUN
{
    // [CustomPropertyDrawer(typeof(PhotonSendData))]
    public class PhotonSendDataPD : PropertyDrawer
    {
        private ReorderableList list;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return list?.GetHeight() + 4 ?? 0;
        }
        
        /*private struct CreationParams 
        {  
            public PhotonCustomData.VariableData.VariableType variableType;
        }

        private void clickHandler(object target)
        {
            var data = (CreationParams)target;
            var index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("target").enumValueIndex = (int)data.variableType;
            list.serializedProperty.serializedObject.ApplyModifiedProperties();
        }*/

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var prop = property.FindPropertyRelative("customData");

            if (list == null)
            {
                list = new ReorderableList(property.serializedObject, prop, true, false, true, true);

                list.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Send Data"); };


                // Adjust heights based on whether or not an element is selected.
                list.elementHeightCallback = (index) => (
                    EditorGUI.GetPropertyHeight(prop.GetArrayElementAtIndex(index)) +
                    EditorGUIUtility.standardVerticalSpacing + 1f
                );
                list.drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    SerializedProperty spProperty = prop.GetArrayElementAtIndex(index);
                    // EditorGUIUtility.labelWidth = 120f;
                    EditorGUI.PropertyField(rect, spProperty, new GUIContent($"{index}. Variable"));
                };
                /*list.onAddDropdownCallback = (rect, list) =>
                {
                    var menu = new GenericMenu();
                    foreach (PhotonCustomData.VariableData.VariableType variableType in System.Enum.GetValues(typeof(PhotonCustomData.VariableData.VariableType))) 
                    {
                        menu.AddItem(new GUIContent(variableType.ToString()), 
                            false, clickHandler,  new CreationParams() {variableType = variableType});
                    }
                    menu.ShowAsContext();
                };*/
            }

            list.DoList(position);
        }
    }
    
    [CustomPropertyDrawer(typeof(PhotonSendData.VariableData))]
    public class VariableDataPD : TargetGenericPD
    {
        protected override SerializedProperty GetProperty(int option, SerializedProperty property)
        {
            if (option == (int)PhotonSendData.VariableData.VariableType.Bool)
            {
                return property.FindPropertyRelative("boolProperty");
            }
            if (option == (int)PhotonSendData.VariableData.VariableType.Number)
            {
                return property.FindPropertyRelative("numberProperty");
            }
            if (option == (int)PhotonSendData.VariableData.VariableType.Color)
            {
                return property.FindPropertyRelative("colorProperty");
            }
            if (option == (int)PhotonSendData.VariableData.VariableType.String)
            {
                return property.FindPropertyRelative("stringProperty");
            }
            if (option == (int)PhotonSendData.VariableData.VariableType.Vector2)
            {
                return property.FindPropertyRelative("vector2Property");
            }
            if (option == (int)PhotonSendData.VariableData.VariableType.Vector3)
            {
                return property.FindPropertyRelative("vector3Property");
            }
            return null;
        }
    }
}