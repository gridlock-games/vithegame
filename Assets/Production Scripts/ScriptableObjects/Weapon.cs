using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Weapon", menuName = "Production/Weapon")]
    public class Weapon : ScriptableObject
    {
        public enum WeaponBone
        {
            Root = -1,
            RightHand = HumanBodyBones.RightHand,
            LeftHand = HumanBodyBones.LeftHand,
            RightArm = HumanBodyBones.RightLowerArm,
            LeftArm = HumanBodyBones.LeftLowerArm,
            RightFoot = HumanBodyBones.RightFoot,
            LeftFoot = HumanBodyBones.LeftFoot,
            Camera = 100,
        }

        [System.Serializable]
        public class WeaponModelData
        {
            public GameObject skinPrefab;
            public Data[] data;

            [System.Serializable]
            public class Data
            {
                public GameObject weaponPrefab;
                public WeaponBone weaponBone = WeaponBone.RightHand;
                public Vector3 weaponPositionOffset;
                public Vector3 weaponRotationOffset;
            }
        }

        // 3d model:
        [SerializeField] private List<WeaponModelData> weaponModelData = new List<WeaponModelData>();

        public List<WeaponModelData> GetWeaponModelData() { return weaponModelData; }
    }
}