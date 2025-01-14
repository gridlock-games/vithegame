using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Vi.UI
{
    [RequireComponent(typeof(Button))]
    public class SetDropdownValueFromButton : MonoBehaviour
    {
        [SerializeField] private int dropdownValue;
        [SerializeField] private TMP_Dropdown dropdown;
        [SerializeField] private SetDropdownValueFromButton[] otherButtons;

        private Button button;
        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(SetDropdown);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(SetDropdown);
        }

        private void Start()
        {
            RefreshButtonInteractability();
            if (dropdownValue >= dropdown.options.Count) { gameObject.SetActive(false); }
        }

        private void SetDropdown()
        {
            dropdown.value = dropdownValue;
            RefreshButtonInteractability();
        }

        private void RefreshButtonInteractability()
        {
            foreach (SetDropdownValueFromButton btn in otherButtons)
            {
                btn.button.interactable = dropdown.value != btn.dropdownValue;
            }
        }
    }
}

