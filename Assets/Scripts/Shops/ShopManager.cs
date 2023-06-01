using System.Security.Cryptography;
using TMPro;
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

    public void TriggerMainPanel(string _name)
    {
        foreach (var item in panelManager)
        {
            item.SetActive(item.name == _name);
        }
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
    }


}
