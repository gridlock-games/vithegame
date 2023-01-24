using GameCreator.Core;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NJG.PUN.Melee
{
    [CustomEditor(typeof(MeleeRegistry))]
    public class WeaponRegistryEditor : Editor
    {
        private SerializedProperty spAmmo;
        private SerializedProperty spWeapon;
        private SerializedProperty spClip;
        private ReorderableList weaponsList;
        private ReorderableList shieldList;
        private ReorderableList clipList;

        private void OnEnable()
        {
            spWeapon = serializedObject.FindProperty("weapons");

            weaponsList = new ReorderableList(
                serializedObject,
                spWeapon,
                true, true, true, true
            );

            weaponsList.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Weapons"); };
            weaponsList.drawElementCallback = Element_Weapons;
            weaponsList.elementHeightCallback = Height_Weapons;

            spAmmo = serializedObject.FindProperty("shields");

            shieldList = new ReorderableList(
                serializedObject,
                spAmmo,
                true, true, true, true
            );

            shieldList.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Shields"); };
            shieldList.drawElementCallback = Element_Ammo;
            shieldList.elementHeightCallback = Height_Ammo;
            
            spClip = serializedObject.FindProperty("clips");

            clipList = new ReorderableList(
                serializedObject,
                spClip,
                true, true, true, true
            );

            clipList.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Clips"); };
            clipList.drawElementCallback = Element_Clip;
            clipList.elementHeightCallback = Height_Clip;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            //EditorGUILayout.PropertyField(this.serializedObject.FindProperty("quest"));
            //this.spNoDropWeight.intValue = Mathf.Max(0, this.spNoDropWeight.intValue);
            //DrawPropertiesExcluding(serializedObject, _dontIncludeMe);

            EditorGUILayout.Space();
            weaponsList.DoLayoutList();

            EditorGUILayout.Space();
            shieldList.DoLayoutList();
            EditorGUILayout.Space();
            
            EditorGUILayout.Space();
            clipList.DoLayoutList();
            EditorGUILayout.Space();

            //this.PaintSummary();

            serializedObject.ApplyModifiedProperties();
        }

        // METHODS: --------------------------------------------------------------------


        private void Element_Weapons(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty spProperty = spWeapon.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, spProperty, GUIContent.none, true);
        }

        private float Height_Weapons(int index)
        {
            return (
                EditorGUI.GetPropertyHeight(spWeapon.GetArrayElementAtIndex(index)) +
                EditorGUIUtility.standardVerticalSpacing
            );
        }

        private void Element_Ammo(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty spProperty = spAmmo.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, spProperty, GUIContent.none, true);
        }

        private float Height_Ammo(int index)
        {
            return (
                EditorGUI.GetPropertyHeight(spAmmo.GetArrayElementAtIndex(index)) +
                EditorGUIUtility.standardVerticalSpacing
            );
        }
        
        private void Element_Clip(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty spProperty = spClip.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, spProperty, GUIContent.none, true);
        }

        private float Height_Clip(int index)
        {
            return (
                EditorGUI.GetPropertyHeight(spClip.GetArrayElementAtIndex(index)) +
                EditorGUIUtility.standardVerticalSpacing
            );
        }
        
        // HIERARCHY CONTEXT MENU: -------------------------------------------------------------------------------------

        [MenuItem("GameObject/Game Creator/Melee/Melee Registry", false, 0)]
        public static void CreateHotspot()
        {
            GameObject weaponRegistry = CreateSceneObject.Create("Melee Registry");
            weaponRegistry.AddComponent<MeleeRegistry>();
        }
    }
}
