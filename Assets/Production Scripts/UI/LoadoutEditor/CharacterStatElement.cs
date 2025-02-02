using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.Core.CombatAgents;
using static Vi.Core.PlayerDataManager;

namespace Vi.UI
{
    public class CharacterStatElement : MonoBehaviour
    {
        [SerializeField] private WebRequestManager.Character.AttributeType attributeType;
        [SerializeField] private Text headerText;
        [SerializeField] private Text progressText;
        [SerializeField] private Image progressBar;
        [SerializeField] private Button addPointButton;

        private int currentStatCount;

        private void OnEnable()
        {
            headerText.text = attributeType.ToString().ToUpper();
            currentStatCount = PlayerDataManager.Singleton.LocalPlayerData.character.GetStat(attributeType);
            UpdateDisplay();

            if (!addPointButton) { return; }
            addPointButton.onClick.AddListener(UpdateAttribute);
        }

        private void OnDisable()
        {
            if (!addPointButton) { return; }
            addPointButton.onClick.RemoveListener(UpdateAttribute);
        }

        private void UpdateAttribute()
        {
            if (currentStatCount >= 100) { return; }

            WebRequestManager.Character localCharacter = PlayerDataManager.Singleton.LocalPlayerData.character;
            PlayerDataManager.Singleton.SetCharAttributes(PlayerDataManager.Singleton.LocalPlayerData.id, attributeType, localCharacter.GetStat(attributeType) + 1);

            currentStatCount++;
            UpdateDisplay();

            addPointButton.interactable = currentStatCount < 100;

            int index = WebRequestManager.Singleton.Characters.FindIndex(item => item._id == PlayerDataManager.Singleton.LocalPlayerData.character._id);
            if (index != -1)
            {
                WebRequestManager.Character newCharacter = PlayerDataManager.Singleton.LocalPlayerData.character.SetStat(attributeType, localCharacter.GetStat(attributeType) + 1);
                WebRequestManager.Singleton.Characters[index] = newCharacter;
            }
        }

        private void UpdateDisplay()
        {
            progressText.text = currentStatCount.ToString() + " / 100";
            progressBar.fillAmount = currentStatCount / 100f;
        }
    }
}