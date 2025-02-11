using UnityEngine;
using System.Collections.Generic;
using Vi.Core.CombatAgents;
using Vi.Core.MovementHandlers;

namespace Vi.Core
{
    public class ObjectiveHandler : Objective
    {
        public void SetObjective(ObjectiveHandler objective)
        {
            Objective = objective;
        }

        public Objective Objective { get; private set; }

        private void OnDisable()
        {
            Objective = default;
        }

        //private void Update()
        //{
        //    List<Attributes> players = PlayerDataManager.Singleton.GetActivePlayerObjects();

        //    foreach (Attributes player in players)
        //    {
        //        if (player == this) { continue; }

        //        if (player.TryGetComponent(out ObjectiveHandler objectiveHandler))
        //        {
        //            SetObjective(objectiveHandler);
        //        }
        //        break;
        //    }
        //}
    }
}