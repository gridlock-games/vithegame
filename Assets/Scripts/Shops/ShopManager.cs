using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private GameObject[] panelManager;

    [SerializeField] private ShopCharacterModel[] characterModel;

    [SerializeField] private Text characterNameTMP;
    [SerializeField] private Text characterDescTMP;
    [SerializeField] private Image characterImage;
    [SerializeField] private Transform spawnLoc;
    
    private GameObject activeCharacter;
    private DataManager datamanager;
    private void Start()
    {
        datamanager = DataManager.Instance;
        PostShopAnalytics("Store");
        
    }

    public void TriggerMainPanel(string _name)
    {
        foreach (var item in panelManager)
        {
            item.SetActive(item.name == _name);
        }

        if (_name == "Character") return;
        PostShopAnalytics(_name);
    }

    public void DeleteActiveCharacter()
    {
        if (activeCharacter != null)
        {
            Destroy(activeCharacter);
        }
    }

    public void GetCharacter(string _id)
    {
        DeleteActiveCharacter();
        var _shopModel = new ShopCharacterModel();
        foreach (var item in characterModel)
        {
            if (item.id == _id)
            {
                _shopModel = item;
            }
        }
        characterNameTMP.text = _shopModel.characterName;
        characterDescTMP.text = _shopModel.characterDescription;
        // characterImage.sprite = _shopModel.characterImage;
        activeCharacter = Instantiate(_shopModel.characterObject, spawnLoc.position, Quaternion.identity);
        activeCharacter.transform.SetParent(spawnLoc.parent);
        activeCharacter.SetActive(true);
        PostShopAnalytics("Character", _shopModel.characterName);
    }

    public void PostShopAnalytics(string _panel, string _character = "0")
    {
        var _userdata = datamanager.Data;
        var _shopData = _userdata.shopAnalytics.FirstOrDefault(x => x.panelName == _panel);

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
            _userdata.shopAnalytics.Add(_panelData);
            datamanager.PostUserdata(_userdata,false);
            return;
        }

        var _index = _userdata.shopAnalytics.IndexOf(_shopData);
        _panelData.count += _shopData.count;
        if (_character != "0")
        {
            var _chardata  = _userdata.shopAnalytics[_index].character.FirstOrDefault(x => x.name == _character);
            if (_chardata == null)
            {
                _userdata.shopAnalytics[_index].character.Add(new UserModel.ShopAnalyticsModel.Characters
                {
                    name = _character,
                    count = 1
                });
            }
            else
            {
                var _charIndex = _userdata.shopAnalytics[_index].character.IndexOf(_chardata);
                _userdata.shopAnalytics[_index].character[_charIndex] = new UserModel.ShopAnalyticsModel.Characters
                {
                    name = _character,
                    count = _chardata.count + 1
                };
            }
        }
        else
        {
            _userdata.shopAnalytics[_index].character.Add(new UserModel.ShopAnalyticsModel.Characters
            {
                name = _character,
                count = 1
            });
        }
        _userdata.shopAnalytics[_index] = new UserModel.ShopAnalyticsModel
        {
            panelName = _panelData.panelName,
            dateTime_added = _panelData.dateTime_added,
            count = _panelData.count,
            character = _userdata.shopAnalytics[_index].character
        };

        datamanager.PostUserdata(_userdata, false);
    }
}
