using System;
using System.Collections.Generic;
using System.Linq;
using GameCreator.Core;
using GameCreator.Variables;
using NJG.PUN;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NJG.PUN
{
    [CustomPropertyDrawer(typeof(PhotonSendData))]
    [CustomPropertyDrawer(typeof(PhotonReceiveData))]
    public class PhotonReceiveDataPD : PropertyDrawer
    {
        private ReorderableList list;
        private EditorSortableList editorSortableList;
        private bool toggle;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!toggle) return EditorGUIUtility.singleLineHeight + 8;
            return list?.GetHeight() + EditorGUIUtility.singleLineHeight + 12 ?? 0;
        }

        private struct CreationParams
        {
            public PhotonSendData.VariableData.VariableType variableType;
        }

        private void clickHandler(object target)
        {
            var data = (CreationParams) target;
            var index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("target").enumValueIndex = (int) data.variableType - 1;

            list.serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        private VariableProperty GetNewVar(PhotonSendData.VariableData.VariableType target)
        {
            VariableProperty prop = new VariableProperty();
            prop.global = new HelperGlobalVariable();
            prop.global.allowTypesMask |= (1 << (int) target);
            return prop;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();

            var prop = property.FindPropertyRelative("customData");

            if (list == null)
            {
                list = new ReorderableList(property.serializedObject, prop, true, false, true, true);

                // list.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Received Data"); };

                // Adjust heights based on whether or not an element is selected.
                list.elementHeightCallback = (index) => (
                    EditorGUI.GetPropertyHeight(prop.GetArrayElementAtIndex(index)) +
                    EditorGUIUtility.standardVerticalSpacing + 1f
                );
                /*list.onAddCallback = reorderableList =>
                {
                    var index = list.serializedProperty.arraySize;
                    list.serializedProperty.arraySize++;
                    list.index = index;
                    var element = list.serializedProperty.GetArrayElementAtIndex(index);
                    // element.FindPropertyRelative("variable").managedReferenceValue =
                    //     new VariableProperty(VariableProperty.GetVarType.GlobalVariable);
                };*/
                list.drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    SerializedProperty spProperty = prop.GetArrayElementAtIndex(index);
                    EditorGUIUtility.labelWidth = 85;
                    EditorGUI.PropertyField(rect, spProperty, new GUIContent($"{index}. Variable"));
                };

                list.onAddDropdownCallback = (rect, list) =>
                {
                    var menu = new GenericMenu();
                    foreach (PhotonSendData.VariableData.VariableType variableType in System.Enum.GetValues(
                        typeof(PhotonSendData.VariableData.VariableType)))
                    {
                        menu.AddItem(new GUIContent(variableType.ToString()),
                            false, clickHandler, new CreationParams() {variableType = variableType});
                    }

                    menu.ShowAsContext();
                };
            }

            var newPos = position;
            // newPos.y += 4;

            var foldPos = newPos;
            foldPos.height = EditorGUIUtility.singleLineHeight + 8;
            
            newPos.y += foldPos.height;
            
            EditorGUI.indentLevel++;
            toggle = EditorGUI.Foldout(foldPos, toggle, "Custom Data", true);
            
            if (toggle)
            {
                // GUI.Box(newPos,string.Empty, "ShurikenEffectBg");
                list.DoList(newPos);
            }
            EditorGUI.indentLevel--;
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
        }
    }

    [CustomPropertyDrawer(typeof(PhotonReceiveData.VariableData))]
    public class VariableRemoteDataPD : TargetGenericPD
    {
        protected override SerializedProperty GetProperty(int option, SerializedProperty property)
        {
            if (option == (int) PhotonSendData.VariableData.VariableType.Bool)
            {
                return property.FindPropertyRelative("boolVariable");
            }

            if (option == (int) PhotonSendData.VariableData.VariableType.Number)
            {
                return property.FindPropertyRelative("numberVariable");
            }

            if (option == (int) PhotonSendData.VariableData.VariableType.Color)
            {
                return property.FindPropertyRelative("colorVariable");
            }

            if (option == (int) PhotonSendData.VariableData.VariableType.String)
            {
                return property.FindPropertyRelative("stringVariable");
            }

            if (option == (int) PhotonSendData.VariableData.VariableType.Vector2)
            {
                return property.FindPropertyRelative("vector2Variable");
            }

            if (option == (int) PhotonSendData.VariableData.VariableType.Vector3)
            {
                return property.FindPropertyRelative("vector3Variable");
            }

            return null;
        }
    }
}