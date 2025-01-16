using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Utility;

namespace Vi.Core
{
    [RequireComponent(typeof(PooledObject))]
    public class StatusVFX : MonoBehaviour
    {
        [SerializeField] private Vector3 positionOffset;
        [SerializeField] private Vector3 rotationOffset;
        [SerializeField] private Vector3 scale = new Vector3(1, 1, 1);

        private PooledObject pooledObject;
        private void Awake()
        {
            pooledObject = GetComponent<PooledObject>();
        }

        private void OnEnable()
        {
            if (pooledObject.IsPrewarmObject()) { return; }

            if (transform.parent == null) { Debug.LogWarning("Status VFX parent is null! " + this); }

            transform.localRotation = Quaternion.Euler(rotationOffset);
            transform.localPosition = transform.localRotation * positionOffset;
            transform.localScale = scale;
        }
    }
}