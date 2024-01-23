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

namespace Vi.UI
{
    public class CharacterSelectUI : MonoBehaviour
    {
        [SerializeField] private Button returnButton;
        [SerializeField] private Text webRequestStatusText;

        [Header("Character Select")]
        [SerializeField] private GameObject characterSelectParent;
        [SerializeField] private CharacterCard characterCardPrefab;
        [SerializeField] private Transform characterCardParent;
        [SerializeField] private Button selectCharacterButton;
        [SerializeField] private Button goToTrainingRoomButton;
        [SerializeField] private Button addCharacterButton;

        [Header("Character Customization")]
        [SerializeField] private GameObject characterCustomizationParent;
        [SerializeField] private GameObject characterCustomizationRowPrefab;
        [SerializeField] private GameObject characterCustomizationButtonPrefab;
        [SerializeField] private GameObject removeEquipmentButtonPrefab;
        [SerializeField] private InputField characterNameInputField;
        [SerializeField] private Button finishCharacterCustomizationButton;
        [SerializeField] private Button deleteCharacterButton;
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
        [SerializeField] private ServerListElement serverListElement;
        [SerializeField] private Transform serverListElementParent;
        [SerializeField] private GameObject serverListParent;
        [SerializeField] private Button connectButton;
        [SerializeField] private Button closeServersMenuButton;
        [SerializeField] private Button refreshServersButton;

        private List<CharacterReference.EquipmentType> equipmentTypesIncludedInCharacterAppearance = new List<CharacterReference.EquipmentType>()
        {
            CharacterReference.EquipmentType.Hair,
            CharacterReference.EquipmentType.Beard,
            CharacterReference.EquipmentType.Brows,
        };

        private void Awake()
        {
            OpenCharacterSelect();
            finishCharacterCustomizationButton.interactable = characterNameInputField.text.Length > 0;
            selectCharacterButton.interactable = !string.IsNullOrEmpty(selectedCharacter._id.ToString()) & !WebRequestManager.Singleton.PlayingOffine;
            goToTrainingRoomButton.interactable = !string.IsNullOrEmpty(selectedCharacter._id.ToString());
        }

        private List<ButtonInfo> characterCardButtonReference = new List<ButtonInfo>();

        private IEnumerator RefreshCharacterCards()
        {
            foreach (Transform child in characterCardParent)
            {
                Destroy(child.gameObject);
            }
            characterCardButtonReference.Clear();

            webRequestStatusText.gameObject.SetActive(true);
            webRequestStatusText.text = "LOADING CHARACTERS";
            addCharacterButton.interactable = false;
            
            WebRequestManager.Singleton.RefreshCharacters();
            yield return new WaitUntil(() => !WebRequestManager.Singleton.IsRefreshingCharacters);

            addCharacterButton.interactable = WebRequestManager.Singleton.PlayingOffine ? WebRequestManager.Singleton.Characters.Count < 1 : WebRequestManager.Singleton.Characters.Count < 5;
            webRequestStatusText.gameObject.SetActive(false);

            // Create character cards
            foreach (WebRequestManager.Character character in WebRequestManager.Singleton.Characters)
            {
                CharacterCard characterCard = Instantiate(characterCardPrefab.gameObject, characterCardParent).GetComponent<CharacterCard>();
                characterCard.Initialize(character);
                characterCard.GetComponent<Button>().onClick.AddListener(delegate { UpdateSelectedCharacter(character); });
                characterCard.editButton.onClick.AddListener(delegate { UpdateSelectedCharacter(character); });
                characterCard.editButton.onClick.AddListener(delegate { OpenCharacterCustomization(character); });
                characterCardButtonReference.Add(new ButtonInfo(characterCard.GetComponent<Button>(), "CharacterCard", character._id.ToString()));
            }
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
                    buttonParent = Instantiate(characterCustomizationRowPrefab, characterCustomizationParent.transform).transform;

                    bool isOnLeftSide = true;
                    isOnLeftSide = !isOnLeftSide;
                    int equipmentCount = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender).FindAll(item => item.materialApplicationLocation == characterMaterial.materialApplicationLocation).Count;
                    if (isOnLeftSide) { leftYLocalPosition += spacing + leftQueuedSpacing; leftQueuedSpacing = equipmentCount / 11 * -50; } else { rightYLocalPosition += spacing + rightQueuedSpacing; rightQueuedSpacing = equipmentCount / 11 * -50; }
                    buttonParent.localPosition = new Vector3(buttonParent.localPosition.x * (isOnLeftSide ? 1 : -1), isOnLeftSide ? leftYLocalPosition : rightYLocalPosition, 0);

                    TextAnchor childAlignment = isOnLeftSide ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
                    buttonParent.GetComponentInChildren<GridLayoutGroup>().childAlignment = childAlignment;
                    GridLayoutGroup.Corner startCorner = isOnLeftSide ? GridLayoutGroup.Corner.UpperRight : GridLayoutGroup.Corner.UpperLeft;
                    buttonParent.GetComponentInChildren<GridLayoutGroup>().startCorner = startCorner;
                    Text headerText = buttonParent.GetComponentInChildren<Text>();
                    headerText.text = characterMaterial.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Body ? "Skin Color" : characterMaterial.materialApplicationLocation.ToString();
                    if (!isOnLeftSide)
                    {
                        headerText.transform.localPosition -= new Vector3(300, 0, 0);
                        headerText.alignment = TextAnchor.MiddleLeft;
                    }
                    characterMaterialParents.Add(new MaterialCustomizationParent() { applicationLocation = characterMaterial.materialApplicationLocation, parent = buttonParent });
                }
                buttonParent = buttonParent.GetComponentInChildren<GridLayoutGroup>().transform;

                Color textureAverageColor = characterMaterial.averageTextureColor;

                Image image = Instantiate(characterCustomizationButtonPrefab, buttonParent).GetComponent<Image>();
                image.color = textureAverageColor;
                materialColorList.Add(new KeyValuePair<CharacterReference.MaterialApplicationLocation, Color>(characterMaterial.materialApplicationLocation, textureAverageColor));

                image.GetComponent<Button>().onClick.AddListener(delegate { ChangeCharacterMaterial(characterMaterial); });
                customizationButtonReference.Add(new ButtonInfo(image.GetComponent<Button>(), characterMaterial.materialApplicationLocation.ToString(), characterMaterial.material.name));
            }

            foreach (CharacterReference.WearableEquipmentOption equipmentOption in PlayerDataManager.Singleton.GetCharacterReference().GetCharacterEquipmentOptions(raceAndGender))
            {
                if (!equipmentTypesIncludedInCharacterAppearance.Contains(equipmentOption.equipmentType)) { continue; }

                Transform buttonParent = characterEquipmentParents.Find(item => item.equipmentType == equipmentOption.equipmentType).parent;
                if (!buttonParent)
                {
                    buttonParent = Instantiate(characterCustomizationRowPrefab, characterCustomizationParent.transform).transform;

                    bool isOnLeftSide = equipmentTypesIncludedInCharacterAppearance.Contains(equipmentOption.equipmentType);
                    isOnLeftSide = !isOnLeftSide;
                    int equipmentCount = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterEquipmentOptions(raceAndGender).FindAll(item => item.equipmentType == equipmentOption.equipmentType).Count;
                    if (isOnLeftSide) { leftYLocalPosition += spacing + leftQueuedSpacing; leftQueuedSpacing = equipmentCount / 11 * -50; } else { rightYLocalPosition += spacing + rightQueuedSpacing; rightQueuedSpacing = equipmentCount / 11 * -50; }
                    buttonParent.localPosition = new Vector3(buttonParent.localPosition.x * (isOnLeftSide ? 1 : -1), isOnLeftSide ? leftYLocalPosition : rightYLocalPosition, 0);

                    TextAnchor childAlignment = isOnLeftSide ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
                    buttonParent.GetComponentInChildren<GridLayoutGroup>().childAlignment = childAlignment;
                    GridLayoutGroup.Corner startCorner = isOnLeftSide ? GridLayoutGroup.Corner.UpperRight : GridLayoutGroup.Corner.UpperLeft;
                    buttonParent.GetComponentInChildren<GridLayoutGroup>().startCorner = startCorner;
                    Text headerText = buttonParent.GetComponentInChildren<Text>();
                    headerText.text = equipmentOption.equipmentType.ToString();
                    if (!isOnLeftSide)
                    {
                        headerText.transform.localPosition -= new Vector3(300, 0, 0);
                        headerText.alignment = TextAnchor.MiddleLeft;
                    }
                    characterEquipmentParents.Add(new EquipmentCustomizationParent() { equipmentType = equipmentOption.equipmentType, parent = buttonParent });

                    buttonParent = buttonParent.GetComponentInChildren<GridLayoutGroup>().transform;
                    Button removeButton = Instantiate(removeEquipmentButtonPrefab, buttonParent).GetComponent<Button>();
                    removeButton.onClick.AddListener(delegate { ChangeCharacterEquipment(new CharacterReference.WearableEquipmentOption(equipmentOption.equipmentType), raceAndGender); });
                    customizationButtonReference.Add(new ButtonInfo(removeButton, equipmentOption.equipmentType.ToString(), "Remove"));
                }
                else
                {
                    buttonParent = buttonParent.GetComponentInChildren<GridLayoutGroup>().transform;
                }

                Color textureAverageColor = equipmentOption.averageTextureColor;

                Image image = Instantiate(characterCustomizationButtonPrefab, buttonParent).GetComponent<Image>();
                image.color = textureAverageColor;
                image.GetComponent<Button>().onClick.AddListener(delegate { ChangeCharacterEquipment(equipmentOption, raceAndGender); });
                customizationButtonReference.Add(new ButtonInfo(image.GetComponent<Button>(), equipmentOption.equipmentType.ToString(), equipmentOption.GetModel(raceAndGender).name));
            }

            Transform raceButtonParent = Instantiate(characterCustomizationRowPrefab, characterCustomizationParent.transform).transform;
            otherCustomizationRowParents.Add(raceButtonParent.gameObject);
            leftYLocalPosition += spacing + leftQueuedSpacing;
            int raceCount = 2;
            leftQueuedSpacing = raceCount / 11 * -50;
            raceButtonParent.localPosition = new Vector3(raceButtonParent.localPosition.x, leftYLocalPosition, 0);
            raceButtonParent.GetComponentInChildren<Text>().text = "Race";
            raceButtonParent = raceButtonParent.GetComponentInChildren<GridLayoutGroup>().transform;

            //foreach (string race in new List<string>() { "Human", "Orc" })
            foreach (string race in new List<string>() { "Human" })
            {
                Image image = Instantiate(characterCustomizationButtonPrefab, raceButtonParent).GetComponent<Image>();

                switch (race)
                {
                    case "Human":
                        image.color = new Color(210 / 255f, 180 / 255f, 140 / 255f, 1);
                        break;
                    case "Orc":
                        image.color = Color.green;
                        break;
                    default:
                        Debug.Log("Not sure how to handle race string " + race);
                        break;
                }

                image.GetComponent<Button>().onClick.AddListener(delegate { ChangeCharacterModel(race, true); });
                customizationButtonReference.Add(new ButtonInfo(image.GetComponent<Button>(), "Race", race));
            }

            Transform genderButtonParent = Instantiate(characterCustomizationRowPrefab, characterCustomizationParent.transform).transform;
            otherCustomizationRowParents.Add(genderButtonParent.gameObject);
            leftYLocalPosition += spacing + leftQueuedSpacing;
            int genderCount = 2;
            leftQueuedSpacing = genderCount / 11 * -50;
            genderButtonParent.localPosition = new Vector3(genderButtonParent.localPosition.x, leftYLocalPosition, 0);
            genderButtonParent.GetComponentInChildren<Text>().text = "Gender";
            genderButtonParent = genderButtonParent.GetComponentInChildren<GridLayoutGroup>().transform;

            Image boyButtonImage = Instantiate(characterCustomizationButtonPrefab, genderButtonParent).GetComponent<Image>();
            boyButtonImage.color = Color.blue;
            boyButtonImage.GetComponent<Button>().onClick.AddListener(delegate { ChangeCharacterModel("Male", false); });
            customizationButtonReference.Add(new ButtonInfo(boyButtonImage.GetComponent<Button>(), "Gender", "Male"));
            Image girlButtonImage = Instantiate(characterCustomizationButtonPrefab, genderButtonParent).GetComponent<Image>();
            girlButtonImage.color = Color.magenta;
            girlButtonImage.GetComponent<Button>().onClick.AddListener(delegate { ChangeCharacterModel("Female", false); });
            customizationButtonReference.Add(new ButtonInfo(girlButtonImage.GetComponent<Button>(), "Gender", "Female"));
        }

        private void RefreshButtonInteractability(bool disableAll = false)
        {
            selectCharacterButton.interactable = !string.IsNullOrEmpty(selectedCharacter._id.ToString()) & !WebRequestManager.Singleton.PlayingOffine;
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
                if (disableAll) { buttonInfo.button.interactable = false; continue; }

                switch (buttonInfo.key)
                {
                    case "Eyes":
                        buttonInfo.button.interactable = selectedCharacter.eyeColor != buttonInfo.value;
                        break;
                    case "Body":
                        buttonInfo.button.interactable = selectedCharacter.bodyColor != buttonInfo.value;
                        break;
                    case "Brows":
                        buttonInfo.button.interactable = selectedCharacter.brows == "" ? buttonInfo.value != "Remove" : buttonInfo.value != selectedCharacter.brows;
                        break;
                    case "Hair":
                        buttonInfo.button.interactable = selectedCharacter.hair == "" ? buttonInfo.value != "Remove" : buttonInfo.value != selectedCharacter.hair;
                        break;
                    case "Beard":
                        buttonInfo.button.interactable = selectedCharacter.beard == "" ? buttonInfo.value != "Remove" : buttonInfo.value != selectedCharacter.beard;
                        break;
                    case "Race":
                        buttonInfo.button.interactable = selectedRace != buttonInfo.value;
                        break;
                    case "Gender":
                        buttonInfo.button.interactable = selectedGender != buttonInfo.value;
                        break;
                    default:
                        Debug.LogError("Not sure how to handle button key " + buttonInfo.key);
                        break;
                }
            }
        }

        private WebRequestManager.Character selectedCharacter;
        private GameObject previewObject;
        public void UpdateSelectedCharacter(WebRequestManager.Character character)
        {
            selectCharacterButton.interactable = !WebRequestManager.Singleton.PlayingOffine;
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
            if (shouldCreateNewModel) { RefreshMaterialsAndEquipmentOptions(raceAndGender); }

            selectedCharacter = previewObject.GetComponentInChildren<AnimatorReference>().GetCharacterWebInfo(character);
            selectedCharacter.raceAndGender = raceAndGender;
            StartCoroutine(previewObject.GetComponent<LoadoutManager>().ApplyDefaultEquipment(raceAndGender));

            finishCharacterCustomizationButton.onClick.RemoveAllListeners();
            finishCharacterCustomizationButton.onClick.AddListener(delegate { StartCoroutine(ApplyCharacterChanges(selectedCharacter)); });
            deleteCharacterButton.onClick.RemoveAllListeners();
            deleteCharacterButton.onClick.AddListener(delegate { StartCoroutine(DeleteCharacterCoroutine(selectedCharacter)); });

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
            previewObject.GetComponent<AnimationHandler>().ApplyWearableEquipment(wearableEquipmentOption, raceAndGender);
            UpdateSelectedCharacter(previewObject.GetComponentInChildren<AnimatorReference>().GetCharacterWebInfo(selectedCharacter));
        }

        private void Start()
        {
            WebRequestManager.Singleton.RefreshServers();
        }

        List<ServerListElement> serverListElementList = new List<ServerListElement>();
        private float lastTextChangeTime;
        private void Update()
        {
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
            }

            serverListElementList = serverListElementList.OrderBy(item => item.pingTime).ToList();
            for (int i = 0; i < serverListElementList.Count; i++)
            {
                serverListElementList[i].gameObject.SetActive(serverListElementList[i].pingTime >= 0);
                serverListElementList[i].transform.SetSiblingIndex(i);
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
            deleteCharacterButton.gameObject.SetActive(false);
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
            deleteCharacterButton.gameObject.SetActive(true);
        }

        public void OpenCharacterSelect()
        {
            if (NetworkManager.Singleton.IsListening)
            {
                connectButton.interactable = true;
                refreshServersButton.interactable = true;
                NetworkManager.Singleton.Shutdown();
            }

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
            deleteCharacterButton.interactable = false;
            returnButton.interactable = false;
            characterNameInputField.interactable = false;

            webRequestStatusText.gameObject.SetActive(true);
            webRequestStatusText.text = "UPLOADING CHARACTER";

            yield return isEditingExistingCharacter ? WebRequestManager.Singleton.UpdateCharacterCosmetics(character) : WebRequestManager.Singleton.CharacterPostRequest(character);

            webRequestStatusText.gameObject.SetActive(true);

            RefreshButtonInteractability();
            finishCharacterCustomizationButton.interactable = true;
            deleteCharacterButton.interactable = true;
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

            webRequestStatusText.gameObject.SetActive(true);
            webRequestStatusText.text = "DELETING CHARACTER";

            yield return WebRequestManager.Singleton.CharacterDisableRequest(character._id.ToString());

            webRequestStatusText.gameObject.SetActive(true);

            RefreshButtonInteractability();
            finishCharacterCustomizationButton.interactable = true;
            deleteCharacterButton.interactable = true;
            returnButton.interactable = true;
            characterNameInputField.interactable = true;

            OpenCharacterSelect();
        }

        public void GoToTrainingRoom()
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(selectedCharacter._id.ToString());
            NetworkManager.Singleton.StartHost();
            NetSceneManager.Singleton.LoadScene("Training Room");
            NetSceneManager.Singleton.LoadScene("Arena Map A");
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
            connectButton.interactable = false;
            refreshServersButton.interactable = false;
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