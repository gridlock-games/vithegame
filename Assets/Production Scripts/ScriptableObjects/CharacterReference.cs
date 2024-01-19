using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using Vi.Utility;
using System.Linq;

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
            public GameObject playerPrefab;
            public GameObject botPrefab;
            public GameObject[] skinOptions;
        }

        [System.Serializable]
        public class WeaponOption
        {
            public string name;
            public Sprite weaponIcon;
            public RuntimeAnimatorController animationController;
            public Weapon weapon;
            public string itemWebId;
        }

        [System.Serializable]
        public class CharacterMaterial
        {
            public MaterialApplicationLocation materialApplicationLocation;
            public RaceAndGender raceAndGender;
            public Material material;
            public Color averageTextureColor;

            public CharacterMaterial(string filePath, Material material, Color averageTextureColor)
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
                else if (material.name.Contains("Facials"))
                {
                    materialApplicationLocation = MaterialApplicationLocation.Brows;
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
                this.averageTextureColor = averageTextureColor;
            }
        }

        private static readonly List<EquipmentType> equipmentTypesThatAreForCharacterCustomization = new List<EquipmentType>()
        {
            EquipmentType.Beard,
            EquipmentType.Brows,
            EquipmentType.Hair,
        };


        [System.Serializable]
        public class WearableEquipmentOption : System.IEquatable<WearableEquipmentOption>
        {
            public string name;
            public Dictionary<RaceAndGender, WearableEquipment> models;
            public EquipmentType equipmentType;
            public Color averageTextureColor;
            public string itemWebId;
            public WearableEquipment[] wearableEquipmentOptions;

            public WearableEquipmentOption(WearableEquipment humanMaleWearableEquipmentPrefab, WearableEquipment humanFemaleWearableEquipmentPrefab,
                WearableEquipment orcMaleWearableEquipmentPrefab, WearableEquipment orcFemaleWearableEquipmentPrefab, Color averageTextureColor)
            {
                models = new Dictionary<RaceAndGender, WearableEquipment>
                {
                    { RaceAndGender.HumanMale, humanMaleWearableEquipmentPrefab },
                    { RaceAndGender.HumanFemale, humanFemaleWearableEquipmentPrefab },
                    { RaceAndGender.OrcMale, orcMaleWearableEquipmentPrefab },
                    { RaceAndGender.OrcFemale, orcFemaleWearableEquipmentPrefab }
                };
                wearableEquipmentOptions = System.Array.FindAll(models.Values.ToArray(), item => item != null);

                bool broken = false;
                foreach (EquipmentType type in System.Enum.GetValues(typeof(EquipmentType)))
                {
                    if (humanMaleWearableEquipmentPrefab.name.Contains(type.ToString()))
                    {
                        equipmentType = type;
                        broken = true;
                        break;
                    }
                }

                if (equipmentTypesThatAreForCharacterCustomization.Contains(equipmentType))
                {
                    name = models.First(item => item.Value != null).Value.name;
                }
                else
                {
                    string parsedName = humanMaleWearableEquipmentPrefab.name[^3] == '_' ? humanMaleWearableEquipmentPrefab.name[0..^3] : humanMaleWearableEquipmentPrefab.name;
                    name = parsedName.Replace("Hu_M_", "").Replace("_", " ");
                }

                if (!broken)
                {
                    Debug.LogError("Unknown equipment type!" + humanMaleWearableEquipmentPrefab.name);
                }

                this.averageTextureColor = averageTextureColor;
            }

            public WearableEquipmentOption(EquipmentType equipmentType)
            {
                this.equipmentType = equipmentType;
                averageTextureColor = Color.white;
            }

            public bool Equals(WearableEquipmentOption other)
            {
                return name == other.name & equipmentType == other.equipmentType;
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
            Eyes,
            Brows
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

        public KeyValuePair<int, int> GetPlayerModelOptionIndices(string characterModelName)
        {
            int characterIndex = System.Array.FindIndex(playerModelOptions, item => System.Array.FindIndex(item.skinOptions, skinItem => skinItem.name == characterModelName) != -1);
            if (characterIndex == -1) { return new KeyValuePair<int, int>(characterIndex, -1); }
            int skinIndex = System.Array.FindIndex(playerModelOptions[characterIndex].skinOptions, skinItem => skinItem.name == characterModelName);
            return new KeyValuePair<int, int>(characterIndex, skinIndex);
        }

        public WeaponOption[] GetWeaponOptions() { return weaponOptions; }

        public List<WearableEquipmentOption> GetWearableEquipmentOptions() { return equipmentOptions; }

        public List<CharacterMaterial> GetCharacterMaterialOptions(RaceAndGender raceAndGender) { return characterMaterialOptions.FindAll(item => item.raceAndGender == raceAndGender | item.raceAndGender == RaceAndGender.Universal); }

        # if UNITY_EDITOR
        [ContextMenu("Set Dirty")]
        private void SetDirtyAtWill()
        {
            EditorUtility.SetDirty(this);
        }

        [ContextMenu("Refresh Equipment List")]
        private void RefreshEquipmentList()
        {
            characterMaterialOptions.Clear();
            string[] filepaths = Directory.GetFiles(@"Assets\PackagedPrefabs\StylizedCharacter\Materials\Character", "*.mat", SearchOption.AllDirectories);
            foreach (string filepath in filepaths)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(filepath);
                if (material.name.Contains("Hair") | material.name.Contains("_UH_") | material.name.Contains("Body_Cloth")) { continue; }

                Texture2D texture2D = (Texture2D)material.GetTexture("_BaseMap");
                TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(texture2D));
                if (!importer.isReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }

                Color color = Color.clear;
                switch (material.name[^3..])
                {
                    case "_Bl":
                        color = Color.blue;
                        break;
                    case "_Br":
                        color = new Color(140 / 255f, 70 / 255f, 20 / 255f, 1);
                        break;
                    case "_Gn":
                        color = Color.green;
                        break;
                    case "_Pe":
                        color = new Color(145 / 255f, 25 / 255f, 145 / 255f, 1);
                        break;
                }
                CharacterMaterial characterMaterial = new CharacterMaterial(filepath, material, color == Color.clear ? AverageColorFromTexture(texture2D) : color);
                // Exclude brow materials from human male because the male model doesn't have brows by default
                if (characterMaterial.materialApplicationLocation == MaterialApplicationLocation.Brows & characterMaterial.raceAndGender == RaceAndGender.HumanMale) { continue; }
                characterMaterialOptions.Add(characterMaterial);
            }

            equipmentOptions.RemoveAll(item => string.IsNullOrEmpty(item.itemWebId));
            filepaths = Directory.GetFiles(@"Assets\PackagedPrefabs\StylizedCharacter\Prefabs", "*.prefab", SearchOption.AllDirectories);
            foreach (string filepath in filepaths)
            {
                string humanMaleString = "Hu_M_";
                string filename = Path.GetFileNameWithoutExtension(filepath);

                // Check that this filename is a human male
                if (filename.Substring(0, humanMaleString.Length) != humanMaleString) { continue; }

                // Check if this file is of an eligible equipment type
                bool isEquipment = false;
                foreach (EquipmentType equipmentType in System.Enum.GetValues(typeof(EquipmentType)))
                {
                    if (filename.Contains(equipmentType.ToString()))
                    {
                        isEquipment = true;
                        break;
                    }
                }
                if (!isEquipment) { continue; }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(filepath);
                if (!prefab.GetComponentInChildren<SkinnedMeshRenderer>()) { Debug.LogWarning("No skinned mesh renderer " + filepath); continue; }

                // Find paths for each race/gender prefab
                string humanMalePath = filepath;

                Dictionary<RaceAndGender, string> fileFinderDictionary = new Dictionary<RaceAndGender, string>()
                {
                    { RaceAndGender.HumanFemale, "Hu_F_" },
                    { RaceAndGender.OrcMale, "Or_M_" },
                    { RaceAndGender.OrcFemale, "Or_F_" }
                };

                string humanFemalePath = "";
                string orcMalePath = "";
                string orcFemalePath = "";

                foreach (KeyValuePair<RaceAndGender, string> kvp in fileFinderDictionary)
                {
                    string modifiedFilename = Path.GetFileNameWithoutExtension(humanMalePath).Replace(humanMaleString, kvp.Value);
                    string[] stringOptions = System.Array.FindAll(filepaths, item => LevenshteinDistance.Calculate(modifiedFilename, Path.GetFileNameWithoutExtension(item)) <= 0 & filename != Path.GetFileNameWithoutExtension(item));
                    System.Array.Sort(stringOptions, (x, y) => LevenshteinDistance.Calculate(Path.GetFileNameWithoutExtension(x), modifiedFilename).CompareTo(LevenshteinDistance.Calculate(Path.GetFileNameWithoutExtension(y), modifiedFilename)));

                    if (stringOptions.Length > 0)
                    {
                        switch (kvp.Key)
                        {
                            case RaceAndGender.HumanFemale:
                                humanFemalePath = stringOptions[0];
                                break;
                            case RaceAndGender.OrcMale:
                                orcMalePath = stringOptions[0];
                                break;
                            case RaceAndGender.OrcFemale:
                                orcFemalePath = stringOptions[0];
                                break;
                            default:
                                Debug.LogError("Race and gender not supported! " + kvp.Key);
                                break;
                        }
                    }
                }

                Dictionary<RaceAndGender, string> modelFilepaths = new Dictionary<RaceAndGender, string>()
                {
                    { RaceAndGender.HumanMale, humanMalePath },
                    { RaceAndGender.HumanFemale, humanFemalePath },
                    { RaceAndGender.OrcMale, orcMalePath },
                    { RaceAndGender.OrcFemale, orcFemalePath }
                };

                Dictionary<RaceAndGender, WearableEquipment> models = new Dictionary<RaceAndGender, WearableEquipment>();
                Texture2D humanMaleTexture = null;

                foreach (KeyValuePair<RaceAndGender, string> kvp in modelFilepaths)
                {
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(kvp.Value);
                    // Add wearable equipment component to prefab here if necessary
                    if (prefab)
                    {
                        if (!prefab.GetComponent<WearableEquipment>())
                        {
                            foreach (Component comp in prefab.GetComponents<Component>())
                            {
                                if (comp.GetType() == typeof(Transform)) { continue; }

                                DestroyImmediate(comp, true);
                            }
                            prefab.AddComponent<WearableEquipment>();
                        }

                        // Create the wearable equipment option object
                        if (prefab.TryGetComponent(out WearableEquipment wearableEquipment))
                        {
                            Texture2D texture2D = (Texture2D)wearableEquipment.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial.GetTexture("_BaseMap");
                            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(texture2D));
                            if (!importer.isReadable)
                            {
                                importer.isReadable = true;
                                importer.SaveAndReimport();
                            }

                            if (kvp.Key == RaceAndGender.HumanMale) { humanMaleTexture = texture2D; }

                            Transform[] children = wearableEquipment.GetComponentsInChildren<Transform>(true);
                            foreach (Transform child in children)
                            {
                                child.gameObject.layer = LayerMask.NameToLayer("Character");
                            }

                            models.Add(kvp.Key, wearableEquipment);
                        }
                    }
                    else
                    {
                        models.Add(kvp.Key, null);
                    }
                }

                WearableEquipmentOption wearableEquipmentOption = new WearableEquipmentOption(models[RaceAndGender.HumanMale], models[RaceAndGender.HumanFemale],
                    models[RaceAndGender.OrcMale], models[RaceAndGender.OrcFemale], AverageColorFromTexture(humanMaleTexture));

                bool allModelsFound = true;
                foreach (KeyValuePair<RaceAndGender, WearableEquipment> kvp in models)
                {
                    if (!equipmentTypesThatAreForCharacterCustomization.Contains(wearableEquipmentOption.equipmentType) & kvp.Value == null)
                    {
                        Debug.LogWarning(filename + " No prefab for " + kvp.Key);
                        allModelsFound = false;
                    }
                }

                if (allModelsFound & !equipmentOptions.Exists(item => item.Equals(wearableEquipmentOption)))
                {
                    equipmentOptions.Add(wearableEquipmentOption);
                }

                foreach (KeyValuePair<RaceAndGender, WearableEquipment> kvp in models)
                {
                    if (kvp.Value) { kvp.Value.equipmentType = wearableEquipmentOption.equipmentType; }
                }
            }
        }

        private Color32 AverageColorFromTexture(Texture2D tex)
        {
            Color32[] texColors = tex.GetPixels32();
            int total = texColors.Length;

            float r = 0;
            float g = 0;
            float b = 0;
            float a = 0;

            for (int i = 0; i < total; i++)
            {
                r += texColors[i].r;
                g += texColors[i].g;
                b += texColors[i].b;
                a += texColors[i].a;
            }

            return new Color32((byte)(r / total), (byte)(g / total), (byte)(b / total), (byte)(a / total));
        }
#endif
    }
}