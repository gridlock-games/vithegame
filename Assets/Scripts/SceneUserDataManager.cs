using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SceneUserDataManager : MonoBehaviour
{
    [SerializeField] private ShopCharacterModel[] characterModel;
    [SerializeField] private GameObject placeholderContainer;
    [SerializeField] private Transform startSpawnLoc;
    [SerializeField] private TextMeshProUGUI nameTMP;
    [SerializeField] public GameObject spotlightPrefab;
    [SerializeField] public GameObject cameraPrefab;

    private GameObject currentSpotlight;
    private GameObject mainCamera;
    private GameObject selectedObject;
    private List<GameObject> placeholderObjects = new List<GameObject>();
    private int currentIndex = -1;
    private DataManager datamanager = new DataManager();

    void Start()
    {
        this.datamanager = DataManager.Instance;
        mainCamera = Instantiate(cameraPrefab);
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

    }
    public void InitDataReferences()
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

    public void SpawnGameObjectsHorizontally()
    {
        List<GameObject> placeholders = new List<GameObject>();
        for (int i = 0; i < characterModel.Length; i++)
        {

            Vector3 spawnPosition = startSpawnLoc.position + new Vector3(2.0f * i, 0f, 0f);
            Instantiate(characterModel[i].characterObject, spawnPosition, Quaternion.Euler(0f, 180f, 0f));
            placeholderContainer.name = placeholderContainer.name + "-" + i;
            Instantiate(placeholderContainer, spawnPosition + Vector3.up * 1.0f, Quaternion.Euler(0f, 180f, 0f));
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


    private void SelectGameObject(GameObject selectedObject)
    {
        // Store the selected game object
        this.selectedObject = selectedObject;

        if (selectedObject == null) return;
        if (selectedObject.tag != "Placeholder") return;

        // Destroy the previous spotlight if it exists
        if (currentSpotlight != null)
        {
            Destroy(currentSpotlight);
        }

        // Instantiate a new spotlight
        currentSpotlight = Instantiate(spotlightPrefab, selectedObject.transform.position + Vector3.up * 2.5f, Quaternion.Euler(90f, 0f, 0f));
        // currentSpotlight.GetComponent<FollowMouse>().TargetObject = selectedObject;
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
