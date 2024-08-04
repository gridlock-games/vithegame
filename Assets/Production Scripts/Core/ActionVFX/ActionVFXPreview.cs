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

        RaycastHit[] allHits = new RaycastHit[10];
        RaycastHit[] fartherAllHits = new RaycastHit[10];
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
                int allHitsCount = Physics.RaycastNonAlloc(startPos, Vector3.down.normalized, allHits, raycastMaxDistance, LayerMask.GetMask(layersToAccountForInRaycasting), QueryTriggerInteraction.Ignore);
                Vector3 fartherStartPos = transform.parent.position + transform.parent.rotation * fartherRaycastOffset;
                int fartherAllHitsCount = Physics.RaycastNonAlloc(fartherStartPos, Vector3.down.normalized, fartherAllHits, raycastMaxDistance * 2, LayerMask.GetMask(layersToAccountForInRaycasting), QueryTriggerInteraction.Ignore);

                bool bHit = false;
                bool fartherBHit = false;
                RaycastHit floorHit = new RaycastHit();
                RaycastHit fartherFloorHit = new RaycastHit();

                float minDistance = 0;
                bool minDistanceInitialized = false;
                for (int i = 0; i < allHitsCount; i++)
                {
                    if (allHits[i].distance > minDistance & minDistanceInitialized) { continue; }

                    bHit = true;
                    floorHit = allHits[i];

                    minDistance = allHits[i].distance;
                    minDistanceInitialized = true;
                }

                minDistance = 0;
                minDistanceInitialized = false;
                for (int i = 0; i < fartherAllHitsCount; i++)
                {
                    if (fartherAllHits[i].distance > minDistance & minDistanceInitialized) { continue; }

                    bHit = true;
                    floorHit = fartherAllHits[i];

                    minDistance = fartherAllHits[i].distance;
                    minDistanceInitialized = true;
                }

                # if UNITY_EDITOR
                if (bHit) { Debug.DrawLine(startPos, floorHit.point, Color.red, Time.deltaTime); }
                if (fartherBHit) { Debug.DrawLine(fartherStartPos, fartherFloorHit.point, Color.magenta, Time.deltaTime); }
                # endif

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