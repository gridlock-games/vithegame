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
            public RaceAndGender raceAndGender;
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
            public EquipmentType equipmentType;
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

                bool broken = false;
                foreach (EquipmentType type in System.Enum.GetValues(typeof(EquipmentType)))
                {
                    if (wearableEquipmentPrefab.name.Contains(type.ToString()))
                    {
                        equipmentType = type;
                        broken = true;
                        break;
                    }
                }

                if (!broken)
                {
                    Debug.LogError("Unknown equipment type!" + wearableEquipmentPrefab.name);
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

        public enum EquipmentType
        {
            Belt,
            Boots,
            Cape,
            Chest,
            Gloves,
            Helm,
            Pants,
            Robe,
            Shoulders,
            Beard,
            Brows,
            Hair
        }

        public PlayerModelOption[] GetPlayerModelOptions() { return playerModelOptions; }

        public WeaponOption[] GetWeaponOptions() { return weaponOptions; }

        public List<WearableEquipmentOption> GetWearableEquipmentOptions(RaceAndGender raceAndGender) { return equipmentOptions.FindAll(item => item.raceAndGender == raceAndGender | item.raceAndGender == RaceAndGender.Universal); }

        public List<CharacterMaterial> GetCharacterMaterialOptions(RaceAndGender raceAndGender) { return characterMaterialOptions.FindAll(item => item.raceAndGender == raceAndGender | item.raceAndGender == RaceAndGender.Universal); }

        # if UNITY_EDITOR
        [ContextMenu("Refresh Equipment List")]
        private void RefreshEquipmentList()
        {
            equipmentOptions.Clear();
            string[] filepaths = Directory.GetFiles(@"Assets\PackagedPrefabs\StylizedCharacter\Prefabs", "*.prefab", SearchOption.AllDirectories);
            foreach (string filepath in filepaths)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(filepath).TryGetComponent(out WearableEquipment wearableEquipment))
                {
                    Texture2D texture2D = (Texture2D)wearableEquipment.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial.GetTexture("_BaseMap");
                    TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(texture2D));
                    if (!importer.isReadable)
                    {
                        importer.isReadable = true;
                        importer.SaveAndReimport();
                    }

                    Transform[] children = wearableEquipment.GetComponentsInChildren<Transform>(true);
                    foreach (Transform child in children)
                    {
                        child.gameObject.layer = LayerMask.NameToLayer("Character");
                    }

                    equipmentOptions.Add(new WearableEquipmentOption(wearableEquipment));
                }
            }

            characterMaterialOptions.Clear();
            filepaths = Directory.GetFiles(@"Assets\PackagedPrefabs\StylizedCharacter\Materials\Character", "*.mat", SearchOption.AllDirectories);
            foreach (string filepath in filepaths)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(filepath);
                if (material.name.Contains("Hair") | material.name.Contains("_UH_") | material.name.Contains("_Facials_") | material.name.Contains("Body_Cloth")) { continue; }

                Texture2D texture2D = (Texture2D)material.GetTexture("_BaseMap");
                TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(texture2D));
                if (!importer.isReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }
                
                characterMaterialOptions.Add(new CharacterMaterial(filepath, material));
            }
        }
        # endif
    }
}