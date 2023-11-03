using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.ProceduralAnimations
{
    public class FollowTarget : MonoBehaviour
    {
        [SerializeField] private bool move = true;
        [SerializeField] private bool rotate = true;
        [SerializeField] private bool lerp;
        [SerializeField] private float lerpSpeed = 5;

        [HideInInspector] public Transform target;

        private void LateUpdate()
        {
            if (target)
            {
                if (lerp)
                {
                    if (move) { transform.position = Vector3.MoveTowards(transform.position, target.position, lerpSpeed * Time.deltaTime); }
                    if (rotate) { transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, lerpSpeed * Time.deltaTime); }
                }
                else
                {
                    if (move) { transform.position = target.position; }
                    if (rotate) { transform.rotation = target.rotation; }
                }
            }
        }
    }
}