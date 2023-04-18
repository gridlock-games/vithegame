using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace NJG.Graph
{
    [Serializable]
    public class Node : ScriptableObject
    {
#if UNITY_EDITOR
        [NonSerialized]
        public float progress;

        [NonSerialized]
        public bool internalExecuting;
        [NonSerialized]
        public bool isSelected;

        public bool hasCustomName;
        public bool disableComments;
#endif
        public enum TransitionMode
        {
            Parallel,
            Selective
        }

        public TransitionMode transitionMode = TransitionMode.Parallel;
        
        public int id;
        public Rect position;
        public int color;

        [TextArea(1, 10)]
        public string description;
        public StateMachine parent;
        public Transition[] transitions = new Transition[0];

        //[NonSerialized]
        private bool isStartNode;
        public bool IsStartNode 
        { 
            get => isStartNode;
            set { isStartNode = value; if(value) parent.startNode = this; } 
        }

        [NonSerialized]
        public bool isEntered;

        public override string ToString()
        {
            return $"Node name: {name} id: {id}";
        }

        public StateMachine Root
        {
            get
            {
                if ((!parent && this is StateMachine)) 
                {
                    return (StateMachine)this;
                }

                if (!parent) return null;

                try
                {
                    StateMachine root = parent.Root;
                    return root;
                }
                catch(Exception e)
                {
                    if (Application.isPlaying)
                    {
                        Debug.LogError("Cannot get Root. " + e.Message);
                    }
                }

                return null;
            }
        }

        public Transition[] InTransitions
        {
            get
            {
                mTransitions.Clear();
                for(int i = 0; i < parent.nodes.Length; i++)
                {
                    Node node = parent.nodes[i];

                    for (int e = 0; e < node.transitions.Length; e++)
                    {
                        Transition transition = node.transitions[e];
                        if (transition.toNode == this) mTransitions.Add(transition);
                    }
                }
                return mTransitions.ToArray();
            }
        }

        private List<Transition> mTransitions = new List<Transition>();

        // PUBLIC FUNCTIONS: ----------------------------------------------------------------------------

        public virtual List<Node> ValidateTransitions(GraphReactions reactions, GameObject invoker)
        {
            List<Node> nodes = new List<Node>();
            if (parent)
            {
                nodes.AddRange(parent.ValidateTransitions(reactions, invoker));
            }
            for (int i = 0, imax = transitions.Length; i < imax; i++)
            {
                Node node = transitions[i].Validate(this, reactions, invoker);
                if (node)
                {
                    nodes.Add(node);
                    if (transitionMode == TransitionMode.Selective) break;
                }
            }
            return nodes;
        }

        public virtual void OnEnter(GraphReactions reactions, GameObject invoker)
        {
            isEntered = true;
        }

        public virtual void OnExit(GraphReactions reactions, GameObject invoker)
        {
            isEntered = false;
        }
    }
}