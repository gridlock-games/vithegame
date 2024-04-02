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
        [SerializeField] private WearableEquipment emptyWearableEquipment;
        [SerializeField] private List<WearableEquipmentOption> equipmentOptions;
        [SerializeField] private List<CharacterMaterial> characterMaterialOptions;

        public WearableEquipment GetEmptyWearableEquipment() { return emptyWearableEquipment; }

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
            public Sprite killFeedIcon;
            public RuntimeAnimatorController animationController;
            public Weapon weapon;
            public GameObject weaponPreviewPrefab;
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

        public static readonly List<EquipmentType> equipmentTypesThatAreForCharacterCustomization = new List<EquipmentType>()
        {
            EquipmentType.Beard,
            EquipmentType.Brows,
            EquipmentType.Hair,
        };

        private static readonly Dictionary<RaceAndGender, string> raceAndGenderPrefixes = new Dictionary<RaceAndGender, string>()
        {
            { RaceAndGender.HumanMale, "Hu_M_" },
            { RaceAndGender.HumanFemale, "Hu_F_" },
            { RaceAndGender.OrcMale, "Or_M_" },
            { RaceAndGender.OrcFemale, "Or_F_" }
        };

        private static EquipmentType GetEquipmentTypeFromFilename(string filename)
        {
            EquipmentType equipmentType = EquipmentType.Beard;
            bool broken = false;
            foreach (EquipmentType type in System.Enum.GetValues(typeof(EquipmentType)))
            {
                if (filename.Contains(type.ToString()))
                {
                    equipmentType = type;
                    broken = true;
                    break;
                }
            }
            if (!broken)
            {
                Debug.LogError("Unknown equipment type! " + filename);
            }
            return equipmentType;
        }

        private static RaceAndGender GetRaceAndGenderFromFilename(string filename)
        {
            RaceAndGender raceAndGender = RaceAndGender.HumanMale;
            bool broken = false;
            foreach (KeyValuePair<RaceAndGender, string> kvp in raceAndGenderPrefixes)
            {
                if (filename.Contains(kvp.Value))
                {
                    raceAndGender = kvp.Key;
                    broken = true;
                    break;
                }
            }
            if (!broken)
            {
                Debug.LogError("Unknown race and gender! " + filename);
            }
            return raceAndGender;
        }

        [System.Serializable]
        public class WearableEquipmentOption : System.IEquatable<WearableEquipmentOption>
        {
            public string name;
            public EquipmentType equipmentType;
            public Color averageTextureColor;
            public string itemWebId;
            [SerializeField] private RaceAndGender[] raceAndGenders = new RaceAndGender[0];
            [SerializeField] private WearableEquipment[] wearableEquipmentOptions = new WearableEquipment[0];

            public WearableEquipment GetModel(RaceAndGender raceAndGender, WearableEquipment emptyWearableEquipment)
            {
                int index = System.Array.IndexOf(raceAndGenders, raceAndGender);
                if (index == -1) { return emptyWearableEquipment; }
                return wearableEquipmentOptions[index];
            }

            public WearableEquipmentOption(Dictionary<RaceAndGender, WearableEquipment> models, Color averageTextureColor)
            {
                equipmentType = GetEquipmentTypeFromFilename(models[RaceAndGender.HumanMale].name);

                if (equipmentTypesThatAreForCharacterCustomization.Contains(equipmentType))
                {
                    Debug.LogError("This constructor should only be used for armor types!");
                }

                string parsedName = models[RaceAndGender.HumanMale].name[^3] == '_' ? models[RaceAndGender.HumanMale].name[0..^3] : models[RaceAndGender.HumanMale].name;
                name = parsedName.Replace("Hu_M_", "").Replace("_", " ");

                this.averageTextureColor = averageTextureColor;
                raceAndGenders = models.Keys.ToArray();
                wearableEquipmentOptions = models.Values.ToArray();
            }

            public WearableEquipmentOption(WearableEquipment wearableEquipmentPrefab, Color averageTextureColor)
            {
                RaceAndGender raceAndGender = GetRaceAndGenderFromFilename(wearableEquipmentPrefab.name);

                equipmentType = GetEquipmentTypeFromFilename(wearableEquipmentPrefab.name);

                if (!equipmentTypesThatAreForCharacterCustomization.Contains(equipmentType))
                {
                    Debug.LogError("This constructor should only be used for customization types!");
                }

                Dictionary<RaceAndGender, WearableEquipment> models = new Dictionary<RaceAndGender, WearableEquipment>();
                foreach (RaceAndGender rg in System.Enum.GetValues(typeof(RaceAndGender)))
                {
                    if (rg == raceAndGender)
                    {
                        models.Add(rg, wearableEquipmentPrefab);
                    }
                    else
                    {
                        models.Add(rg, null);
                    }
                }

                name = wearableEquipmentPrefab.name;

                this.averageTextureColor = averageTextureColor;
                raceAndGenders = models.Keys.ToArray();
                wearableEquipmentOptions = models.Values.ToArray();
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

        public List<WearableEquipmentOption> GetArmorEquipmentOptions() { return equipmentOptions.FindAll(item => !equipmentTypesThatAreForCharacterCustomization.Contains(item.equipmentType)); }

        public List<WearableEquipmentOption> GetCharacterEquipmentOptions(RaceAndGender raceAndGender) { return equipmentOptions.FindAll(item => equipmentTypesThatAreForCharacterCustomization.Contains(item.equipmentType) & item.GetModel(raceAndGender, GetEmptyWearableEquipment()) != null); }

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

            // Non-character customization equipment options (armor)
            equipmentOptions.RemoveAll(item => string.IsNullOrEmpty(item.itemWebId));
            filepaths = Directory.GetFiles(@"Assets\PackagedPrefabs\StylizedCharacter\Prefabs", "*.prefab", SearchOption.AllDirectories);
            foreach (string filepath in filepaths)
            {
                string filename = Path.GetFileNameWithoutExtension(filepath);
                string humanMaleString = "Hu_M_";

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

                if (equipmentTypesThatAreForCharacterCustomization.Contains(GetEquipmentTypeFromFilename(filename))) { continue; }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(filepath);
                if (!prefab.GetComponentInChildren<SkinnedMeshRenderer>()) { Debug.LogWarning("No skinned mesh renderer " + filepath); continue; }

                // Find paths for each race/gender prefab
                string humanMalePath = filepath;

                string humanFemalePath = "";
                string orcMalePath = "";
                string orcFemalePath = "";

                foreach (KeyValuePair<RaceAndGender, string> kvp in raceAndGenderPrefixes)
                {
                    if (kvp.Key == RaceAndGender.HumanMale) { continue; }

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

                WearableEquipmentOption wearableEquipmentOption = new WearableEquipmentOption(models, AverageColorFromTexture(humanMaleTexture));

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

            // Character customization equipment
            foreach (string filepath in filepaths)
            {
                string filename = Path.GetFileNameWithoutExtension(filepath);

                // Check if this file is of an eligible equipment type
                bool isEquipment = false;
                foreach (EquipmentType equipmentType in System.Enum.GetValues(typeof(EquipmentType)))
                {
                    if (Path.GetFileNameWithoutExtension(filepath).Contains(equipmentType.ToString()))
                    {
                        isEquipment = true;
                        break;
                    }
                }

                if (!isEquipment) { continue; }

                if (!equipmentTypesThatAreForCharacterCustomization.Contains(GetEquipmentTypeFromFilename(filename))) { continue; }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(filepath);
                if (!prefab.GetComponentInChildren<SkinnedMeshRenderer>()) { continue; }

                // Add wearable equipment component to prefab here if necessary
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

                    Transform[] children = wearableEquipment.GetComponentsInChildren<Transform>(true);
                    foreach (Transform child in children)
                    {
                        child.gameObject.layer = LayerMask.NameToLayer("Character");
                    }

                    WearableEquipmentOption wearableEquipmentOption = new WearableEquipmentOption(wearableEquipment, AverageColorFromTexture(texture2D));
                    if (!equipmentOptions.Exists(item => item.Equals(wearableEquipmentOption))) { equipmentOptions.Add(wearableEquipmentOption); }
                    wearableEquipment.equipmentType = wearableEquipmentOption.equipmentType;
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