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
        public GameObject PlayerPrefab { get { return playerPrefab; } }
        [SerializeField] private GameObject playerPrefab;
        public GameObject BotPrefab { get { return botPrefab; } }
        [SerializeField] private GameObject botPrefab;
        public WearableEquipment EmptyWearableEquipment { get { return emptyWearableEquipment; } }
        [SerializeField] private WearableEquipment emptyWearableEquipment;

        [SerializeField] private PlayerModelOption[] playerModelOptions;
        [SerializeField] private WeaponOption[] weaponOptions;
        [SerializeField] private List<WearableEquipmentOption> equipmentOptions;
        [SerializeField] private List<CharacterMaterial> characterMaterialOptions;

        [System.Serializable]
        public class PlayerModelOption
        {
            public RaceAndGender raceAndGender;
            public GameObject model;
        }

        [System.Serializable]
        public class WeaponOption
        {
            public string name;
            public string itemWebId;
            public bool isBasicGear;
            public Sprite weaponIcon;
            public Sprite killFeedIcon;
            public AnimatorOverrideController animationController;
            public Weapon weapon;
            public GameObject weaponPreviewPrefab;
        }

        [System.Serializable]
        public class CharacterMaterial : System.IEquatable<CharacterMaterial>
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

            public bool Equals(CharacterMaterial other)
            {
                return material == other.material;
            }
        }

        public static readonly List<EquipmentType> equipmentTypesThatAreForCharacterCustomization = new List<EquipmentType>()
        {
            EquipmentType.Beard,
            EquipmentType.Brows,
            EquipmentType.Hair,
        };

        [System.Serializable]
        public class WearableEquipmentOption : System.IEquatable<WearableEquipmentOption>
        {
            public string name;
            public string groupName;
            public string itemWebId;
            public bool isBasicGear;
            public EquipmentType equipmentType;
            [SerializeField] private List<RaceAndGender> raceAndGenders = new List<RaceAndGender>();
            [SerializeField] private List<WearableEquipment> wearableEquipmentOptions = new List<WearableEquipment>();
            [SerializeField] private List<Sprite> equipmentIcons = new List<Sprite>();

            public int GetTotalModelCount()
            {
                return wearableEquipmentOptions.Count(item => item != null);
            }

            public WearableEquipment GetModel(RaceAndGender raceAndGender, WearableEquipment emptyWearableEquipment)
            {
                int index = raceAndGenders.IndexOf(raceAndGender);
                if (index == -1) { return emptyWearableEquipment; }
                return wearableEquipmentOptions[index];
            }

            public Sprite GetIcon(RaceAndGender raceAndGender)
            {
                int index = raceAndGenders.IndexOf(raceAndGender);
                if (index == -1 | index >= equipmentIcons.Count) { return null; }
                return equipmentIcons[index];
            }

            public WearableEquipmentOption(string name, string groupName, EquipmentType equipmentType)
            {
                this.name = name + " " + equipmentType.ToString();
                this.groupName = groupName;
                this.equipmentType = equipmentType;
            }

            public void AddModel(RaceAndGender raceAndGender, WearableEquipment wearableEquipment)
            {
                if (raceAndGenders.Contains(raceAndGender)) { return; }
                raceAndGenders.Add(raceAndGender);
                wearableEquipmentOptions.Add(wearableEquipment);
            }

            public void AddIcon(RaceAndGender raceAndGender, Sprite icon)
            {
                if (!icon) { Debug.LogError("Icon is null"); }

                int index = raceAndGenders.IndexOf(raceAndGender);
                if (index == -1) { Debug.LogError("Race and gender index is -1"); return; }

                if (equipmentIcons.Count != raceAndGenders.Count)
                {
                    equipmentIcons = new List<Sprite>();
                    for (int i = 0; i < raceAndGenders.Count; i++)
                    {
                        equipmentIcons.Add(null);
                    }
                }

                equipmentIcons[index] = icon;
            }

            public WearableEquipmentOption(EquipmentType equipmentType)
            {
                this.equipmentType = equipmentType;
            }

            public bool Equals(WearableEquipmentOption other)
            {
                return groupName == other.groupName & equipmentType == other.equipmentType;
            }
        }

        public enum RaceAndGender
        {
            HumanMale,
            HumanFemale,
            OrcMale,
            OrcFemale,
            Universal,
            MonsterHuge,
            MonsterInfernal,
            MonsterLargeA,
            MonsterLargeB,
            MonsterMediumA,
            MonsterMediumB,
            MonsterSkeleton,
            MonsterSmallA,
            MonsterSmallB,
            MonsterSpider,
            MonsterSwamp,
            MonsterZombie
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

        public PlayerModelOption GetCharacterModel(RaceAndGender raceAndGender)
        {
            return System.Array.Find(playerModelOptions, item => item.raceAndGender == raceAndGender);
        }

        public WeaponOption[] GetWeaponOptions() { return weaponOptions; }

        private Dictionary<string, WeaponOption> cachedWeaponDictionary;

        private void RefreshCachedWeaponDictionary()
        {
            cachedWeaponDictionary = new Dictionary<string, WeaponOption>();
            foreach (WeaponOption weaponOption in weaponOptions)
            {
                cachedWeaponDictionary.Add(weaponOption.itemWebId, weaponOption);
            }
        }

        public Dictionary<string, WeaponOption> GetWeaponOptionsDictionary()
        {
            if (cachedWeaponDictionary == null)
            {
                RefreshCachedWeaponDictionary();
            }
            return cachedWeaponDictionary;
        }

        public List<WearableEquipmentOption> GetArmorEquipmentOptions(RaceAndGender raceAndGender) { return equipmentOptions.FindAll(item => !equipmentTypesThatAreForCharacterCustomization.Contains(item.equipmentType) & item.GetModel(raceAndGender, null) != null); }

        public List<WearableEquipmentOption> GetAllArmorEquipmentOptions() { return equipmentOptions.FindAll(item => !equipmentTypesThatAreForCharacterCustomization.Contains(item.equipmentType)); }

        public List<WearableEquipmentOption> GetCharacterEquipmentOptions(RaceAndGender raceAndGender) { return equipmentOptions.FindAll(item => equipmentTypesThatAreForCharacterCustomization.Contains(item.equipmentType) & item.GetModel(raceAndGender, null) != null); }

        public List<CharacterMaterial> GetCharacterMaterialOptions(RaceAndGender raceAndGender) { return characterMaterialOptions.FindAll(item => item.raceAndGender == raceAndGender | item.raceAndGender == RaceAndGender.Universal); }

# if UNITY_EDITOR
        [ContextMenu("Clean Empty Entries")]
        private void CleanEmptyEntries()
        {
            Debug.Log("Removed " + equipmentOptions.RemoveAll(item => item.GetTotalModelCount() == 0) + " equipment options");
            Debug.Log("Removed " + characterMaterialOptions.RemoveAll(item => item.material == null) + " character material options");
        }

        [ContextMenu("Create Character Material Option From Material")]
        private void CreateCharacterMaterialOptionFromMaterial()
        {
            string[] materialNamesToSearchFor = new string[]
            {
                "M_HuF_Body_01",
                "M_HuF_Head_01_A",
                "M_HuF_Body_02",
                "M_HuF_Head_02_A",
                "M_HuF_Body_03",
                "M_HuF_Head_03_A",
                "M_HuF_Body_04",
                "M_HuF_Head_04_A",
                "M_HuF_Body_05",
                "M_HuF_Head_05_A",

                "M_HuM_Body_01",
                "M_HuM_Head_01_A",
                "M_HuM_Body_02",
                "M_HuM_Head_02_A",
                "M_HuM_Body_03",
                "M_HuM_Head_03_A",
                "M_HuM_Body_04",
                "M_HuM_Head_04_A",
                "M_HuM_Body_05",
                "M_HuM_Head_05_A",

                "M_Eye_Br",
                "M_Eye_Bl",
                "M_Eye_Gn",
                "M_Eye_Pe",

                "M_HuF_Facials_Bd",
                "M_HuF_Facials_Bk",
                "M_HuF_Facials_Br",
                "M_HuF_Facials_Gr"
            };

            foreach (string materialName in materialNamesToSearchFor)
            {
                string[] results = Directory.GetFiles(@"Assets\PackagedPrefabs\MODEL_CHAR_StylizedCharacter\Materials", materialName + ".mat", SearchOption.AllDirectories);
                if (results.Length == 0)
                {
                    Debug.LogError("Could not find material for name " + materialName);
                }
                else if (results.Length == 1)
                {
                    Material loadedMaterial = AssetDatabase.LoadAssetAtPath<Material>(results[0]);

                    Texture baseMap = loadedMaterial.GetTexture("_BaseMap");
                    TextureImporter textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(baseMap)) as TextureImporter;
                    if (!textureImporter.isReadable)
                    {
                        textureImporter.isReadable = true;
                        textureImporter.SaveAndReimport();
                    }

                    CharacterMaterial characterMaterial = new CharacterMaterial(results[0], loadedMaterial, AverageColorFromTexture((Texture2D)baseMap));
                    if (!characterMaterialOptions.Exists(item => item.Equals(characterMaterial))) { characterMaterialOptions.Add(characterMaterial); }
                }
                else
                {
                    Debug.LogError("More than one result for material name " + materialName);
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

        [ContextMenu("Create Equipment From Universal Material")]
        private void CreateEquipmentFromUniversalMaterial()
        {
            string[] materialNamesToSearchFor = new string[]
            {
                "M_Pants_NArcher_U_Bl",
                "M_Pants_NRanger_U_Bl"
            };

            foreach (string materialName in materialNamesToSearchFor)
            {
                if (!materialName.Contains("_U_")) { Debug.LogError("This is not a universal material! " + materialName); continue; }

                string[] results = Directory.GetFiles(@"Assets\PackagedPrefabs\MODEL_CHAR_StylizedCharacter\Materials", materialName + ".mat", SearchOption.AllDirectories);
                if (results.Length == 0)
                {
                    Debug.LogError("Could not find material for name " + materialName);
                }
                else if (results.Length == 1)
                {
                    Material loadedMaterial = AssetDatabase.LoadAssetAtPath<Material>(results[0]);
                    EquipmentType equipmentType = default;
                    foreach (EquipmentType et in System.Enum.GetValues(typeof(EquipmentType)))
                    {
                        if (loadedMaterial.name.ToUpper().Contains(et.ToString().ToUpper()))
                        {
                            equipmentType = et;
                            break;
                        }
                    }

                    if (equipmentType == EquipmentType.Pants)
                    {
                        string[] basePrefabPaths = new string[]
                        {
                            @"Assets\PackagedPrefabs\MODEL_CHAR_StylizedCharacter\Prefabs\Item\Equipment\Pants\Hu_F_Pants.prefab",
                            @"Assets\PackagedPrefabs\MODEL_CHAR_StylizedCharacter\Prefabs\Item\Equipment\Pants\Hu_M_Pants.prefab"
                        };

                        foreach (string basePrefabPath in basePrefabPaths)
                        {
                            GameObject loadedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);

                            GameObject instance = Instantiate(loadedPrefab);
                            instance.name = instance.name.Replace("(Clone)", "");

                            foreach (Behaviour component in instance.GetComponents<Behaviour>())
                            {
                                DestroyImmediate(component);
                            }

                            foreach (SkinnedMeshRenderer smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                            {
                                smr.sharedMaterial = loadedMaterial;
                            }

                            instance.AddComponent<PooledObject>();
                            WearableEquipment wearableEquipment = instance.AddComponent<WearableEquipment>();
                            wearableEquipment.equipmentType = equipmentType;

                            AssignCharacterLayerRecursively(instance.transform);

                            RaceAndGender raceAndGender = RaceAndGender.Universal;
                            string groupName = "";
                            if (instance.name.Contains("Hu_M_"))
                            {
                                raceAndGender = RaceAndGender.HumanMale;
                            }
                            else if (instance.name.Contains("Hu_F_"))
                            {
                                raceAndGender = RaceAndGender.HumanFemale;
                            }

                            groupName = materialName.Replace("M_Pants_", "").Replace("_U_", "");

                            if (AssetDatabase.AssetPathExists(@"Assets\Production\Prefabs\WearableEquipment\" + groupName + @"\" + instance.name + ".prefab"))
                            {
                                DestroyImmediate(instance);
                                continue;
                            }

                            if (!AssetDatabase.IsValidFolder(@"Assets\Production\Prefabs\WearableEquipment\" + groupName)) { AssetDatabase.CreateFolder(@"Assets\Production\Prefabs\WearableEquipment", groupName); }

                            wearableEquipment.shouldDisableCharSkinRenderer = wearableEquipment.equipmentType == EquipmentType.Pants
                                | wearableEquipment.equipmentType == EquipmentType.Chest
                                | wearableEquipment.equipmentType == EquipmentType.Helm;
                            foreach (SkinnedMeshRenderer smr in wearableEquipment.GetRenderList())
                            {
                                if (smr.name.ToLower().Contains("_body") | smr.name.ToLower().Contains("_naked"))
                                {
                                    smr.tag = WearableEquipment.equipmentBodyMaterialTag;
                                    wearableEquipment.shouldDisableCharSkinRenderer = true;
                                }
                            }

                            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, @"Assets\Production\Prefabs\WearableEquipment\" + groupName + @"\" + instance.name + ".prefab");

                            var option = new WearableEquipmentOption(groupName, groupName, wearableEquipment.equipmentType);
                            option.AddModel(raceAndGender, prefab.GetComponent<WearableEquipment>());
                            option.isBasicGear = true;

                            if (equipmentOptions.Exists(item => item.Equals(option)))
                            {
                                equipmentOptions.Find(item => item.Equals(option)).AddModel(raceAndGender, prefab.GetComponent<WearableEquipment>());
                            }
                            else
                            {
                                equipmentOptions.Add(option);
                            }
                            UnityEditor.EditorUtility.SetDirty(this);

                            DestroyImmediate(instance);
                        }
                    }
                    else
                    {
                        Debug.LogError("Unsure what to do with universal material of equipment type " + equipmentType);
                    }
                }
                else
                {
                    Debug.LogError("More than one result for material name " + materialName);
                }
            }
        }

        [ContextMenu("Create Equipment From Prefab")]
        private void CreateEquipmentFromPrefab()
        {
            string[] prefabNamesToSearchFor = new string[]
            {
                "Hu_M_Boots_NArcher_Bl",
                "Hu_M_Belt_NArcher_Bl",
                "Hu_M_Cape_NArcher_Bl",
                "Hu_M_Chest_NArcher_Bl",
                "Hu_M_Gloves_NArcher",

                "Hu_F_Boots_NArcher_Bl",
                "Hu_F_Belt_NArcher_Bl",
                "Hu_F_Cape_NArcher_Bl",
                "Hu_F_Chest_NArcher_Bl",
                "Hu_F_Gloves_NArcher",

                "Hu_M_Helm_NMage_Gn",
                "Hu_M_Shoulders_NMage_Gn",
                "Hu_M_Cape_NMage_Gn",
                "Hu_M_Gloves_NMage_Gn",
                "Hu_M_Belt_NMage_Gn",
                "Hu_M_Robe_NMage_Gn",
                "Hu_M_Boots_NMage_01_Gn",
                "Hu_M_Chest_NMage",

                "Hu_F_Helm_NMage_Gn",
                "Hu_F_Shoulders_NMage_Gn",
                "Hu_F_Cape_NMage_Gn",
                "Hu_F_Gloves_NMage_Gn",
                "Hu_F_Belt_NMage_Gn",
                "Hu_F_Robe_NMage_Gn",
                "Hu_F_Boots_NMage_01_Gn",
                "Hu_F_Chest_NMage",

                "Hu_M_Helm_NRanger_Bl",
                "Hu_M_Shoulders_NRanger_Bl",
                "Hu_M_Chest_NRanger_Bl",
                "Hu_M_Cape_NRanger_Bl",
                "Hu_M_Gloves_NRanger_Bl",
                "Hu_M_Belt_NRanger_Bl",
                "Hu_M_Boots_NRanger_Bl",

                "Hu_F_Helm_NRanger_Bl",
                "Hu_F_Shoulders_NRanger_Bl",
                "Hu_F_Chest_NRanger_Bl",
                "Hu_F_Cape_NRanger_Bl",
                "Hu_F_Gloves_NRanger_Bl",
                "Hu_F_Belt_NRanger_Bl",
                "Hu_F_Boots_NRanger_Bl",

                "Hu_M_Shoulders_NWarrior_Rd",
                "Hu_M_Helm_NWarrior_Rd",
                "Hu_M_Chest_NWarrior_Rd",
                "Hu_M_Cape_NWarrior_Rd",
                "Hu_M_Gloves_NWarrior_Rd",
                "Hu_M_Belt_NWarrior_Rd",
                "Hu_M_Pants_NWarrior_Rd",
                "Hu_M_Boots_NWarrior_Rd",

                "Hu_F_Shoulders_NWarrior_Rd",
                "Hu_F_Helm_NWarrior_Rd",
                "Hu_F_Chest_NWarrior_Rd",
                "Hu_F_Cape_NWarrior_Rd",
                "Hu_F_Gloves_NWarrior_Rd",
                "Hu_F_Belt_NWarrior_Rd",
                "Hu_F_Pants_NWarrior_Rd",
                "Hu_F_Boots_NWarrior_Rd",

                "Hu_M_Helm_DungPlate_Pe",
                "Hu_M_Shoulders_DungPlate_Pe",
                "Hu_M_Chest_DungPlate_Pe",
                "Hu_M_Cape_DungPlate_Pe",
                "Hu_M_Gloves_DungPlate_Pe",
                "Hu_M_Belt_DungPlate_Pe",
                "Hu_M_Pants_DungPlate_Pe",
                "Hu_M_Boots_DungPlate_Pe",

                "Hu_F_Helm_DungPlate_Pe",
                "Hu_F_Shoulders_DungPlate_Pe",
                "Hu_F_Chest_DungPlate_Pe",
                "Hu_F_Cape_DungPlate_Pe",
                "Hu_F_Gloves_DungPlate_Pe",
                "Hu_F_Belt_DungPlate_Pe",
                "Hu_F_Pants_DungPlate_Pe",
                "Hu_F_Boots_DungPlate_Pe"
            };

            string[] folderPathsToAppend = new string[]
            {
                @"Assets\PackagedPrefabs\MODEL_CHAR_StylizedCharacter\Prefabs\Character\Human\Female\Customization\Hair",
                @"Assets\PackagedPrefabs\MODEL_CHAR_StylizedCharacter\Prefabs\Character\Human\Female\Customization\Brows",
                @"Assets\PackagedPrefabs\MODEL_CHAR_StylizedCharacter\Prefabs\Character\Human\Male\Customization\Facial",
                @"Assets\PackagedPrefabs\MODEL_CHAR_StylizedCharacter\Prefabs\Character\Human\Male\Customization\Hair"
            };

            List<string> finalPrefabNameList = new List<string>();
            finalPrefabNameList.AddRange(prefabNamesToSearchFor);
            foreach (string folderPath in folderPathsToAppend)
            {
                foreach (string file in Directory.GetFiles(folderPath, "*.prefab", SearchOption.TopDirectoryOnly))
                {
                    finalPrefabNameList.Add(file[(file.LastIndexOf(@"\") + 1)..^0].Replace(".prefab", ""));
                }
            }

            foreach (string prefabName in finalPrefabNameList)
            {
                string[] results = Directory.GetFiles(@"Assets\PackagedPrefabs\MODEL_CHAR_StylizedCharacter\Prefabs", prefabName + ".prefab", SearchOption.AllDirectories);
                if (results.Length == 0)
                {
                    Debug.LogError("Could not find prefab for name " + prefabName);
                }
                else if (results.Length == 1)
                {
                    GameObject loadedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(results[0]);

                    GameObject instance = Instantiate(loadedPrefab);
                    instance.name = instance.name.Replace("(Clone)", "");

                    foreach (Behaviour component in instance.GetComponents<Behaviour>())
                    {
                        DestroyImmediate(component);
                    }

                    instance.AddComponent<PooledObject>();
                    WearableEquipment wearableEquipment = instance.AddComponent<WearableEquipment>();
                    EquipmentType equipmentTypeInName = default;
                    foreach (EquipmentType equipmentType in System.Enum.GetValues(typeof(EquipmentType)))
                    {
                        if (instance.name.ToUpper().Contains(equipmentType.ToString().ToUpper()))
                        {
                            wearableEquipment.equipmentType = equipmentType;
                            equipmentTypeInName = equipmentType;
                            if (equipmentType == EquipmentType.Robe) { wearableEquipment.equipmentType = EquipmentType.Pants; }
                            break;
                        }
                    }

                    AssignCharacterLayerRecursively(instance.transform);

                    RaceAndGender raceAndGender = RaceAndGender.Universal;
                    string groupName = "";
                    if (instance.name.Contains("Hu_M_"))
                    {
                        raceAndGender = RaceAndGender.HumanMale;
                        groupName = instance.name.Replace("Hu_M_", "").Replace(equipmentTypeInName.ToString(), "").Replace("_", "");
                    }
                    else if (instance.name.Contains("Hu_F_"))
                    {
                        raceAndGender = RaceAndGender.HumanFemale;
                        groupName = instance.name.Replace("Hu_F_", "").Replace(equipmentTypeInName.ToString(), "").Replace("_", "");
                    }

                    if (wearableEquipment.equipmentType == EquipmentType.Brows & raceAndGender != RaceAndGender.HumanMale)
                    {
                        DestroyImmediate(instance);
                        continue;
                    }

                    if (AssetDatabase.AssetPathExists(@"Assets\Production\Prefabs\WearableEquipment\" + groupName + @"\" + instance.name + ".prefab"))
                    {
                        DestroyImmediate(instance);
                        continue;
                    }

                    if (!AssetDatabase.IsValidFolder(@"Assets\Production\Prefabs\WearableEquipment\" + groupName)) { AssetDatabase.CreateFolder(@"Assets\Production\Prefabs\WearableEquipment", groupName); }

                    wearableEquipment.shouldDisableCharSkinRenderer = wearableEquipment.equipmentType == EquipmentType.Pants;
                    foreach (SkinnedMeshRenderer smr in wearableEquipment.GetRenderList())
                    {
                        if (smr.name.ToLower().Contains("_body") | smr.name.ToLower().Contains("_naked"))
                        {
                            smr.tag = WearableEquipment.equipmentBodyMaterialTag;
                            wearableEquipment.shouldDisableCharSkinRenderer = true;
                        }
                    }

                    GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, @"Assets\Production\Prefabs\WearableEquipment\" + groupName + @"\" + instance.name + ".prefab");

                    var option = new WearableEquipmentOption(groupName, groupName, wearableEquipment.equipmentType);
                    option.AddModel(raceAndGender, prefab.GetComponent<WearableEquipment>());
                    option.isBasicGear = true;

                    if (equipmentOptions.Exists(item => item.Equals(option)))
                    {
                        equipmentOptions.Find(item => item.Equals(option)).AddModel(raceAndGender, prefab.GetComponent<WearableEquipment>());
                    }
                    else
                    {
                        equipmentOptions.Add(option);
                    }
                    UnityEditor.EditorUtility.SetDirty(this);

                    DestroyImmediate(instance);
                }
                else
                {
                    Debug.LogError("More than one result for prefab name " + prefabName);
                }
            }
        }

        private void AssignCharacterLayerRecursively(Transform root)
        {
            root.gameObject.layer = LayerMask.NameToLayer("Character");
            foreach (Transform child in root)
            {
                child.gameObject.layer = LayerMask.NameToLayer("Character");
                AssignCharacterLayerRecursively(child);
            }
        }

        [ContextMenu("Set Dirty")]
        private void SetDirtyAtWill()
        {
            EditorUtility.SetDirty(this);
        }

        [ContextMenu("Assign Equipment Icons")]
        private void AssignEquipmentIcons()
        {
            foreach (string armorIconPath in Directory.GetFiles(@"Assets\Production\Images\Equipment Icons", "*.png", SearchOption.TopDirectoryOnly))
            {
                string filename = Path.GetFileNameWithoutExtension(armorIconPath);

                string[] splitString = filename.Split('-');

                int equipmentOptionIndex = equipmentOptions.FindIndex(item => item.name == splitString[0]);
                if (equipmentOptionIndex == -1) { Debug.LogError("Equipment Option Index is -1"); continue; }
                RaceAndGender raceAndGender = System.Enum.Parse<RaceAndGender>(splitString[1]);

                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(armorIconPath);
                if (importer.textureType != TextureImporterType.Sprite | importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                }

                equipmentOptions[equipmentOptionIndex].AddIcon(raceAndGender, AssetDatabase.LoadAssetAtPath<Sprite>(armorIconPath));
            }
        }

        [ContextMenu("Assign Animations On Weapons")]
        private void AssignAnimationsOnWeapons()
        {
            foreach (WeaponOption weaponOption in weaponOptions)
            {
                weaponOption.weapon.FindAnimations();
            }
        }
#endif
    }
}