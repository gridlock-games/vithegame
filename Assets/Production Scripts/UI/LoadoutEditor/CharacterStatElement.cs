using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.Core.CombatAgents;
using UnityEngine.Events;

namespace Vi.UI
{
    public class CharacterStatElement : MonoBehaviour
    {
        [SerializeField] private CharacterManager.Character.AttributeType attributeType;
        [SerializeField] private Text headerText;
        [SerializeField] private Text progressText;
        [SerializeField] private Image progressBar;
        [SerializeField] private Button addPointButton;

        public CharacterManager.Character.AttributeType AttributeType { get { return attributeType; } }
        public Button GetAddPointButton() { return addPointButton; }

        public int CurrentStatCount { get; set; }

        public UnityAction<CharacterStatElement, int> OnStatCountChange;

        private void OnEnable()
        {
            headerText.text = attributeType.ToString().ToUpper();
            CurrentStatCount = PlayerDataManager.Singleton.LocalPlayerData.character.GetStat(attributeType);
            UpdateDisplay();

            if (!addPointButton) { return; }
            addPointButton.interactable = CurrentStatCount < 100 & GetAvailableSkillPoints() > 0;
            addPointButton.gameObject.SetActive(PlayerDataManager.Singleton.GetGameMode() == PlayerDataManager.GameMode.None);
            addPointButton.onClick.AddListener(UpdateAttribute);
        }

        private void OnDisable()
        {
            if (!addPointButton) { return; }
            addPointButton.onClick.RemoveListener(UpdateAttribute);
        }

        private int GetAvailableSkillPoints()
        {
            int characterIndex = WebRequestManager.Singleton.CharacterManager.Characters.FindIndex(item => item._id == PlayerDataManager.Singleton.LocalPlayerData.character._id);
            int availableSkillPoints = 0;
            if (characterIndex != -1)
            {
                if (WebRequestManager.Singleton.CharacterManager.TryGetCharacterAttributesInLookup(WebRequestManager.Singleton.CharacterManager.Characters[characterIndex]._id.ToString(), out var stats))
                {
                    availableSkillPoints = stats.GetAvailableSkillPoints(WebRequestManager.Singleton.CharacterManager.Characters[characterIndex].attributes);
                    return availableSkillPoints;
                }
            }
            return 0;
        }

        private void UpdateAttribute()
        {
            addPointButton.interactable = CurrentStatCount < 100 & GetAvailableSkillPoints() > 0;
            if (!addPointButton.interactable) { return; }

            CurrentStatCount++;
            PlayerDataManager.Singleton.SetCharAttributes(PlayerDataManager.Singleton.LocalPlayerData.id, attributeType, CurrentStatCount);

            UpdateDisplay();

            int characterIndex = WebRequestManager.Singleton.CharacterManager.Characters.FindIndex(item => item._id == PlayerDataManager.Singleton.LocalPlayerData.character._id);
            if (characterIndex != -1)
            {
                CharacterManager.Character newCharacter = PlayerDataManager.Singleton.LocalPlayerData.character.SetStat(attributeType, CurrentStatCount);
                WebRequestManager.Singleton.CharacterManager.Characters[characterIndex] = newCharacter;
            }

            int availableStatPoints = GetAvailableSkillPoints();
            addPointButton.interactable = CurrentStatCount < 100 & availableStatPoints > 0;

            OnStatCountChange?.Invoke(this, availableStatPoints);
        }

        public void UpdateDisplay()
        {
            progressText.text = CurrentStatCount.ToString() + " / 100";
            progressBar.fillAmount = CurrentStatCount / 100f;
        }
    }
}