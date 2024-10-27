using UnityEngine;
using Vi.Core;
using Vi.Player;
using Vi.ScriptableObjects;
using static Vi.ScriptableObjects.CharacterReference;

namespace Vi.UI
{
    public class ShopKeeperNPC : NetworkInteractable, ExternalUI
    {
        [SerializeField] private GameObject worldSpaceLabel;
        [SerializeField] private Canvas UICanvas;
        [SerializeField] private ShopKeeperItem shopKeeperItemPrefab;
        [SerializeField] private Transform armorParent;
        [SerializeField] private Transform weaponParent;

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
                Instantiate(shopKeeperItemPrefab, weaponParent).GetComponent<ShopKeeperItem>().InitializeAsWeapon(weaponOption);
            }

            foreach (CharacterReference.WearableEquipmentOption wearableEquipmentOption in PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(PlayerDataManager.Singleton.LocalPlayerData.character.raceAndGender))
            {
                // If this armor option is in our inventory, continue
                if (WebRequestManager.Singleton.InventoryItems[PlayerDataManager.Singleton.LocalPlayerData.character._id.ToString()].Exists(item => item.itemId == wearableEquipmentOption.itemWebId)) { continue; }
                Instantiate(shopKeeperItemPrefab, armorParent).GetComponent<ShopKeeperItem>().InitializeAsArmor(wearableEquipmentOption);
            }
        }

        public void OnPause()
        {
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
        }
    }
}