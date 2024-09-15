using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core.Structures;
using Vi.Core.MovementHandlers;

namespace Vi.Core
{
    [DisallowMultipleComponent]
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
            if (targetStructure) { return movementHandler.SetDestination(targetStructure); }
            if (targetCombatAgent) { return movementHandler.SetDestination(targetCombatAgent); }
            Debug.LogError("No Target!");
            return false;
        }

        public Vector3 GetPotentialDestination(MovementHandler movementHandler)
        {
            if (targetStructure) { return movementHandler.GetPotentialDestination(targetStructure); }
            if (targetCombatAgent) { return movementHandler.GetPotentialDestination(targetCombatAgent); }
            Debug.LogError("No Target!");
            return Vector3.zero;
        }

        public List<CombatAgent> ActiveCombatAgents { get; private set; } = new List<CombatAgent>();
        private void UpdateActivePlayersList() { ActiveCombatAgents = PlayerDataManager.Singleton.GetActiveCombatAgents(combatAgent); }

        public Structure[] ActiveStructures { get; private set; } = new Structure[0];
        private void UpdateStructureList() { ActiveStructures = PlayerDataManager.Singleton.GetActiveStructures(); }
    }
}