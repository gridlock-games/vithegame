using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.ScriptableObjects;
using UnityEngine.Events;
using System.Linq;

namespace Vi.UI
{
    public class CharacterCustomizationRow : MonoBehaviour
    {
        [SerializeField] private GridLayoutGroup buttonLayoutGroup;
        [SerializeField] private GridLayoutGroup arrowLayoutGroup;
        public Button LeftArrowButton { get { return leftArrowButton; } }
        [SerializeField] private Button leftArrowButton;
        public Button RightArrowButton { get { return rightArrowButton; } }
        [SerializeField] private Button rightArrowButton;
        [SerializeField] private CharacterCustomizationButton[] buttons;
        public Text rowHeaderText;

        private CharacterReference.WearableEquipmentOption[] optionList;
        public void SetAsArrowGroup(IEnumerable<CharacterReference.WearableEquipmentOption> optionList)
        {
            buttonLayoutGroup.gameObject.SetActive(false);
            arrowLayoutGroup.gameObject.SetActive(true);
            this.optionList = optionList.ToArray();
        }

        public GridLayoutGroup GetLayoutGroup()
        {
            if (buttonLayoutGroup.gameObject.activeInHierarchy)
            {
                return buttonLayoutGroup;
            }
            else
            {
                return arrowLayoutGroup;
            }
        }

        public UnityAction<CharacterReference.WearableEquipmentOption> OnArrowPress;

        public int CounterIndex { get; set; } = -1;
        public void IncrementOption()
        {
            CounterIndex++;
            if (CounterIndex >= optionList.Length) { CounterIndex = 0; }
            if (OnArrowPress != null) { OnArrowPress.Invoke(optionList[CounterIndex]); }
        }

        public void DecrementOption()
        {
            CounterIndex--;
            if (CounterIndex < 0) { CounterIndex = optionList.Length-1; }
            if (OnArrowPress != null) { OnArrowPress.Invoke(optionList[CounterIndex]); }
        }

        public CharacterCustomizationButton GetUninitializedButton()
        {
            foreach (CharacterCustomizationButton button in buttons)
            {
                if (!button.gameObject.activeInHierarchy) { continue; }
                if (button.Initialized) { continue; }
                return button;
            }
            Debug.LogError("Returning null instead of a uninitialized button!");
            return null;
        }

        public void SelectRandom()
        {
            if (buttonLayoutGroup.gameObject.activeInHierarchy)
            {
                List<CharacterCustomizationButton> options = new List<CharacterCustomizationButton>();
                options.AddRange(System.Array.FindAll(buttons, item => item.Initialized & item.gameObject.activeInHierarchy));

                if (options.Count == 0) { Debug.LogWarning(this + " option count is 0"); return; }

                options[Random.Range(0, options.Count)].Button.onClick.Invoke();
            }
            else
            {
                CounterIndex = Random.Range(0, optionList.Length);
                IncrementOption();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) { return; }

            buttons = GetComponentsInChildren<CharacterCustomizationButton>(true);
        }
#endif
    }
}