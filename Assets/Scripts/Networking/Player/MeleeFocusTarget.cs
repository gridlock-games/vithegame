using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using GameCreator.Melee;

namespace LightPat.Player
{
    [RequireComponent(typeof(CharacterMelee))]
    public class MeleeFocusTarget : NetworkBehaviour
    {
        [SerializeField] private Transform characterCamera;
        [SerializeField] private Vector3 boxCastHalfExtents = Vector3.one;
        [SerializeField] private float boxCastDistance = 5;

        private CharacterMelee melee;

        private void Start()
        {
            melee = GetComponent<CharacterMelee>();
        }

        private void Update()
        {
            if (!IsLocalPlayer) return;

            // Visualize boxcast
            Vector3 origin = transform.position;
            Quaternion orientation = characterCamera.rotation;
            Vector3 direction = characterCamera.forward;
            Core.ExtDebug.DrawBoxCastOnHit(origin, boxCastHalfExtents, orientation, direction, boxCastDistance, Color.green);

            if (!melee.IsAttacking.Value) return;

            // Get all hits in boxcast
            RaycastHit[] allHits = Physics.BoxCastAll(origin, boxCastHalfExtents, direction, orientation, boxCastDistance);
            // Sort hits by distance
            System.Array.Sort(allHits, (x, y) => x.distance.CompareTo(y.distance));

            // Iterate through box hits and find the first hit that has a CharacterMelee component
            Transform target = null;
            foreach (RaycastHit hit in allHits)
            {
                if (hit.transform.root == transform) { continue; }
                if (hit.transform.root.TryGetComponent(out CharacterMelee melee))
                {
                    Core.ExtDebug.DrawBoxCastOnHit(origin, boxCastHalfExtents, orientation, direction, hit.distance, Color.red);
                    target = hit.transform.root;
                    break;
                }
            }

            if (!target) return;
            // If we have a target character, then look at them
            Vector3 relativePos = target.position - characterCamera.position;
            relativePos.y = 0;
            transform.rotation = Quaternion.LookRotation(relativePos);

            Debug.Log(Time.time + " Locked onto " + target);
        }
    }
}