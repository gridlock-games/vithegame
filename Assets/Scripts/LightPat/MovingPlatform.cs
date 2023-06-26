using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LightPat.Core
{
    public class MovingPlatform : MonoBehaviour
    {
        public float moveSpeed = 7;
        public Vector3 moveTo;
        private Vector3 moveFrom;

        bool mode;

        private void Start()
        {
            moveFrom = transform.position;
        }

        private void Update()
        {
            if (!mode)
                transform.position = Vector3.MoveTowards(transform.position, moveTo, Time.deltaTime * moveSpeed);
            else
                transform.position = Vector3.MoveTowards(transform.position, moveFrom, Time.deltaTime * moveSpeed);

            if (!mode)
            {
                if (Vector3.Distance(transform.position, moveTo) < 1)
                    mode = !mode;
            }
            else
            {
                if (Vector3.Distance(transform.position, moveFrom) < 1)
                    mode = !mode;
            }
        }
    }
}