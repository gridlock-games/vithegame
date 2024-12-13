using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core.Structures;
using Vi.Core.MovementHandlers;
using Vi.Core.CombatAgents;

namespace Vi.Core
{
    [DisallowMultipleComponent]
    public class GameplayTargetFinder : MonoBehaviour
    {
        private CombatAgent combatAgent;
        private Attributes attributes;
        private ObjectiveHandler objectiveHandler;
        private void Awake()
        {
            combatAgent = GetComponent<CombatAgent>();
            attributes = GetComponent<Attributes>();
            objectiveHandler = GetComponent<ObjectiveHandler>();
        }

        private void OnEnable()
        {
            UpdateActivePlayersList();
            UpdateActiveCombatAgentsList();
            UpdateStructureList();
        }

        private void Update()
        {
            if (PlayerDataManager.Singleton.LocalPlayersWasUpdatedThisFrame)
            {
                UpdateActiveCombatAgentsList();
                UpdateActivePlayersList();
            }
            if (PlayerDataManager.Singleton.StructuresListWasUpdatedThisFrame) { UpdateStructureList(); }
        }

        private Structure targetStructure;
        private CombatAgent targetCombatAgent;

        public void SetTarget(Structure structure) { targetStructure = structure; targetCombatAgent = null; }
        public void SetTarget(CombatAgent combatAgent) { targetCombatAgent = combatAgent; targetStructure = null; }
        public void ClearTarget() { targetCombatAgent = null; targetStructure = null; }

        public HittableAgent GetTarget()
        {
            if (targetStructure) { return targetStructure; }
            if (targetCombatAgent) { return targetCombatAgent; }
            return null;
        }

        public bool SetDestination(MovementHandler movementHandler)
        {
            if (objectiveHandler.Objective) { return movementHandler.SetDestination(objectiveHandler.Objective.transform.position); }
            if (targetStructure) { return movementHandler.SetDestination(targetStructure); }
            if (targetCombatAgent) { return movementHandler.SetDestination(targetCombatAgent); }
            Debug.LogError("No Target!");
            return false;
        }

        public List<Attributes> ActivePlayers { get; private set; } = new List<Attributes>();
        private void UpdateActivePlayersList() { ActivePlayers = PlayerDataManager.Singleton.GetActivePlayerObjects(attributes); }

        public List<CombatAgent> ActiveCombatAgents { get; private set; } = new List<CombatAgent>();
        private void UpdateActiveCombatAgentsList() { ActiveCombatAgents = PlayerDataManager.Singleton.GetActiveCombatAgents(combatAgent); }

        public Structure[] ActiveStructures { get; private set; } = new Structure[0];
        private void UpdateStructureList() { ActiveStructures = PlayerDataManager.Singleton.GetActiveStructures(); }
    }
}