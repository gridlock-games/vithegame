using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

#if USE_TMP
using TMPro;
#endif

namespace NJG.PUN.UI
{
    public class ScoreboardEntry : MonoBehaviour
    {
#if USE_TMP
        [SerializeField] TextMeshProUGUI scoreLabel;
        [SerializeField] TextMeshProUGUI nameLabel;
#else
        [SerializeField] Text scoreLabel;
        [SerializeField] Text nameLabel;
#endif
        public Player Player => player;
        public int Score => player.GetScore();

        private Player player;
        private Scoreboard scoreboard;

        private const string NO = "n0";

        //store player for this entry
        //set init value and color
        public void Set(Player player, Scoreboard scoreboard)
        {
            this.player = player;
            this.scoreboard = scoreboard;
            UpdateScore();
#if USE_TMP
            if (scoreboard.boldLocalPlayer && player.IsLocal) nameLabel.fontStyle = FontStyles.Bold;
#else
            if (scoreboard.boldLocalPlayer && player.IsLocal) nameLabel.fontStyle = FontStyle.Bold;
#endif
            //m_label.color = PhotonNetwork.LocalPlayer == m_player ? Color.green : Color.red;
        }

        public void UpdateName()
        {
            string nickName = string.IsNullOrEmpty(player.NickName) ? 
                string.Format(scoreboard.defaultNameFormat, player.UserId) : player.NickName;

            nameLabel.text = string.Format(scoreboard.entryFormat, transform.GetSiblingIndex() + 1, nickName);

        }
        
        public void UpdateScore()
        {
            scoreLabel.text = player.GetScore().ToString(NO);;
        }
    }
}