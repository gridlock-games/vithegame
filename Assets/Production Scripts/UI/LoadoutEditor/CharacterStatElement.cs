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

        public int CurrentStatCount { get; set; }

        private void OnEnable()
        {
            headerText.text = attributeType.ToString().ToUpper();
            CurrentStatCount = PlayerDataManager.Singleton.LocalPlayerData.character.GetStat(attributeType);
            UpdateDisplay();

            if (!addPointButton) { return; }
            addPointButton.gameObject.SetActive(PlayerDataManager.Singleton.GetGameMode() == GameMode.None);
            addPointButton.onClick.AddListener(UpdateAttribute);
        }

        private void OnDisable()
        {
            if (!addPointButton) { return; }
            addPointButton.onClick.RemoveListener(UpdateAttribute);
        }

        private void UpdateAttribute()
        {
            int characterIndex = WebRequestManager.Singleton.Characters.FindIndex(item => item._id == PlayerDataManager.Singleton.LocalPlayerData.character._id);
            int availableSkillPoints = 0;
            if (characterIndex != -1)
            {
                if (WebRequestManager.Singleton.TryGetCharacterAttributesInLookup(WebRequestManager.Singleton.Characters[characterIndex]._id.ToString(), out var stats))
                {
                    availableSkillPoints = stats.GetAvailableSkillPoints(WebRequestManager.Singleton.Characters[characterIndex].attributes);
                    if (availableSkillPoints <= 0) { return; }
                }
            }

            if (CurrentStatCount >= 100) { return; }

            CurrentStatCount++;
            PlayerDataManager.Singleton.SetCharAttributes(PlayerDataManager.Singleton.LocalPlayerData.id, attributeType, CurrentStatCount);

            UpdateDisplay();

            addPointButton.interactable = CurrentStatCount < 100 & availableSkillPoints > 0;

            if (characterIndex != -1)
            {
                WebRequestManager.Character newCharacter = PlayerDataManager.Singleton.LocalPlayerData.character.SetStat(attributeType, CurrentStatCount);
                WebRequestManager.Singleton.Characters[characterIndex] = newCharacter;
            }
        }

        public void UpdateDisplay()
        {
            progressText.text = CurrentStatCount.ToString() + " / 100";
            progressBar.fillAmount = CurrentStatCount / 100f;
        }
    }
}