#pragma warning disable

//#define USE_TMP

using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Photon.Pun.UtilityScripts;

#if USE_TMP
using TMPro;
#endif

namespace NJG.PUN.UI
{

    public class Scoreboard : MonoBehaviourPunCallbacks
    {
        // [System.Serializable]
        // public class ScoreEntry
        // {
        //     public Player player;
        //     public int rank;
        //     public int score;
        // }

        [SerializeField] private int maxEntries = 5;
        [SerializeField] internal bool boldLocalPlayer = true;
        [SerializeField] internal string entryFormat = "{0}. {1}";
        [SerializeField] internal string defaultNameFormat = "Player {0}";
        [SerializeField] private Transform container = null;
        [SerializeField] private ScoreboardEntry prefab;
        [SerializeField] private List<ScoreboardEntry> entries = new List<ScoreboardEntry>();

        public static FirstPlayerEvent OnFirstPlayer = new FirstPlayerEvent();
        private Player lastFirtPlayer;

        [System.Serializable]
        public class FirstPlayerEvent : UnityEvent<Player> { }

        private void Start()
        {
            lastFirtPlayer = null;
            UpdateContent();
        }

        /*private void UpdateContent()
        {
            if (!NetworkManager.Instance || NetworkManager.Instance.Players == null) return;

            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }

            bool playerIncluded = false;
            ScoreEntry playerEntry = null;

            entries.Clear();
            for (int i = 0; i < NetworkManager.Instance.Players.Length; i++)
            {
                var player = NetworkManager.Instance.Players[i];
                int score = player.GetScore();
                ScoreEntry entry = new ScoreEntry() { player = player, score = score, rank = i + 1 };
                if (player.NickName == PhotonNetwork.LocalPlayer.NickName) playerEntry = entry;
                entries.Add(entry);
            }

            entries.Sort((x, y) => y.score.CompareTo(x.score));

            for (int i = 0; i < entries.Count; i++)
            {
                if (i >= maxEntries) break;

                var s = entries[i];

                // Check that the player is part of the top N
                if (s.player.IsLocal) playerIncluded = true;

                // Our player is #1 lets brag about it
                if (i == 0) OnFirstPlayer?.Invoke(s.player);

                // If the player has not been added yet and the last entry is not the player ignore it
                // We will add the player afterwards
                if (i == maxEntries - 1 && !playerIncluded && !s.player.IsLocal) break;

                AddEntry(s, i + 1);
            }

            // Player was not part of the top N lets add it to the list.
            if (!playerIncluded) AddEntry(playerEntry);
        }*/
        
        private void UpdateContent()
        {
            if(!PhotonNetwork.InRoom) return;
            
            //iterate through all player to update score
            //if no entry exists create one
            foreach (var targetPlayer in PhotonNetwork.CurrentRoom.Players.Values)
            {
                var targetEntry = entries.Find(x => Equals(x.Player, targetPlayer));

                if (!targetEntry)
                {
                    targetEntry = CreateNewEntry(targetPlayer);
                }

                targetEntry.UpdateScore();
            }

            SortEntries();
        }
        
        private void SortEntries()
        {
            //sort entries in list
            entries.Sort((a, b) => b.Score.CompareTo(a.Score));

            //sort child order
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                entry.transform.SetSiblingIndex(i);
                entry.UpdateName();
                
                // Our player is #1 lets brag about it
                if (i == 0 && !Equals(lastFirtPlayer, entry.Player))
                {
                    //Debug.LogWarningFormat("#1 Player: {0}", entry.Player);
                    lastFirtPlayer = entry.Player;
                    OnFirstPlayer?.Invoke(entry.Player);
                }

                if (i >= maxEntries)
                {
                    entries.Remove(entry);
                    Destroy(entry.gameObject);
                }
            }
        }
        
        private ScoreboardEntry CreateNewEntry(Player newPlayer)
        {
            var newEntry = Instantiate(prefab, container, false);
            newEntry.Set(newPlayer, this);
            entries.Add(newEntry);
            return newEntry;
        }
        
        private void RemoveEntry(Player targetPlayer)
        {
            var targetEntry = entries.Find(x => Equals(x.Player, targetPlayer));
            entries.Remove(targetEntry);
            Destroy(targetEntry.gameObject);
        }

        /*private void AddEntry(ScoreEntry entry, int rank = -1)
        {
            if (entry?.player == null) return;

            GameObject go = Instantiate(prefab, container, false);
            string nickName = string.IsNullOrEmpty(entry.player.NickName) ? string.Format(defaultNameFormat, entry.player.UserId) : entry.player.NickName;
            go.name = string.Format(entryFormat, rank == -1 ? entry.rank : rank, nickName);

#if USE_TMP
            TextMeshProUGUI scoreLabel = go.transform.Find(SCORE).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI nameLabel = go.transform.Find(NAME).GetComponent<TextMeshProUGUI>();
#else
            Text scoreLabel = go.transform.Find(SCORE).GetComponent<Text>();
            Text nameLabel = go.transform.Find(NAME).GetComponent<Text>();
#endif

            scoreLabel.text = entry.score.ToString(NO);
            nameLabel.text = string.Format(entryFormat, rank == -1 ? entry.rank : rank, nickName);
#if USE_TMP
            if (boldLocalPlayer && entry.player.IsLocal) nameLabel.fontStyle = FontStyles.Bold;
#else
            if (boldLocalPlayer && entry.player.IsLocal) nameLabel.fontStyle = FontStyle.Bold;
#endif

        }*/

        public override void OnJoinedRoom()
        {
            CreateNewEntry(PhotonNetwork.LocalPlayer);
            UpdateContent();
        }

        public override void OnLeftRoom()
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
            entries.Clear();
            lastFirtPlayer = null;
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            CreateNewEntry(newPlayer);
            UpdateContent();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            RemoveEntry(otherPlayer);

            UpdateContent();
        }

        public override void OnPlayerPropertiesUpdate(Player target, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (changedProps.ContainsKey(PunPlayerScores.PlayerScoreProp))
            {
                UpdateContent();
            }
        }

        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            //UpdateContent();
        }
    }
}
