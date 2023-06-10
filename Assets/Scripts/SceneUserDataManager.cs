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
    [SerializeField] private ShopCharacterModel[] characterModel;
    [SerializeField] private GameObject placeholderContainer;
    [SerializeField] private Transform startSpawnLoc;
    [SerializeField] private TextMeshProUGUI nameTMP;
    [SerializeField] public GameObject spotlightPrefab;
    [SerializeField] public GameObject cameraPrefab;
    [SerializeField] private GameObject charDesc_Panel;
    [SerializeField] private TextMeshProUGUI charDesc_Name;
    [SerializeField] private TextMeshProUGUI charDesc_Lore;

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
    private void Update()
    {
        // Check for mouse click
        if (Input.GetMouseButtonDown(0))
        {
            // Cast a ray from the mouse position into the scene
            Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // Check if the raycast hit a game object
                GameObject hitObject = hit.collider.gameObject;

                // Select the clicked game object
                SelectGameObject(hitObject);
            }
        }

        // Check for left arrow key press
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            // Cycle to the previous game object
            SelectPreviousObject();
        }

        // Check for right arrow key press
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            // Cycle to the next game object
            SelectNextObject();
        }

        if(Input.GetKeyDown(KeyCode.Escape)) {
            UnSelect();
        }

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
        for (int i = 0; i < characterModel.Length; i++)
        {
            Vector3 spawnPosition = startSpawnLoc.position + new Vector3(2.0f * i, 0f, 0f);

            string placeholderName = "Container-" + i;
            
            placeholderContainer.name = placeholderName;
            // GameObject placeholderObject = Instantiate(placeholderContainer, spawnPosition + Vector3.up * 1.0f, Quaternion.Euler(0f, 0f, 0f));

            GameObject playerObject = Instantiate(characterModel[i].characterObject, spawnPosition, Quaternion.Euler(0f, 180f, 0f));

            if (NetworkManager.Singleton.IsServer) {
                playerObject.GetComponent<NetworkObject>().Spawn();
            }

            placeholderObjects.Add(placeholderContainer);
        }
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

    private void SelectGameObject(GameObject selectedObject)
    {

        if(isPreviewActive) return;

        // Store the selected game object
        this.selectedObject = selectedObject;

        if (selectedObject == null) return;
        if (selectedObject.tag != "Character") return;

        char_InitRot = selectedObject.transform.rotation;

        // Destroy the previous spotlight if it exists
        if (currentSpotlight != null)
        {
            Destroy(currentSpotlight);
            Destroy(currentCharDesc);
        }
        
        selectedObject.transform.rotation = Quaternion.Euler(0f, -215f, 0f);

        // Instantiate a new spotlight
        currentSpotlight = Instantiate(spotlightPrefab, selectedObject.transform.position + Vector3.up * 2.5f, Quaternion.Euler(90f, 0f, 0f));

        charDesc_Panel.SetActive(true);

        isPreviewActive = true;
        
        
        // Move the camera in front of the selected game object
        Vector3 cameraPosition = selectedObject.transform.position + new Vector3(1.0f, 1.0f, -2.0f);
        mainCamera.transform.position = cameraPosition;
        mainCamera.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }

    public void UnSelect() {
        mainCamera.transform.position = this.cameraDesc_InitPos;
        mainCamera.transform.rotation = this.charDesc_InitRot;

        selectedObject.transform.rotation = char_InitRot;

        if (currentSpotlight != null)
        {
            Destroy(currentSpotlight);
        }

        charDesc_Panel.SetActive(false);
        isPreviewActive = false;
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
}
