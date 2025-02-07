using UnityEngine;
using Vi.Core;
using Vi.Player;
using Vi.ScriptableObjects;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Vi.Utility;
using static Vi.ScriptableObjects.CharacterReference;

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

        private GameObject invoker;
        public override void Interact(GameObject invoker)
        {
            this.invoker = invoker;
            invoker.GetComponent<ActionMapHandler>().SetExternalUI(this);
            currencyCountText.text = FasterPlayerPrefs.Singleton.GetInt("Tokens").ToString();
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
            foreach (ShopKeeperItem item in cartContents.ToArray())
            {
                Purchase(item.ItemId, item.Price);
                yield return new WaitUntil(() => !item.gameObject.activeInHierarchy);
            }
        }

        private void Purchase(string itemId, int price)
        {
            if (!IsClient) { Debug.LogError("Calling Purchase() while not being a client!"); return; }

            if (FasterPlayerPrefs.Singleton.GetInt("Tokens") < price)
            {
                purchaseErrorText.text = "Not Enough Vi Essence!";
                AudioManager.Singleton.Play2DClip(gameObject, purchaseUnsuccessfulSounds[Random.Range(0, purchaseUnsuccessfulSounds.Length)], 0.3f);
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
            if (!PlayerDataManager.Singleton.ContainsId((int)purchaserClientId)) { yield break; }

            var allOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(PlayerDataManager.Singleton.GetPlayerData((int)purchaserClientId).character.raceAndGender);
            foreach (CharacterReference.WearableEquipmentOption wearableEquipmentOption in allOptions)
            {
                if (wearableEquipmentOption.equipmentType == CharacterReference.EquipmentType.Chest)
                {
                    if (itemId == wearableEquipmentOption.itemWebId)
                    {
                        var pants = allOptions.Find(item => item.groupName == wearableEquipmentOption.groupName & item.equipmentType == CharacterReference.EquipmentType.Pants);
                        if (pants != null)
                        {
                            yield return WebRequestManager.Singleton.CharacterManager.AddItemToInventory(characterId, pants.itemWebId);
                        }
                        bool setSuccess = WebRequestManager.Singleton.CharacterManager.InventoryAddWasSuccessful;

                        var boots = allOptions.Find(item => item.groupName == wearableEquipmentOption.groupName & item.equipmentType == CharacterReference.EquipmentType.Boots);
                        if (boots != null)
                        {
                            yield return WebRequestManager.Singleton.CharacterManager.AddItemToInventory(characterId, boots.itemWebId);
                        }
                        setSuccess &= WebRequestManager.Singleton.CharacterManager.InventoryAddWasSuccessful;

                        var belt = allOptions.Find(item => item.groupName == wearableEquipmentOption.groupName & item.equipmentType == CharacterReference.EquipmentType.Belt);
                        if (belt != null)
                        {
                            yield return WebRequestManager.Singleton.CharacterManager.AddItemToInventory(characterId, belt.itemWebId);
                        }
                        setSuccess &= WebRequestManager.Singleton.CharacterManager.InventoryAddWasSuccessful;

                        if (setSuccess)
                        {
                            yield return WebRequestManager.Singleton.CharacterManager.AddItemToInventory(characterId, itemId);
                            setSuccess = WebRequestManager.Singleton.CharacterManager.InventoryAddWasSuccessful;
                            if (setSuccess) { yield return WebRequestManager.Singleton.CharacterManager.GetCharacterInventory(characterId); }
                        }

                        PurchaseClientRpc(purchaserClientId, setSuccess, characterId, itemId, price);
                        yield break;
                    }
                }
            }

            yield return WebRequestManager.Singleton.CharacterManager.AddItemToInventory(characterId, itemId);
            bool success = WebRequestManager.Singleton.CharacterManager.InventoryAddWasSuccessful;
            if (success) { yield return WebRequestManager.Singleton.CharacterManager.GetCharacterInventory(characterId); }
            PurchaseClientRpc(purchaserClientId, success, characterId, itemId, price);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void PurchaseClientRpc(ulong purchaserClientId, bool purchaseSuccessful, string characterId, string itemId, int price)
        {
            if (purchaseSuccessful)
            {
                PersistentLocalObjects.Singleton.StartCoroutine(WebRequestManager.Singleton.CharacterManager.GetCharacterInventory(characterId));
            }

            if (purchaserClientId == NetworkManager.LocalClientId)
            {
                var instance = shopKeeperItemInstances.Find(item => item.ItemId == itemId);
                RemoveFromCart(instance);
                instance.gameObject.SetActive(false);
                waitingForPurchase = false;
                if (purchaseSuccessful)
                {
                    purchaseErrorText.text = "Purchase successful!";
                    FasterPlayerPrefs.Singleton.SetInt("Tokens", FasterPlayerPrefs.Singleton.GetInt("Tokens") - price);
                    currencyCountText.text = FasterPlayerPrefs.Singleton.GetInt("Tokens").ToString();

                    AudioManager.Singleton.Play2DClip(gameObject, purchaseSuccessfulSounds[Random.Range(0, purchaseSuccessfulSounds.Length)], 0.3f);
                }
                else
                {
                    purchaseErrorText.text = "There was a problem.";
                    AudioManager.Singleton.Play2DClip(gameObject, purchaseUnsuccessfulSounds[Random.Range(0, purchaseUnsuccessfulSounds.Length)], 0.3f);
                }
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
                & FasterPlayerPrefs.Singleton.GetInt("Tokens") >= cartPriceSum
                & cartContents.Count > 0;

            cartCostText.text = cartPriceSum.ToString();
        }
    }
}