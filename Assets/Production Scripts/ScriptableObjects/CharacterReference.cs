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
                this.name = name;
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
                if (equipmentIcons.Count != raceAndGenders.Count)
                {
                    equipmentIcons = new List<Sprite>();
                    for (int i = 0; i < raceAndGenders.Count; i++)
                    {
                        equipmentIcons.Add(null);
                    }
                }

                int index = raceAndGenders.IndexOf(raceAndGender);
                if (index == -1) { Debug.LogError("Index is -1"); return; }

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
        [ContextMenu("Create Equipment From Universal Material")]
        private void CreateEquipmentFromUniversalMaterial()
        {
            string[] materialNamesToSearchFor = new string[]
            {
                "M_Pants_NArcher_U_Bl"
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

                            if (AssetDatabase.AssetPathExists(@"Assets\Production\Prefabs\WearableEquipment\" + loadedPrefab.name + ".prefab")) { continue; }

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

                            TraverseHierarchy(instance.transform);

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

                            if (!AssetDatabase.IsValidFolder(@"Assets\Production\Prefabs\WearableEquipment\" + groupName)) { AssetDatabase.CreateFolder(@"Assets\Production\Prefabs\WearableEquipment", groupName); }

                            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, @"Assets\Production\Prefabs\WearableEquipment\" + groupName + @"\" + instance.name + ".prefab");

                            var option = new WearableEquipmentOption(groupName, groupName, wearableEquipment.equipmentType);
                            option.AddModel(raceAndGender, prefab.GetComponent<WearableEquipment>());

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
                "Hu_F_Boots_NArcher_Bl",
                "Hu_F_Belt_NArcher_Bl",
                "Hu_F_Cape_NArcher_Bl",
                "Hu_F_Chest_NArcher_Bl"
            };

            foreach (string prefabName in prefabNamesToSearchFor)
            {
                string[] results = Directory.GetFiles(@"Assets\PackagedPrefabs\MODEL_CHAR_StylizedCharacter\Prefabs", prefabName + ".prefab", SearchOption.AllDirectories);
                if (results.Length == 0)
                {
                    Debug.LogError("Could not find prefab for name " + prefabName);
                }
                else if (results.Length == 1)
                {
                    GameObject loadedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(results[0]);

                    if (AssetDatabase.AssetPathExists(@"Assets\Production\Prefabs\WearableEquipment\" + loadedPrefab.name + ".prefab")) { continue; }

                    GameObject instance = Instantiate(loadedPrefab);
                    instance.name = instance.name.Replace("(Clone)", "");

                    foreach (Behaviour component in instance.GetComponents<Behaviour>())
                    {
                        DestroyImmediate(component);
                    }

                    instance.AddComponent<PooledObject>();
                    WearableEquipment wearableEquipment = instance.AddComponent<WearableEquipment>();
                    foreach (EquipmentType equipmentType in System.Enum.GetValues(typeof(EquipmentType)))
                    {
                        if (instance.name.ToUpper().Contains(equipmentType.ToString().ToUpper()))
                        {
                            wearableEquipment.equipmentType = equipmentType;
                            break;
                        }
                    }

                    TraverseHierarchy(instance.transform);

                    RaceAndGender raceAndGender = RaceAndGender.Universal;
                    string groupName = "";
                    if (instance.name.Contains("Hu_M_"))
                    {
                        raceAndGender = RaceAndGender.HumanMale;
                        groupName = instance.name.Replace("Hu_M_", "").Replace(wearableEquipment.equipmentType.ToString(), "").Replace("_", "");
                    }
                    else if (instance.name.Contains("Hu_F_"))
                    {
                        raceAndGender = RaceAndGender.HumanFemale;
                        groupName = instance.name.Replace("Hu_F_", "").Replace(wearableEquipment.equipmentType.ToString(), "").Replace("_", "");
                    }

                    if (!AssetDatabase.IsValidFolder(@"Assets\Production\Prefabs\WearableEquipment\" + groupName)) { AssetDatabase.CreateFolder(@"Assets\Production\Prefabs\WearableEquipment", groupName); }

                    GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, @"Assets\Production\Prefabs\WearableEquipment\" + groupName + @"\" + instance.name + ".prefab");

                    var option = new WearableEquipmentOption(groupName, groupName, wearableEquipment.equipmentType);
                    option.AddModel(raceAndGender, prefab.GetComponent<WearableEquipment>());

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

        private void TraverseHierarchy(Transform root)
        {
            root.gameObject.layer = LayerMask.NameToLayer("Character");
            foreach (Transform child in root)
            {
                child.gameObject.layer = LayerMask.NameToLayer("Character");
                TraverseHierarchy(child);
            }
        }

        [ContextMenu("Set Dirty")]
        private void SetDirtyAtWill()
        {
            EditorUtility.SetDirty(this);
        }

        [ContextMenu("Refresh Equipment List")]
        private void RefreshEquipmentList()
        {
            equipmentOptions.RemoveAll(item => string.IsNullOrEmpty(item.itemWebId) & !equipmentTypesThatAreForCharacterCustomization.Contains(item.equipmentType));

            // Clone Folder Structure first
            string destinationTopFolder = @"Assets\Production\Prefabs\WearableEquipment";
            foreach (string raceAndGenderFolder in Directory.GetDirectories(@"Assets\PackagedPrefabs\Vi_Character"))
            {
                if (raceAndGenderFolder.Contains("CharacterModels")) { continue; }

                RaceAndGender raceAndGender = RaceAndGender.HumanMale;
                if (raceAndGenderFolder.Contains("HumanMale"))
                {
                    raceAndGender = RaceAndGender.HumanMale;
                }
                else if (raceAndGenderFolder.Contains("HumanFemale"))
                {
                    raceAndGender = RaceAndGender.HumanFemale;
                }
                else
                {
                    Debug.LogError("Unsure how to handle path - " + raceAndGenderFolder);
                    continue;
                }

                foreach (string armorSetFolder in Directory.GetDirectories(raceAndGenderFolder))
                {
                    string armorSetName = armorSetFolder[(armorSetFolder.LastIndexOf('\\') + 1)..].Replace("_", " ").Replace("Armor", "")[1..].Trim();
                    Dictionary<string, string> materialDictionary = new Dictionary<string, string>();

                    foreach (string textureFilePath in Directory.GetFiles(Path.Join(armorSetFolder, "Texture"), "*.png", SearchOption.TopDirectoryOnly))
                    {
                        string materialName = "";
                        string filename = Path.GetFileNameWithoutExtension(textureFilePath);
                        if (filename.Contains("Cloth") | filename.Contains("cloth"))
                        {
                            materialName = "M_Cloth";
                        }
                        else if (filename.Contains("Armor") | filename.Contains("armor"))
                        {
                            materialName = "M_Armor";
                        }
                        else
                        {
                            Debug.LogWarning("Not sure how to handle texture path - " + filename);
                            materialName = "M_Armor";
                        }

                        if (filename.Contains("Normal"))
                        {
                            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(textureFilePath);
                            importer.textureType = TextureImporterType.NormalMap;
                            importer.SaveAndReimport();
                        }

                        string materialFilePath = Path.Join(Path.Join(armorSetFolder, "Texture"), materialName + ".mat");

                        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureFilePath);

                        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialFilePath);
                        bool materialAlreadyExists = material != null;
                        if (!materialAlreadyExists) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); }

                        // Render both faces if this is a cloth material
                        if (materialName == "M_Cloth") { material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off); }

                        if (filename.Contains("Albedo"))
                        {
                            material.mainTexture = texture;
                        }
                        else if (filename.Contains("Emission"))
                        {
                            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                            material.SetTexture("_EmissionMap", texture);
                        }
                        else if (filename.Contains("Metallic"))
                        {
                            material.SetTexture("_MetallicGlossMap", texture);
                        }
                        else if (filename.Contains("Normal"))
                        {
                            material.SetTexture("_BumpMap", texture);
                        }
                        else
                        {
                            Debug.LogError("Not sure where to assign texture - " + filename);
                        }

                        if (!materialAlreadyExists) { AssetDatabase.CreateAsset(material, materialFilePath); }

                        if (!materialDictionary.ContainsKey(materialName)) { materialDictionary.Add(materialName, materialFilePath); }
                    }

                    foreach (string modelFilePath in Directory.GetFiles(Path.Join(armorSetFolder, "Mesh"), "*.fbx", SearchOption.TopDirectoryOnly))
                    {
                        string dest = Path.Join(Path.Join(destinationTopFolder, raceAndGender.ToString()), armorSetName);
                        string prefabVariantPath = Path.Join(dest, Path.GetFileNameWithoutExtension(modelFilePath) + ".prefab");
                        if (File.Exists(prefabVariantPath)) { continue; }

                        Directory.CreateDirectory(dest);

                        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelFilePath);
                        GameObject modelSource = (GameObject)PrefabUtility.InstantiatePrefab(model);

                        foreach (SkinnedMeshRenderer skinnedMeshRenderer in modelSource.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            Material[] newMaterials = new Material[skinnedMeshRenderer.sharedMaterials.Length];
                            for (int i = 0; i < skinnedMeshRenderer.sharedMaterials.Length; i++)
                            {
                                if (skinnedMeshRenderer.sharedMaterials[i].name.Contains("Cloth"))
                                {
                                    newMaterials[i] = AssetDatabase.LoadAssetAtPath<Material>(materialDictionary["M_Cloth"]);
                                }
                                else if (skinnedMeshRenderer.sharedMaterials[i].name.Contains("Armor"))
                                {
                                    newMaterials[i] = AssetDatabase.LoadAssetAtPath<Material>(materialDictionary["M_Armor"]);
                                }
                                else
                                {
                                    Debug.LogWarning("Not sure how to handle material - " + skinnedMeshRenderer.sharedMaterials[i]);
                                    newMaterials[i] = AssetDatabase.LoadAssetAtPath<Material>(materialDictionary["M_Armor"]);
                                }
                            }
                            skinnedMeshRenderer.sharedMaterials = newMaterials;
                        }

                        WearableEquipment wearableEquipment = modelSource.AddComponent<WearableEquipment>();
                        wearableEquipment.equipmentType = System.Enum.Parse<EquipmentType>(modelSource.name);

                        try
                        {
                            Texture2D texture2D = (Texture2D)wearableEquipment.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial.GetTexture("_BaseMap");
                            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture2D));
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

                            WearableEquipmentOption wearableEquipmentOption = new WearableEquipmentOption(armorSetName + " " + wearableEquipment.name, "", wearableEquipment.equipmentType);
                            if (!equipmentOptions.Exists(item => item.Equals(wearableEquipmentOption))) { equipmentOptions.Add(wearableEquipmentOption); }

                            PrefabUtility.SaveAsPrefabAsset(modelSource, prefabVariantPath);
                            DestroyImmediate(modelSource);

                            int index = equipmentOptions.FindIndex(item => item.Equals(wearableEquipmentOption));
                            if (index == -1)
                            {
                                Debug.LogError("Couldn't find wearable equipment option " + wearableEquipmentOption.name);
                            }
                            else
                            {
                                equipmentOptions[index].AddModel(raceAndGender, AssetDatabase.LoadAssetAtPath<GameObject>(prefabVariantPath).GetComponent<WearableEquipment>());
                            }
                        }
                        catch
                        {
                            Debug.LogError("Error while importing " + modelFilePath);
                        }
                    }
                }
            }
            AssetDatabase.SaveAssets();
        }

        private const string armorIconFolder = @"Assets\Production\Images\Equipment Icons";
        
        [ContextMenu("Assign Equipment Icons")]
        private void AssignEquipmentIcons()
        {
            foreach (string armorIconPath in Directory.GetFiles(armorIconFolder, "*.png", SearchOption.TopDirectoryOnly))
            {
                string filename = Path.GetFileNameWithoutExtension(armorIconPath);

                string[] splitString = filename.Split('-');

                int equipmentOptionIndex = equipmentOptions.FindIndex(item => item.name == splitString[0]);
                if (equipmentOptionIndex == -1) { Debug.LogError("Equipment Option Index is -1"); continue; }
                RaceAndGender raceAndGender = System.Enum.Parse<RaceAndGender>(splitString[1]);

                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(armorIconPath);
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();

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