using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Core;
using Vi.ScriptableObjects;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using Vi.Utility;

namespace Vi.UI
{
    public class CharacterSelectUI : MonoBehaviour
    {
        [SerializeField] private Button returnButton;
        [SerializeField] private Text webRequestStatusText;
        [SerializeField] private Text gameVersionText;

        [Header("Character Select")]
        [SerializeField] private GameObject characterSelectParent;
        [SerializeField] private CharacterCard[] characterCardInstances;
        [SerializeField] private Transform characterCardParent;
        [SerializeField] private Button selectCharacterButton;
        [SerializeField] private Button goToTrainingRoomButton;
        [SerializeField] private Sprite defaultEquipmentSprite;
        [SerializeField] private RectTransform selectionBarSelectedImage;
        [SerializeField] private RectTransform selectionBarUnselectedImage;
        [SerializeField] private Button deleteCharacterButton;
        [Header("Stats Section")]
        [SerializeField] private GameObject statsParent;
        [Header("Gear Section")]
        [SerializeField] private GameObject gearParent;
        [SerializeField] private Image primaryWeaponIcon;
        [SerializeField] private Text primaryWeaponText;
        [SerializeField] private Image secondaryWeaponIcon;
        [SerializeField] private Text secondaryWeaponText;
        [SerializeField] private CharacterReference.EquipmentType[] equipmentTypeKeys;
        [SerializeField] private Image[] equipmentImageValues;

        [Header("Character Customization")]
        [SerializeField] private GameObject characterCustomizationParent;
        [SerializeField] private Transform customizationRowsParent;
        [SerializeField] private CharacterCustomizationRow characterCustomizationRowPrefab;
        [SerializeField] private CharacterCustomizationButton characterCustomizationButtonPrefab;
        [SerializeField] private InputField characterNameInputField;
        [SerializeField] private Button finishCharacterCustomizationButton;
        [SerializeField] private Vector3 previewCharacterPosition = new Vector3(0.6f, 0, -7);
        [SerializeField] private Vector3 previewCharacterRotation = new Vector3(0, 180, 0);

        private List<MaterialCustomizationParent> characterMaterialParents = new List<MaterialCustomizationParent>();
        private List<EquipmentCustomizationParent> characterEquipmentParents = new List<EquipmentCustomizationParent>();
        private List<GameObject> otherCustomizationRowParents = new List<GameObject>();

        private struct EquipmentCustomizationParent
        {
            public CharacterReference.EquipmentType equipmentType;
            public Transform parent;
        }

        private struct MaterialCustomizationParent
        {
            public CharacterReference.MaterialApplicationLocation applicationLocation;
            public Transform parent;
        }

        [Header("Server Browser")]
        [SerializeField] private GameObject alertBoxPrefab;
        [SerializeField] private ServerListElement serverListElement;
        [SerializeField] private Transform serverListElementParent;
        [SerializeField] private GameObject serverListParent;
        [SerializeField] private Button connectButton;
        [SerializeField] private Button closeServersMenuButton;

        private List<CharacterReference.EquipmentType> equipmentTypesIncludedInCharacterAppearance = new List<CharacterReference.EquipmentType>()
        {
            CharacterReference.EquipmentType.Hair,
            CharacterReference.EquipmentType.Beard,
            CharacterReference.EquipmentType.Brows
        };

        private Vector2 statsOpenAnchoredPosition;
        private Vector2 statsOpenAnchorMin;
        private Vector2 statsOpenAnchorMax;
        private Vector2 statsOpenPivot;

        private Vector2 gearOpenAnchoredPosition;
        private Vector2 gearOpenAnchorMin;
        private Vector2 gearOpenAnchorMax;
        private Vector2 gearOpenPivot;

        private bool statsSelected = true;
        public void OpenStats()
        {
            statsSelected = true;
        }

        public void OpenGear()
        {
            statsSelected = false;
        }

        private const float selectionBarTransitionTime = 8;
        private void UpdateSelectionBarPositions()
        {
            if (statsSelected)
            {
                selectionBarSelectedImage.anchorMin = Vector2.Lerp(selectionBarSelectedImage.anchorMin, statsOpenAnchorMin, Time.deltaTime * selectionBarTransitionTime);
                selectionBarSelectedImage.anchorMax = Vector2.Lerp(selectionBarSelectedImage.anchorMax, statsOpenAnchorMax, Time.deltaTime * selectionBarTransitionTime);
                selectionBarSelectedImage.pivot = Vector2.Lerp(selectionBarSelectedImage.pivot, statsOpenPivot, Time.deltaTime * selectionBarTransitionTime);

                selectionBarUnselectedImage.anchorMin = Vector2.Lerp(selectionBarUnselectedImage.anchorMin, gearOpenAnchorMin, Time.deltaTime * selectionBarTransitionTime);
                selectionBarUnselectedImage.anchorMax = Vector2.Lerp(selectionBarUnselectedImage.anchorMax, gearOpenAnchorMax, Time.deltaTime * selectionBarTransitionTime);
                selectionBarUnselectedImage.pivot = Vector2.Lerp(selectionBarUnselectedImage.pivot, gearOpenPivot, Time.deltaTime * selectionBarTransitionTime);
            }
            else
            {
                selectionBarSelectedImage.anchorMin = Vector2.Lerp(selectionBarSelectedImage.anchorMin, gearOpenAnchorMin, Time.deltaTime * selectionBarTransitionTime);
                selectionBarSelectedImage.anchorMax = Vector2.Lerp(selectionBarSelectedImage.anchorMax, gearOpenAnchorMax, Time.deltaTime * selectionBarTransitionTime);
                selectionBarSelectedImage.pivot = Vector2.Lerp(selectionBarSelectedImage.pivot, gearOpenPivot, Time.deltaTime * selectionBarTransitionTime);

                selectionBarUnselectedImage.anchorMin = Vector2.Lerp(selectionBarUnselectedImage.anchorMin, statsOpenAnchorMin, Time.deltaTime * selectionBarTransitionTime);
                selectionBarUnselectedImage.anchorMax = Vector2.Lerp(selectionBarUnselectedImage.anchorMax, statsOpenAnchorMax, Time.deltaTime * selectionBarTransitionTime);
                selectionBarUnselectedImage.pivot = Vector2.Lerp(selectionBarUnselectedImage.pivot, statsOpenPivot, Time.deltaTime * selectionBarTransitionTime);
            }

            selectionBarSelectedImage.anchoredPosition = Vector2.Lerp(selectionBarSelectedImage.anchoredPosition, statsSelected ? statsOpenAnchoredPosition : gearOpenAnchoredPosition, Time.deltaTime * selectionBarTransitionTime);
            selectionBarUnselectedImage.anchoredPosition = Vector2.Lerp(selectionBarUnselectedImage.anchoredPosition, statsSelected ? gearOpenAnchoredPosition : statsOpenAnchoredPosition, Time.deltaTime * selectionBarTransitionTime);
        }

        private void Awake()
        {
            OpenStats();

            statsOpenAnchoredPosition = selectionBarSelectedImage.anchoredPosition;
            statsOpenAnchorMin = selectionBarSelectedImage.anchorMin;
            statsOpenAnchorMax = selectionBarSelectedImage.anchorMax;
            statsOpenPivot = selectionBarSelectedImage.pivot;

            gearOpenAnchoredPosition = selectionBarUnselectedImage.anchoredPosition;
            gearOpenAnchorMin = selectionBarUnselectedImage.anchorMin;
            gearOpenAnchorMax = selectionBarUnselectedImage.anchorMax;
            gearOpenPivot = selectionBarUnselectedImage.pivot;

            OpenCharacterSelect();
            finishCharacterCustomizationButton.interactable = characterNameInputField.text.Length > 0;
            selectCharacterButton.interactable = !string.IsNullOrEmpty(selectedCharacter._id.ToString()) & WebRequestManager.Singleton.GameIsUpToDate;
            selectCharacterButton.onClick.AddListener(() => StartCoroutine(AutoConnectToHubServer()));
            goToTrainingRoomButton.interactable = !string.IsNullOrEmpty(selectedCharacter._id.ToString());

            deleteCharacterButton.onClick.RemoveAllListeners();
            deleteCharacterButton.onClick.AddListener(() => StartCoroutine(DeleteCharacterCoroutine(selectedCharacter)));
        }

        private List<ButtonInfo> characterCardButtonReference = new List<ButtonInfo>();

        private IEnumerator RefreshCharacterCards()
        {
            characterCardButtonReference.Clear();

            for (int i = 0; i < characterCardInstances.Length; i++)
            {
                characterCardInstances[i].InitializeAsLockedCharacter();
            }

            webRequestStatusText.gameObject.SetActive(true);
            webRequestStatusText.text = "LOADING CHARACTERS";
            
            WebRequestManager.Singleton.RefreshCharacters();
            yield return new WaitUntil(() => !WebRequestManager.Singleton.IsRefreshingCharacters);

            bool addButtonCreated = false;
            for (int i = 0; i < characterCardInstances.Length; i++)
            {
                if (i < WebRequestManager.Singleton.Characters.Count)
                {
                    WebRequestManager.Character character = WebRequestManager.Singleton.Characters[i];
                    characterCardInstances[i].InitializeAsCharacter(character);
                    characterCardInstances[i].Button.onClick.RemoveAllListeners();
                    characterCardInstances[i].Button.onClick.AddListener(delegate { UpdateSelectedCharacter(character); });
                    characterCardButtonReference.Add(new ButtonInfo(characterCardInstances[i].Button, "CharacterCard", character._id.ToString()));
                }
                else if (!addButtonCreated)
                {
                    addButtonCreated = true;
                    characterCardInstances[i].InitializeAsAddButton();
                    characterCardInstances[i].Button.onClick.RemoveAllListeners();
                    characterCardInstances[i].Button.onClick.AddListener(delegate { OpenCharacterCustomization(); });
                }
            }

            webRequestStatusText.gameObject.SetActive(false);
        }

        private readonly int leftStartOffset = 400;
        private readonly int rightStartOffset = 450;
        private readonly int spacing = -110;
        private int leftYLocalPosition;
        private int rightYLocalPosition;
        private int leftQueuedSpacing;
        private int rightQueuedSpacing;

        private List<ButtonInfo> customizationButtonReference = new List<ButtonInfo>();

        private struct ButtonInfo
        {
            public Button button;
            public string key;
            public string value;

            public ButtonInfo(Button button, string key, string value)
            {
                this.button = button;
                this.key = key;
                this.value = value;
            }
        }

        private void ClearMaterialsAndEquipmentOptions()
        {
            foreach (MaterialCustomizationParent materialCustomizationParent in characterMaterialParents)
            {
                Destroy(materialCustomizationParent.parent.gameObject);
            }
            characterMaterialParents.Clear();

            foreach (EquipmentCustomizationParent equipmentCustomizationParent in characterEquipmentParents)
            {
                Destroy(equipmentCustomizationParent.parent.gameObject);
            }
            characterEquipmentParents.Clear();

            foreach (GameObject rowParent in otherCustomizationRowParents)
            {
                Destroy(rowParent);
            }
            otherCustomizationRowParents.Clear();

            foreach (ButtonInfo buttonInfo in customizationButtonReference)
            {
                Destroy(buttonInfo.button.gameObject);
            }
            customizationButtonReference.Clear();
        }

        private void RefreshMaterialsAndEquipmentOptions(CharacterReference.RaceAndGender raceAndGender)
        {
            ClearMaterialsAndEquipmentOptions();

            leftYLocalPosition = leftStartOffset;
            rightYLocalPosition = rightStartOffset;
            leftQueuedSpacing = 0;
            rightQueuedSpacing = 0;

            List<KeyValuePair<CharacterReference.MaterialApplicationLocation, Color>> materialColorList = new List<KeyValuePair<CharacterReference.MaterialApplicationLocation, Color>>();
            foreach (CharacterReference.CharacterMaterial characterMaterial in PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender))
            {
                if (characterMaterial.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Head) { continue; }

                Transform buttonParent = characterMaterialParents.Find(item => item.applicationLocation == characterMaterial.materialApplicationLocation).parent;
                if (!buttonParent)
                {
                    buttonParent = Instantiate(characterCustomizationRowPrefab.gameObject, customizationRowsParent).transform;

                    bool isOnLeftSide = true;
                    //isOnLeftSide = !isOnLeftSide;
                    int equipmentCount = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender).FindAll(item => item.materialApplicationLocation == characterMaterial.materialApplicationLocation).Count;
                    if (isOnLeftSide) { leftYLocalPosition += spacing + leftQueuedSpacing; leftQueuedSpacing = equipmentCount / 11 * customizationRowSpacing; } else { rightYLocalPosition += spacing + rightQueuedSpacing; rightQueuedSpacing = equipmentCount / 11 * customizationRowSpacing; }
                    buttonParent.localPosition = new Vector3(buttonParent.localPosition.x * (isOnLeftSide ? 1 : -1), isOnLeftSide ? leftYLocalPosition : rightYLocalPosition, 0);

                    TextAnchor childAlignment = isOnLeftSide ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
                    buttonParent.GetComponent<CharacterCustomizationRow>().GetLayoutGroup().childAlignment = childAlignment;
                    Text headerText = buttonParent.GetComponentInChildren<Text>();
                    headerText.text = characterMaterial.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Body ? "Skin Color" : characterMaterial.materialApplicationLocation.ToString();
                    if (!isOnLeftSide)
                    {
                        headerText.transform.localPosition += new Vector3(300, 0, 0);
                        headerText.alignment = TextAnchor.MiddleLeft;
                    }
                    characterMaterialParents.Add(new MaterialCustomizationParent() { applicationLocation = characterMaterial.materialApplicationLocation, parent = buttonParent });
                }
                CharacterCustomizationRow rowElement = buttonParent.GetComponent<CharacterCustomizationRow>();
                buttonParent = rowElement.GetLayoutGroup().transform;

                Color textureAverageColor = characterMaterial.averageTextureColor;

                CharacterCustomizationButton buttonElement = rowElement.GetUninitializedButton();
                buttonElement.InitializeAsColor(textureAverageColor);
                materialColorList.Add(new KeyValuePair<CharacterReference.MaterialApplicationLocation, Color>(characterMaterial.materialApplicationLocation, textureAverageColor));

                buttonElement.Button.onClick.AddListener(delegate { ChangeCharacterMaterial(characterMaterial); });
                customizationButtonReference.Add(new ButtonInfo(buttonElement.Button, characterMaterial.materialApplicationLocation.ToString(), characterMaterial.material.name));
            }

            foreach (CharacterReference.WearableEquipmentOption equipmentOption in PlayerDataManager.Singleton.GetCharacterReference().GetCharacterEquipmentOptions(raceAndGender))
            {
                if (!equipmentTypesIncludedInCharacterAppearance.Contains(equipmentOption.equipmentType)) { continue; }

                Transform buttonParent = characterEquipmentParents.Find(item => item.equipmentType == equipmentOption.equipmentType).parent;
                CharacterCustomizationRow rowElement = null;
                if (!buttonParent)
                {
                    buttonParent = Instantiate(characterCustomizationRowPrefab.gameObject, customizationRowsParent).transform;

                    bool isOnLeftSide = equipmentTypesIncludedInCharacterAppearance.Contains(equipmentOption.equipmentType);
                    //isOnLeftSide = !isOnLeftSide;
                    int equipmentCount = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterEquipmentOptions(raceAndGender).FindAll(item => item.equipmentType == equipmentOption.equipmentType).Count;
                    if (isOnLeftSide) { leftYLocalPosition += spacing + leftQueuedSpacing; leftQueuedSpacing = equipmentCount / 11 * customizationRowSpacing; } else { rightYLocalPosition += spacing + rightQueuedSpacing; rightQueuedSpacing = equipmentCount / 11 * customizationRowSpacing; }
                    buttonParent.localPosition = new Vector3(buttonParent.localPosition.x * (isOnLeftSide ? 1 : -1), isOnLeftSide ? leftYLocalPosition : rightYLocalPosition, 0);

                    TextAnchor childAlignment = isOnLeftSide ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
                    rowElement = buttonParent.GetComponent<CharacterCustomizationRow>();
                    rowElement.GetLayoutGroup().childAlignment = childAlignment;
                    Text headerText = buttonParent.GetComponentInChildren<Text>();
                    headerText.text = equipmentOption.equipmentType.ToString();
                    if (!isOnLeftSide)
                    {
                        headerText.transform.localPosition += new Vector3(300, 0, 0);
                        headerText.alignment = TextAnchor.MiddleLeft;
                    }
                    characterEquipmentParents.Add(new EquipmentCustomizationParent() { equipmentType = equipmentOption.equipmentType, parent = buttonParent });

                    buttonParent = rowElement.GetLayoutGroup().transform;
                    CharacterCustomizationButton removeButton = rowElement.GetUninitializedButton();
                    removeButton.InitializeAsRemoveEquipment();
                    removeButton.Button.onClick.RemoveAllListeners();
                    removeButton.Button.onClick.AddListener(delegate { ChangeCharacterEquipment(new CharacterReference.WearableEquipmentOption(equipmentOption.equipmentType), raceAndGender); });
                    customizationButtonReference.Add(new ButtonInfo(removeButton.Button, equipmentOption.equipmentType.ToString(), "Remove"));
                }
                else
                {
                    rowElement = buttonParent.GetComponentInParent<CharacterCustomizationRow>();
                    buttonParent = rowElement.GetLayoutGroup().transform;
                }

                CharacterCustomizationButton buttonElement = rowElement.GetUninitializedButton();
                buttonElement.InitializeAsEquipment(equipmentOption, raceAndGender);
                buttonElement.Button.onClick.AddListener(delegate { ChangeCharacterEquipment(equipmentOption, raceAndGender); });
                customizationButtonReference.Add(new ButtonInfo(buttonElement.Button, equipmentOption.equipmentType.ToString(), equipmentOption.GetModel(raceAndGender, PlayerDataManager.Singleton.GetCharacterReference().GetEmptyWearableEquipment()).name));
            }

            Transform raceButtonParent = Instantiate(characterCustomizationRowPrefab.gameObject, customizationRowsParent).transform;
            CharacterCustomizationRow raceRowElement = raceButtonParent.GetComponent<CharacterCustomizationRow>();
            otherCustomizationRowParents.Add(raceButtonParent.gameObject);
            leftYLocalPosition += spacing + leftQueuedSpacing;
            int raceCount = 2;
            leftQueuedSpacing = raceCount / 11 * customizationRowSpacing;
            raceRowElement.transform.localPosition = new Vector3(raceButtonParent.localPosition.x, leftYLocalPosition, 0);
            raceRowElement.rowHeaderText.text = "Race";
            raceButtonParent = raceRowElement.GetLayoutGroup().transform;

            foreach (string race in new List<string>() { "Human" })
            {
                CharacterCustomizationButton buttonElement = raceRowElement.GetUninitializedButton();

                switch (race)
                {
                    case "Human":
                        buttonElement.InitializeAsColor(new Color(210 / 255f, 180 / 255f, 140 / 255f, 1));
                        break;
                    case "Orc":
                        buttonElement.InitializeAsColor(Color.green);
                        break;
                    default:
                        Debug.Log("Not sure how to handle race string " + race);
                        break;
                }

                buttonElement.Button.onClick.AddListener(delegate { ChangeCharacterModel(race, true); });
                customizationButtonReference.Add(new ButtonInfo(buttonElement.Button, "Race", race));
            }

            Transform genderButtonParent = Instantiate(characterCustomizationRowPrefab.gameObject, customizationRowsParent).transform;
            CharacterCustomizationRow genderRowElement = genderButtonParent.GetComponent<CharacterCustomizationRow>();
            otherCustomizationRowParents.Add(genderButtonParent.gameObject);
            leftYLocalPosition += spacing + leftQueuedSpacing;
            int genderCount = 2;
            leftQueuedSpacing = genderCount / 11 * customizationRowSpacing;
            genderRowElement.transform.localPosition = new Vector3(genderButtonParent.localPosition.x, leftYLocalPosition, 0);
            genderRowElement.rowHeaderText.text = "Gender";
            genderButtonParent = genderRowElement.GetLayoutGroup().transform;

            CharacterCustomizationButton boyButtonElement = genderRowElement.GetUninitializedButton();
            boyButtonElement.InitializeAsColor(Color.blue);
            boyButtonElement.Button.onClick.AddListener(delegate { ChangeCharacterModel("Male", false); });
            customizationButtonReference.Add(new ButtonInfo(boyButtonElement.Button, "Gender", "Male"));
            
            CharacterCustomizationButton girlButtonElement = genderRowElement.GetUninitializedButton();
            girlButtonElement.InitializeAsColor(Color.magenta);
            girlButtonElement.Button.onClick.AddListener(delegate { ChangeCharacterModel("Female", false); });
            customizationButtonReference.Add(new ButtonInfo(girlButtonElement.Button, "Gender", "Female"));
        }

        private const int customizationRowSpacing = -2000;

        private void RefreshButtonInteractability(bool disableAll = false)
        {
            selectCharacterButton.interactable = !string.IsNullOrEmpty(selectedCharacter._id.ToString()) & WebRequestManager.Singleton.GameIsUpToDate;
            goToTrainingRoomButton.interactable = !string.IsNullOrEmpty(selectedCharacter._id.ToString());

            foreach (ButtonInfo buttonInfo in characterCardButtonReference)
            {
                if (disableAll) { buttonInfo.button.interactable = false; continue; }

                switch (buttonInfo.key)
                {
                    case "CharacterCard":
                        buttonInfo.button.interactable = selectedCharacter._id != buttonInfo.value;
                        break;
                    default:
                        Debug.LogError("Not sure how to handle button key " + buttonInfo.key);
                        break;
                }
            }

            foreach (ButtonInfo buttonInfo in customizationButtonReference)
            {
                if (disableAll)
                {
                    buttonInfo.button.GetComponent<CharacterCustomizationButton>().SetSelectedState(true);
                    buttonInfo.button.interactable = false;
                    continue;
                }

                switch (buttonInfo.key)
                {
                    case "Eyes":
                        buttonInfo.button.GetComponent<CharacterCustomizationButton>().SetSelectedState(selectedCharacter.eyeColor != buttonInfo.value);
                        break;
                    case "Body":
                        buttonInfo.button.GetComponent<CharacterCustomizationButton>().SetSelectedState(selectedCharacter.bodyColor != buttonInfo.value);
                        break;
                    case "Brows":
                        buttonInfo.button.GetComponent<CharacterCustomizationButton>().SetSelectedState(selectedCharacter.brows == "" ? buttonInfo.value != "Remove" : buttonInfo.value != selectedCharacter.brows);
                        break;
                    case "Hair":
                        buttonInfo.button.GetComponent<CharacterCustomizationButton>().SetSelectedState(selectedCharacter.hair == "" ? buttonInfo.value != "Remove" : buttonInfo.value != selectedCharacter.hair);
                        break;
                    case "Beard":
                        buttonInfo.button.GetComponent<CharacterCustomizationButton>().SetSelectedState(selectedCharacter.beard == "" ? buttonInfo.value != "Remove" : buttonInfo.value != selectedCharacter.beard);
                        break;
                    case "Race":
                        buttonInfo.button.GetComponent<CharacterCustomizationButton>().SetSelectedState(selectedRace != buttonInfo.value);
                        break;
                    case "Gender":
                        buttonInfo.button.GetComponent<CharacterCustomizationButton>().SetSelectedState(selectedGender != buttonInfo.value);
                        break;
                    default:
                        Debug.LogError("Not sure how to handle button key " + buttonInfo.key);
                        break;
                }
            }
        }

        private WebRequestManager.Character selectedCharacter;
        private GameObject previewObject;
        private LoadoutManager loadoutManager;
        public void UpdateSelectedCharacter(WebRequestManager.Character character)
        {
            goToTrainingRoomButton.interactable = true;
            characterNameInputField.text = character.name.ToString();
            var playerModelOptionList = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions();
            KeyValuePair<int, int> kvp = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptionIndices(character.model.ToString());
            int characterIndex = kvp.Key;
            int skinIndex = kvp.Value;

            if (characterIndex == -1)
            {
                if (previewObject) { Destroy(previewObject); }
                selectedCharacter = default;
                RefreshButtonInteractability();
                return;
            }
            
            CharacterReference.PlayerModelOption playerModelOption = playerModelOptionList[characterIndex];

            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();

            bool shouldCreateNewModel = selectedCharacter.model != character.model;

            if (shouldCreateNewModel)
            {
                ClearMaterialsAndEquipmentOptions();
                if (previewObject) { Destroy(previewObject); }
                // Instantiate the player model
                previewObject = Instantiate(playerModelOptionList[characterIndex].playerPrefab, previewCharacterPosition, Quaternion.Euler(previewCharacterRotation));
                SceneManager.MoveGameObjectToScene(previewObject, gameObject.scene);
            }
            
            previewObject.GetComponent<AnimationHandler>().ChangeCharacter(character);

            string[] raceAndGenderStrings = Regex.Matches(playerModelOption.raceAndGender.ToString(), @"([A-Z][a-z]+)").Cast<Match>().Select(m => m.Value).ToArray();
            selectedRace = raceAndGenderStrings[0];
            selectedGender = raceAndGenderStrings[1];
            CharacterReference.RaceAndGender raceAndGender = System.Enum.Parse<CharacterReference.RaceAndGender>(selectedRace + selectedGender);

            if (WebRequestManager.Singleton.InventoryItems.ContainsKey(character._id.ToString()))
            {
                primaryWeaponIcon.gameObject.SetActive(true);
                secondaryWeaponIcon.gameObject.SetActive(true);
                for (int i = 0; i < equipmentTypeKeys.Length; i++)
                {
                    equipmentImageValues[i].gameObject.SetActive(true);
                }

                loadoutManager = previewObject.GetComponent<LoadoutManager>();
                loadoutManager.ApplyLoadout(raceAndGender, character.GetActiveLoadout(), character._id.ToString());

                primaryWeaponIcon.sprite = loadoutManager.PrimaryWeaponOption.weaponIcon;
                primaryWeaponText.text = loadoutManager.PrimaryWeaponOption.name;
                secondaryWeaponIcon.sprite = loadoutManager.SecondaryWeaponOption.weaponIcon;
                secondaryWeaponText.text = loadoutManager.SecondaryWeaponOption.name;
            }
            else
            {
                primaryWeaponIcon.gameObject.SetActive(false);
                secondaryWeaponIcon.gameObject.SetActive(false);
                for (int i = 0; i < equipmentTypeKeys.Length; i++)
                {
                    equipmentImageValues[i].gameObject.SetActive(false);
                }

                if (previewObject) { previewObject.GetComponent<LoadoutManager>().ApplyLoadout(raceAndGender, WebRequestManager.Singleton.GetDefaultDisplayLoadout(raceAndGender), character._id.ToString()); }
            }

            if (shouldCreateNewModel) { RefreshMaterialsAndEquipmentOptions(raceAndGender); }

            selectedCharacter = previewObject.GetComponentInChildren<AnimatorReference>().GetCharacterWebInfo(character);
            selectedCharacter.raceAndGender = raceAndGender;

            finishCharacterCustomizationButton.onClick.RemoveAllListeners();
            finishCharacterCustomizationButton.onClick.AddListener(delegate { StartCoroutine(ApplyCharacterChanges(selectedCharacter)); });

            RefreshButtonInteractability();
        }

        private string selectedRace = "Human";
        private string selectedGender = "Male";

        public void ChangeCharacterModel(string stringChange, bool isRace)
        {
            if (isRace)
                selectedRace = stringChange;
            else
                selectedGender = stringChange;

            CharacterReference.PlayerModelOption option = System.Array.Find(PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions(), item => item.raceAndGender == System.Enum.Parse<CharacterReference.RaceAndGender>(selectedRace + selectedGender));
            if (option == null) { Debug.LogError("Can't find player model option for " + selectedRace + " " + selectedGender); return; }
            WebRequestManager.Character character = selectedCharacter;
            character.model = option.skinOptions[0].name;
            UpdateSelectedCharacter(character);
        }

        public void ChangeCharacterMaterial(CharacterReference.CharacterMaterial characterMaterial)
        {
            previewObject.GetComponent<AnimationHandler>().ApplyCharacterMaterial(characterMaterial);
            UpdateSelectedCharacter(previewObject.GetComponentInChildren<AnimatorReference>().GetCharacterWebInfo(selectedCharacter));
        }

        public void ChangeCharacterEquipment(CharacterReference.WearableEquipmentOption wearableEquipmentOption, CharacterReference.RaceAndGender raceAndGender)
        {
            previewObject.GetComponent<AnimationHandler>().ApplyWearableEquipment(wearableEquipmentOption.equipmentType, wearableEquipmentOption, raceAndGender);
            UpdateSelectedCharacter(previewObject.GetComponentInChildren<AnimatorReference>().GetCharacterWebInfo(selectedCharacter));
        }

        private Unity.Netcode.Transports.UTP.UnityTransport networkTransport;
        private void Start()
        {
            networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();

            WebRequestManager.Singleton.RefreshServers();

            primaryWeaponIcon.gameObject.SetActive(false);
            secondaryWeaponIcon.gameObject.SetActive(false);
            for (int i = 0; i < equipmentTypeKeys.Length; i++)
            {
                equipmentImageValues[i].gameObject.SetActive(false);
            }

            PlayerDataManager.Singleton.SetGameModeSettings("");
        }

        List<ServerListElement> serverListElementList = new List<ServerListElement>();
        private float lastTextChangeTime;
        private bool lastClientState;
        private void Update()
        {
            statsParent.SetActive(statsSelected & !string.IsNullOrEmpty(selectedCharacter._id.ToString()));
            gearParent.SetActive(!statsSelected & !string.IsNullOrEmpty(selectedCharacter._id.ToString()));

            UpdateSelectionBarPositions();

            gameVersionText.text = WebRequestManager.Singleton.GameIsUpToDate ? "" : "GAME IS OUT OF DATE";

            if (lastClientState & !NetworkManager.Singleton.IsClient) { OpenCharacterSelect(); }
            lastClientState = NetworkManager.Singleton.IsClient;

            connectButton.interactable = serverListElementList.Exists(item => item.Server.ip == networkTransport.ConnectionData.Address & ushort.Parse(item.Server.port) == networkTransport.ConnectionData.Port) & !NetworkManager.Singleton.IsListening;

            if (webRequestStatusText.gameObject.activeSelf)
            {
                if (Time.time - lastTextChangeTime > 0.5f)
                {
                    lastTextChangeTime = Time.time;
                    switch (webRequestStatusText.text.Split(".").Length)
                    {
                        case 1:
                            webRequestStatusText.text = webRequestStatusText.text.Replace(".", "") + ".";
                            break;
                        case 2:
                            webRequestStatusText.text = webRequestStatusText.text.Replace(".", "") + "..";
                            break;
                        case 3:
                            webRequestStatusText.text = webRequestStatusText.text.Replace(".", "") + "...";
                            break;
                        case 4:
                            webRequestStatusText.text = webRequestStatusText.text.Replace(".", "");
                            break;
                    }
                }
            }

            if (!WebRequestManager.Singleton.IsRefreshingServers)
            {
                foreach (WebRequestManager.Server server in WebRequestManager.Singleton.HubServers)
                {
                    if (!serverListElementList.Find(item => item.Server._id == server._id))
                    {
                        ServerListElement serverListElementInstance = Instantiate(serverListElement.gameObject, serverListElementParent).GetComponent<ServerListElement>();
                        serverListElementInstance.Initialize(this, server);
                        serverListElementList.Add(serverListElementInstance);
                    }
                }

                if (Application.isEditor)
                {
                    foreach (WebRequestManager.Server server in WebRequestManager.Singleton.LobbyServers)
                    {
                        if (!serverListElementList.Find(item => item.Server._id == server._id))
                        {
                            ServerListElement serverListElementInstance = Instantiate(serverListElement.gameObject, serverListElementParent).GetComponent<ServerListElement>();
                            serverListElementInstance.Initialize(this, server);
                            serverListElementList.Add(serverListElementInstance);
                        }
                    }
                }
            }

            serverListElementList = serverListElementList.OrderBy(item => item.pingTime).ToList();
            for (int i = 0; i < serverListElementList.Count; i++)
            {
                serverListElementList[i].gameObject.SetActive(serverListElementList[i].pingTime >= 0);
                serverListElementList[i].transform.SetSiblingIndex(i);
            }

            if (loadoutManager)
            {
                CharacterReference.RaceAndGender raceAndGender = System.Enum.Parse<CharacterReference.RaceAndGender>(selectedRace + selectedGender);

                for (int i = 0; i < equipmentTypeKeys.Length; i++)
                {
                    equipmentImageValues[i].sprite = loadoutManager.GetEquippedEquipmentOption(equipmentTypeKeys[i]) == null ? defaultEquipmentSprite : loadoutManager.GetEquippedEquipmentOption(equipmentTypeKeys[i]).GetIcon(raceAndGender);
                }
            }
            else
            {
                for (int i = 0; i < equipmentTypeKeys.Length; i++)
                {
                    equipmentImageValues[i].sprite = defaultEquipmentSprite;
                }
            }
        }

        public void OnUsernameChange()
        {
            finishCharacterCustomizationButton.interactable = characterNameInputField.text.Length > 0;
            selectedCharacter.name = characterNameInputField.text;
        }

        public void ReturnToMainMenu()
        {
            NetSceneManager.Singleton.LoadScene("Main Menu");
        }

        private bool isEditingExistingCharacter;
        public void OpenCharacterCustomization()
        {
            returnButton.gameObject.SetActive(true);
            characterSelectParent.SetActive(false);
            characterCustomizationParent.SetActive(true);

            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(OpenCharacterSelect);

            selectedCharacter = new WebRequestManager.Character();
            UpdateSelectedCharacter(WebRequestManager.Singleton.GetDefaultCharacter());
            finishCharacterCustomizationButton.GetComponentInChildren<Text>().text = "CREATE";
            isEditingExistingCharacter = false;
        }

        private void OpenCharacterCustomization(WebRequestManager.Character character)
        {
            returnButton.gameObject.SetActive(true);
            characterSelectParent.SetActive(false);
            characterCustomizationParent.SetActive(true);

            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(OpenCharacterSelect);

            selectedCharacter = new WebRequestManager.Character();
            UpdateSelectedCharacter(character);
            finishCharacterCustomizationButton.GetComponentInChildren<Text>().text = "APPLY";
            isEditingExistingCharacter = true;
        }

        public void OpenCharacterSelect()
        {
            if (NetworkManager.Singleton.IsListening) { NetworkManager.Singleton.Shutdown(FasterPlayerPrefs.shouldDiscardMessageQueueOnNetworkShutdown); }

            StartCoroutine(RefreshCharacterCards());

            returnButton.gameObject.SetActive(true);
            characterSelectParent.SetActive(true);
            characterCustomizationParent.SetActive(false);
            serverListParent.SetActive(false);

            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(ReturnToMainMenu);

            UpdateSelectedCharacter(default);
        }

        private IEnumerator ApplyCharacterChanges(WebRequestManager.Character character)
        {
            RefreshButtonInteractability(true);
            finishCharacterCustomizationButton.interactable = false;
            returnButton.interactable = false;
            characterNameInputField.interactable = false;

            webRequestStatusText.gameObject.SetActive(true);
            webRequestStatusText.text = "UPLOADING CHARACTER";

            yield return isEditingExistingCharacter ? WebRequestManager.Singleton.UpdateCharacterCosmetics(character) : WebRequestManager.Singleton.CharacterPostRequest(character);

            webRequestStatusText.gameObject.SetActive(true);

            RefreshButtonInteractability();
            finishCharacterCustomizationButton.interactable = true;
            returnButton.interactable = true;
            characterNameInputField.interactable = true;

            OpenCharacterSelect();
        }

        private IEnumerator DeleteCharacterCoroutine(WebRequestManager.Character character)
        {
            RefreshButtonInteractability(true);
            finishCharacterCustomizationButton.interactable = false;
            deleteCharacterButton.interactable = false;
            returnButton.interactable = false;
            characterNameInputField.interactable = false;

            foreach (CharacterCard card in characterCardInstances)
            {
                card.Button.interactable = false;
            }

            webRequestStatusText.gameObject.SetActive(true);
            webRequestStatusText.text = "DELETING CHARACTER";

            yield return WebRequestManager.Singleton.CharacterDisableRequest(character._id.ToString());

            webRequestStatusText.gameObject.SetActive(false);

            RefreshButtonInteractability();
            finishCharacterCustomizationButton.interactable = true;
            deleteCharacterButton.interactable = true;
            returnButton.interactable = true;
            characterNameInputField.interactable = true;

            foreach (CharacterCard card in characterCardInstances)
            {
                card.Button.interactable = true;
            }

            OpenCharacterSelect();
        }

        public void GoToTrainingRoom()
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(selectedCharacter._id.ToString());
            if (NetworkManager.Singleton.StartHost())
            {
                NetSceneManager.Singleton.LoadScene("Training Room");
                NetSceneManager.Singleton.LoadScene("Eclipse Grove");
            }
            else
            {
                Debug.LogError("Error trying to start host to go to training room");
            }
        }

        public IEnumerator AutoConnectToHubServer()
        {
            selectCharacterButton.interactable = false;

            WebRequestManager.Singleton.RefreshServers();
            WebRequestManager.Singleton.CheckGameVersion();

            yield return new WaitUntil(() => !WebRequestManager.Singleton.IsRefreshingServers & !WebRequestManager.Singleton.IsCheckingGameVersion);

            if (!WebRequestManager.Singleton.GameIsUpToDate) { yield break; }

            if (WebRequestManager.Singleton.HubServers.Length > 0)
            {
                networkTransport.ConnectionData.Address = WebRequestManager.Singleton.HubServers[0].ip;
                networkTransport.ConnectionData.Port = ushort.Parse(WebRequestManager.Singleton.HubServers[0].port);
                StartClient();
            }
            else
            {
                Instantiate(alertBoxPrefab).GetComponentInChildren<Text>().text = "No Hub Server Online.";
            }
            
            selectCharacterButton.interactable = true;
        }

        public void OpenServerBrowser()
        {
            returnButton.gameObject.SetActive(false);
            characterSelectParent.SetActive(false);
            serverListParent.SetActive(true);
            RefreshServerBrowser();
        }

        public void RefreshServerBrowser()
        {
            WebRequestManager.Singleton.RefreshServers();
            foreach (ServerListElement serverListElement in serverListElementList)
            {
                Destroy(serverListElement.gameObject);
            }
            serverListElementList.Clear();
        }

        public void StartClient()
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(selectedCharacter._id.ToString());
            NetworkManager.Singleton.StartClient();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(previewCharacterPosition, 0.5f);
        }
    }
}