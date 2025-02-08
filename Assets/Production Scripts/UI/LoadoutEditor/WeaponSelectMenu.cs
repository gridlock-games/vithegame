using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.ScriptableObjects;
using UnityEngine.UI;
using Vi.Player;
using UnityEngine.Video;
using Vi.Utility;

namespace Vi.UI
{
    public class WeaponSelectMenu : Menu
    {
        [SerializeField] private Transform weaponOptionScrollParent;
        [SerializeField] private LoadoutOptionElement loadoutOptionPrefab;
        [SerializeField] private Image[] abilityImages;
        [SerializeField] private List<AbilityPreviewVideo> abilityPreviewVideos = new List<AbilityPreviewVideo>();
        //[SerializeField] private Light previewLightPrefab;

        //private GameObject weaponPreviewParent;
        protected override void Awake()
        {
            base.Awake();
            //foreach (ImageOnDragData data in GetComponentsInChildren<ImageOnDragData>(true))
            //{
            //    data.OnDragEvent += OnCharPreviewDrag;
            //}

            //weaponPreviewParent = new GameObject("WeaponPreviewParent");
        }

        //private void OnCharPreviewDrag(Vector2 delta)
        //{
        //    if (weaponPreviewObject & weaponPreviewParent)
        //    {
        //        weaponPreviewParent.transform.rotation *= Quaternion.Euler(0, -delta.x * 0.25f, 0);
        //    }
        //}

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

            foreach (CharacterManager.InventoryItem inventoryItem in CharacterManager.GetInventory(playerData.character._id.ToString()))
            {
                CharacterReference.WeaponOption weaponOption = CharacterManager.GetWeaponOption(inventoryItem);

                if (weaponOption == null) { continue; }

                LoadoutOptionElement ele;
                if (loadoutOptionPrefab.TryGetComponent(out PooledObject pooledObject))
                {
                    ele = ObjectPoolingManager.SpawnObject(pooledObject, weaponOptionScrollParent).GetComponent<LoadoutOptionElement>();
                }
                else
                {
                    ele = Instantiate(loadoutOptionPrefab.gameObject, weaponOptionScrollParent).GetComponent<LoadoutOptionElement>();
                }

                ele.InitializeWeapon(weaponOption);
                Button button = ele.GetComponentInChildren<Button>();
                button.onClick.AddListener(delegate { ChangeWeapon(button, weaponOption, inventoryItem, loadoutSlot); });

                // Always keep other weapon's button non-interactable
                if (otherWeapon != null)
                {
                    if (weaponOption.weapon.GetWeaponClass() != otherWeapon.weapon.GetWeaponClass()) { buttonList.Add(button); }
                    else { button.interactable = false; }
                }
                else
                {
                    buttonList.Add(button);
                }

                if (initialOption != null)
                {
                    if (weaponOption.itemWebId == initialOption.itemWebId) { invokeThis = button; }
                }
            }

            if (invokeThis)
            {
                invokeThis.onClick.Invoke();
            }
        }

        private void ChangeWeapon(Button button, CharacterReference.WeaponOption weaponOption, CharacterManager.InventoryItem inventoryItem, int loadoutSlot)
        {
            PlayerDataManager.PlayerData playerData = PlayerDataManager.Singleton.GetPlayerData(playerDataId);
            CharacterManager.Loadout newLoadout = playerData.character.GetLoadoutFromSlot(loadoutSlot);

            string inventoryItemId = inventoryItem.id;
            
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

            //if (weaponPreviewObject)
            //{
            //    if (weaponPreviewObject.TryGetComponent(out PooledObject pooledObject))
            //    {
            //        if (pooledObject.IsSpawned)
            //        {
            //            ObjectPoolingManager.ReturnObjectToPool(pooledObject);
            //        }
            //        weaponPreviewObject = null;
            //    }
            //    else
            //    {
            //        Destroy(weaponPreviewObject);
            //    }
            //}

            //if (weaponPreviewCamera) { Destroy(weaponPreviewCamera.gameObject); }

            //if (weaponOption.weaponPreviewPrefab)
            //{
            //    if (weaponOption.weaponPreviewPrefab.TryGetComponent(out PooledObject pooledObject))
            //    {
            //        weaponPreviewObject = ObjectPoolingManager.SpawnObject(pooledObject, weaponPreviewParent.transform).gameObject;
            //    }
            //    else
            //    {
            //        weaponPreviewObject = Instantiate(weaponOption.weaponPreviewPrefab, weaponPreviewParent.transform);
            //    }

            //    GameObject lightInstance = Instantiate(previewLightPrefab.gameObject);
            //    lightInstance.transform.SetParent(weaponPreviewObject.transform, true);
            //    lightInstance.transform.localPosition = new Vector3(0, 3, 4);
            //    lightInstance.transform.localEulerAngles = new Vector3(30, 180, 0);
            //    lightInstance.transform.SetParent(null, true);

            //    weaponPreviewCamera = weaponPreviewObject.GetComponentInChildren<Camera>();
            //    if (weaponPreviewCamera)
            //    {
            //        weaponPreviewCamera.transform.SetParent(null, true);
            //    }
            //}

            if (!newLoadout.Equals(playerData.character.GetLoadoutFromSlot(loadoutSlot)))
            {
                PlayerDataManager.Singleton.StartCoroutine(WebRequestManager.Singleton.CharacterManager.UpdateCharacterLoadout(playerData.character._id.ToString(), newLoadout, true));

                playerData.character = playerData.character.ChangeLoadoutFromSlot(loadoutSlot, newLoadout);
                PlayerDataManager.Singleton.SetPlayerData(playerData);
            }
            
            for (int i = 0; i < abilityImages.Length; i++)
            {
                ActionClip ability = weaponOption.weapon.GetAbilities()[i];
                abilityImages[i].sprite = ability.abilityImageIcon;
                if (abilityImages[i].TryGetComponent(out Button previewButton))
                {
                    previewButton.onClick.RemoveAllListeners();
                    previewButton.onClick.AddListener(() => StartCoroutine(ShowAbilityPreviewVideo(abilityPreviewVideos.Find(item => item.ability == ability))));
                }
                else
                {
                    Debug.LogError(i + " doesn't have a button component for preview ability video!");
                }
            }
        }

        [SerializeField] private Image abilityPreviewBackground;
        [SerializeField] private Text videoOverlayText;
        [SerializeField] private VideoPlayer abilityPreviewVideoPlayer;
        [SerializeField] private RawImage abilityPreviewRawImage;

        private bool videoRunning;
        private IEnumerator ShowAbilityPreviewVideo(AbilityPreviewVideo abilityPreviewVideo)
        {
            if (videoRunning) { yield break; }
            videoRunning = true;
            abilityPreviewBackground.enabled = true;
            if (abilityPreviewVideo == null) // Show "no preview video"
            {
                if (abilityPreviewVideoPlayer.isPlaying) { abilityPreviewVideoPlayer.Stop(); }
                abilityPreviewVideoPlayer.clip = null;
                videoOverlayText.text = "No Preview Video For This Ability";
                abilityPreviewRawImage.enabled = false;
            }
            else if (abilityPreviewVideo.video)
            {
                videoOverlayText.text = "";
                abilityPreviewRawImage.enabled = false;
                abilityPreviewVideoPlayer.clip = abilityPreviewVideo.video;
                abilityPreviewVideoPlayer.Prepare();
                yield return new WaitUntil(() => abilityPreviewVideoPlayer.isPrepared);
                abilityPreviewVideoPlayer.Play();
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

        private void OnEnable()
        {
            abilityPreviewVideoPlayer.transform.localScale = Vector3.zero;
        }

        private const float videoUIAnimationSpeed = 3;
        private void Update()
        {
            abilityPreviewVideoPlayer.transform.localScale = Vector3.MoveTowards(abilityPreviewVideoPlayer.transform.localScale, videoRunning ? Vector3.one : Vector3.zero, Time.deltaTime * videoUIAnimationSpeed);
        }

        public void CloseAbilityPreviewWindow()
        {
            if (videoRunning)
            {
                StartCoroutine(StopVideoPlayer());
                abilityPreviewBackground.enabled = false;
                videoRunning = false;
            }
            else
            {
                GoBackToLastMenu();
            }
        }

        private IEnumerator StopVideoPlayer()
        {
            yield return new WaitUntil(() => abilityPreviewVideoPlayer.transform.localScale == Vector3.zero);
            if (abilityPreviewVideoPlayer.isPlaying) { abilityPreviewVideoPlayer.Stop(); }
        }

        //private GameObject weaponPreviewObject;
        //private Camera weaponPreviewCamera;
        private void OnDestroy()
        {
            //if (weaponPreviewParent) { Destroy(weaponPreviewParent); }
            
            //foreach (ImageOnDragData data in GetComponentsInChildren<ImageOnDragData>(true))
            //{
            //    data.OnDragEvent -= OnCharPreviewDrag;
            //}

            //if (weaponPreviewObject)
            //{
            //    if (weaponPreviewObject.TryGetComponent(out PooledObject pooledObject))
            //    {
            //        if (pooledObject.IsSpawned)
            //        {
            //            ObjectPoolingManager.ReturnObjectToPool(pooledObject);
            //        }
            //        weaponPreviewObject = null;
            //    }
            //    else
            //    {
            //        Destroy(weaponPreviewObject);
            //    }
            //}

            //if (weaponPreviewCamera) { Destroy(weaponPreviewCamera.gameObject); }
        }
    }
}