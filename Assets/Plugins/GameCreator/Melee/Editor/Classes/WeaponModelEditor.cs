using System.Collections;
using System.Collections.Generic;
using GameCreator.Melee;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GameCreator.Melee
{
    
    public class WeaponModelEditor : IMeleeEditor
    {
        // private SerializedProperty spPrefab1;
        // private SerializedProperty spAttachment1;
        // private SerializedProperty spPosition1;
        // private SerializedProperty spRotation1;
        private static readonly GUIContent GC_WM = new GUIContent("WeaponModelData Model");
        private SerializedProperty spTest;
        private ReorderableList testList;
        
        private const float PADDING = 10f;
        private const float SPACING = 2f;
        
        public static float GetHeight()
        {
            return (
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing +
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing +
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing +
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing
            );
        }
        
        private void OnEnable()
        {
            // this.spPrefab1 = this.serializedObject.FindProperty("prefab1");
            // this.spAttachment1 = this.serializedObject.FindProperty("attachment1");
            // this.spPosition1 = this.serializedObject.FindProperty("positionOffset1");
            // this.spRotation1 = this.serializedObject.FindProperty("rotationOffset1");
            
            //weapon model
            this.spTest = this.serializedObject.FindProperty("weaponModelDatas");
            
            this.testList = new ReorderableList(
                this.serializedObject,
                this.spTest,
                true, true, true, true
            );
            
            this.testList.elementHeight = GetHeight() + PADDING * 5F;
            this.testList.drawHeaderCallback += this.PaintWeaponModel_Header;
            this.testList.drawElementCallback += this.PaintWeaponModel_Element;
        }

        public override void OnInspectorGUI()
        {
            // EditorGUILayout.PropertyField(spPrefab1);
            // EditorGUILayout.PropertyField(spAttachment1);
            //
            // EditorGUILayout.Space();
            // EditorGUILayout.PropertyField(spPosition1);
            // EditorGUILayout.PropertyField(spRotation1);
            
            testList.DoLayoutList();
            EditorGUILayout.Space();
        }
        
        
        private void PaintWeaponModel_Header(Rect rect)
        {
            EditorGUI.LabelField(rect, "WeaponModelData");
        }
        
        private void PaintWeaponModel_Element(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect = new Rect(
                rect.x,
                rect.y + PADDING,
                rect.width,
                rect.height - (PADDING * 2f)
            );
            EditorGUI.PropertyField(
                rect, this.spTest.GetArrayElementAtIndex(index),
                GC_WM, true
            );
            
        }
    }
}
