using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using Vi.Core.Structures;

namespace Vi.UI
{
    [RequireComponent(typeof(Canvas))]
    public class OnScreenHittableAgentHealthBar : MonoBehaviour
    {
        [SerializeField] private Image HPImage;
        [SerializeField] private Image intermHPImage;
        [SerializeField] private StatusIconLayoutGroup statusIconLayoutGroup;

        public HittableAgent HittableAgent { get; private set; }
        public void Initialize(HittableAgent hittableAgent)
        {
            HittableAgent = hittableAgent;
            statusIconLayoutGroup.Initialize(hittableAgent.StatusAgent);
            HPImage.color = PlayerDataManager.Singleton.GetRelativeTeamColor(hittableAgent.GetTeam());
            Update();
            SetActive(true);
        }

        public void SetActive(bool isActive)
        {
            canvas.enabled = isActive;
        }

        private Canvas canvas;
        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            SetActive(false);
        }

        private float lastHP = -1;
        private float lastMaxHP = -1;
        private void Update()
        {
            if (!HittableAgent) { SetActive(false); return; }

            float HP = HittableAgent.GetHP();
            if (HP < 0.1f & HP > 0) { HP = 0.1f; }

            float maxHP = HittableAgent.GetMaxHP();

            if (!Mathf.Approximately(lastHP, HP) | !Mathf.Approximately(lastMaxHP, maxHP))
            {
                HPImage.fillAmount = HP / maxHP;
            }

            lastHP = HP;
            lastMaxHP = maxHP;

            intermHPImage.fillAmount = Mathf.Lerp(intermHPImage.fillAmount, HP / maxHP, Time.deltaTime * PlayerCard.fillSpeed);
        }
    }
}