namespace NJG.PUN
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using GameCreator.Core;
    using GameCreator.Variables;

    [CustomPropertyDrawer(typeof(TargetPhotonView))]
    public class TargetPhotonViewPD : TargetGenericPD
    {
        public const string PROP_VIEW = "photonView";
        public const string PROP_GLOBAL = "global";
        public const string PROP_LOCAL = "local";
        public const string PROP_LIST = "list";

        // PAINT METHODS: -------------------------------------------------------------------------

        protected override SerializedProperty GetProperty(int option, SerializedProperty property)
        {
            TargetPhotonView.Target optionTyped = (TargetPhotonView.Target)option;
            switch (optionTyped)
            {
                case TargetPhotonView.Target.PhotonView:
                    return property.FindPropertyRelative(PROP_VIEW);

                case TargetPhotonView.Target.LocalVariable:
                    return property.FindPropertyRelative(PROP_LOCAL);

                case TargetPhotonView.Target.ListVariable:
                    return property.FindPropertyRelative(PROP_LIST);

                case TargetPhotonView.Target.GlobalVariable:
                    return property.FindPropertyRelative(PROP_GLOBAL);
            }

            return null;
        }

        protected override void Initialize(SerializedProperty property)
        {
            int allowTypesMask = (1 << (int)Variable.DataType.GameObject);

            property
                .FindPropertyRelative(PROP_GLOBAL)
                .FindPropertyRelative(HelperGenericVariablePD.PROP_ALLOW_TYPES_MASK)
                .intValue = allowTypesMask;

            property
                .FindPropertyRelative(PROP_LOCAL)
                .FindPropertyRelative(HelperGenericVariablePD.PROP_ALLOW_TYPES_MASK)
                .intValue = allowTypesMask;
        }
    }
}