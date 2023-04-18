using System;
using System.Collections.Generic;
using UnityEngine;

namespace NJG.Graph
{
    [Serializable]
    public class TriggerState : Node
    {
        private GraphTrigger trigger;

        public GraphTrigger GetTrigger(GraphReactions reactions)
        {
            if (!reactions) reactions = Root.sourceReactions;
            return reactions ? reactions.GetReaction<GraphTrigger>(this) : null;
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            bool canDelete =  Root != null && Root.sourceReactions != null;
            if (canDelete)
            {
                Root.sourceReactions.RemoveReaction(this);
            }
#endif
            
        }

        private void OnDisable()
        {
            if (trigger)
            {
                trigger.onTriggerExecute = null;
                trigger.onExecute.RemoveAllListeners();
                trigger.onExecute = null;
                trigger = null;
            }
        }

        public override void OnEnter(GraphReactions reactions, GameObject invoker)
        {
            if (!reactions) return;
            base.OnEnter(reactions, invoker);
            if (Application.isPlaying && !reactions.nodesSubscribed.Contains(id))
            {
                trigger = GetTrigger(reactions);
                if (trigger)
                {
                    trigger.onTriggerExecute += (inv =>
                    {
                        OnExit(reactions, inv); 
                    } );
                }
                reactions.nodesSubscribed.Add(id);
            }
        }

        public override void OnExit(GraphReactions reactions, GameObject invoker)
        {
            base.OnExit(reactions, invoker);
#if UNITY_EDITOR
            internalExecuting = true;
#endif

            List<Node> nodes = ValidateTransitions(reactions, invoker);
            for (int i = 0, imax = nodes.Count; i < imax; i++) nodes[i].OnEnter(reactions, invoker);
        }
    }
}