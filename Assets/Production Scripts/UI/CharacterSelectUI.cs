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

        [Header("Character Select")]
        [SerializeField] private GameObject characterSelectParent;
        [SerializeField] private CharacterCard characterCardPrefab;
        [SerializeField] private Transform characterCardParent;
        [SerializeField] private Button selectCharacterButton;

        [Header("Character Customization")]
        [SerializeField] private GameObject characterCustomizationParent;
        [SerializeField] private GameObject characterCustomizationRowPrefab;
        [SerializeField] private GameObject characterCustomizationButtonPrefab;
        [SerializeField] private GameObject removeEquipmentButtonPrefab;
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
            selectCharacterButton.interactable = !string.IsNullOrEmpty(selectedCharacter.characterId);
        }

        private List<ButtonInfo> characterCardButtonReference = new List<ButtonInfo>();

        private void RefreshCharacterCards()
        {
            foreach (Transform child in characterCardParent)
            {
                Destroy(child.gameObject);
            }
            characterCardButtonReference.Clear();

            // Create character cards
            foreach (WebRequestManager.Character character in WebRequestManager.GetCharacters())
            {
                CharacterCard characterCard = Instantiate(characterCardPrefab.gameObject, characterCardParent).GetComponent<CharacterCard>();
                characterCard.Initialize(character);
                characterCard.GetComponent<Button>().onClick.AddListener(delegate { UpdateSelectedCharacter(character); });
                characterCard.editButton.onClick.AddListener(delegate { UpdateSelectedCharacter(character); });
                characterCard.editButton.onClick.AddListener(delegate { OpenCharacterCustomization(character); });
                characterCardButtonReference.Add(new ButtonInfo(characterCard.GetComponent<Button>(), "CharacterCard", character.characterId));
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
                    headerText.text = characterMaterial.materialApplicationLocation.ToString();
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

            foreach (CharacterReference.WearableEquipmentOption equipmentOption in PlayerDataManager.Singleton.GetCharacterReference().GetWearableEquipmentOptions(raceAndGender))
            {
                if (!equipmentTypesIncludedInCharacterAppearance.Contains(equipmentOption.equipmentType)) { continue; }

                Transform buttonParent = characterEquipmentParents.Find(item => item.equipmentType == equipmentOption.equipmentType).parent;
                if (!buttonParent)
                {
                    buttonParent = Instantiate(characterCustomizationRowPrefab, characterCustomizationParent.transform).transform;

                    bool isOnLeftSide = equipmentTypesIncludedInCharacterAppearance.Contains(equipmentOption.equipmentType);
                    isOnLeftSide = !isOnLeftSide;
                    int equipmentCount = PlayerDataManager.Singleton.GetCharacterReference().GetWearableEquipmentOptions(raceAndGender).FindAll(item => item.equipmentType == equipmentOption.equipmentType).Count;
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
                    removeButton.onClick.AddListener(delegate { ChangeCharacterEquipment(new CharacterReference.WearableEquipmentOption(equipmentOption.equipmentType, Color.white)); });
                    customizationButtonReference.Add(new ButtonInfo(removeButton, equipmentOption.equipmentType.ToString(), "Remove"));
                }
                else
                {
                    buttonParent = buttonParent.GetComponentInChildren<GridLayoutGroup>().transform;
                }

                Color textureAverageColor = equipmentOption.averageTextureColor;

                Image image = Instantiate(characterCustomizationButtonPrefab, buttonParent).GetComponent<Image>();
                image.color = textureAverageColor;
                image.GetComponent<Button>().onClick.AddListener(delegate { ChangeCharacterEquipment(equipmentOption); });
                customizationButtonReference.Add(new ButtonInfo(image.GetComponent<Button>(), equipmentOption.equipmentType.ToString(), equipmentOption.wearableEquipmentPrefab.name));
            }

            Transform raceButtonParent = Instantiate(characterCustomizationRowPrefab, characterCustomizationParent.transform).transform;
            otherCustomizationRowParents.Add(raceButtonParent.gameObject);
            leftYLocalPosition += spacing + leftQueuedSpacing;
            int raceCount = 2;
            leftQueuedSpacing = raceCount / 11 * -50;
            raceButtonParent.localPosition = new Vector3(raceButtonParent.localPosition.x, leftYLocalPosition, 0);
            raceButtonParent.GetComponentInChildren<Text>().text = "Race";
            raceButtonParent = raceButtonParent.GetComponentInChildren<GridLayoutGroup>().transform;

            foreach (string race in new List<string>() { "Human", "Orc" })
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

        private void RefreshButtonInteractability()
        {
            selectCharacterButton.interactable = !string.IsNullOrEmpty(selectedCharacter.characterId);

            foreach (ButtonInfo buttonInfo in characterCardButtonReference)
            {
                switch (buttonInfo.key)
                {
                    case "CharacterCard":
                        buttonInfo.button.interactable = selectedCharacter.characterId != buttonInfo.value;
                        break;
                    default:
                        Debug.LogError("Not sure how to handle button key " + buttonInfo.key);
                        break;
                }
            }

            foreach (ButtonInfo buttonInfo in customizationButtonReference)
            {
                switch (buttonInfo.key)
                {
                    case "Eyes":
                        buttonInfo.button.interactable = selectedCharacter.eyeColorName != buttonInfo.value;
                        break;
                    case "Body":
                        buttonInfo.button.interactable = selectedCharacter.bodyColorName != buttonInfo.value;
                        break;
                    case "Brows":
                        buttonInfo.button.interactable = selectedCharacter.browsName == "" ? buttonInfo.value != "Remove" : buttonInfo.value != selectedCharacter.browsName;
                        break;
                    case "Head":
                        buttonInfo.button.interactable = selectedCharacter.headColorName != buttonInfo.value;
                        break;
                    case "Hair":
                        buttonInfo.button.interactable = selectedCharacter.hairName == "" ? buttonInfo.value != "Remove" : buttonInfo.value != selectedCharacter.hairName;
                        break;
                    case "Beard":
                        buttonInfo.button.interactable = selectedCharacter.beardName == "" ? buttonInfo.value != "Remove" : buttonInfo.value != selectedCharacter.beardName;
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
            selectCharacterButton.interactable = true;
            characterNameInputField.text = character.characterName;
            var playerModelOptionList = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions();
            int characterIndex = System.Array.FindIndex(playerModelOptionList, item => System.Array.FindIndex(item.skinOptions, skinItem => skinItem.name == character.characterModelName) != -1);
            
            if (characterIndex == -1)
            {
                if (previewObject) { Destroy(previewObject); }
                selectedCharacter = default;
                RefreshButtonInteractability();
                return;
            }
            
            CharacterReference.PlayerModelOption playerModelOption = playerModelOptionList[characterIndex];

            bool shouldCreateNewModel = selectedCharacter.characterModelName != character.characterModelName;
            if (shouldCreateNewModel)
            {
                ClearMaterialsAndEquipmentOptions();
                if (previewObject) { Destroy(previewObject); }
                // Instantiate the player model
                previewObject = Instantiate(playerModelOptionList[characterIndex].playerPrefab, previewCharacterPosition, Quaternion.Euler(previewCharacterRotation));
                SceneManager.MoveGameObjectToScene(previewObject, gameObject.scene);
                int skinIndex = System.Array.FindIndex(playerModelOptionList[characterIndex].skinOptions, skinItem => skinItem.name == character.characterModelName);
                previewObject.GetComponent<AnimationHandler>().SetCharacter(characterIndex, skinIndex);
            }

            AnimationHandler animationHandler = previewObject.GetComponent<AnimationHandler>();

            List<CharacterReference.CharacterMaterial> characterMaterialOptions = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(playerModelOption.raceAndGender);
            animationHandler.ApplyCharacterMaterial(characterMaterialOptions.Find(item => item.material.name == character.bodyColorName));
            animationHandler.ApplyCharacterMaterial(characterMaterialOptions.Find(item => item.material.name == character.headColorName));
            animationHandler.ApplyCharacterMaterial(characterMaterialOptions.Find(item => item.material.name == character.eyeColorName));

            StartCoroutine(ApplyEquipment(character, playerModelOption, shouldCreateNewModel));
        }

        private IEnumerator ApplyEquipment(WebRequestManager.Character character, CharacterReference.PlayerModelOption playerModelOption, bool shouldRefreshEquipmentOptions)
        {
            yield return null;
            AnimationHandler animationHandler = previewObject.GetComponent<AnimationHandler>();
            List<CharacterReference.WearableEquipmentOption> equipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWearableEquipmentOptions(playerModelOption.raceAndGender);
            animationHandler.ApplyWearableEquipment(equipmentOptions.Find(item => item.wearableEquipmentPrefab.name == character.beardName));
            animationHandler.ApplyWearableEquipment(equipmentOptions.Find(item => item.wearableEquipmentPrefab.name == character.browsName));
            animationHandler.ApplyWearableEquipment(equipmentOptions.Find(item => item.wearableEquipmentPrefab.name == character.hairName));

            string[] raceAndGenderStrings = Regex.Matches(playerModelOption.raceAndGender.ToString(), @"([A-Z][a-z]+)").Cast<Match>().Select(m => m.Value).ToArray();
            selectedRace = raceAndGenderStrings[0];
            selectedGender = raceAndGenderStrings[1];
            if (shouldRefreshEquipmentOptions) { RefreshMaterialsAndEquipmentOptions(System.Enum.Parse<CharacterReference.RaceAndGender>(selectedRace + selectedGender)); }

            selectedCharacter = previewObject.GetComponentInChildren<AnimatorReference>().GetCharacterWebInfo(character);

            finishCharacterCustomizationButton.onClick.RemoveAllListeners();
            finishCharacterCustomizationButton.onClick.AddListener(delegate { ApplyCharacterChanges(selectedCharacter); });

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
            character.characterModelName = option.skinOptions[0].name;
            UpdateSelectedCharacter(character);
        }

        public void ChangeCharacterMaterial(CharacterReference.CharacterMaterial characterMaterial)
        {
            previewObject.GetComponent<AnimationHandler>().ApplyCharacterMaterial(characterMaterial);
            UpdateSelectedCharacter(previewObject.GetComponentInChildren<AnimatorReference>().GetCharacterWebInfo(selectedCharacter));
        }

        public void ChangeCharacterEquipment(CharacterReference.WearableEquipmentOption wearableEquipmentOption)
        {
            previewObject.GetComponent<AnimationHandler>().ApplyWearableEquipment(wearableEquipmentOption);
            UpdateSelectedCharacter(previewObject.GetComponentInChildren<AnimatorReference>().GetCharacterWebInfo(selectedCharacter));
        }

        private void Start()
        {
            StartCoroutine(WebRequestManager.GetRequest());
        }

        List<ServerListElement> serverListElementList = new List<ServerListElement>();
        private void Update()
        {
            if (!WebRequestManager.IsRefreshingServers)
            {
                foreach (WebRequestManager.Server server in WebRequestManager.Servers)
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
            selectedCharacter.characterName = characterNameInputField.text;
            //NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(characterNameInputField.text + "|0|0");
        }

        public void ReturnToMainMenu()
        {
            NetSceneManager.Singleton.LoadScene("Main Menu");
        }

        public void OpenCharacterCustomization()
        {
            characterSelectParent.SetActive(false);
            characterCustomizationParent.SetActive(true);

            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(OpenCharacterSelect);

            selectedCharacter = new WebRequestManager.Character();
            UpdateSelectedCharacter(WebRequestManager.DefaultCharacter);
            finishCharacterCustomizationButton.GetComponentInChildren<Text>().text = "CREATE";
        }

        private void OpenCharacterCustomization(WebRequestManager.Character character)
        {
            characterSelectParent.SetActive(false);
            characterCustomizationParent.SetActive(true);

            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(OpenCharacterSelect);

            selectedCharacter = new WebRequestManager.Character();
            UpdateSelectedCharacter(character);
            finishCharacterCustomizationButton.GetComponentInChildren<Text>().text = "APPLY";
        }

        public void OpenCharacterSelect()
        {
            RefreshCharacterCards();

            characterSelectParent.SetActive(true);
            characterCustomizationParent.SetActive(false);
            serverListParent.SetActive(false);

            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(ReturnToMainMenu);

            UpdateSelectedCharacter(default);
        }

        public void ApplyCharacterChanges(WebRequestManager.Character character)
        {
            WebRequestManager.AddCharacter(character);
            OpenCharacterSelect();
        }

        public void OpenServerBrowser()
        {
            characterSelectParent.SetActive(false);
            serverListParent.SetActive(true);
            RefreshServerBrowser();
        }

        public void RefreshServerBrowser()
        {
            StartCoroutine(WebRequestManager.GetRequest());
            foreach (ServerListElement serverListElement in serverListElementList)
            {
                Destroy(serverListElement.gameObject);
            }
            serverListElementList.Clear();
        }

        public void StartClient()
        {
            connectButton.interactable = false;
            closeServersMenuButton.interactable = false;
            refreshServersButton.interactable = false;
            NetworkManager.Singleton.StartClient();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(previewCharacterPosition, 0.5f);
        }
    }
}