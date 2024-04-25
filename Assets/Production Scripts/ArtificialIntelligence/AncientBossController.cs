using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Vi.ArtificialIntelligence
{
    public class AncientBossController : MonoBehaviour
    {
        private const float roamRadius = 5;

        private NavMeshAgent navMeshAgent;
        private Vector3 startingPosition;
        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            startingPosition = transform.position;
        }

        private void Start()
        {
            if (NavMesh.SamplePosition(gameObject.transform.position, out NavMeshHit closestHit, 500f, NavMesh.AllAreas))
                gameObject.transform.position = closestHit.position;
            else
                Debug.LogError("Could not find position on NavMesh!");
        }

        private void Update()
        {
            if (navMeshAgent.isOnNavMesh)
            {
                if (Vector3.Distance(navMeshAgent.destination, transform.position) <= navMeshAgent.stoppingDistance)
                {
                    navMeshAgent.destination = startingPosition + new Vector3(Random.Range(-roamRadius, roamRadius), transform.position.y, Random.Range(-roamRadius, roamRadius));
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(navMeshAgent.destination, 0.5f);
            }
        }
    }
}