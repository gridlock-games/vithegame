using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;

namespace Vi.ArtificialIntelligence
{
    public class LobbyManagerNPC : NetworkInteractable
    {
        [SerializeField] private GameObject worldSpaceLabel;
        [SerializeField] private GameObject UI;

        public override void Interact()
        {
            UI.SetActive(true);
            Debug.Log(Time.time + " interact");
        }

        private bool localPlayerInRange;
        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.root.TryGetComponent(out Attributes attributes))
            {
                if (attributes.IsLocalPlayer) { localPlayerInRange = true; }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.transform.root.TryGetComponent(out Attributes attributes))
            {
                if (attributes.IsLocalPlayer) { localPlayerInRange = false; }
            }
        }

        private void Start()
        {
            worldSpaceLabel.transform.localScale = Vector3.zero;
            UI.SetActive(false);
        }

        private const float scalingSpeed = 8;
        private const float rotationSpeed = 15;
        private void Update()
        {
            worldSpaceLabel.transform.localScale = Vector3.Lerp(worldSpaceLabel.transform.localScale, localPlayerInRange ? Vector3.one : Vector3.zero, Time.deltaTime * scalingSpeed);

            if (Camera.main)
            {
                worldSpaceLabel.transform.rotation = Quaternion.Slerp(worldSpaceLabel.transform.rotation, Quaternion.LookRotation(Camera.main.transform.position - worldSpaceLabel.transform.position), Time.deltaTime * rotationSpeed);
            }
        }
    }
}