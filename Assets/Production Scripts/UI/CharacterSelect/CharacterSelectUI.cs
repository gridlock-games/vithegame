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
using TMPro;

namespace Vi.UI
{
    public class CharacterSelectUI : MonoBehaviour
    {
        [SerializeField] private UIElementHighlight UIElementHighlightPrefab;
        [SerializeField] private AlertBox tutorialAlertBox;
        [SerializeField] private Button returnButton;
        [SerializeField] private Text webRequestStatusText;
        [SerializeField] private Text gameVersionText;

        [Header("Character Preview")]
        [SerializeField] private Camera characterPreviewCamera;
        [SerializeField] private SpawnPoints.TransformData defaultCameraOrientation;
        [SerializeField] private SpawnPoints.TransformData headCameraOrientation;
        [SerializeField] private List<ComboCameraOrientation> previewUltimateCameraOrientation;

        [System.Serializable]
        private struct ComboCameraOrientation
        {
            public Weapon.WeaponClass weaponClass;
            public Weapon.Vector3AnimationCurve position;
            public Weapon.Vector3AnimationCurve rotation;
        }

        [Header("Character Select")]
        [SerializeField] private GameObject characterSelectParent;

        [SerializeField] private CharacterCard[] characterCardInstances;
        [SerializeField] private Transform characterCardParent;
        [SerializeField] private Button selectCharacterButton;
        [SerializeField] private Button goToTrainingRoomButton;
        [SerializeField] private RectTransform selectionBarSelectedImage;
        [SerializeField] private Image selectionBarGlowImage;
        [SerializeField] private RectTransform selectionBarUnselectedImage;
        [SerializeField] private Button deleteCharacterButton;
        [SerializeField] private Button tutorialButton;
        [SerializeField] private GameObject statsAndGearParent;

        [Header("Training Room Configuration")]
        [SerializeField] private TMP_Dropdown trainingRoomMapDropdown;
        [SerializeField] private InputField trainingRoomBotNumberDropdown;
        [SerializeField] private Button trainingRoomSettingsButton;

        [Header("Stats Section")]
        [SerializeField] private GameObject statsParent;

        [Header("Gear Section")]
        [SerializeField] private GameObject gearParent;

        [SerializeField] private WeaponDisplayElement primaryWeaponDisplayElement;
        [SerializeField] private WeaponDisplayElement secondaryWeaponDisplayElement;
        [SerializeField] private CharacterReference.EquipmentType[] equipmentTypeKeys;
        [SerializeField] private ArmorDisplayElement[] equipmentImageValues;

        [Header("Character Customization")]
        [SerializeField] private GameObject characterCustomizationParent;

        [SerializeField] private Transform customizationRowsParentLeft;
        [SerializeField] private Transform customizationRowsParentRight;
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
        [SerializeField] private AlertBox alertBoxPrefab;

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
            Color selectionBarGlowTarget = originalSelectionBarGlowColor;
            if (statsSelected)
            {
                selectionBarSelectedImage.anchorMin = Vector2.Lerp(selectionBarSelectedImage.anchorMin, statsOpenAnchorMin, Time.deltaTime * selectionBarTransitionTime);
                selectionBarSelectedImage.anchorMax = Vector2.Lerp(selectionBarSelectedImage.anchorMax, statsOpenAnchorMax, Time.deltaTime * selectionBarTransitionTime);
                selectionBarSelectedImage.pivot = Vector2.Lerp(selectionBarSelectedImage.pivot, statsOpenPivot, Time.deltaTime * selectionBarTransitionTime);

                selectionBarUnselectedImage.anchorMin = Vector2.Lerp(selectionBarUnselectedImage.anchorMin, gearOpenAnchorMin, Time.deltaTime * selectionBarTransitionTime);
                selectionBarUnselectedImage.anchorMax = Vector2.Lerp(selectionBarUnselectedImage.anchorMax, gearOpenAnchorMax, Time.deltaTime * selectionBarTransitionTime);
                selectionBarUnselectedImage.pivot = Vector2.Lerp(selectionBarUnselectedImage.pivot, gearOpenPivot, Time.deltaTime * selectionBarTransitionTime);

                if (Vector2.Distance(selectionBarSelectedImage.pivot, statsOpenPivot) > 0.1f)
                {
                    selectionBarGlowTarget = new Color(1, 1, 1, 0);
                }
            }
            else
            {
                selectionBarSelectedImage.anchorMin = Vector2.Lerp(selectionBarSelectedImage.anchorMin, gearOpenAnchorMin, Time.deltaTime * selectionBarTransitionTime);
                selectionBarSelectedImage.anchorMax = Vector2.Lerp(selectionBarSelectedImage.anchorMax, gearOpenAnchorMax, Time.deltaTime * selectionBarTransitionTime);
                selectionBarSelectedImage.pivot = Vector2.Lerp(selectionBarSelectedImage.pivot, gearOpenPivot, Time.deltaTime * selectionBarTransitionTime);

                selectionBarUnselectedImage.anchorMin = Vector2.Lerp(selectionBarUnselectedImage.anchorMin, statsOpenAnchorMin, Time.deltaTime * selectionBarTransitionTime);
                selectionBarUnselectedImage.anchorMax = Vector2.Lerp(selectionBarUnselectedImage.anchorMax, statsOpenAnchorMax, Time.deltaTime * selectionBarTransitionTime);
                selectionBarUnselectedImage.pivot = Vector2.Lerp(selectionBarUnselectedImage.pivot, statsOpenPivot, Time.deltaTime * selectionBarTransitionTime);

                if (Vector2.Distance(selectionBarSelectedImage.pivot, gearOpenPivot) > 0.1f)
                {
                    selectionBarGlowTarget = new Color(1, 1, 1, 0);
                }
            }

            selectionBarGlowImage.color = Color.Lerp(selectionBarGlowImage.color, selectionBarGlowTarget, Time.deltaTime * selectionBarTransitionTime);

            selectionBarSelectedImage.anchoredPosition = Vector2.Lerp(selectionBarSelectedImage.anchoredPosition, statsSelected ? statsOpenAnchoredPosition : gearOpenAnchoredPosition, Time.deltaTime * selectionBarTransitionTime);
            selectionBarUnselectedImage.anchoredPosition = Vector2.Lerp(selectionBarUnselectedImage.anchoredPosition, statsSelected ? gearOpenAnchoredPosition : statsOpenAnchoredPosition, Time.deltaTime * selectionBarTransitionTime);
        }

        private Color originalSelectionBarGlowColor;

        private Vector2 originalLeftPos;
        private Vector2 originalRightPos;

        private void Awake()
        {
            originalLeftPos = ((RectTransform)customizationRowsParentLeft).anchoredPosition;
            originalRightPos = ((RectTransform)customizationRowsParentRight).anchoredPosition;

            weaponClasses = (Weapon.WeaponClass[])System.Enum.GetValues(typeof(Weapon.WeaponClass));

            if (FasterPlayerPrefs.Singleton.GetBool("TutorialCompleted")) { tutorialAlertBox.DestroyAlert(); }
            else { tutorialAlertBox.gameObject.SetActive(true); }

            OpenStats();

            originalSelectionBarGlowColor = selectionBarGlowImage.color;

            statsOpenAnchoredPosition = selectionBarSelectedImage.anchoredPosition;
            statsOpenAnchorMin = selectionBarSelectedImage.anchorMin;
            statsOpenAnchorMax = selectionBarSelectedImage.anchorMax;
            statsOpenPivot = selectionBarSelectedImage.pivot;

            gearOpenAnchoredPosition = selectionBarUnselectedImage.anchoredPosition;
            gearOpenAnchorMin = selectionBarUnselectedImage.anchorMin;
            gearOpenAnchorMax = selectionBarUnselectedImage.anchorMax;
            gearOpenPivot = selectionBarUnselectedImage.pivot;

            StartCoroutine(OpenCharacterSelect(false));
            
            if (characterNameInputField.text.Length < 6)
            {
                finishCharacterCustomizationButton.interactable = false;
            }
            else if (characterNameInputField.text.Length > 10)
            {
                finishCharacterCustomizationButton.interactable = false;
            }
            else
            {
                finishCharacterCustomizationButton.interactable = true;
            }
            characterNameInputErrorText.text = "";

            selectCharacterButton.interactable = !string.IsNullOrEmpty(selectedCharacter._id.ToString()) & WebRequestManager.Singleton.GameIsUpToDate;
            selectCharacterButton.onClick.AddListener(() => AutoConnectToHub());
            goToTrainingRoomButton.interactable = !string.IsNullOrEmpty(selectedCharacter._id.ToString());

            deleteCharacterButton.onClick.RemoveAllListeners();
            deleteCharacterButton.onClick.AddListener(() => StartCoroutine(DeleteCharacterCoroutine(selectedCharacter)));

            foreach (ImageOnDragData data in GetComponentsInChildren<ImageOnDragData>(true))
            {
                data.OnDragEvent += OnCharPreviewDrag;
            }

            trainingRoomMapDropdown.ClearOptions();
            trainingRoomMapDropdown.AddOptions(trainingRoomMapOptions);
        }

        private void OnDestroy()
        {
            foreach (ImageOnDragData data in GetComponentsInChildren<ImageOnDragData>(true))
            {
                data.OnDragEvent -= OnCharPreviewDrag;
            }
        }

        private void OnCharPreviewDrag(Vector2 delta)
        {
            if (!characterSelectParent.activeSelf) { return; }

            if (previewObject)
            {
                if (previewObject.TryGetComponent(out AnimationHandler animationHandler))
                {
                    if (animationHandler.IsPlayingPreviewClip) { return; }
                }
                previewObject.transform.rotation *= Quaternion.Euler(0, -delta.x * 0.25f, 0);
            }
        }

        private List<ButtonInfo> characterCardButtonReference = new List<ButtonInfo>();

        private bool characterCardsAreDirty;
        private IEnumerator RefreshCharacterCards()
        {
            characterCardButtonReference.Clear();

            for (int i = 0; i < characterCardInstances.Length; i++)
            {
                characterCardInstances[i].InitializeAsLockedCharacter();
            }

            webRequestStatusText.gameObject.SetActive(true);
            webRequestStatusText.text = "LOADING CHARACTERS";

            yield return new WaitUntil(() => !WebRequestManager.Singleton.IsRefreshingCharacters);

            if (characterCardsAreDirty)
            {
                WebRequestManager.Singleton.RefreshCharacters();
                yield return new WaitUntil(() => !WebRequestManager.Singleton.IsRefreshingCharacters);
            }

            bool addButtonCreated = false;
            bool invokeFirstCharacterCard = false;
            for (int i = 0; i < characterCardInstances.Length; i++)
            {
                if (i < WebRequestManager.Singleton.Characters.Count)
                {
                    invokeFirstCharacterCard = true;
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
                    characterCardInstances[i].Button.onClick.AddListener(delegate { StartCoroutine(OpenCharacterCustomization()); });
                }
            }

            webRequestStatusText.gameObject.SetActive(false);

            if (invokeFirstCharacterCard)
            {
                if (characterCardInstances.Length > 0)
                {
                    yield return null;
                    characterCardInstances[0].Button.onClick.Invoke();
                }
            }
            else if (FasterPlayerPrefs.Singleton.GetBool("TutorialInProgress"))
            {
                CreateUIElementHighlight((RectTransform)characterCardInstances[0].transform);
            }
        }

        private readonly int leftStartOffset = 400;
        private readonly int rightStartOffset = 400;
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

        private List<CharacterCustomizationRow> customizationRowList = new List<CharacterCustomizationRow>();

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

            customizationRowList.Clear();
        }

        private void RefreshMaterialsAndEquipmentOptions(CharacterReference.RaceAndGender raceAndGender)
        {
            ClearMaterialsAndEquipmentOptions();

            leftYLocalPosition = leftStartOffset;
            rightYLocalPosition = rightStartOffset;
            leftQueuedSpacing = 0;
            rightQueuedSpacing = 0;

            //Transform raceButtonParent = Instantiate(characterCustomizationRowPrefab.gameObject, customizationRowsParentLeft).transform;
            //CharacterCustomizationRow raceRowElement = raceButtonParent.GetComponent<CharacterCustomizationRow>();
            //otherCustomizationRowParents.Add(raceButtonParent.gameObject);
            //leftYLocalPosition += spacing + leftQueuedSpacing;
            //int raceCount = 2;
            //leftQueuedSpacing = raceCount / 11 * customizationRowSpacing;
            //raceRowElement.transform.localPosition = new Vector3(raceButtonParent.localPosition.x, leftYLocalPosition, 0);
            //raceRowElement.rowHeaderText.text = "Race";
            //raceButtonParent = raceRowElement.GetLayoutGroup().transform;

            //foreach (string race in new List<string>() { "Human" })
            //{
            //    CharacterCustomizationButton buttonElement = raceRowElement.GetUninitializedButton();

            //    switch (race)
            //    {
            //        case "Human":
            //            buttonElement.InitializeAsColor(new Color(210 / 255f, 180 / 255f, 140 / 255f, 1));
            //            break;

            //        case "Orc":
            //            buttonElement.InitializeAsColor(Color.green);
            //            break;

            //        default:
            //            Debug.LogError("Not sure how to handle race string " + race);
            //            break;
            //    }

            //    buttonElement.Button.onClick.AddListener(delegate { ChangeCharacterModel(race, true); });
            //    customizationButtonReference.Add(new ButtonInfo(buttonElement.Button, "Race", race));
            //}

            Transform genderButtonParent = Instantiate(characterCustomizationRowPrefab.gameObject, customizationRowsParentLeft).transform;
            CharacterCustomizationRow genderRowElement = genderButtonParent.GetComponent<CharacterCustomizationRow>();
            otherCustomizationRowParents.Add(genderButtonParent.gameObject);
            leftYLocalPosition += spacing + leftQueuedSpacing;
            int genderCount = 2;
            leftQueuedSpacing = genderCount / 11 * customizationRowSpacing;
            genderRowElement.transform.localPosition = new Vector3(genderButtonParent.localPosition.x, leftYLocalPosition, 0);
            genderRowElement.rowHeaderText.text = "Gender".ToUpper();
            genderButtonParent = genderRowElement.GetLayoutGroup().transform;

            CharacterCustomizationButton boyButtonElement = genderRowElement.GetUninitializedButton();
            boyButtonElement.InitializeAsColor(Color.blue);
            boyButtonElement.Button.onClick.AddListener(delegate { ChangeCharacterModel("Male", false); });
            //customizationButtonReference.Add(new ButtonInfo(boyButtonElement.Button, "Gender", "Male"));

            CharacterCustomizationButton girlButtonElement = genderRowElement.GetUninitializedButton();
            girlButtonElement.InitializeAsColor(Color.magenta);
            girlButtonElement.Button.onClick.AddListener(delegate { ChangeCharacterModel("Female", false); });
            //customizationButtonReference.Add(new ButtonInfo(girlButtonElement.Button, "Gender", "Female"));

            if (selectedCharacter.raceAndGender == CharacterReference.RaceAndGender.HumanFemale)
            {
                genderRowElement.CounterIndex = 1;
            }
            else if (selectedCharacter.raceAndGender == CharacterReference.RaceAndGender.HumanMale)
            {
                genderRowElement.CounterIndex = 0;
            }
            else
            {
                Debug.LogWarning("Unsure how to set initial counter index from " + selectedCharacter.raceAndGender);
            }

            customizationButtonReference.Add(new ButtonInfo(genderRowElement.LeftArrowButton, "Arrow", "Arrow"));
            customizationButtonReference.Add(new ButtonInfo(genderRowElement.RightArrowButton, "Arrow", "Arrow"));
            genderRowElement.SetAsArrowGroupUsingButtons(new Color[] { Color.blue, Color.magenta });

            List<KeyValuePair<CharacterReference.MaterialApplicationLocation, Color>> materialColorList = new List<KeyValuePair<CharacterReference.MaterialApplicationLocation, Color>>();
            List<(CharacterCustomizationRow, List<Color>)> rowsToSetAsArrows = new List<(CharacterCustomizationRow, List<Color>)>();
            foreach (CharacterReference.CharacterMaterial characterMaterial in PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender))
            {
                if (characterMaterial.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Head) { continue; }

                Transform buttonParent = characterMaterialParents.Find(item => item.applicationLocation == characterMaterial.materialApplicationLocation).parent;
                CharacterCustomizationRow rowElement = null;
                if (!buttonParent)
                {
                    bool isOnLeftSide = true;

                    buttonParent = Instantiate(characterCustomizationRowPrefab.gameObject, isOnLeftSide ? customizationRowsParentLeft : customizationRowsParentRight).transform;

                    //isOnLeftSide = !isOnLeftSide;
                    int equipmentCount = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender).Count(item => item.materialApplicationLocation == characterMaterial.materialApplicationLocation);
                    if (isOnLeftSide) { leftYLocalPosition += spacing + leftQueuedSpacing; leftQueuedSpacing = equipmentCount / 11 * customizationRowSpacing; } else { rightYLocalPosition += spacing + rightQueuedSpacing; rightQueuedSpacing = equipmentCount / 11 * customizationRowSpacing; }
                    buttonParent.localPosition = new Vector3(buttonParent.localPosition.x * (isOnLeftSide ? 1 : -1), isOnLeftSide ? leftYLocalPosition : rightYLocalPosition, 0);

                    TextAnchor childAlignment = isOnLeftSide ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
                    rowElement = buttonParent.GetComponent<CharacterCustomizationRow>();
                    rowElement.GetLayoutGroup().childAlignment = childAlignment;
                    customizationRowList.Add(rowElement);
                    rowsToSetAsArrows.Add((rowElement, new List<Color>()));
                    Text headerText = buttonParent.GetComponentInChildren<Text>();
                    headerText.text = characterMaterial.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Body ? "SKIN COLOR" : characterMaterial.materialApplicationLocation.ToString().ToUpper();
                    if (!isOnLeftSide)
                    {
                        RectTransform rt = (RectTransform)headerText.transform;
                        rt.anchorMin = Vector2.one;
                        rt.anchorMax = Vector2.one;
                        rt.pivot = Vector2.one;
                        rt.anchoredPosition = Vector2.zero;
                        headerText.alignment = TextAnchor.MiddleLeft;
                    }
                    characterMaterialParents.Add(new MaterialCustomizationParent() { applicationLocation = characterMaterial.materialApplicationLocation, parent = buttonParent });
                }
                rowElement = buttonParent.GetComponent<CharacterCustomizationRow>();
                buttonParent = rowElement.GetLayoutGroup().transform;

                Color textureAverageColor = characterMaterial.averageTextureColor;

                int rowToSetIndex = rowsToSetAsArrows.FindIndex(item => item.Item1 == rowElement);
                rowsToSetAsArrows[rowToSetIndex].Item2.Add(textureAverageColor);

                var defaultChar = WebRequestManager.Singleton.GetDefaultCharacter(raceAndGender);
                switch (characterMaterial.materialApplicationLocation)
                {
                    case CharacterReference.MaterialApplicationLocation.Body:
                        if (defaultChar.bodyColor == characterMaterial.material.name)
                        {
                            rowElement.CounterIndex = rowsToSetAsArrows[rowToSetIndex].Item2.IndexOf(textureAverageColor);
                        }
                        break;
                    case CharacterReference.MaterialApplicationLocation.Eyes:
                        if (defaultChar.eyeColor == characterMaterial.material.name)
                        {
                            rowElement.CounterIndex = rowsToSetAsArrows[rowToSetIndex].Item2.IndexOf(textureAverageColor);
                        }
                        break;
                    case CharacterReference.MaterialApplicationLocation.Brows:
                        if (defaultChar.brows == characterMaterial.material.name)
                        {
                            rowElement.CounterIndex = rowsToSetAsArrows[rowToSetIndex].Item2.IndexOf(textureAverageColor);
                        }
                        break;
                    default:
                        Debug.LogWarning("Unsure how to handle material application location " + characterMaterial.materialApplicationLocation);
                        break;
                }

                CharacterCustomizationButton buttonElement = rowElement.GetUninitializedButton();
                buttonElement.InitializeAsColor(textureAverageColor);
                materialColorList.Add(new KeyValuePair<CharacterReference.MaterialApplicationLocation, Color>(characterMaterial.materialApplicationLocation, textureAverageColor));

                buttonElement.Button.onClick.AddListener(delegate { ChangeCharacterMaterial(characterMaterial); });
                customizationButtonReference.Add(new ButtonInfo(buttonElement.Button, characterMaterial.materialApplicationLocation.ToString(), characterMaterial.material.name));
            }

            foreach ((CharacterCustomizationRow row, List<Color> colorList) in rowsToSetAsArrows)
            {
                row.SetAsArrowGroupUsingButtons(colorList);
            }

            List<CharacterCustomizationRow> rowsToInvoke = new List<CharacterCustomizationRow>();
            foreach (CharacterReference.WearableEquipmentOption equipmentOption in PlayerDataManager.Singleton.GetCharacterReference().GetCharacterEquipmentOptions(raceAndGender))
            {
                if (!equipmentTypesIncludedInCharacterAppearance.Contains(equipmentOption.equipmentType)) { continue; }

                Transform buttonParent = characterEquipmentParents.Find(item => item.equipmentType == equipmentOption.equipmentType).parent;
                CharacterCustomizationRow rowElement = null;
                if (!buttonParent)
                {
                    bool isOnLeftSide = true;

                    buttonParent = Instantiate(characterCustomizationRowPrefab.gameObject, isOnLeftSide ? customizationRowsParentLeft : customizationRowsParentRight).transform;

                    //bool isOnLeftSide = equipmentTypesIncludedInCharacterAppearance.Contains(equipmentOption.equipmentType);
                    //isOnLeftSide = !isOnLeftSide;
                    int equipmentCount = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterEquipmentOptions(raceAndGender).Count(item => item.equipmentType == equipmentOption.equipmentType);
                    if (isOnLeftSide) { leftYLocalPosition += spacing + leftQueuedSpacing; leftQueuedSpacing = equipmentCount / 11 * customizationRowSpacing; } else { rightYLocalPosition += spacing + rightQueuedSpacing; rightQueuedSpacing = equipmentCount / 11 * customizationRowSpacing; }
                    buttonParent.localPosition = new Vector3(buttonParent.localPosition.x * (isOnLeftSide ? 1 : -1), isOnLeftSide ? leftYLocalPosition : rightYLocalPosition, 0);

                    TextAnchor childAlignment = isOnLeftSide ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
                    rowElement = buttonParent.GetComponent<CharacterCustomizationRow>();
                    customizationRowList.Add(rowElement);
                    rowElement.OnArrowPress += (option) => ChangeCharacterEquipment(option, raceAndGender);
                    rowElement.SetAsArrowGroup(PlayerDataManager.Singleton.GetCharacterReference().GetCharacterEquipmentOptions(raceAndGender).Where(item => item.equipmentType == equipmentOption.equipmentType));
                    //rowElement.GetLayoutGroup().childAlignment = childAlignment;
                    //rowElement.GetLayoutGroup().startCorner = isOnLeftSide ? GridLayoutGroup.Corner.UpperLeft : GridLayoutGroup.Corner.UpperRight;
                    Text headerText = buttonParent.GetComponentInChildren<Text>();
                    headerText.text = equipmentOption.equipmentType.ToString();
                    if (!isOnLeftSide)
                    {
                        RectTransform rt = (RectTransform)headerText.transform;
                        rt.anchorMin = Vector2.one;
                        rt.anchorMax = Vector2.one;
                        rt.pivot = Vector2.one;
                        rt.anchoredPosition = Vector2.zero;
                        headerText.alignment = TextAnchor.MiddleRight;
                    }
                    characterEquipmentParents.Add(new EquipmentCustomizationParent() { equipmentType = equipmentOption.equipmentType, parent = buttonParent });

                    buttonParent = rowElement.GetLayoutGroup().transform;
                    CharacterCustomizationButton removeButton = rowElement.GetUninitializedButton();
                    removeButton.InitializeAsResetButton();
                    removeButton.Button.onClick.RemoveAllListeners();
                    removeButton.Button.onClick.AddListener(delegate
                    {
                        rowElement.CounterIndex = -1;
                        rowElement.IncrementOption();
                    });

                    rowsToInvoke.Add(rowElement);

                    //removeButton.Button.onClick.AddListener(delegate { ChangeCharacterEquipment(new CharacterReference.WearableEquipmentOption(equipmentOption.equipmentType), raceAndGender); });
                    customizationButtonReference.Add(new ButtonInfo(removeButton.Button, equipmentOption.equipmentType.ToString(), "Remove"));

                    customizationButtonReference.Add(new ButtonInfo(rowElement.LeftArrowButton, "Arrow", "Arrow"));
                    customizationButtonReference.Add(new ButtonInfo(rowElement.RightArrowButton, "Arrow", "Arrow"));
                }
                else
                {
                    rowElement = buttonParent.GetComponentInParent<CharacterCustomizationRow>();
                    buttonParent = rowElement.GetLayoutGroup().transform;
                }
            }

            StartCoroutine(InvokeRows(rowsToInvoke));
        }

        private IEnumerator InvokeRows(List<CharacterCustomizationRow> rowsToInvoke)
        {
            yield return null;
            foreach (CharacterCustomizationRow rowToInvoke in rowsToInvoke)
            {
                rowToInvoke.IncrementOption();
            }
            shouldUseHeadCameraOrientation = false;
        }

        private const int customizationRowSpacing = -2000;

        private void RefreshButtonInteractability(bool disableAll = false)
        {
            bool tutorialInProgress = FasterPlayerPrefs.Singleton.GetBool("TutorialInProgress");

            selectCharacterButton.interactable = !string.IsNullOrEmpty(selectedCharacter._id.ToString()) & (WebRequestManager.Singleton.GameIsUpToDate | tutorialInProgress);
            goToTrainingRoomButton.interactable = !string.IsNullOrEmpty(selectedCharacter._id.ToString());

            if (tutorialInProgress)
            {
                if (selectCharacterButton.interactable)
                {
                    CreateUIElementHighlight((RectTransform)selectCharacterButton.transform);
                }
            }

            foreach (ButtonInfo buttonInfo in characterCardButtonReference)
            {
                if (disableAll) { buttonInfo.button.interactable = false; continue; }

                switch (buttonInfo.key)
                {
                    case "CharacterCard":
                        buttonInfo.button.GetComponent<CharacterCard>().SetSelectedState(selectedCharacter._id != buttonInfo.value);
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
                    if (buttonInfo.button.TryGetComponent(out CharacterCustomizationButton characterCustomizationButton))
                    {
                        characterCustomizationButton.SetSelectedState(true);
                    }
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
                        buttonInfo.button.GetComponent<CharacterCustomizationButton>().SetSelectedState(selectedCharacter.brows == "" | selectedCharacter.brows == "EmptyWearableEquipment" ? buttonInfo.value != "Remove" : buttonInfo.value != selectedCharacter.brows);
                        break;

                    case "Hair":
                        buttonInfo.button.GetComponent<CharacterCustomizationButton>().SetSelectedState(selectedCharacter.hair == "" | selectedCharacter.hair == "EmptyWearableEquipment" ? buttonInfo.value != "Remove" : buttonInfo.value != selectedCharacter.hair);
                        break;

                    case "Beard":
                        buttonInfo.button.GetComponent<CharacterCustomizationButton>().SetSelectedState(selectedCharacter.beard == "" | selectedCharacter.beard == "EmptyWearableEquipment" ? buttonInfo.value != "Remove" : buttonInfo.value != selectedCharacter.beard);
                        break;

                    case "Race":
                        buttonInfo.button.GetComponent<CharacterCustomizationButton>().SetSelectedState(selectedRace != buttonInfo.value);
                        break;

                    case "Gender":
                        buttonInfo.button.GetComponent<CharacterCustomizationButton>().SetSelectedState(selectedGender != buttonInfo.value);
                        break;

                    case "Arrow":
                        break;

                    default:
                        Debug.LogError("Not sure how to handle button key " + buttonInfo.key);
                        break;
                }
            }
        }

        [SerializeField] private RawImage characterPreviewImage;
        [SerializeField] private RawImage characterCustomizationPreviewImage;
        private Queue<WebRequestManager.Character> characterQueue = new Queue<WebRequestManager.Character>();

        private void ProcessCharacterQueue()
        {
            if (characterQueue.Count > 0)
            {
                if (updateCharCoroutine != null)
                {
                    StopCoroutine(updateCharCoroutine);
                }

                updateCharCoroutine = StartCoroutine(UpdateDisplayCharacter(characterQueue.Dequeue()));
            }
            else if (!updateDisplayCharacterRunning)
            {
                characterPreviewImage.color = StringUtility.SetColorAlpha(characterPreviewImage.color, Mathf.MoveTowards(characterPreviewImage.color.a, 1, Time.deltaTime * characterPreviewFadeSpeed));
            }
        }

        private const float characterPreviewFadeSpeed = 3;

        private Coroutine updateCharCoroutine;
        private bool updateDisplayCharacterRunning;
        private IEnumerator UpdateDisplayCharacter(WebRequestManager.Character character)
        {
            updateDisplayCharacterRunning = true;

            shouldUseHeadCameraOrientation = false;
            goToTrainingRoomButton.interactable = true;
            characterNameInputField.text = character.name.ToString();

            var playerModelOption = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterModel(character.raceAndGender);
            if (string.IsNullOrWhiteSpace(character.model.ToString()))
            {
                if (previewObject)
                {
                    if (previewObject.TryGetComponent(out PooledObject pooledObject))
                    {
                        ObjectPoolingManager.ReturnObjectToPool(pooledObject);
                        previewObject = null;
                    }
                    else
                    {
                        Destroy(previewObject);
                    }
                }
                selectedCharacter = default;
                RefreshButtonInteractability();
                updateDisplayCharacterRunning = false;
                yield break;
            }

            bool idsAreEqual = selectedCharacter._id == character._id;
            bool raceAndGendersAreEqual = selectedCharacter.raceAndGender == character.raceAndGender;
            bool shouldCreateNewModel = !raceAndGendersAreEqual | selectedCharacter.model != character.model;

            string[] raceAndGenderStrings = Regex.Matches(playerModelOption.raceAndGender.ToString(), @"([A-Z][a-z]+)").Cast<Match>().Select(m => m.Value).ToArray();
            selectedRace = raceAndGenderStrings[0];
            selectedGender = raceAndGenderStrings[1];
            CharacterReference.RaceAndGender raceAndGender = System.Enum.Parse<CharacterReference.RaceAndGender>(selectedRace + selectedGender);

            selectedCharacter = character;
            selectedCharacter.raceAndGender = raceAndGender;

            RefreshButtonInteractability();

            if (!string.IsNullOrWhiteSpace(character._id.ToString()))
            {
                if (shouldCreateNewModel | !idsAreEqual)
                {
                    while (true)
                    {
                        characterPreviewImage.color = StringUtility.SetColorAlpha(characterPreviewImage.color, Mathf.MoveTowards(characterPreviewImage.color.a, 0, Time.deltaTime * characterPreviewFadeSpeed));
                        if (Mathf.Approximately(characterPreviewImage.color.a, 0)) { break; }
                        yield return null;
                    }
                }

                if (!idsAreEqual)
                {
                    if (previewObject)
                    {
                        previewObject.transform.rotation = Quaternion.Euler(previewCharacterRotation);
                    }
                }
            }
            
            if (shouldCreateNewModel)
            {
                ClearMaterialsAndEquipmentOptions();
                if (previewObject)
                {
                    if (previewObject.TryGetComponent(out PooledObject pooledObject))
                    {
                        ObjectPoolingManager.ReturnObjectToPool(pooledObject);
                        previewObject = null;
                    }
                    else
                    {
                        Destroy(previewObject);
                    }
                }
                // Instantiate the player model
                previewObject = ObjectPoolingManager.SpawnObject(PlayerDataManager.Singleton.GetCharacterReference().PlayerPrefab.GetComponent<PooledObject>(), previewCharacterPosition, Quaternion.Euler(previewCharacterRotation)).gameObject;
                SceneManager.MoveGameObjectToScene(previewObject, gameObject.scene);
            }

            previewObject.GetComponent<AnimationHandler>().ChangeCharacter(character);

            if (WebRequestManager.HasCharacterInventory(character._id.ToString()))
            {
                primaryWeaponDisplayElement.gameObject.SetActive(true);
                secondaryWeaponDisplayElement.gameObject.SetActive(true);
                for (int i = 0; i < equipmentTypeKeys.Length; i++)
                {
                    equipmentImageValues[i].gameObject.SetActive(true);
                }

                loadoutManager = previewObject.GetComponent<LoadoutManager>();
                loadoutManager.ApplyLoadout(raceAndGender, character.GetActiveLoadout(), character._id.ToString());

                primaryWeaponDisplayElement.Initialize(loadoutManager.PrimaryWeaponOption);
                secondaryWeaponDisplayElement.Initialize(loadoutManager.SecondaryWeaponOption);
            }
            else
            {
                primaryWeaponDisplayElement.gameObject.SetActive(false);
                secondaryWeaponDisplayElement.gameObject.SetActive(false);
                for (int i = 0; i < equipmentTypeKeys.Length; i++)
                {
                    equipmentImageValues[i].gameObject.SetActive(false);
                }

                if (previewObject & shouldCreateNewModel) { previewObject.GetComponent<LoadoutManager>().ApplyLoadout(raceAndGender, WebRequestManager.GetDefaultDisplayLoadout(raceAndGender), character._id.ToString()); }
            }

            if (shouldCreateNewModel & characterCustomizationParent.activeSelf) { RefreshMaterialsAndEquipmentOptions(raceAndGender); }

            if (!string.IsNullOrWhiteSpace(character._id.ToString())
                | !raceAndGendersAreEqual)
            {
                playUltimateAnimation = true;
                weaponClassIndex = System.Array.IndexOf(weaponClasses, Weapon.WeaponClass.Greatsword);
                CharacterReference.WeaponOption weaponOption = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions().First(item => item.weapon.GetWeaponClass() == Weapon.WeaponClass.Greatsword);
                weaponClassPreviewImage.sprite = weaponOption.weaponIcon;
            }

            finishCharacterCustomizationButton.onClick.RemoveAllListeners();
            finishCharacterCustomizationButton.onClick.AddListener(delegate { StartCoroutine(ApplyCharacterChanges(selectedCharacter)); });

            RefreshButtonInteractability();

            if (playUltimateAnimation)
            {
                yield return PlayUltimateAnimation(previewObject.GetComponent<CombatAgent>());
                playUltimateAnimation = false;
            }

            updateDisplayCharacterRunning = false;
        }

        private WebRequestManager.Character selectedCharacter;
        private GameObject previewObject;
        private LoadoutManager loadoutManager;

        private void UpdateSelectedCharacter(WebRequestManager.Character character)
        {
            characterQueue.Enqueue(character);
        }

        [SerializeField] private Image weaponClassPreviewImage;
        private Weapon.WeaponClass[] weaponClasses;
        private int weaponClassIndex;
        public void IncrementWeaponClass()
        {
            weaponClassIndex++;

            if (weaponClassIndex >= weaponClasses.Length) { weaponClassIndex = 0; }

            ChangeDisplayCharacterWeaponClass(selectedCharacter.raceAndGender, weaponClasses[weaponClassIndex]);
        }

        public void DecrementWeaponClass()
        {
            weaponClassIndex--;

            if (weaponClassIndex < 0) { weaponClassIndex = weaponClasses.Length - 1; }

            ChangeDisplayCharacterWeaponClass(selectedCharacter.raceAndGender, weaponClasses[weaponClassIndex]);
        }

        private void ChangeDisplayCharacterWeaponClass(CharacterReference.RaceAndGender raceAndGender, Weapon.WeaponClass weaponClass)
        {
            if (!previewObject) { return; }

            CharacterReference.WeaponOption weaponOption = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions().First(item => item.weapon.GetWeaponClass() == weaponClass);

            weaponClassPreviewImage.sprite = weaponOption.weaponIcon;

            WebRequestManager.Loadout loadout = WebRequestManager.GetDefaultDisplayLoadout(raceAndGender);
            loadout.weapon1ItemId = weaponOption.itemWebId;
            previewObject.GetComponent<LoadoutManager>().ApplyLoadout(raceAndGender, loadout, selectedCharacter._id.ToString());

            StartCoroutine(PlayUltimateAnimation(previewObject.GetComponent<CombatAgent>()));
        }

        private const float comboCameraOrientationSpeed = 1;
        private float comboCameraOrientationTime;
        private float comboCameraOrientationMaxTime;
        ComboCameraOrientation comboCameraOrientation;
        private bool ultimateAnimationRunning;
        private IEnumerator PlayUltimateAnimation(CombatAgent combatAgent)
        {
            ultimateAnimationRunning = true;

            if (characterCustomizationParent.activeSelf)
            {
                characterCreationOpacityGroup.interactable = false;

                int index = previewUltimateCameraOrientation.FindIndex(item => item.weaponClass == combatAgent.LoadoutManager.PrimaryWeaponOption.weapon.GetWeaponClass());
                if (index != -1)
                {
                    comboCameraOrientation = previewUltimateCameraOrientation[index];
                }
                else
                {
                    Debug.LogWarning("No combo camera orientation for weapon class " + combatAgent.LoadoutManager.PrimaryWeaponOption.weapon.GetWeaponClass());
                    comboCameraOrientation = previewUltimateCameraOrientation[0];
                }

                float t = Mathf.InverseLerp(1, 0, characterCreationOpacityGroup.alpha);
                while (!Mathf.Approximately(t, 1))
                {
                    t += Time.deltaTime * cameraLerpSpeed;
                    t = Mathf.Clamp01(t);

                    characterCreationOpacityGroup.alpha = Mathf.Lerp(1, 0, t);

                    characterPreviewCamera.transform.position = Vector3.Slerp(shouldUseHeadCameraOrientation ? headCameraOrientation.position : defaultCameraOrientation.position, comboCameraOrientation.position.EvaluateNormalized(0), t);
                    characterPreviewCamera.transform.rotation = Quaternion.Slerp(shouldUseHeadCameraOrientation ? headCameraOrientation.rotation : defaultCameraOrientation.rotation, Quaternion.Euler(comboCameraOrientation.rotation.EvaluateNormalized(0)), t);
                    yield return null;
                }

                if (characterCustomizationPreviewImage.color.a < 1)
                {
                    characterPreviewCamera.transform.position = comboCameraOrientation.position.EvaluateNormalized(0);
                    characterPreviewCamera.transform.rotation = Quaternion.Euler(comboCameraOrientation.rotation.EvaluateNormalized(0));

                    t = 0;
                    while (!Mathf.Approximately(t, 1))
                    {
                        t += Time.deltaTime * cameraLerpSpeed;
                        t = Mathf.Clamp01(t);
                        characterCustomizationPreviewImage.color = StringUtility.SetColorAlpha(characterCustomizationPreviewImage.color, Mathf.Lerp(0, 1, t));
                        yield return null;
                    }
                }
                
                ((RectTransform)customizationRowsParentLeft).anchoredPosition = originalLeftPos + new Vector2(customizationRowsSlidingAnimationOffset, 0);
                ((RectTransform)customizationRowsParentRight).anchoredPosition = originalRightPos + new Vector2(customizationRowsSlidingAnimationOffset, 0);
            }
            
            yield return new WaitUntil(() => combatAgent.AnimationHandler.Animator);

            if (characterSelectParent.activeSelf)
            {
                combatAgent.AnimationHandler.Animator.CrossFadeInFixedTime("MVP", 0.25f, combatAgent.AnimationHandler.Animator.GetLayerIndex("Actions"));
                ultimateAnimationRunning = false;
                yield break;
            }

            comboCameraOrientationTime = 0;
            comboCameraOrientationMaxTime = 0;
            int i = 0;
            foreach (Weapon.PreviewActionClip previewClip in combatAgent.WeaponHandler.GetWeapon().PreviewCombo)
            {
                i++;
                float length = combatAgent.AnimationHandler.GetTotalActionClipLengthInSeconds(previewClip.actionClip);
                if (i < combatAgent.WeaponHandler.GetWeapon().PreviewCombo.Count) { length *= previewClip.normalizedTimeToPlayNext; }
                comboCameraOrientationMaxTime += length;
            }

            combatAgent.AnimationHandler.PlayPreviewCombo();
            ultimateAnimationRunning = false;
        }

        private string selectedRace = "Human";
        private string selectedGender = "Female";

        public void ChangeCharacterModel(string stringChange, bool isRace)
        {
            if (isRace)
                selectedRace = stringChange;
            else
                selectedGender = stringChange;

            var raceAndGender = System.Enum.Parse<CharacterReference.RaceAndGender>(selectedRace + selectedGender);
            CharacterReference.PlayerModelOption option = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterModel(raceAndGender);
            if (option == null) { Debug.LogError("Can't find player model option for " + selectedRace + " " + selectedGender); return; }
            WebRequestManager.Character character = selectedCharacter;
            character.model = option.model.name;
            character.raceAndGender = raceAndGender;
            character.bodyColor = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender).First(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Body).material.name;
            character.eyeColor = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender).First(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Eyes).material.name;
            if (raceAndGender == CharacterReference.RaceAndGender.HumanFemale)
            {
                character.brows = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender).First(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Brows).material.name;
            }
            UpdateSelectedCharacter(character);
        }

        private bool shouldUseHeadCameraOrientation;
        public void ChangeCharacterMaterial(CharacterReference.CharacterMaterial characterMaterial)
        {
            previewObject.GetComponent<AnimationHandler>().ApplyCharacterMaterial(characterMaterial);
            UpdateSelectedCharacter(previewObject.GetComponentInChildren<AnimatorReference>().GetCharacterWebInfo(selectedCharacter));
            shouldUseHeadCameraOrientation = characterMaterial.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Eyes
                | characterMaterial.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Brows;
        }

        public void ChangeCharacterEquipment(CharacterReference.WearableEquipmentOption wearableEquipmentOption, CharacterReference.RaceAndGender raceAndGender)
        {
            previewObject.GetComponent<AnimationHandler>().ApplyWearableEquipment(wearableEquipmentOption.equipmentType, wearableEquipmentOption, raceAndGender);
            UpdateSelectedCharacter(previewObject.GetComponentInChildren<AnimatorReference>().GetCharacterWebInfo(selectedCharacter));
            shouldUseHeadCameraOrientation = wearableEquipmentOption.equipmentType == CharacterReference.EquipmentType.Hair
                | wearableEquipmentOption.equipmentType == CharacterReference.EquipmentType.Beard
                | wearableEquipmentOption.equipmentType == CharacterReference.EquipmentType.Brows;
        }

        private Unity.Netcode.Transports.UTP.UnityTransport networkTransport;

        private void Start()
        {
            networkTransport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();

            WebRequestManager.Singleton.RefreshServers();

            primaryWeaponDisplayElement.gameObject.SetActive(false);
            secondaryWeaponDisplayElement.gameObject.SetActive(false);
            for (int i = 0; i < equipmentTypeKeys.Length; i++)
            {
                equipmentImageValues[i].gameObject.SetActive(false);
            }
            PlayerDataManager.Singleton.SetGameModeSettings("");

            tutorialButton.gameObject.SetActive(FasterPlayerPrefs.Singleton.GetBool("TutorialCompleted"));
            tutorialButton.onClick.AddListener(() => GoToTutorial());

            trainingRoomSettingsButton.gameObject.SetActive(FasterPlayerPrefs.Singleton.GetBool("TutorialCompleted"));
            goToTrainingRoomButton.gameObject.SetActive(FasterPlayerPrefs.Singleton.GetBool("TutorialCompleted"));

            HandlePlatformAPI(false);
        }

        [SerializeField] private CanvasGroup characterCreationOpacityGroup;

        private List<ServerListElement> serverListElementList = new List<ServerListElement>();
        private float lastTextChangeTime;
        private bool lastClientState;

        private const float customizationRowsSlidingAnimationOffset = 300;
        private const float cameraLerpSpeed = 2;
        private float customizationAnimationTime;
        private void Update()
        {
            ProcessCharacterQueue();

            bool evaluateCameraTransform = true;
            if (previewObject)
            {
                if (previewObject.TryGetComponent(out AnimationHandler animationHandler))
                {
                    evaluateCameraTransform = !animationHandler.IsPlayingPreviewClip;
                }
            }

            if (evaluateCameraTransform)
            {
                if (!ultimateAnimationRunning)
                {
                    customizationAnimationTime += Time.deltaTime * cameraLerpSpeed;
                    customizationAnimationTime = Mathf.Clamp01(customizationAnimationTime);

                    characterCreationOpacityGroup.alpha = Mathf.Lerp(0, 1, customizationAnimationTime);
                    characterCreationOpacityGroup.interactable = true;

                    characterPreviewCamera.transform.position = Vector3.Slerp(characterPreviewCamera.transform.position, shouldUseHeadCameraOrientation ? headCameraOrientation.position : defaultCameraOrientation.position, Time.deltaTime * cameraLerpSpeed);
                    characterPreviewCamera.transform.rotation = Quaternion.Slerp(characterPreviewCamera.transform.rotation, shouldUseHeadCameraOrientation ? headCameraOrientation.rotation : defaultCameraOrientation.rotation, Time.deltaTime * cameraLerpSpeed);

                    ((RectTransform)customizationRowsParentLeft).anchoredPosition = Vector2.Lerp(originalLeftPos - new Vector2(customizationRowsSlidingAnimationOffset, 0), originalLeftPos, customizationAnimationTime);
                    ((RectTransform)customizationRowsParentRight).anchoredPosition = Vector2.Lerp(originalRightPos + new Vector2(customizationRowsSlidingAnimationOffset, 0), originalRightPos, customizationAnimationTime);
                }
                else
                {
                    customizationAnimationTime = 0;
                }
            }
            else
            {
                comboCameraOrientationTime += Time.deltaTime * comboCameraOrientationSpeed;
                characterPreviewCamera.transform.position = comboCameraOrientation.position.EvaluateNormalized(comboCameraOrientationTime / comboCameraOrientationMaxTime);
                characterPreviewCamera.transform.rotation = Quaternion.Euler(comboCameraOrientation.rotation.EvaluateNormalized(comboCameraOrientationTime / comboCameraOrientationMaxTime));
                customizationAnimationTime = 0;
            }

            statsAndGearParent.SetActive(!string.IsNullOrEmpty(selectedCharacter._id.ToString()));
            statsParent.SetActive(statsSelected);
            gearParent.SetActive(!statsSelected);

            UpdateSelectionBarPositions();

            gameVersionText.text = WebRequestManager.Singleton.GameVersionErrorMessage;

            if (lastClientState & !NetworkManager.Singleton.IsClient) { StartCoroutine(OpenCharacterSelect(false)); }
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

#if UNITY_EDITOR
                foreach (WebRequestManager.Server server in WebRequestManager.Singleton.LobbyServers)
                {
                    if (!serverListElementList.Find(item => item.Server._id == server._id))
                    {
                        ServerListElement serverListElementInstance = Instantiate(serverListElement.gameObject, serverListElementParent).GetComponent<ServerListElement>();
                        serverListElementInstance.Initialize(this, server);
                        serverListElementList.Add(serverListElementInstance);
                    }
                }
# endif
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
                    equipmentImageValues[i].Initialize(loadoutManager.GetEquippedEquipmentOption(equipmentTypeKeys[i]), raceAndGender);
                }
            }
            else
            {
                for (int i = 0; i < equipmentTypeKeys.Length; i++)
                {
                    equipmentImageValues[i].Initialize(null, default);
                }
            }
        }

        [SerializeField] private Text characterNameInputErrorText;
        public void OnUsernameChange()
        {
            if (characterNameInputField.text.Length < 6)
            {
                characterNameInputErrorText.text = "Name must be longer than 5 characters";
                finishCharacterCustomizationButton.interactable = false;
            }
            else if (characterNameInputField.text.Length > 10)
            {
                characterNameInputErrorText.text = "Name must be shorter than 11 characters";
                finishCharacterCustomizationButton.interactable = false;
            }
            else
            {
                characterNameInputErrorText.text = "";
                finishCharacterCustomizationButton.interactable = true;
            }
            selectedCharacter.name = characterNameInputField.text;
        }

        public void ReturnToMainMenu()
        {
            if (NetworkManager.Singleton.IsListening) { return; }
            returnButton.interactable = false;
            FasterPlayerPrefs.Singleton.SetBool("TutorialInProgress", false);
            NetSceneManager.Singleton.LoadScene("Main Menu");
        }

        private bool isEditingExistingCharacter;
        private bool playUltimateAnimation;

        [SerializeField] private TransitionController transitionController;

        private IEnumerator OpenCharacterCustomization()
        {
            if (transitionController.TransitionRunning) { yield break; }
            StartCoroutine(transitionController.PlayTransition());
            yield return new WaitUntil(() => transitionController.TransitionPeakReached);

            selectedRace = "Human";
            selectedGender = "Female";

            returnButton.gameObject.SetActive(true);
            characterSelectParent.SetActive(false);
            characterCustomizationParent.SetActive(true);

            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(() => StartCoroutine(OpenCharacterSelect()));

            selectedCharacter = new WebRequestManager.Character();
            UpdateSelectedCharacter(WebRequestManager.Singleton.GetDefaultCharacter(System.Enum.Parse<CharacterReference.RaceAndGender>(selectedRace + selectedGender)));
            finishCharacterCustomizationButton.GetComponentInChildren<Text>().text = "CREATE";
            isEditingExistingCharacter = false;

            if (FasterPlayerPrefs.Singleton.GetBool("TutorialInProgress"))
            {
                CreateUIElementHighlight((RectTransform)characterNameInputField.transform);
            }

            weaponClassIndex = System.Array.IndexOf(weaponClasses, Weapon.WeaponClass.Greatsword);
            CharacterReference.WeaponOption weaponOption = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions().First(item => item.weapon.GetWeaponClass() == Weapon.WeaponClass.Greatsword);
            weaponClassPreviewImage.sprite = weaponOption.weaponIcon;

            characterCreationOpacityGroup.alpha = 0;
            characterCustomizationPreviewImage.color = StringUtility.SetColorAlpha(characterCustomizationPreviewImage.color, 0);

            playUltimateAnimation = true;
        }

        private void OpenCharacterCustomization(WebRequestManager.Character character)
        {
            returnButton.gameObject.SetActive(true);
            characterSelectParent.SetActive(false);
            characterCustomizationParent.SetActive(true);

            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(() => StartCoroutine(OpenCharacterSelect()));

            selectedCharacter = new WebRequestManager.Character();
            UpdateSelectedCharacter(character);
            finishCharacterCustomizationButton.GetComponentInChildren<Text>().text = "APPLY";
            isEditingExistingCharacter = true;
        }

        private IEnumerator OpenCharacterSelect(bool playTransition = true)
        {
            if (playTransition)
            {
                if (transitionController.TransitionRunning) { yield break; }
                StartCoroutine(transitionController.PlayTransition());
                yield return new WaitUntil(() => transitionController.TransitionPeakReached);
            }
            
            shouldUseHeadCameraOrientation = false;
            characterPreviewCamera.transform.position = defaultCameraOrientation.position;
            characterPreviewCamera.transform.rotation = defaultCameraOrientation.rotation;

            if (NetworkManager.Singleton.IsListening) { NetworkManager.Singleton.Shutdown(FasterPlayerPrefs.shouldDiscardMessageQueueOnNetworkShutdown); }

            StartCoroutine(RefreshCharacterCards());

            selectedRace = "Human";
            selectedGender = "Female";

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

            characterCardsAreDirty = true;

            webRequestStatusText.gameObject.SetActive(false);

            RefreshButtonInteractability();
            finishCharacterCustomizationButton.interactable = true;
            returnButton.interactable = true;
            characterNameInputField.interactable = true;

            if (string.IsNullOrWhiteSpace(WebRequestManager.Singleton.CharacterCreationError))
            {
                StartCoroutine(OpenCharacterSelect(false));
            }
            else
            {
                characterNameInputErrorText.text = WebRequestManager.Singleton.CharacterCreationError;
            }
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

            characterCardsAreDirty = true;

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

            StartCoroutine(OpenCharacterSelect(false));
        }

        private List<string> trainingRoomMapOptions = new List<string>()
        {
            "Tutorial Map",
            "Duo's Sanctum",
            "Octagon Skirmish",
            "Twilight Chasm",
            "Eclipse Grove"
        };

        public void GoToTrainingRoom()
        {
            goToTrainingRoomButton.interactable = false;
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(selectedCharacter._id.ToString());
            if (NetworkManager.Singleton.StartHost())
            {
                if (FasterPlayerPrefs.Singleton.GetBool("TutorialCompleted"))
                {
                    NetSceneManager.Singleton.LoadScene("Training Room", trainingRoomMapOptions[trainingRoomMapDropdown.value]);
                }
                else
                {
                    NetSceneManager.Singleton.LoadScene("Tutorial Room", "Tutorial Map");
                }
            }
            else
            {
                Debug.LogError("Error trying to start host to go to training room");
                goToTrainingRoomButton.interactable = true;
            }
        }

        private void GoToTutorial()
        {
            goToTrainingRoomButton.interactable = false;
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(selectedCharacter._id.ToString());
            if (NetworkManager.Singleton.StartHost())
            {
                NetSceneManager.Singleton.LoadScene("Tutorial Room", "Tutorial Map");
            }
            else
            {
                goToTrainingRoomButton.interactable = true;
                Debug.LogError("Error trying to start host to go to tutorial");
            }
        }

        private GameObject UIElementHighlightInstance;

        private void CreateUIElementHighlight(RectTransform parentRT)
        {
            if (UIElementHighlightInstance) { Destroy(UIElementHighlightInstance); }
            UIElementHighlightInstance = Instantiate(UIElementHighlightPrefab.gameObject, parentRT, true);
        }

        public void RandomizeCharacter()
        {
            foreach (CharacterCustomizationRow row in customizationRowList)
            {
                row.SelectRandom();
            }
            shouldUseHeadCameraOrientation = true;
        }

        public void StartTutorial()
        {
            FasterPlayerPrefs.Singleton.SetBool("TutorialInProgress", true);

            if (string.IsNullOrEmpty(selectedCharacter._id.ToString()))
            {
                CreateUIElementHighlight((RectTransform)characterCardInstances[0].transform);
            }
            else if (selectCharacterButton.interactable)
            {
                CreateUIElementHighlight((RectTransform)selectCharacterButton.transform);
            }

            selectCharacterButton.onClick.RemoveAllListeners();
            selectCharacterButton.onClick.AddListener(() => GoToTrainingRoom());
        }

        public void SkipTutorial()
        {
            FasterPlayerPrefs.Singleton.SetBool("TutorialCompleted", true);
            trainingRoomSettingsButton.gameObject.SetActive(true);
            goToTrainingRoomButton.gameObject.SetActive(true);
        }

        private const string connectToServerCode = "217031";

        private void AutoConnectToHub()
        {
            if (autoConnectToHubServerCoroutine != null) { StopCoroutine(autoConnectToHubServerCoroutine); }
            autoConnectToHubServerCoroutine = StartCoroutine(AutoConnectToHubServer());
        }

        private Coroutine autoConnectToHubServerCoroutine;

        private IEnumerator AutoConnectToHubServer()
        {
            selectCharacterButton.interactable = false;
            goToTrainingRoomButton.interactable = false;
            trainingRoomSettingsButton.interactable = false;

            WebRequestManager.Singleton.RefreshServers();
            WebRequestManager.Singleton.CheckGameVersion(false);

            yield return new WaitUntil(() => !WebRequestManager.Singleton.IsRefreshingServers & !WebRequestManager.Singleton.IsCheckingGameVersion);

            if (!WebRequestManager.Singleton.GameIsUpToDate) { yield break; }

            if (WebRequestManager.Singleton.HubServers.Length > 0)
            {
                networkTransport.SetConnectionData(WebRequestManager.Singleton.HubServers[0].ip, ushort.Parse(WebRequestManager.Singleton.HubServers[0].port), FasterPlayerPrefs.serverListenAddress);
                StartClient();
            }
            else
            {
                Instantiate(alertBoxPrefab.gameObject).GetComponent<AlertBox>().SetText("Servers are currently offline for maintenance.");
            }

            selectCharacterButton.interactable = true;
            goToTrainingRoomButton.interactable = true;
            trainingRoomSettingsButton.interactable = true;
        }

        public void OpenViDiscord()
        {
            Application.OpenURL(FasterPlayerPrefs.persistentDiscordInviteLink);
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

        public void HandlePlatformAPI(bool newCharacterCreation, string characterName = "XYZ")
        {
            //Rich presence
            // if (PlatformRichPresence.instance != null)
            // {
            //     if (!newCharacterCreation)
            //         //Change logic here that would handle scenario where the player is host.
            //         PlatformRichPresence.instance.UpdatePlatformStatus("At Character Select", "Selecting a Character");
            //     else PlatformRichPresence.instance.UpdatePlatformStatus("At Character Select", "Creating a New Character");
            // }
        }
    }
}