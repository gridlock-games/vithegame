using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    public class ActionVFXPreview : ActionVFX
    {
        private void LateUpdate()
        {
            if (transformType == TransformType.Projectile)
            {
                transform.LookAt(transform.parent.GetComponent<AnimationHandler>().GetAimPoint());
            }
            else if (transformType == TransformType.ConformToGround)
            {
                Vector3 startPos = transform.parent.position + transform.parent.rotation * raycastOffset;
                RaycastHit[] allHits = Physics.RaycastAll(startPos, Vector3.down, raycastMaxDistance, LayerMask.GetMask(new string[] { "Default" }), QueryTriggerInteraction.Ignore);
                Debug.DrawRay(startPos, Vector3.down * raycastMaxDistance, Color.red, 3);
                System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));

                bool bHit = false;
                RaycastHit floorHit = new RaycastHit();

                foreach (RaycastHit hit in allHits)
                {
                    bHit = true;
                    floorHit = hit;

                    break;
                }

                if (bHit)
                {
                    transform.position = floorHit.point + transform.parent.rotation * vfxPositionOffset;
                    transform.rotation = Quaternion.LookRotation(Vector3.Cross(floorHit.normal, crossProductDirection), lookRotationUpDirection) * transform.parent.rotation * Quaternion.Euler(vfxRotationOffset);
                }
                else
                {
                    transform.position = transform.parent.position + transform.parent.rotation * vfxPositionOffset;
                }
            }
        }
    }
}