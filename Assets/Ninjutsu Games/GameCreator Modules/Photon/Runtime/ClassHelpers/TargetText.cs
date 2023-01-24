using UnityEngine;

namespace GameCreator.Core
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using GameCreator.Core.Hooks;
    using GameCreator.Variables;
    using NJG.PUN;
    using UnityEngine.UI;

    [System.Serializable]
    public class TargetText
    {
        public enum Target
        {
            Text,
            Input,
            String,
            Random
        }

        // PROPERTIES: ----------------------------------------------------------------------------

        public Target target = Target.Text;
        public Text text;
        public InputField input;
        public StringProperty stringProperty = new StringProperty();
        public StringProperty prefix = new StringProperty() { value = "Guest" };

        // PUBLIC METHODS: ------------------------------------------------------------------------

        public string GetValue()
        {
            string result = null;

            switch (this.target)
            {
                case Target.Input:
                    result = input.text;
                    break;
                case Target.Text:
                    result = text.text;
                    break;
                case Target.String:
                    result = stringProperty.GetValue(null);
                    break;
                case Target.Random:
                    result = prefix.GetValue(null) +UnityEngine.Random.Range(0, 1000);
                    break;
            }

            return result;
        }

        // UTILITIES: -----------------------------------------------------------------------------

        public override string ToString()
        {
            string result = "(unknown)";
            switch (this.target)
            {
                case Target.Text: result = text == null || string.IsNullOrEmpty(text.text) ? "(Text Value)" : text.text; break;
                case Target.Input: result = input == null || string.IsNullOrEmpty(input.text) ? "(Input Value)" : input.text; break;
                case Target.String:
                    string value = this.stringProperty.GetValue(null);
                    result = (string.IsNullOrEmpty(value) ? "(none)" : value);
                    break;
                case Target.Random:
                    string value2 = this.prefix.GetValue(null);
                    result = (string.IsNullOrEmpty(value2) ? "(none)" : value2);
                    break;
            }

            return result;
        }
    }
}