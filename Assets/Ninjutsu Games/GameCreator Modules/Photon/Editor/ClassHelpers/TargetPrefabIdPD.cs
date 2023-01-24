namespace NJG.PUN
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using GameCreator.Core;
    using GameCreator.Characters;

    [CustomPropertyDrawer(typeof(TargetPrefabId))]
    public class TargetPrefabIdPD : TargetGenericPD
    {
        protected override SerializedProperty GetProperty(int option, SerializedProperty property)
        {
           
            if (option == (int)TargetPrefabId.Target.GameObject)
            {
                return property.FindPropertyRelative("gameObject");
            }
            else if (option == (int)TargetPrefabId.Target.CachedPrefab)
            {
                return property.FindPropertyRelative("prefab");
            }

            return null;
        }
    }
}