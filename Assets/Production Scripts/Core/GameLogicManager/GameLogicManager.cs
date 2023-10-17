using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.GameLogicManager
{
    public class GameLogicManager : MonoBehaviour
    {
        public enum GameMode
        {
            Duel,
            TeamElimination,
            TeamDeathmatch
        }

        public enum Team
        {
            Environment,
            Spectator,
            Competitor,
            Red,
            Blue,
        }

        public static GameLogicManager Singleton { get { return _singleton; } }
        private static GameLogicManager _singleton;

        public bool ProcessMeleeHit()
        {
            return true;
        }

        public bool ProcessProjectileHit()
        {
            return true;
        }
    }
}