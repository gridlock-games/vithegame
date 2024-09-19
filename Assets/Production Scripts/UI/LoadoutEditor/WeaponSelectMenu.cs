using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.ScriptableObjects;
using UnityEngine.UI;
using Vi.Player;
using UnityEngine.Video;

namespace Vi.UI
{
    public class WeaponSelectMenu : Menu
    {
        [SerializeField] private Transform weaponOptionScrollParent;
        [SerializeField] private LoadoutOptionElement loadoutOptionPrefab;
        [SerializeField] private Image[] abilityImages;
        [SerializeField] private List<AbilityPreviewVideo> abilityPreviewVideos = new List<AbilityPreviewVideo>();

        [System.Serializable]
        private class AbilityPreviewVideo
        {
            public ActionClip ability;
            public VideoClip video;
        }

        private List<Button> buttonList = new List<Button>();
        private LoadoutManager.WeaponSlotType weaponType;
        private int playerDataId;
        public void Initialize(CharacterReference.WeaponOption initialOption, CharacterReference.WeaponOption otherWeapon, LoadoutManager.WeaponSlotType weaponType, int loadoutSlot, int playerDataId)
        {
            this.weaponType = weaponType;
            this.playerDataId = playerDataId;
            Button invokeThis = null;
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(playerDataId);
            foreach (CharacterReference.WeaponOption weaponOption in PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions())
            {
                // If this weapon option isn't in our inventory, continue
                if (!WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Exists(item => item.itemId == weaponOption.itemWebId)) { continue; }

                LoadoutOptionElement ele = Instantiate(loadoutOptionPrefab.gameObject, weaponOptionScrollParent).GetComponent<LoadoutOptionElement>();
                ele.InitializeWeapon(weaponOption);
                Button button = ele.GetComponentInChildren<Button>();
                button.onClick.AddListener(delegate { ChangeWeapon(button, weaponOption, loadoutSlot); });

                // Always keep other weapon's button non-interactable
                if (weaponOption.weapon.GetWeaponClass() != otherWeapon.weapon.GetWeaponClass()) { buttonList.Add(button); }
                else { button.interactable = false; }

                if (weaponOption.itemWebId == initialOption.itemWebId) { invokeThis = button; }
            }

            invokeThis.onClick.Invoke();
        }

        private void ChangeWeapon(Button button, CharacterReference.WeaponOption weaponOption, int loadoutSlot)
        {
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(playerDataId);
            WebRequestManager.Loadout newLoadout = playerData.character.GetLoadoutFromSlot(loadoutSlot);
            string inventoryItemId = WebRequestManager.Singleton.InventoryItems[playerData.character._id.ToString()].Find(item => item.itemId == weaponOption.itemWebId).id;
            if (string.IsNullOrWhiteSpace(inventoryItemId)) { Debug.LogError("Unable to find inventory item for weapon option " + weaponOption.name); return; }

            foreach (Button b in buttonList)
            {
                b.interactable = true;
            }
            button.interactable = false;

            switch (weaponType)
            {
                case LoadoutManager.WeaponSlotType.Primary:
                    newLoadout.weapon1ItemId = inventoryItemId;
                    break;
                case LoadoutManager.WeaponSlotType.Secondary:
                    newLoadout.weapon2ItemId = inventoryItemId;
                    break;
                default:
                    Debug.LogError("Not sure how to handle weapon slot type " + weaponType);
                    break;
            }

            if (weaponPreviewObject) { Destroy(weaponPreviewObject); }
            if (weaponOption.weaponPreviewPrefab) { weaponPreviewObject = Instantiate(weaponOption.weaponPreviewPrefab); }

            if (!newLoadout.Equals(playerData.character.GetLoadoutFromSlot(loadoutSlot)))
            {
                PlayerDataManager.Singleton.StartCoroutine(WebRequestManager.Singleton.UpdateCharacterLoadout(playerData.character._id.ToString(), newLoadout));

                playerData.character = playerData.character.ChangeLoadoutFromSlot(loadoutSlot, newLoadout);
                PlayerDataManager.Singleton.SetPlayerData(playerData);
            }
            
            for (int i = 0; i < abilityImages.Length; i++)
            {
                ActionClip ability = weaponOption.weapon.GetAbilities()[i];
                abilityImages[i].sprite = ability.abilityImageIcon;
                if (abilityImages[i].TryGetComponent(out Button previewButton))
                {
                    previewButton.onClick.AddListener(() => ShowAbilityPreviewVideo(abilityPreviewVideos.Find(item => item.ability == ability)));
                }
                else
                {
                    Debug.LogError(i + " doesn't have a button component for preview ability video!");
                }
            }
        }

        [SerializeField] private GameObject abilityPreviewParent;
        [SerializeField] private Text videoOverlayText;
        [SerializeField] private VideoPlayer abilityPreviewVideoPlayer;
        [SerializeField] private RawImage abilityPreviewRawImage;

        private void ShowAbilityPreviewVideo(AbilityPreviewVideo abilityPreviewVideo)
        {
            abilityPreviewParent.SetActive(true);
            if (abilityPreviewVideo == null) // Show "no preview video"
            {
                if (abilityPreviewVideoPlayer.isPlaying) { abilityPreviewVideoPlayer.Stop(); }
                abilityPreviewVideoPlayer.clip = null;
                videoOverlayText.text = "No Preview Video For This Ability";
                abilityPreviewRawImage.enabled = false;
            }
            else if (abilityPreviewVideo.video)
            {
                abilityPreviewVideoPlayer.clip = abilityPreviewVideo.video;
                abilityPreviewVideoPlayer.Play();
                videoOverlayText.text = "";
                abilityPreviewRawImage.enabled = true;
            }
            else // Show "no preview video"
            {
                if (abilityPreviewVideoPlayer.isPlaying) { abilityPreviewVideoPlayer.Stop(); }
                abilityPreviewVideoPlayer.clip = null;
                videoOverlayText.text = "No Preview Video For This Ability";
                abilityPreviewRawImage.enabled = false;
            }
        }

        public void CloseAbilityPreviewWindow()
        {
            abilityPreviewParent.SetActive(false);
            if (abilityPreviewVideoPlayer.isPlaying) { abilityPreviewVideoPlayer.Stop(); }
            abilityPreviewVideoPlayer.clip = null;
            videoOverlayText.text = "";
            abilityPreviewRawImage.enabled = false;
        }

        private GameObject weaponPreviewObject;
        private void OnDestroy()
        {
            Destroy(weaponPreviewObject);
        }
    }
}