using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.Player;

namespace Vi.UI
{
    public class LobbyManagerNPC : NetworkInteractable
    {
        [SerializeField] private GameObject worldSpaceLabel;
        [SerializeField] private GameObject UI;

        private GameObject invoker;
        public override void Interact(GameObject invoker)
        {
            this.invoker = invoker;
            invoker.GetComponent<ActionMapHandler>().SetExternalUI(this);
            UI.SetActive(true);
        }

        private void OnPause()
        {
            CloseServerBrowser();
        }

        public void CloseServerBrowser()
        {
            invoker.GetComponent<ActionMapHandler>().SetExternalUI(null);
            invoker = null;
            UI.SetActive(false);
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