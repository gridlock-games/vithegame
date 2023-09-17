using System;
using System.Collections;
using System.Collections.Generic;
using GameCreator.Melee;
using UnityEngine;

[CreateAssetMenu(fileName = "Melee Weapon Collection", menuName = "Game Creator/Melee/Melee Weapon Collection")]
public class WeaponMeleeSO : ScriptableObject
{
    public List<WeaponData> weaponCollections = new List<WeaponData>();
}

[Serializable]
public class WeaponData
{
    public MeleeWeapon meleeWeapon;
    public WeaponType weaponType;
}

public enum WeaponType
{
    GREATSWORD,
    LANCE,
    HAMMER,
    SWORD,
    DAGGER,
    BRAWLER,
    CROSSBOW
}