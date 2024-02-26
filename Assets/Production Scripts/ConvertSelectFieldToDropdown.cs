using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DuloGames.UI;
using TMPro;

[RequireComponent(typeof(TMP_Dropdown))]
public class ConvertSelectFieldToDropdown : MonoBehaviour
{
    private UISelectField selectField;
    private Text dropdownLabelText;
    private TMP_Dropdown dropdown;

    private void Start()
    {
        selectField = GetComponentInChildren<UISelectField>();
        dropdownLabelText = selectField.GetComponentInChildren<Text>();
        dropdown = GetComponent<TMP_Dropdown>();

        selectField.ClearOptions();
        foreach (TMP_Dropdown.OptionData option in dropdown.options)
        {
            selectField.AddOption(option.text);
        }
        selectField.value = dropdown.options[dropdown.value].text;
    }

    public void UpdateDropdownValue()
    {
        dropdown.value = selectField.options.IndexOf(selectField.value);
        dropdownLabelText.text = selectField.value;
    }
}
