namespace NJG.PUN
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using GameCreator.Core;
    using GameCreator.Characters;

    [CustomPropertyDrawer(typeof(TargetPhotonVariable))]
    public class TargetPhotonVariablePD : TargetGenericPD
    {
        protected override SerializedProperty GetProperty(int option, SerializedProperty property)
        {

            if (option == (int)TargetPhotonVariable.Target.PlayerName)
            {
                return property.FindPropertyRelative("player");
            }
            else if (option == (int)TargetText.Target.Input)
            {
                return property.FindPropertyRelative("input");
            }
            else if (option == (int)TargetText.Target.String)
            {
                return property.FindPropertyRelative("stringProperty");
            }

            return null;
        }
    }
}