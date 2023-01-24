namespace NJG.PUN
{
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(PrefabIDAttribute), true)]
    public class ActorAttributePD : PropertyDrawer
    {
        public SerializedProperty property;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            this.property = property;

            Rect activatorRect = EditorGUI.PrefixLabel(position, label);

            string itemName = "(none)";
            if (property != null && !string.IsNullOrEmpty(property.stringValue))
            {
                itemName = property.stringValue;
                if (string.IsNullOrEmpty(itemName))
                {
                    itemName = "No-name";
                }
            }

            GUIContent variableContent = new GUIContent(itemName);
            if (EditorGUI.DropdownButton(activatorRect, variableContent, FocusType.Keyboard))
            {
                PopupWindow.Show(activatorRect, new CachedPrefabPDWindow(activatorRect, c =>
                {
                    property.stringValue = c;
                    property.serializedObject.ApplyModifiedProperties();
                    property.serializedObject.Update();
                }));
            }
        }
    }
}
