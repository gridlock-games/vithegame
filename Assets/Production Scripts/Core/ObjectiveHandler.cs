using UnityEngine;
using System.Collections.Generic;
using Vi.Core.CombatAgents;
using Vi.Core.MovementHandlers;

namespace Vi.Core
{
    public class ObjectiveHandler : Objective
    {
        public void SetObjective(Objective objective)
        {
            this.objective = objective;
        }

        public Objective Objective
        {
            get
            {
                if (objective)
                {
                    if (objective.gameObject.activeInHierarchy)
                    {
                        return objective;
                    }
                }
                return null;
            }
        }

        private Objective objective;

        private void OnDisable()
        {
            objective = default;
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