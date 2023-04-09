using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameCreator.Melee
{ 
    [CreateAssetMenu(fileName = "WeaponModelSO", menuName = "Game Creator/Melee/WeaponModel")]
    public class WeaponModel : ScriptableObject
    {
        public List<WeaponModelData> weaponModelDatas = new List<WeaponModelData>();
    }

    [Serializable]
    public class WeaponModelData
    {
        public GameObject prefabWeapon;
        public MeleeWeapon.WeaponBone attachmentWeapon = MeleeWeapon.WeaponBone.RightHand;
        public Vector3 positionOffsetWeapon;
        public Vector3 rotationOffsetWeapon;
    }
}
