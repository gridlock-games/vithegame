using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Vi.Misc
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(LineRenderer))]
    public class NavMeshAgentDebug : MonoBehaviour
    {
        LineRenderer line; //to hold the line Renderer
        NavMeshAgent agent; //to hold the agent of this gameObject

        void Start()
        {
            line = GetComponent<LineRenderer>(); //get the line renderer
            agent = GetComponent<NavMeshAgent>(); //get the agent
        }

        private void Update()
        {
            DrawPath(agent.path);
        }

        void DrawPath(NavMeshPath path)
        {
            if (path.corners.Length < 2) //if the path has 1 or no corners, there is no need
                return;

            line.positionCount = path.corners.Length; //set the array of positions to the amount of corners

            for (int i = 1; i < path.corners.Length; i++)
            {
                line.SetPosition(i, path.corners[i]); //go through each corner and set that to the line renderer's position
            }
        }
    }
}