using UnityEngine;

namespace Vi.Core
{
    public class Objective : MonoBehaviour
    {
        [SerializeField] private Vector3 UIOffset;

        public Vector3 GetUIPosition()
        {
            return transform.position + transform.rotation * UIOffset;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(GetUIPosition(), 0.25f);
        }
    }
}