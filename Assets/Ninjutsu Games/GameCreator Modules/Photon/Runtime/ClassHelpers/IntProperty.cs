namespace GameCreator.Variables
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class IntProperty : NumberProperty
    {
        public IntProperty() : base() { }
        public IntProperty(int value) : base(value) { }
        
        public new int GetValue(GameObject invoker)
        {
            switch (optionIndex)
            {
                case OPTION.Value: return Mathf.FloorToInt((float)value);
                case OPTION.UseGlobalVariable : return Mathf.FloorToInt((float)global.Get());
                case OPTION.UseLocalVariable: return Mathf.FloorToInt((float)local.Get(invoker));
                case OPTION.UseListVariable: return Mathf.FloorToInt((float)list.Get(invoker));
            }

            return default(int);
        }
    }
}