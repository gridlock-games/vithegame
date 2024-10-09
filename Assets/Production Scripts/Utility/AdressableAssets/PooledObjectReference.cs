using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Vi.Utility
{
    [Serializable]
    public class PooledObjectReference : ComponentReference<PooledObject>
    {
        public PooledObjectReference(string guid) : base(guid)
        {

        }
    }
}