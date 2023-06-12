using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using GameCreator.Core;
using GameCreator.Characters;

public class SceneUserDataManager : MonoBehaviour
{
    [SerializeField] private List<CreateCharacterModel> charactersList = new List<CreateCharacterModel>();
    [SerializeField] private GameObject placeholderContainer;
    [SerializeField] private Transform startSpawnLoc;
    [SerializeField] private TextMeshProUGUI nameTMP;
    [SerializeField] public GameObject spotlightPrefab;
    [SerializeField] public GameObject cameraPrefab;
    [SerializeField] private GameObject charDesc_Panel;
    [SerializeField] private Text charDesc_Name;
    [SerializeField] private Text charDesc_Lore;
    [SerializeField] private GridLayoutGroup gridLayoutGroup;
    [SerializeField] private Image gridImgPrefab;

    private GameObject currentSpotlight;
    private GameObject currentCharDesc;
    private GameObject mainCamera;
    private GameObject selectedObject;
    private List<GameObject> placeholderObjects = new List<GameObject>();
    private int currentIndex = -1;
    public float cameraDistance = 5.0f;
    private DataManager datamanager = new DataManager();
    private Boolean isPreviewActive = false;

    private Vector3 cameraDesc_InitPos;
    private Quaternion charDesc_InitRot; 
    private Quaternion char_InitRot; 

    void Start()
    {
        this.datamanager = DataManager.Instance;
        mainCamera = Instantiate(cameraPrefab);

        this.cameraDesc_InitPos = mainCamera.transform.position;
        this.charDesc_InitRot = mainCamera.transform.rotation;

        this.InitDataReferences();
    }

    private void InitDataReferences()
    {
        var _userdata = datamanager;
        if (_userdata)
        {
            if (nameTMP)
            {
                nameTMP.text = this.datamanager.Data.account_name;
            }
            this.PostCharacterSelectAnalytics("CharacterSelect");
            this.SpawnGameObjectsHorizontally();
        }
    }

    private void SpawnGameObjectsHorizontally()
    {
        List<GameObject> placeholders = new List<GameObject>();
        gridLayoutGroup.padding = new RectOffset(10, 10, 60, 0);

        for (int i = 0; i < charactersList.Count; i++)
        {
            gridImgPrefab.sprite = charactersList[i].characterImage;
            gridImgPrefab.gameObject.name = charactersList[i].characterName;
            gridImgPrefab.gameObject.SetActive(true);
            GameObject newItem = Instantiate(gridImgPrefab.gameObject, gridLayoutGroup.transform);
        }

        this.SelectGameObject(null);
    }

    private void SelectPreviousObject()
    {
        if (placeholderObjects.Count == 0)
            return;

        // Decrement the current index
        currentIndex--;

        // Wrap around to the last index if the current index is less than 0
        if (currentIndex < 0)
        {
            currentIndex = placeholderObjects.Count - 1;
        }

        // Select the game object at the current index
        SelectGameObject(placeholderObjects[currentIndex]);
    }

    private void SelectNextObject()
    {
        if (placeholderObjects.Count == 0)
            return;

        // Increment the current index
        currentIndex++;

        // Wrap around to the first index if the current index is greater than or equal to the length of the array
        if (currentIndex >= placeholderObjects.Count)
        {
            currentIndex = 0;
        }

        // Select the game object at the current index
        SelectGameObject(placeholderObjects[currentIndex]);
    }

    public void SelectGameObject(GameObject selectedObject)
    {
        if(isPreviewActive) return;

        CreateCharacterModel charDesc = null;

        if(this.selectedObject == null) {
            charDesc = charactersList[0];
        } else {
            Destroy(this.selectedObject, 0f);
            charDesc = charactersList.FirstOrDefault(model => model.characterName == selectedObject.name.Replace("(Clone)", ""));
        }

        if(charDesc == null) return;
        if(charDesc.characterObject == this.selectedObject) return;

        Vector3 spawnPosition = startSpawnLoc.position;

        this.charDesc_Name.text = charDesc.characterName;
        this.charDesc_Lore.text = charDesc.characterDescription;


        // Store the selected game object
        this.selectedObject = Instantiate(charDesc.characterObject, spawnPosition, Quaternion.Euler(0f, 180f, 0f));

        if (NetworkManager.Singleton.IsServer) {
            this.selectedObject.GetComponent<NetworkObject>().Spawn();
        }
    }

    public void PostCharacterSelectAnalytics(string _panel, string _character = "0")
    {
        var _userdata = datamanager.Data;
        var _shopData = _userdata.characterAnalytics.FirstOrDefault(x => x.panelName == _panel);

        var _panelData = new UserModel.ShopAnalyticsModel
        {
            panelName = _panel,
            dateTime_added = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"),
            count = 1
        };
        if (_shopData == null)
        {
            if (_character != "0")
            {
                _panelData.character.Add(new UserModel.ShopAnalyticsModel.Characters
                {
                    name = _character,
                    count = 1
                });
            }
            _userdata.characterAnalytics.Add(_panelData);
            datamanager.PostUserdata(_userdata, false);
            return;
        }

        var _index = _userdata.characterAnalytics.IndexOf(_shopData);
        _panelData.count += _shopData.count;
        if (_character != "0")
        {
            var _chardata = _userdata.characterAnalytics[_index].character.FirstOrDefault(x => x.name == _character);
            if (_chardata == null)
            {
                _userdata.characterAnalytics[_index].character.Add(new UserModel.ShopAnalyticsModel.Characters
                {
                    name = _character,
                    count = 1
                });
            }
            else
            {
                var _charIndex = _userdata.characterAnalytics[_index].character.IndexOf(_chardata);
                _userdata.characterAnalytics[_index].character[_charIndex] = new UserModel.ShopAnalyticsModel.Characters
                {
                    name = _character,
                    count = _chardata.count + 1
                };
            }
        }
        else
        {
            _userdata.characterAnalytics[_index].character.Add(new UserModel.ShopAnalyticsModel.Characters
            {
                name = _character,
                count = 1
            });
        }
        _userdata.characterAnalytics[_index] = new UserModel.ShopAnalyticsModel
        {
            panelName = _panelData.panelName,
            dateTime_added = _panelData.dateTime_added,
            count = _panelData.count,
            character = _userdata.characterAnalytics[_index].character
        };

        datamanager.PostUserdata(_userdata, false);
    }

    private void OnDrawGizmosSelected()
    {
        if (selectedObject != null)
        {
            // Draw a wire sphere around the selected game object
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(selectedObject.transform.position, 1f);
        }
    }

    private void NextScene() {
        // Selected CharacterObject is this.selectedObject
    }
}
