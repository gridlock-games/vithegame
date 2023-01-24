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
    [CustomPropertyDrawer(typeof(PhotonRoomData))]
    public class PhotonRoomDataPD : PropertyDrawer
    {
        private static GUIContent guiKey = new GUIContent("Key");
        private static GUIContent guiVariable = new GUIContent("Variable");
        private ReorderableList list;
        private bool toggle;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!toggle) return EditorGUIUtility.singleLineHeight + 8;
            return list?.GetHeight() + (EditorGUIUtility.singleLineHeight) + 14 ?? 0;
        }

        private struct CreationParams
        {
            public PhotonRoomData.VariableRoomData.VariableType variableType;
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

        private SerializedProperty GetProp(SerializedProperty prop)
        {
            PhotonRoomData.VariableRoomData.VariableType type = (PhotonRoomData.VariableRoomData.VariableType)prop.FindPropertyRelative("type").enumValueIndex;
            switch (type)
            {
                case PhotonRoomData.VariableRoomData.VariableType.Number:
                    return prop.FindPropertyRelative("numberVariable");
                case PhotonRoomData.VariableRoomData.VariableType.Bool: 
                    return prop.FindPropertyRelative("boolVariable");
                case PhotonRoomData.VariableRoomData.VariableType.String: 
                    return prop.FindPropertyRelative("stringVariable");
            }
            return default;
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
                    EditorGUIUtility.singleLineHeight +
                    EditorGUIUtility.standardVerticalSpacing + 4f
                );
                list.drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    SerializedProperty spProperty = prop.GetArrayElementAtIndex(index);
                    // EditorGUI.PropertyField(rect, spProperty, GUIContent.none);
                    
                    RectOffset offset = new RectOffset(0, 0, -2, -3);
                    // rect = offset.Add(rect);
                    // rect.x -= 15;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    // rect.width /= 2;
                    EditorGUIUtility.labelWidth = 85;
                    rect.y += 2;
                    EditorGUI.PropertyField(rect, spProperty.FindPropertyRelative("key"));
                    rect.y += EditorGUIUtility.singleLineHeight + 2;
                    // rect.x += rect.width;
                    // EditorGUIUtility.labelWidth = 1;
                    EditorGUI.PropertyField(rect, spProperty, new GUIContent("Value"));
                };

                list.onAddDropdownCallback = (rect, list) =>
                {
                    var menu = new GenericMenu();
                    foreach (PhotonRoomData.VariableRoomData.VariableType variableType in System.Enum.GetValues(
                        typeof(PhotonRoomData.VariableRoomData.VariableType)))
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
            toggle = EditorGUI.Foldout(foldPos, toggle, property.FindPropertyRelative("title").stringValue, true);
            
            if (toggle)
            {
                // GUI.Box(newPos,string.Empty, "ShurikenEffectBg");
                if(list != null) list.DoList(newPos);
            }
            EditorGUI.indentLevel--;
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
        }
    }

    [CustomPropertyDrawer(typeof(PhotonRoomData.VariableRoomData))]
    public class VariableRoomDataPD : TargetGenericPD
    {
        protected override SerializedProperty GetProperty(int option, SerializedProperty property)
        {
            if (option == (int) PhotonRoomData.VariableRoomData.VariableType.Bool)
            {
                return property.FindPropertyRelative("boolProperty");
            }
            if (option == (int) PhotonRoomData.VariableRoomData.VariableType.Number)
            {
                return property.FindPropertyRelative("numberProperty");
            }
            if (option == (int) PhotonRoomData.VariableRoomData.VariableType.String)
            {
                return property.FindPropertyRelative("stringProperty");
            }
            return null;
        }
    }
}