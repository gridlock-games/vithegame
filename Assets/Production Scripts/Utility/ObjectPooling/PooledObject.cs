using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Vi.Utility
{
    public class PooledObject : MonoBehaviour
    {
        [SerializeField] private int numberOfObjectsToPool = 5;

        public int GetNumberOfObjectsToPool() { return numberOfObjectsToPool; }

        [SerializeField] private int pooledObjectIndex = -1;
        public int GetPooledObjectIndex() { return pooledObjectIndex; }

        public void SetPooledObjectIndex(int index)
        {
            #if UNITY_EDITOR
            if (index != pooledObjectIndex) { UnityEditor.EditorUtility.SetDirty(this); }
            #endif
            pooledObjectIndex = index;
        }

        [SerializeField] private bool isPrewarmObject;

        public void SetIsPrewarmStatus(bool isPrewarmObject)
        {
            this.isPrewarmObject = isPrewarmObject;
        }

        public bool IsPrewarmObject() { return isPrewarmObject; }

        public UnityAction OnSpawnFromPool;
        public UnityAction OnReturnToPool;

        public void InvokeOnSpawnFromPoolEvent()
        {
            if (OnSpawnFromPool != null) { OnSpawnFromPool.Invoke(); }
        }
        public void InvokeOnReturnToPoolEvent()
        {
            if (OnReturnToPool != null) { OnReturnToPool.Invoke(); }
        }

        private List<Vector2> childSizeDeltas = new List<Vector2>();
        private Vector2 offsetMax;
        private Vector2 offsetMin;
        private void Awake()
        {
            if (transform is RectTransform rectTransform)
            {
                offsetMax = rectTransform.offsetMax;
                offsetMin = rectTransform.offsetMin;

                foreach (Transform child in transform)
                {
                    childSizeDeltas.Add(((RectTransform)child).sizeDelta);
                }
                OnSpawnFromPool += ResetSizes;
            }
        }

        private void ResetSizes()
        {
            if (transform is RectTransform rectTransform)
            {
                rectTransform.offsetMax = offsetMax;
                rectTransform.offsetMin = offsetMin;

                int counter = 0;
                foreach (Transform child in transform)
                {
                    if (counter < childSizeDeltas.Count)
                    {
                        ((RectTransform)child).sizeDelta = childSizeDeltas[counter];
                        counter++;
                    }
                }
            }
            else
            {
                Debug.LogError("Calling PooledObject.ResetSizes() on a transform that isn't a rect transform!");
            }
        }
    }
}