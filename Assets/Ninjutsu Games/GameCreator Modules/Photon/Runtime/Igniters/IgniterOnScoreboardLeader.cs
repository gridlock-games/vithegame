using System;
using ExitGames.Client.Photon;
using GameCreator.Variables;
using NJG.PUN;
using NJG.PUN.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace GameCreator.Core
{
    [AddComponentMenu("")]
    public class IgniterOnScoreboardLeader : Igniter
    {
#if UNITY_EDITOR
        public new static string NAME = "Photon/On Scoreboard Leader";
        public new static string COMMENT = "Triggers when a player reaches #1 rank in the scoreboard.";
        public new static string ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Igniters/";
#endif
        [Space]
        [VariableFilter(Variable.DataType.GameObject)]
        public VariableProperty number1Player = new VariableProperty(Variable.VarType.GlobalVariable);

        new private void OnEnable()
        {
#if UNITY_EDITOR
            hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
#endif
            Scoreboard.OnFirstPlayer.AddListener(OnScoreboardLeader);
        }

        private void OnDisable()
        {
            Scoreboard.OnFirstPlayer.RemoveListener(OnScoreboardLeader);

        }

        private void OnScoreboardLeader(Player player)
        {
            var invoker = player.TagObject as GameObject;
            number1Player.Set(invoker, gameObject);
            ExecuteTrigger(invoker);
        }
    }
}