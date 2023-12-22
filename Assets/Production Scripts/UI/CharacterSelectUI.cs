using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Core;
using Vi.ScriptableObjects;
using UnityEngine.UI;
using System.Linq;

namespace Vi.UI
{
    public class CharacterSelectUI : MonoBehaviour
    {
        [SerializeField] private CharacterCard characterCardPrefab;
        [SerializeField] private Transform characterCardParent;
        [SerializeField] private GameObject characterSelectParent;

        [Header("Server Browser")]
        [SerializeField] private ServerListElement serverListElement;
        [SerializeField] private Transform serverListElementParent;
        [SerializeField] private GameObject serverListParent;
        [SerializeField] private Button connectButton;
        [SerializeField] private Button closeServersMenuButton;
        [SerializeField] private Button refreshServersButton;

        [Header("Old")]
        [SerializeField] private InputField usernameInputField;
        [SerializeField] private Button createCharacterButton;
        [SerializeField] private Vector3 previewCharacterPosition = new Vector3(0.6f, 0, -7);
        [SerializeField] private Vector3 previewCharacterRotation = new Vector3(0, 180, 0);

        private CharacterReference.RaceAndGender raceAndGender = CharacterReference.RaceAndGender.HumanMale;

        private void Awake()
        {
            foreach (WebRequestManager.Character character in WebRequestManager.Characters)
            {
                CharacterCard characterCard = Instantiate(characterCardPrefab.gameObject, characterCardParent).GetComponent<CharacterCard>();
                characterCard.Initialize(character);
                characterCard.GetComponent<Button>().onClick.AddListener(delegate { UpdateCharacterPreview(character); });
            }

            
            //characterCard.editButton.onClick.AddListener(delegate { });
            /*
            List<KeyValuePair<CharacterReference.MaterialApplicationLocation, Color>> materialColorList = new List<KeyValuePair<CharacterReference.MaterialApplicationLocation, Color>>();
            foreach (CharacterReference.CharacterMaterial characterMaterial in PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender))
            {
                Transform parent = null;
                if (characterMaterial.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Body)
                    parent = bodyColorButtonParent;
                else if (characterMaterial.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Head)
                    parent = headColorButtonParent;
                else if (characterMaterial.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Eyes)
                    parent = eyeColorButtonParent;

                Texture2D texture2D = (Texture2D)characterMaterial.material.GetTexture("_BaseMap");
                Color textureAverageColor = Color.black;

                switch (characterMaterial.material.name[^3..])
                {
                    case "_Bl":
                        textureAverageColor = Color.blue;
                        break;
                    case "_Br":
                        textureAverageColor = new Color(255 / 255, 248 / 255, 220 / 255, 1);
                        break;
                    case "_Gn":
                        textureAverageColor = Color.green;
                        break;
                    case "_Pe":
                        textureAverageColor = Color.magenta;
                        break;
                    default:
                        textureAverageColor = AverageColorFromTexture(texture2D);
                        break;
                }

                Image image = Instantiate(coloredButtonPrefab, parent).GetComponent<Image>();
                image.color = textureAverageColor;
                materialColorList.Add(new KeyValuePair<CharacterReference.MaterialApplicationLocation, Color>(characterMaterial.materialApplicationLocation, textureAverageColor));

                image.GetComponent<Button>().onClick.AddListener(delegate { ChangeCharacterMaterial(characterMaterial); });
                
                //var kvp = materialColorList.Find(item => item.Key == characterMaterial.materialApplicationLocation & Vector4.Distance(item.Value, textureAverageColor) > 1);
                //if (kvp.Key == default & kvp.Value == default)
                //{
                //    Image image = Instantiate(coloredButtonPrefab, parent).GetComponent<Image>();
                //    image.color = textureAverageColor;
                //    materialColorList.Add(new KeyValuePair<CharacterReference.MaterialApplicationLocation, Color>(characterMaterial.materialApplicationLocation, textureAverageColor));

                //    image.GetComponent<Button>().onClick.AddListener(delegate { ChangeCharacterMaterial(characterMaterial); });
                //}
            }

            List<KeyValuePair<CharacterReference.EquipmentType, Color>> equipmentColorList = new List<KeyValuePair<CharacterReference.EquipmentType, Color>>();
            foreach (CharacterReference.WearableEquipmentOption equipmentOption in PlayerDataManager.Singleton.GetCharacterReference().GetWearableEquipmentOptions(raceAndGender))
            {
                Transform parent = null;
                if (equipmentOption.equipmentType == CharacterReference.EquipmentType.Belt)
                    parent = beltParent;
                else if (equipmentOption.equipmentType == CharacterReference.EquipmentType.Boots)
                    parent = bootsParent;
                else if (equipmentOption.equipmentType == CharacterReference.EquipmentType.Cape)
                    parent = capeParent;
                else if (equipmentOption.equipmentType == CharacterReference.EquipmentType.Chest)
                    parent = chestParent;
                else if (equipmentOption.equipmentType == CharacterReference.EquipmentType.Gloves)
                    parent = glovesParent;
                else if (equipmentOption.equipmentType == CharacterReference.EquipmentType.Helm)
                    parent = helmParent;
                else if (equipmentOption.equipmentType == CharacterReference.EquipmentType.Pants)
                    parent = pantsParent;
                else if (equipmentOption.equipmentType == CharacterReference.EquipmentType.Robe)
                    parent = robeParent;
                else if (equipmentOption.equipmentType == CharacterReference.EquipmentType.Shoulders)
                    parent = shouldersParent;
                else if (equipmentOption.equipmentType == CharacterReference.EquipmentType.Beard)
                    parent = beardParent;
                else if (equipmentOption.equipmentType == CharacterReference.EquipmentType.Brows)
                    parent = browsParent;
                else if (equipmentOption.equipmentType == CharacterReference.EquipmentType.Hair)
                    parent = hairParent;

                Texture2D texture2D = (Texture2D)equipmentOption.wearableEquipmentPrefab.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial.GetTexture("_BaseMap");
                Color textureAverageColor = AverageColorFromTexture(texture2D);

                var kvp = equipmentColorList.Find(item => item.Key == equipmentOption.equipmentType & Vector4.Distance(item.Value, textureAverageColor) > 1);
                if (kvp.Key == default & kvp.Value == default)
                {
                    Image image = Instantiate(coloredButtonPrefab, parent).GetComponent<Image>();
                    image.color = textureAverageColor;
                    equipmentColorList.Add(new KeyValuePair<CharacterReference.EquipmentType, Color>(equipmentOption.equipmentType, textureAverageColor));

                    image.GetComponent<Button>().onClick.AddListener(delegate { ChangeCharacterEquipment(equipmentOption); });
                }
            }*/

            CloseServerBrowser();
            createCharacterButton.interactable = usernameInputField.text.Length > 0;
        }

        public void ChangeCharacterMaterial(CharacterReference.CharacterMaterial characterMaterial)
        {
            previewObject.GetComponent<AnimationHandler>().ApplyCharacterMaterial(characterMaterial);
        }

        public void ChangeCharacterEquipment(CharacterReference.WearableEquipmentOption wearableEquipmentOption)
        {
            previewObject.GetComponent<AnimationHandler>().ApplyWearableEquipment(wearableEquipmentOption);
        }

        private Color32 AverageColorFromTexture(Texture2D tex)
        {
            Color32[] texColors = tex.GetPixels32();

            int total = texColors.Length;

            float r = 0;
            float g = 0;
            float b = 0;
            float a = 0;

            for (int i = 0; i < total; i++)
            {

                r += texColors[i].r;

                g += texColors[i].g;

                b += texColors[i].b;

                a += texColors[i].a;
            }

            return new Color32((byte)(r / total), (byte)(g / total), (byte)(b / total), (byte)(a / total));
        }

        private void Start()
        {
            UpdateCharacterPreview(0, 0);
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
            createCharacterButton.interactable = usernameInputField.text.Length > 0;
            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(usernameInputField.text + "|0|0");
        }

        public void OpenServerBrowser()
        {
            characterSelectParent.SetActive(false);
            serverListParent.SetActive(true);
            RefreshServerBrowser();
        }

        public void CloseServerBrowser()
        {
            characterSelectParent.SetActive(true);
            serverListParent.SetActive(false);
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

        private GameObject previewObject;
        public void UpdateCharacterPreview(int characterIndex, int skinIndex)
        {
            if (previewObject) { Destroy(previewObject); }

            CharacterReference.PlayerModelOption playerModelOption = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions()[characterIndex];
            previewObject = Instantiate(playerModelOption.playerPrefab, previewCharacterPosition, Quaternion.Euler(previewCharacterRotation));
            previewObject.GetComponent<AnimationHandler>().SetCharacter(characterIndex, skinIndex);
        }

        public void UpdateCharacterPreview(WebRequestManager.Character character)
        {
            if (previewObject) { Destroy(previewObject); }
            var playerModelOptionList = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions();
            int characterIndex = System.Array.FindIndex(playerModelOptionList, item => System.Array.FindIndex(item.skinOptions, skinItem => skinItem.name == character.characterModelName) != -1);
            previewObject = Instantiate(playerModelOptionList[characterIndex].playerPrefab, previewCharacterPosition, Quaternion.Euler(previewCharacterRotation));
            int skinIndex = System.Array.FindIndex(playerModelOptionList[characterIndex].skinOptions, skinItem => skinItem.name == character.characterModelName);
            AnimationHandler animationHandler = previewObject.GetComponent<AnimationHandler>();
            animationHandler.SetCharacter(characterIndex, skinIndex);

            var playerModelOption = playerModelOptionList[characterIndex];

            var characterMaterialOptions = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(playerModelOption.raceAndGender);
            animationHandler.ApplyCharacterMaterial(characterMaterialOptions.Find(item => item.material.name == character.bodyColorName));
            animationHandler.ApplyCharacterMaterial(characterMaterialOptions.Find(item => item.material.name == character.headColorName));
            animationHandler.ApplyCharacterMaterial(characterMaterialOptions.Find(item => item.material.name == character.eyeColorName));

            var equipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWearableEquipmentOptions(playerModelOption.raceAndGender);
            animationHandler.ApplyWearableEquipment(equipmentOptions.Find(item => item.wearableEquipmentPrefab.name == character.beardName));
            animationHandler.ApplyWearableEquipment(equipmentOptions.Find(item => item.wearableEquipmentPrefab.name == character.browsName));
            animationHandler.ApplyWearableEquipment(equipmentOptions.Find(item => item.wearableEquipmentPrefab.name == character.hairName));
        }

        public void ChangeSkin()
        {
            PlayerDataManager.ParsedConnectionData parsedConnectionData = PlayerDataManager.ParseConnectionData(NetworkManager.Singleton.NetworkConfig.ConnectionData);

            parsedConnectionData.skinIndex += 1;
            if (parsedConnectionData.skinIndex > PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions()[parsedConnectionData.characterIndex].skinOptions.Length - 1) { parsedConnectionData.skinIndex = 0; }

            PlayerDataManager.SetConnectionData(parsedConnectionData);

            UpdateCharacterPreview(parsedConnectionData.characterIndex, parsedConnectionData.skinIndex);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(previewCharacterPosition, 0.5f);
        }
    }
}