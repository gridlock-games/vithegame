using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vi.UI
{
    public class CharacterCustomizationRow : MonoBehaviour
    {
        [SerializeField] private HorizontalLayoutGroup layoutGroup;
        [SerializeField] private CharacterCustomizationButton[] buttons;
        public Text rowHeaderText;

        public HorizontalLayoutGroup GetLayoutGroup() { return layoutGroup; }

        public CharacterCustomizationButton GetUninitializedButton()
        {
            foreach (CharacterCustomizationButton button in buttons)
            {
                if (button.Initialized) { continue; }
                return button;
            }
            Debug.LogError("Returning null instead of a uninitialized button!");
            return null;
        }
    }
}