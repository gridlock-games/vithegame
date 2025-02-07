using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.Player;
using Vi.Utility;

namespace Vi.UI
{
    public class MalakasLeaderboardNPC : NetworkInteractable, ExternalUI
    {
        [SerializeField] private GameObject worldSpaceLabel;
        [SerializeField] private LeaderboardUI UI;

        private GameObject invoker;

        public override void Interact(GameObject invoker)
        {
            this.invoker = invoker;
            invoker.GetComponent<ActionMapHandler>().SetExternalUI(this);
            UI.gameObject.SetActive(true);
        }

        public void OnPause()
        {
            CloseLeaderboard();
        }

        public void CloseLeaderboard()
        {
            if (invoker)
            {
                invoker.GetComponent<ActionMapHandler>().SetExternalUI(null);
            }
            invoker = null;
            UI.gameObject.SetActive(false);
        }

        private bool localPlayerInRange;

        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (networkCollider.CombatAgent.IsLocalPlayer)
                {
                    localPlayerInRange = true;
                    networkCollider.MovementHandler.SetInteractableInRange(this, true);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (networkCollider.CombatAgent.IsLocalPlayer)
                {
                    localPlayerInRange = false;
                    networkCollider.MovementHandler.SetInteractableInRange(this, false);
                }
            }
        }

        private void Awake()
        {
            StartCoroutine(WebRequestManager.Singleton.LeaderboardManager.GetLeaderboard());
        }

        private Vector3 originalScale;

        private void Start()
        {
            originalScale = worldSpaceLabel.transform.localScale;
            worldSpaceLabel.transform.localScale = Vector3.zero;
            UI.gameObject.SetActive(false);
        }

        private const float scalingSpeed = 8;
        private const float rotationSpeed = 15;

        private void Update()
        {
            worldSpaceLabel.transform.localScale = Vector3.Lerp(worldSpaceLabel.transform.localScale, localPlayerInRange ? originalScale : Vector3.zero, Time.deltaTime * scalingSpeed);

            if (FindMainCamera.MainCamera)
            {
                worldSpaceLabel.transform.rotation = Quaternion.Slerp(worldSpaceLabel.transform.rotation, Quaternion.LookRotation(FindMainCamera.MainCamera.transform.position - worldSpaceLabel.transform.position), Time.deltaTime * rotationSpeed);
            }
        }
    }
}