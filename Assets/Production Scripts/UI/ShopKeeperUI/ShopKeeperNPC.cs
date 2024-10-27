using UnityEngine;
using Vi.Core;
using Vi.Player;
using Vi.ScriptableObjects;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace Vi.UI
{
    public class ShopKeeperNPC : NetworkInteractable, ExternalUI
    {
        [SerializeField] private GameObject worldSpaceLabel;
        [SerializeField] private Canvas UICanvas;
        [SerializeField] private ShopKeeperItem shopKeeperItemPrefab;
        [SerializeField] private Transform armorParent;
        [SerializeField] private Transform weaponParent;
        [SerializeField] private Text purchaseErrorText;
        [SerializeField] private Selectable[] selectablesThatRespondToPurchaseRpc;

        private GameObject invoker;
        public override void Interact(GameObject invoker)
        {
            this.invoker = invoker;
            invoker.GetComponent<ActionMapHandler>().SetExternalUI(this);
            UICanvas.gameObject.SetActive(true);

            foreach (CharacterReference.WeaponOption weaponOption in PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions())
            {
                // If this weapon option is in our inventory, continue
                if (WebRequestManager.Singleton.InventoryItems[PlayerDataManager.Singleton.LocalPlayerData.character._id.ToString()].Exists(item => item.itemId == weaponOption.itemWebId)) { continue; }
                GameObject g = Instantiate(shopKeeperItemPrefab.gameObject, weaponParent);
                g.GetComponent<ShopKeeperItem>().InitializeAsWeapon(weaponOption);

                string characterId = PlayerDataManager.Singleton.LocalPlayerData.character._id.ToString();
                string itemId = weaponOption.itemWebId;
                g.GetComponent<Button>().onClick.AddListener(() => Purchase(characterId, itemId));
            }

            foreach (CharacterReference.WearableEquipmentOption wearableEquipmentOption in PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(PlayerDataManager.Singleton.LocalPlayerData.character.raceAndGender))
            {
                // If this armor option is in our inventory, continue
                if (WebRequestManager.Singleton.InventoryItems[PlayerDataManager.Singleton.LocalPlayerData.character._id.ToString()].Exists(item => item.itemId == wearableEquipmentOption.itemWebId)) { continue; }
                GameObject g = Instantiate(shopKeeperItemPrefab.gameObject, armorParent);
                g.GetComponent<ShopKeeperItem>().InitializeAsArmor(wearableEquipmentOption);

                string characterId = PlayerDataManager.Singleton.LocalPlayerData.character._id.ToString();
                string itemId = wearableEquipmentOption.itemWebId;
                g.GetComponent<Button>().onClick.AddListener(() => Purchase(characterId, itemId));
            }
        }

        private bool waitingForPurchase;

        private void Purchase(string characterId, string itemId)
        {
            if (!IsClient) { Debug.LogError("Calling Purchase() while not being a client!"); return; }

            waitingForPurchase = true;
            purchaseErrorText.text = "Approving Purchase...";
            PurchaseServerRpc(NetworkManager.LocalClientId, characterId, itemId);
        }

        [Rpc(SendTo.Server)]
        private void PurchaseServerRpc(ulong clientId, string characterId, string itemId)
        {
            StartCoroutine(PurchaseOnServer(clientId, characterId, itemId));
        }

        private IEnumerator PurchaseOnServer(ulong clientId, string characterId, string itemId)
        {
            Debug.Log("Purchasing item " + itemId + " for char " + characterId);
            yield return WebRequestManager.Singleton.AddItemToInventory(characterId, itemId);
            yield return WebRequestManager.Singleton.GetCharacterInventory(characterId);
            PurchaseClientRpc(true, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void PurchaseClientRpc(bool purchaseSuccessful, RpcParams rpcParams)
        {
            waitingForPurchase = false;
            if (purchaseSuccessful)
            {
                purchaseErrorText.text = "Purchase successful!";
            }
            else
            {
                purchaseErrorText.text = "There was a problem.";
            }
        }

        public void OnPause()
        {
            if (waitingForPurchase) { return; }
            CloseShop();
        }

        public void CloseShop()
        {
            if (invoker)
            {
                invoker.GetComponent<ActionMapHandler>().SetExternalUI(null);
            }
            invoker = null;
            UICanvas.gameObject.SetActive(false);
            waitingForPurchase = false;

            foreach (Transform child in armorParent)
            {
                Destroy(child.gameObject);
            }

            foreach (Transform child in weaponParent)
            {
                Destroy(child.gameObject);
            }
        }

        private bool localPlayerInRange;

        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (networkCollider.CombatAgent.IsLocalPlayer) { localPlayerInRange = true; }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.transform.root.TryGetComponent(out NetworkCollider networkCollider))
            {
                if (networkCollider.CombatAgent.IsLocalPlayer) { localPlayerInRange = false; }
            }
        }

        private Vector3 originalScale;

        private void Start()
        {
            originalScale = worldSpaceLabel.transform.localScale;
            worldSpaceLabel.transform.localScale = Vector3.zero;
            UICanvas.gameObject.SetActive(false);
            purchaseErrorText.text = "";
        }

        private const float scalingSpeed = 8;
        private const float rotationSpeed = 15;

        private Camera mainCamera;

        private void FindMainCamera()
        {
            if (mainCamera)
            {
                if (mainCamera.gameObject.CompareTag("MainCamera"))
                {
                    return;
                }
            }
            mainCamera = Camera.main;
        }

        private void Update()
        {
            FindMainCamera();

            worldSpaceLabel.transform.localScale = Vector3.Lerp(worldSpaceLabel.transform.localScale, localPlayerInRange ? originalScale : Vector3.zero, Time.deltaTime * scalingSpeed);

            if (mainCamera)
            {
                worldSpaceLabel.transform.rotation = Quaternion.Slerp(worldSpaceLabel.transform.rotation, Quaternion.LookRotation(mainCamera.transform.position - worldSpaceLabel.transform.position), Time.deltaTime * rotationSpeed);
            }

            foreach (Selectable selectable in selectablesThatRespondToPurchaseRpc)
            {
                selectable.interactable = !waitingForPurchase;
            }
        }
    }
}