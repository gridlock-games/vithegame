using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Utility;
using Unity.Netcode;

namespace Vi.Core.VFX
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PooledObject))]
    public class ActionVFXPreview : MonoBehaviour
    {
        private ActionClip attack;
        private ActionVFX actionVFX;
        public void Initialize(ActionClip attack, ActionVFX actionVFX)
        {
            this.attack = attack;
            this.actionVFX = actionVFX;
        }

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
            if (actionVFX.transformType == ActionVFX.TransformType.Projectile)
            {
                transform.LookAt(transform.parent.GetComponent<AnimationHandler>().GetAimPoint());
                CanCast = true;
            }
            else if (actionVFX.transformType == ActionVFX.TransformType.ConformToGround)
            {
                Vector3 startPos = transform.parent.position + transform.parent.rotation * actionVFX.raycastOffset;
                int allHitsCount = Physics.RaycastNonAlloc(startPos, Vector3.down.normalized, allHits, actionVFX.raycastMaxDistance, LayerMask.GetMask(ActionVFX.layersToAccountForInRaycasting), QueryTriggerInteraction.Ignore);
                Vector3 fartherStartPos = transform.parent.position + transform.parent.rotation * actionVFX.fartherRaycastOffset;
                int fartherAllHitsCount = Physics.RaycastNonAlloc(fartherStartPos, Vector3.down.normalized, fartherAllHits, actionVFX.raycastMaxDistance * 2, LayerMask.GetMask(ActionVFX.layersToAccountForInRaycasting), QueryTriggerInteraction.Ignore);

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

                    fartherBHit = true;
                    fartherFloorHit = fartherAllHits[i];

                    minDistance = fartherAllHits[i].distance;
                    minDistanceInitialized = true;
                }

                # if UNITY_EDITOR
                if (bHit) { Debug.DrawLine(startPos, floorHit.point, Color.red, Time.deltaTime); }
                if (fartherBHit) { Debug.DrawLine(fartherStartPos, fartherFloorHit.point, Color.magenta, Time.deltaTime); }
                # endif

                if (bHit & fartherBHit)
                {
                    Vector3 offset = transform.parent.rotation * actionVFX.vfxPositionOffset;
                    transform.position = floorHit.point + offset;
                    Vector3 rel = fartherFloorHit.point + offset - transform.position;
                    Quaternion groundRotation = rel == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(rel, actionVFX.lookRotationUpDirection);
                    transform.rotation = groundRotation * Quaternion.Euler(actionVFX.vfxRotationOffset);

                    CanCast = true;
                }
                else
                {
                    transform.position = new Vector3(startPos.x, transform.parent.position.y, startPos.z);
                    transform.rotation = transform.parent.rotation * Quaternion.Euler(actionVFX.vfxRotationOffset);

                    CanCast = false;
                }

                transform.rotation *= Quaternion.Euler(attack.previewActionVFXRotationOffset);
                transform.position += transform.rotation * attack.previewActionVFXPositionOffset;

                ChangeParticleColor(CanCast ? originalParticleSystemColor : noVFXWillBeSpawnedColor);
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