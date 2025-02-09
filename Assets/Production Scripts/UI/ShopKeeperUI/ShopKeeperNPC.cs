using UnityEngine;
using Vi.Core;
using Vi.Player;
using Vi.ScriptableObjects;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Vi.Utility;
using System.Linq;

namespace Vi.UI
{
    public class ShopKeeperNPC : NetworkInteractable, ExternalUI
    {
        [SerializeField] private GameObject worldSpaceLabel;
        [SerializeField] private Canvas UICanvas;
        [SerializeField] private ShopKeeperItem shopKeeperItemPrefab;
        [SerializeField] private Transform itemParent;
        [SerializeField] private Transform cartParent;
        [SerializeField] private GameObject youOwnEverythingObject;
        [SerializeField] private Text purchaseErrorText;
        [SerializeField] private Text currencyCountText;
        [SerializeField] private Text cartCostText;
        [SerializeField] private Button buyButton;
        [SerializeField] private Selectable[] selectablesThatRespondToPurchaseRpc;
        [SerializeField] private AudioClip[] purchaseSuccessfulSounds;
        [SerializeField] private AudioClip[] purchaseUnsuccessfulSounds;

        private List<ShopKeeperItem> shopKeeperItemInstances = new List<ShopKeeperItem>();

        private IEnumerator RefreshViEssenceAmount()
        {
            currencyCountText.text = "";
            yield return WebRequestManager.Singleton.ItemShopManager.GetViEssenceOfUser(WebRequestManager.Singleton.UserManager.CurrentlyLoggedInUserId, null);
            currencyCountText.text = WebRequestManager.Singleton.ItemShopManager.ViEssenceCount.ToString();
        }

        private GameObject invoker;
        public override void Interact(GameObject invoker)
        {
            this.invoker = invoker;
            invoker.GetComponent<ActionMapHandler>().SetExternalUI(this);
            UICanvas.gameObject.SetActive(true);
            purchaseErrorText.text = "";

            string localCharacterId = PlayerDataManager.Singleton.LocalPlayerData.character._id.ToString();
            foreach (CharacterReference.WeaponOption weaponOption in PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions())
            {
                // If this weapon option is in our inventory, make it inactive in the UI
                bool isInInventory = CharacterManager.IsItemInInventory(localCharacterId, weaponOption.itemWebId);
                shopKeeperItemInstances.FindAll(item => item.IsWeapon).Find(item => item.weaponOption.itemWebId == weaponOption.itemWebId).gameObject.SetActive(!isInInventory);
            }

            foreach (CharacterReference.WearableEquipmentOption wearableEquipmentOption in PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(PlayerDataManager.Singleton.LocalPlayerData.character.raceAndGender))
            {
                if (wearableEquipmentOption.equipmentType == CharacterReference.EquipmentType.Boots
                    | wearableEquipmentOption.equipmentType == CharacterReference.EquipmentType.Pants
                    | wearableEquipmentOption.equipmentType == CharacterReference.EquipmentType.Belt) { continue; }

                // If this armor option is in our inventory, make it inactive in the UI
                bool isInInventory = CharacterManager.IsItemInInventory(localCharacterId, wearableEquipmentOption.itemWebId);
                shopKeeperItemInstances.FindAll(item => item.IsArmor).Find(item => item.equipmentOption.itemWebId == wearableEquipmentOption.itemWebId).gameObject.SetActive(!isInInventory);
            }
        }

        private bool waitingForPurchase;

        private List<ShopKeeperItem> cartContents = new List<ShopKeeperItem>();
        private void AddToCart(ShopKeeperItem shopKeeperItem)
        {
            if (!cartContents.Contains(shopKeeperItem))
            {
                cartContents.Add(shopKeeperItem);
                shopKeeperItem.transform.SetParent(cartParent, true);
                shopKeeperItem.CloseButton.gameObject.SetActive(true);
            }
        }

        private void RemoveFromCart(ShopKeeperItem shopKeeperItem)
        {
            cartContents.Remove(shopKeeperItem);
            shopKeeperItem.transform.SetParent(itemParent, true);
            shopKeeperItem.CloseButton.gameObject.SetActive(false);
        }

        public IEnumerator PurchaseCart()
        {
            List<ItemShopManager.PurchaseItem> purchaseItems = new List<ItemShopManager.PurchaseItem>();
            foreach (ShopKeeperItem item in cartContents.ToArray())
            {
                purchaseItems.Add(new ItemShopManager.PurchaseItem(item.ItemId, 1, item.Price));
            }
            Purchase(purchaseItems.ToArray());
            yield return null;
        }

        private void Purchase(ItemShopManager.PurchaseItem[] purchaseItems)
        {
            if (!IsClient) { Debug.LogError("Calling Purchase() while not being a client!"); return; }

            int totalCost = 0;
            foreach (var purchaseItem in purchaseItems)
            {
                totalCost += purchaseItem.cost;
            }

            if (WebRequestManager.Singleton.ItemShopManager.ViEssenceCount < totalCost)
            {
                purchaseErrorText.text = "Not Enough Vi Essence!";
                AudioManager.Singleton.Play2DClip(gameObject, purchaseUnsuccessfulSounds[Random.Range(0, purchaseUnsuccessfulSounds.Length)], 0.3f);
                return;
            }

            waitingForPurchase = true;
            purchaseErrorText.text = "Approving Purchase...";
            PurchaseServerRpc(NetworkManager.LocalClientId, purchaseItems);
        }

        [Rpc(SendTo.Server)]
        private void PurchaseServerRpc(ulong purchaserClientId, ItemShopManager.PurchaseItem[] purchaseItems)
        {
            StartCoroutine(PurchaseOnServer(purchaserClientId, purchaseItems));
        }

        private IEnumerator PurchaseOnServer(ulong purchaserClientId, ItemShopManager.PurchaseItem[] itemsToPurchase)
        {
            //if (!PlayerDataManager.Singleton.ContainsId((int)purchaserClientId)) { yield break; }

            string characterId = "";
            string userId = "";
            if (PlayerDataManager.Singleton.ContainsId((int)purchaserClientId))
            {
                characterId = PlayerDataManager.Singleton.GetPlayerData((int)purchaserClientId).character._id.ToString();
            }
            else
            {
                PurchaseClientRpc(purchaserClientId, false, itemsToPurchase, 0);
                yield break;
            }

            int viEssenceCount = 0;
            yield return WebRequestManager.Singleton.ItemShopManager.GetViEssenceOfUser(userId, (count) => viEssenceCount = count);

            List<ItemShopManager.PurchaseItem> purchaseItems = itemsToPurchase.ToList();

            List<CharacterReference.WearableEquipmentOption> allOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(PlayerDataManager.Singleton.GetPlayerData((int)purchaserClientId).character.raceAndGender);
            foreach (ItemShopManager.PurchaseItem purchaseItem in itemsToPurchase)
            {
                foreach (CharacterReference.WearableEquipmentOption wearableEquipmentOption in allOptions)
                {
                    if (wearableEquipmentOption.equipmentType == CharacterReference.EquipmentType.Chest)
                    {
                        if (purchaseItem.itemId == wearableEquipmentOption.itemWebId)
                        {
                            CharacterReference.WearableEquipmentOption pants = allOptions.Find(item => item.groupName == wearableEquipmentOption.groupName & item.equipmentType == CharacterReference.EquipmentType.Pants);
                            if (pants != null)
                            {
                                purchaseItems.Add(new ItemShopManager.PurchaseItem(pants.itemWebId, 1, 0));
                            }

                            CharacterReference.WearableEquipmentOption boots = allOptions.Find(item => item.groupName == wearableEquipmentOption.groupName & item.equipmentType == CharacterReference.EquipmentType.Boots);
                            if (boots != null)
                            {
                                purchaseItems.Add(new ItemShopManager.PurchaseItem(boots.itemWebId, 1, 0));
                            }

                            CharacterReference.WearableEquipmentOption belt = allOptions.Find(item => item.groupName == wearableEquipmentOption.groupName & item.equipmentType == CharacterReference.EquipmentType.Belt);
                            if (belt != null)
                            {
                                purchaseItems.Add(new ItemShopManager.PurchaseItem(belt.itemWebId, 1, 0));
                            }
                        }
                    }
                }
            }

            bool success = true;
            yield return WebRequestManager.Singleton.ItemShopManager.PurchaseItems(characterId, purchaseItems, (result) => success = result);

            if (success)
            {
                yield return WebRequestManager.Singleton.CharacterManager.GetCharacterInventory(characterId);
                yield return WebRequestManager.Singleton.ItemShopManager.GetViEssenceOfUser(userId, (count) => viEssenceCount = count);
            }
            
            PurchaseClientRpc(purchaserClientId, success, itemsToPurchase, viEssenceCount);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void PurchaseClientRpc(ulong purchaserClientId, bool purchaseSuccessful, ItemShopManager.PurchaseItem[] purchaseItems, int newViEssenceAmount)
        {
            string characterId;
            if (PlayerDataManager.Singleton.ContainsId((int)purchaserClientId))
            {
                characterId = PlayerDataManager.Singleton.GetPlayerData((int)purchaserClientId).character._id.ToString();
            }
            else
            {
                waitingForPurchase = false;
                return;
            }

            if (purchaseSuccessful)
            {
                PersistentLocalObjects.Singleton.StartCoroutine(WebRequestManager.Singleton.CharacterManager.GetCharacterInventory(characterId));
            }

            if (purchaserClientId == NetworkManager.LocalClientId)
            {
                currencyCountText.text = newViEssenceAmount.ToString();

                foreach (ItemShopManager.PurchaseItem purchaseItem in purchaseItems)
                {
                    ShopKeeperItem instance = shopKeeperItemInstances.Find(item => item.ItemId == purchaseItem.itemId);
                    RemoveFromCart(instance);
                    instance.gameObject.SetActive(false);
                }

                if (purchaseSuccessful)
                {
                    purchaseErrorText.text = "Purchase successful!";
                    StartCoroutine(RefreshViEssenceAmount());
                    AudioManager.Singleton.Play2DClip(gameObject, purchaseSuccessfulSounds[Random.Range(0, purchaseSuccessfulSounds.Length)], 0.3f);
                }
                else
                {
                    purchaseErrorText.text = "There was a problem.";
                    AudioManager.Singleton.Play2DClip(gameObject, purchaseUnsuccessfulSounds[Random.Range(0, purchaseUnsuccessfulSounds.Length)], 0.3f);
                }
            }

            waitingForPurchase = false;
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
            purchaseErrorText.text = "";
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

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsClient)
            {
                StartCoroutine(RefreshViEssenceAmount());

                foreach (CharacterReference.WeaponOption weaponOption in PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions())
                {
                    ShopKeeperItem shopKeeperItem = Instantiate(shopKeeperItemPrefab.gameObject, itemParent).GetComponent<ShopKeeperItem>();
                    shopKeeperItemInstances.Add(shopKeeperItem);
                    shopKeeperItem.InitializeAsWeapon(weaponOption);

                    string itemId = weaponOption.itemWebId;
                    shopKeeperItem.MainButton.onClick.AddListener(() => AddToCart(shopKeeperItem));
                    shopKeeperItem.CloseButton.onClick.AddListener(() => RemoveFromCart(shopKeeperItem));
                    shopKeeperItem.CloseButton.gameObject.SetActive(false);
                }

                foreach (CharacterReference.WearableEquipmentOption wearableEquipmentOption in PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(PlayerDataManager.Singleton.LocalPlayerData.character.raceAndGender))
                {
                    if (wearableEquipmentOption.equipmentType == CharacterReference.EquipmentType.Boots
                        | wearableEquipmentOption.equipmentType == CharacterReference.EquipmentType.Pants
                        | wearableEquipmentOption.equipmentType == CharacterReference.EquipmentType.Belt) { continue; }

                    ShopKeeperItem shopKeeperItem = Instantiate(shopKeeperItemPrefab.gameObject, itemParent).GetComponent<ShopKeeperItem>();
                    shopKeeperItemInstances.Add(shopKeeperItem);
                    shopKeeperItem.InitializeAsArmor(wearableEquipmentOption);

                    string itemId = wearableEquipmentOption.itemWebId;
                    shopKeeperItem.MainButton.onClick.AddListener(() => AddToCart(shopKeeperItem));
                    shopKeeperItem.CloseButton.onClick.AddListener(() => RemoveFromCart(shopKeeperItem));
                    shopKeeperItem.CloseButton.gameObject.SetActive(false);
                }
                buyButton.onClick.AddListener(() => PersistentLocalObjects.Singleton.StartCoroutine(PurchaseCart()));
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

        private void Update()
        {
            worldSpaceLabel.transform.localScale = Vector3.Lerp(worldSpaceLabel.transform.localScale, localPlayerInRange ? originalScale : Vector3.zero, Time.deltaTime * scalingSpeed);

            if (FindMainCamera.MainCamera)
            {
                worldSpaceLabel.transform.rotation = Quaternion.Slerp(worldSpaceLabel.transform.rotation, Quaternion.LookRotation(FindMainCamera.MainCamera.transform.position - worldSpaceLabel.transform.position), Time.deltaTime * rotationSpeed);
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

            foreach (ShopKeeperItem shopKeeperItem in cartContents)
            {
                foreach (Selectable selectable in shopKeeperItem.Selectables)
                {
                    selectable.interactable = !waitingForPurchase;
                }
            }

            youOwnEverythingObject.SetActive(shopKeeperItemInstances.TrueForAll(item => !item.gameObject.activeInHierarchy));

            int cartPriceSum = 0;
            foreach (ShopKeeperItem item in cartContents)
            {
                if (!item.gameObject.activeInHierarchy) { continue; }
                cartPriceSum += item.Price;
            }

            buyButton.interactable = !waitingForPurchase
                & WebRequestManager.Singleton.ItemShopManager.ViEssenceCount >= cartPriceSum
                & cartContents.Count > 0;

            cartCostText.text = cartPriceSum.ToString();
        }
    }
}