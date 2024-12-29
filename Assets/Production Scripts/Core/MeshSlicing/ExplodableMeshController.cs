using UnityEngine;
using System.Collections.Generic;
using Vi.Utility;

namespace Vi.Core.MeshSlicing
{
    public class ExplodableMeshController : MonoBehaviour
    {
        public bool CanCreateMoreInstances { get; private set; }

        public List<PooledObject> Instances { get; private set; }


        private ExplodableMesh[] explodableMeshes;

        private void Awake()
        {
            explodableMeshes = GetComponentsInChildren<ExplodableMesh>();
        }

        private const int instanceLimit = 32;

        private List<PooledObject> sliceInstances = new List<PooledObject>();
        public void Explode()
        {
            foreach (ExplodableMesh explodableMesh in explodableMeshes)
            {
                sliceInstances.AddRange(explodableMesh.Explode(instanceLimit / explodableMeshes.Length));
            }
        }

        public void ClearInstances()
        {
            foreach (PooledObject sliceInstance in sliceInstances)
            {
                ObjectPoolingManager.ReturnObjectToPool(sliceInstance);
            }
            sliceInstances.Clear();
        }
    }
}