using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ShopCharacterModel
{
    public string id;
    public string characterName;
    public string characterDescription;
    public Sprite characterImage;
    public GameObject characterObject;
}

[Serializable]
public class CreateCharacterModel
{
    public string id;
    public string characterName;
    public string characterDescription;
    public Sprite characterImage;
    public GameObject characterObject;
}

