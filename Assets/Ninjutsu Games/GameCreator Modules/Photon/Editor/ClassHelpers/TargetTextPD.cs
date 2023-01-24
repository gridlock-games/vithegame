namespace NJG.PUN
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using GameCreator.Core;
    using GameCreator.Characters;

    [CustomPropertyDrawer(typeof(TargetText))]
    public class TargetTextPD : TargetGenericPD
    {
        protected override SerializedProperty GetProperty(int option, SerializedProperty property)
        {

            if (option == (int)TargetText.Target.Text)
            {
                return property.FindPropertyRelative("text");
            }
            else if (option == (int)TargetText.Target.Input)
            {
                return property.FindPropertyRelative("input");
            }
            else if (option == (int)TargetText.Target.String)
            {
                return property.FindPropertyRelative("stringProperty");
            }
            else if (option == (int)TargetText.Target.Random)
            {
                return property.FindPropertyRelative("prefix");
            }
            return null;
        }
    }
}