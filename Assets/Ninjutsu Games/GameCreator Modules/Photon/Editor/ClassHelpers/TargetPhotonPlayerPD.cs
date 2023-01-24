namespace NJG.PUN
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using GameCreator.Core;

    [CustomPropertyDrawer(typeof(TargetPhotonPlayer))]
    public class TargetPhotonPlayerPD : TargetGenericPD
    {
        protected override SerializedProperty GetProperty(int option, SerializedProperty property)
        {
            if (option == (int)TargetPhotonPlayer.Target.GameObject)
            {
                return property.FindPropertyRelative("character");
            }
            if (option == (int)TargetPhotonPlayer.Target.Id)
            {
                return property.FindPropertyRelative("playerId");
            }

            return null;
        }
    }
}