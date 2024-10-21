using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vi.UI
{
    public class CharacterCustomizationRow : MonoBehaviour
    {
        [SerializeField] private GridLayoutGroup layoutGroup;
        [SerializeField] private CharacterCustomizationButton[] buttons;
        public Text rowHeaderText;

        public GridLayoutGroup GetLayoutGroup() { return layoutGroup; }

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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) { return; }

            buttons = GetComponentsInChildren<CharacterCustomizationButton>();
        }
#endif
    }
}