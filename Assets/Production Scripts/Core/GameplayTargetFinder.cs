using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core.Structures;

namespace Vi.Core
{
    public class GameplayTargetFinder : MonoBehaviour
    {
        private CombatAgent combatAgent;
        private void Awake()
        {
            combatAgent = GetComponent<CombatAgent>();
        }

        private void OnEnable()
        {
            UpdateActivePlayersList();
            UpdateStructureList();
        }

        private void Update()
        {
            if (PlayerDataManager.Singleton.LocalPlayersWasUpdatedThisFrame) { UpdateActivePlayersList(); }
            if (PlayerDataManager.Singleton.StructuresListWasUpdatedThisFrame) { UpdateStructureList(); }
        }

        [HideInInspector] public HittableAgent target;

        public List<CombatAgent> ActiveCombatAgents { get; private set; } = new List<CombatAgent>();
        private void UpdateActivePlayersList() { ActiveCombatAgents = PlayerDataManager.Singleton.GetActiveCombatAgents(combatAgent); }

        public Structure[] ActiveStructures { get; private set; } = new Structure[0];
        private void UpdateStructureList() { ActiveStructures = PlayerDataManager.Singleton.GetActiveStructures(); }
    }
}