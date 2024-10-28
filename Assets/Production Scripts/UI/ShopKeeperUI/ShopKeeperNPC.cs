using UnityEngine;
using Vi.Core;
using Vi.Player;
using Vi.ScriptableObjects;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Vi.Utility;

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
        [SerializeField] private Text currencyCountText;
        [SerializeField] private Selectable[] selectablesThatRespondToPurchaseRpc;

        private List<ShopKeeperItem> shopKeeperItemInstances = new List<ShopKeeperItem>();

        private GameObject invoker;
        public override void Interact(GameObject invoker)
        {
            this.invoker = invoker;
            invoker.GetComponent<ActionMapHandler>().SetExternalUI(this);
            currencyCountText.text = FasterPlayerPrefs.Singleton.GetInt("Tokens").ToString();
            UICanvas.gameObject.SetActive(true);

            foreach (CharacterReference.WeaponOption weaponOption in PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions())
            {
                // If this weapon option is in our inventory, make it inactive in the UI
                bool isInInventory = WebRequestManager.Singleton.InventoryItems[PlayerDataManager.Singleton.LocalPlayerData.character._id.ToString()].Exists(item => item.itemId == weaponOption.itemWebId);
                shopKeeperItemInstances.FindAll(item => item.IsWeapon).Find(item => item.weaponOption.itemWebId == weaponOption.itemWebId).gameObject.SetActive(!isInInventory);
            }

            foreach (CharacterReference.WearableEquipmentOption wearableEquipmentOption in PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(PlayerDataManager.Singleton.LocalPlayerData.character.raceAndGender))
            {
                // If this armor option is in our inventory, make it inactive in the UI
                bool isInInventory = WebRequestManager.Singleton.InventoryItems[PlayerDataManager.Singleton.LocalPlayerData.character._id.ToString()].Exists(item => item.itemId == wearableEquipmentOption.itemWebId);
                shopKeeperItemInstances.FindAll(item => item.IsArmor).Find(item => item.equipmentOption.itemWebId == wearableEquipmentOption.itemWebId).gameObject.SetActive(!isInInventory);
            }
        }

        private bool waitingForPurchase;

        private void Purchase(string itemId, int price)
        {
            if (!IsClient) { Debug.LogError("Calling Purchase() while not being a client!"); return; }

            if (FasterPlayerPrefs.Singleton.GetInt("Tokens") < price)
            {
                purchaseErrorText.text = "Not Enough Vi Essence!";
                return;
            }

            waitingForPurchase = true;
            purchaseErrorText.text = "Approving Purchase...";
            PurchaseServerRpc(NetworkManager.LocalClientId, PlayerDataManager.Singleton.LocalPlayerData.character._id.ToString(), itemId, price);
        }

        [Rpc(SendTo.Server)]
        private void PurchaseServerRpc(ulong purchaserClientId, string characterId, string itemId, int price)
        {
            StartCoroutine(PurchaseOnServer(purchaserClientId, characterId, itemId, price));
        }

        private IEnumerator PurchaseOnServer(ulong purchaserClientId, string characterId, string itemId, int price)
        {
            yield return WebRequestManager.Singleton.AddItemToInventory(characterId, itemId);
            bool success = WebRequestManager.Singleton.InventoryAddWasSuccessful;
            if (success) { yield return WebRequestManager.Singleton.GetCharacterInventory(characterId); }
            PurchaseClientRpc(success, characterId, price);
        }

        [Rpc(SendTo.NotServer)]
        private void PurchaseClientRpc(bool purchaseSuccessful, string characterId, int price)
        {
            waitingForPurchase = false;
            if (purchaseSuccessful)
            {
                purchaseErrorText.text = "Purchase successful!";
                PersistentLocalObjects.Singleton.StartCoroutine(WebRequestManager.Singleton.GetCharacterInventory(characterId));
                FasterPlayerPrefs.Singleton.SetInt("Tokens", FasterPlayerPrefs.Singleton.GetInt("Tokens") - price);
                currencyCountText.text = FasterPlayerPrefs.Singleton.GetInt("Tokens").ToString();
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

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsClient)
            {
                foreach (CharacterReference.WeaponOption weaponOption in PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions())
                {
                    ShopKeeperItem shopKeeperItem = Instantiate(shopKeeperItemPrefab.gameObject, weaponParent).GetComponent<ShopKeeperItem>();
                    shopKeeperItemInstances.Add(shopKeeperItem);
                    shopKeeperItem.InitializeAsWeapon(weaponOption);

                    string itemId = weaponOption.itemWebId;
                    shopKeeperItem.GetComponent<Button>().onClick.AddListener(() => Purchase(itemId, shopKeeperItem.Price));
                }

                foreach (CharacterReference.WearableEquipmentOption wearableEquipmentOption in PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(PlayerDataManager.Singleton.LocalPlayerData.character.raceAndGender))
                {
                    ShopKeeperItem shopKeeperItem = Instantiate(shopKeeperItemPrefab.gameObject, armorParent).GetComponent<ShopKeeperItem>();
                    shopKeeperItemInstances.Add(shopKeeperItem);
                    shopKeeperItem.InitializeAsArmor(wearableEquipmentOption);

                    string itemId = wearableEquipmentOption.itemWebId;
                    shopKeeperItem.GetComponent<Button>().onClick.AddListener(() => Purchase(itemId, shopKeeperItem.Price));
                }
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

            foreach (ShopKeeperItem shopKeeperItem in shopKeeperItemInstances)
            {
                foreach (Selectable selectable in shopKeeperItem.Selectables)
                {
                    selectable.interactable = !waitingForPurchase;
                }
            }
        }
    }
}