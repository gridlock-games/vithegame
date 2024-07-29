using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Utility;

namespace Vi.Core.VFX
{
    [RequireComponent(typeof(PooledObject))]
    public class ActionVFXPreview : ActionVFX
    {
        public bool CanCast { get; private set; }

        private ParticleSystem ps;
        private Color originalParticleSystemColor;
        private Color noVFXWillBeSpawnedColor = Color.red;
        
        private void Start()
        {
            ps = GetComponentInChildren<ParticleSystem>();
            originalParticleSystemColor = ps.main.startColor.color;
            noVFXWillBeSpawnedColor.a = originalParticleSystemColor.a;
        }

        private void LateUpdate()
        {
            if (transformType == TransformType.Projectile)
            {
                transform.LookAt(transform.parent.GetComponent<AnimationHandler>().GetAimPoint());
                CanCast = true;
            }
            else if (transformType == TransformType.ConformToGround)
            {
                Vector3 startPos = transform.parent.position + transform.parent.rotation * raycastOffset;
                RaycastHit[] allHits = Physics.RaycastAll(startPos, Vector3.down, raycastMaxDistance, LayerMask.GetMask(MovementHandler.layersToAccountForInMovement), QueryTriggerInteraction.Ignore);
                Vector3 fartherStartPos = transform.parent.position + transform.parent.rotation * fartherRaycastOffset;
                RaycastHit[] fartherAllHits = Physics.RaycastAll(fartherStartPos, Vector3.down, raycastMaxDistance * 2, LayerMask.GetMask(MovementHandler.layersToAccountForInMovement), QueryTriggerInteraction.Ignore);
                System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));
                System.Array.Sort(fartherAllHits, (x, y) => x.distance.CompareTo(y.distance));

                bool bHit = false;
                bool fartherBHit = false;
                RaycastHit floorHit = new RaycastHit();
                RaycastHit fartherFloorHit = new RaycastHit();

                foreach (RaycastHit hit in allHits)
                {
                    bHit = true;
                    floorHit = hit;
                    break;
                }

                foreach (RaycastHit hit in fartherAllHits)
                {
                    fartherBHit = true;
                    fartherFloorHit = hit;
                    break;
                }

                if (Application.isEditor)
                {
                    if (bHit) { Debug.DrawLine(startPos, floorHit.point, Color.red, Time.deltaTime); }
                    if (fartherBHit) { Debug.DrawLine(fartherStartPos, fartherFloorHit.point, Color.magenta, Time.deltaTime); }
                }

                if (bHit & fartherBHit)
                {
                    transform.position = floorHit.point + transform.parent.rotation * vfxPositionOffset;
                    Vector3 rel = fartherFloorHit.point - transform.position;
                    Quaternion groundRotation = rel == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(rel, lookRotationUpDirection);
                    transform.rotation = groundRotation * Quaternion.Euler(vfxRotationOffset);

                    CanCast = true;
                }
                else
                {
                    transform.position = new Vector3(startPos.x, transform.parent.position.y, startPos.z);
                    transform.rotation = transform.parent.rotation * Quaternion.Euler(vfxRotationOffset);
                }
                ChangeParticleColor(bHit ? originalParticleSystemColor : noVFXWillBeSpawnedColor);
            }
            else
            {
                CanCast = true;
            }
        }

        private Color lastColorSet;
        private void ChangeParticleColor(Color color)
        {
            if (!ps) { return; }
            if (color == lastColorSet) { return; }

            var main = ps.main;
            main.startColor = color;
            ps.Clear();
            ps.Play();

            lastColorSet = color;
        }
    }
}