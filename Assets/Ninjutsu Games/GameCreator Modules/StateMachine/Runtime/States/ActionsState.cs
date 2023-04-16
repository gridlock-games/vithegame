using UnityEngine;
using GameCreator.Core;
using System.Collections.Generic;

namespace NJG.Graph
{
    [System.Serializable]
    public class ActionsState : Node
    {
        public bool resetState;
        public System.Action<GraphReactions> onExecute;

        public IActionsList GetActions(GraphReactions reactions)
        {
            if (!reactions) reactions = this.Root.sourceReactions;
            return reactions ? reactions.GetReaction<IActionsList>(this) : null;
        }

        public override void OnEnter(GraphReactions reactions, GameObject invoker)
        {
            if (Application.isPlaying)
            {
#if UNITY_EDITOR
                internalExecuting = true;
#endif
                base.OnEnter(reactions, invoker);
                IActionsList actions = GetActions(reactions);
                
                if (actions)
                {
                    onExecute?.Invoke(reactions);
                    actions.Execute(invoker, () => OnExit(reactions, invoker));
                }
            }
        }

        public override void OnExit(GraphReactions reactions, GameObject invoker)
        {
            base.OnExit(reactions, invoker);
            List<Node> nodes = ValidateTransitions(reactions, invoker);
            for (int i = 0, imax = nodes.Count; i < imax; i++)
            {
                if(nodes[i] is StateMachine)
                {
                    var sm = nodes[i] as StateMachine;
                    if (sm.startNode) sm.startNode.OnEnter(reactions, invoker);
                }
                nodes[i].OnEnter(reactions, invoker);
            }
        }
    }
}