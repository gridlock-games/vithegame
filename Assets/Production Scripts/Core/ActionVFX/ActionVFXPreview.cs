using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;

namespace Vi.Core
{
    public class ActionVFXPreview : ActionVFX
    {
        private ParticleSystem ps;
        private Color originalParticleSystemColor;
        private Color noVFXWillBeSpawnedColor = Color.red;
        
        private void Start()
        {
            ps = GetComponentInChildren<ParticleSystem>();
            originalParticleSystemColor = ps.main.startColor.color;
            noVFXWillBeSpawnedColor.a = originalParticleSystemColor.a;
        }

        private bool lastColorState;
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

                ChangeParticleColor(bHit ? originalParticleSystemColor : noVFXWillBeSpawnedColor);

                if (bHit)
                {
                    transform.position = floorHit.point + transform.parent.rotation * vfxPositionOffset;
                    transform.rotation = Quaternion.LookRotation(Vector3.Cross(floorHit.normal, crossProductDirection), lookRotationUpDirection) * transform.parent.rotation * Quaternion.Euler(vfxRotationOffset);
                }
                else
                {
                    transform.position = new Vector3(startPos.x, transform.parent.position.y, startPos.z);
                    Vector3 normal = new Vector3(0, 1, 0);
                    transform.rotation = Quaternion.LookRotation(Vector3.Cross(normal, crossProductDirection), lookRotationUpDirection) * transform.parent.rotation * Quaternion.Euler(vfxRotationOffset);
                }
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