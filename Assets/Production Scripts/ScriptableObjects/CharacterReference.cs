using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;

namespace Vi.ScriptableObjects
{
    [CreateAssetMenu(fileName = "CharacterReference", menuName = "Production/Character Reference")]
    public class CharacterReference : ScriptableObject
    {
        [SerializeField] private PlayerModelOption[] playerModelOptions;
        [SerializeField] private WeaponOption[] weaponOptions;
        [SerializeField] private List<WearableEquipmentOption> equipmentOptions;
        [SerializeField] private List<CharacterMaterial> characterMaterialOptions;

        [System.Serializable]
        public class PlayerModelOption
        {
            public string name;
            public Weapon weapon;
            public string role;
            public string characterDescription = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.";
            public Sprite characterImage;
            public GameObject playerPrefab;
            public GameObject botPrefab;
            public GameObject[] skinOptions;
        }

        [System.Serializable]
        public class WeaponOption
        {
            public RuntimeAnimatorController animationController;
            public Weapon weapon;
        }

        [System.Serializable]
        public class CharacterMaterial
        {
            public MaterialApplicationLocation materialApplicationLocation;
            public RaceAndGender raceAndGender;
            public Material material;

            public CharacterMaterial(string filePath, Material material)
            {
                if (material.name.Contains("Eye"))
                {
                    materialApplicationLocation = MaterialApplicationLocation.Eyes;
                    raceAndGender = RaceAndGender.Universal;
                }
                else if (material.name.Contains("Body"))
                {
                    materialApplicationLocation = MaterialApplicationLocation.Body;
                }
                else if (material.name.Contains("Head"))
                {
                    materialApplicationLocation = MaterialApplicationLocation.Head;
                }
                else
                {
                    Debug.LogError("Unknown material application location! " + material.name);
                }

                if (filePath.Contains("Human") & material.name[2..].Contains("M_"))
                {
                    raceAndGender = RaceAndGender.HumanMale;
                }
                else if (filePath.Contains("Human") & material.name[2..].Contains("F_"))
                {
                    raceAndGender = RaceAndGender.HumanFemale;
                }
                else if (filePath.Contains("Orc") & material.name[2..].Contains("M_"))
                {
                    raceAndGender = RaceAndGender.OrcMale;
                }
                else if (filePath.Contains("Orc") & material.name[2..].Contains("F_"))
                {
                    raceAndGender = RaceAndGender.OrcFemale;
                }
                else if (raceAndGender == default)
                {
                    raceAndGender = RaceAndGender.HumanMale;
                    Debug.LogError("Unknown race and gender! " + material.name);
                }

                this.material = material;
            }
        }

        [System.Serializable]
        public class WearableEquipmentOption
        {
            public RaceAndGender raceAndGender;
            public WearableEquipment wearableEquipmentPrefab;

            public WearableEquipmentOption(WearableEquipment wearableEquipmentPrefab)
            {
                if (wearableEquipmentPrefab.name.Contains("Hu_M_"))
                {
                    raceAndGender = RaceAndGender.HumanMale;
                }
                else if (wearableEquipmentPrefab.name.Contains("Hu_F_"))
                {
                    raceAndGender = RaceAndGender.HumanFemale;
                }
                else if (wearableEquipmentPrefab.name.Contains("Or_M_"))
                {
                    raceAndGender = RaceAndGender.OrcMale;
                }
                else if (wearableEquipmentPrefab.name.Contains("Or_F_"))
                {
                    raceAndGender = RaceAndGender.OrcFemale;
                }
                else
                {
                    raceAndGender = RaceAndGender.HumanMale;
                    Debug.LogError("Unknown race and gender! " + wearableEquipmentPrefab.name);
                }
                this.wearableEquipmentPrefab = wearableEquipmentPrefab;
            }
        }

        public enum RaceAndGender
        {
            HumanMale,
            HumanFemale,
            OrcMale,
            OrcFemale,
            Universal
        }

        public enum MaterialApplicationLocation
        {
            Body,
            Head,
            Eyes
        }

        public PlayerModelOption[] GetPlayerModelOptions() { return playerModelOptions; }

        public WeaponOption[] GetWeaponOptions() { return weaponOptions; }

        public List<WearableEquipmentOption> GetWearableEquipmentOptions(RaceAndGender raceAndGender) { return equipmentOptions.FindAll(item => item.raceAndGender == raceAndGender | item.raceAndGender == RaceAndGender.Universal); }

        public List<CharacterMaterial> GetCharacterMaterialOptions(RaceAndGender raceAndGender) { return characterMaterialOptions.FindAll(item => item.raceAndGender == raceAndGender | item.raceAndGender == RaceAndGender.Universal); }

        [ContextMenu("Refresh Equipment List")]
        private void RefreshEquipmentList()
        {
            equipmentOptions.Clear();
            string[] filepaths = Directory.GetFiles(@"Assets\PackagedPrefabs\StylizedCharacter\Prefabs", "*.prefab", SearchOption.AllDirectories);
            foreach (string filepath in filepaths)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(filepath).TryGetComponent(out WearableEquipment wearableEquipment))
                {
                    equipmentOptions.Add(new WearableEquipmentOption(wearableEquipment));
                }
            }

            characterMaterialOptions.Clear();
            filepaths = Directory.GetFiles(@"Assets\PackagedPrefabs\StylizedCharacter\Materials\Character", "*.mat", SearchOption.AllDirectories);
            foreach (string filepath in filepaths)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(filepath);
                if (material.name.Contains("Hair") | material.name.Contains("_UH_") | material.name.Contains("_Facials_")) { continue; }
                characterMaterialOptions.Add(new CharacterMaterial(filepath, material));
            }
        }
    }
}