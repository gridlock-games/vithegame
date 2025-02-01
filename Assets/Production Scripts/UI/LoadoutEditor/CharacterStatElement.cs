using UnityEngine;
using UnityEngine.UI;

namespace Vi.UI
{
    public class CharacterStatElement : MonoBehaviour
    {
        public enum AttributeType
        {
            Strength,
            Vitality,
            Agility,
            Dexterity,
            Intelligence
        }

        [SerializeField] private AttributeType attributeType;
        [SerializeField] private Text headerText;
        [SerializeField] private Text progressText;
        [SerializeField] private Image progressBar;
        [SerializeField] private Button addPointButton;

        private void Start()
        {
            headerText.text = attributeType.ToString().ToUpper();
        }

        public void Initialize(string characterId)
        {

        }
    }
}