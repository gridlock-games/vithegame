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
        public Sprite defaultEnvironmentKillIcon;
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
            public AnimatorOverrideController animationController;
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

        [System.Serializable]
        public class WearableEquipmentOption : System.IEquatable<WearableEquipmentOption>
        {
            public string name;
            public EquipmentType equipmentType;
            public string itemWebId;
            public Sprite equipmentIcon;
            [SerializeField] private List<RaceAndGender> raceAndGenders = new List<RaceAndGender>();
            [SerializeField] private List<WearableEquipment> wearableEquipmentOptions = new List<WearableEquipment>();

            public WearableEquipment GetModel(RaceAndGender raceAndGender, WearableEquipment emptyWearableEquipment)
            {
                int index = raceAndGenders.IndexOf(raceAndGender);
                if (index == -1) { return emptyWearableEquipment; }
                return wearableEquipmentOptions[index];
            }

            public WearableEquipmentOption(string name, EquipmentType equipmentType)
            {
                this.name = name;
                this.equipmentType = equipmentType;
            }

            public void AddModel(RaceAndGender raceAndGender, WearableEquipment wearableEquipment)
            {
                if (raceAndGenders.Contains(raceAndGender)) { return; }
                raceAndGenders.Add(raceAndGender);
                wearableEquipmentOptions.Add(wearableEquipment);
            }

            public WearableEquipmentOption(EquipmentType equipmentType)
            {
                this.equipmentType = equipmentType;
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

        public List<WearableEquipmentOption> GetArmorEquipmentOptions(RaceAndGender raceAndGender) { return equipmentOptions.FindAll(item => !equipmentTypesThatAreForCharacterCustomization.Contains(item.equipmentType) & item.GetModel(raceAndGender, null) != null); }

        public List<WearableEquipmentOption> GetAllArmorEquipmentOptions() { return equipmentOptions.FindAll(item => !equipmentTypesThatAreForCharacterCustomization.Contains(item.equipmentType)); }

        public List<WearableEquipmentOption> GetCharacterEquipmentOptions(RaceAndGender raceAndGender) { return equipmentOptions.FindAll(item => equipmentTypesThatAreForCharacterCustomization.Contains(item.equipmentType) & item.GetModel(raceAndGender, null) != null); }

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
            equipmentOptions.RemoveAll(item => string.IsNullOrEmpty(item.itemWebId));

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
                        if (filename.Contains("Cloth"))
                        {
                            materialName = "M_Cloth";
                        }
                        else if (filename.Contains("Armor"))
                        {
                            materialName = "M_Armor";
                        }
                        else
                        {
                            Debug.LogError("Not sure how to handle texture path - " + filename);
                            continue;
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
                                    Debug.LogError("Not sure how to handle material - " + skinnedMeshRenderer.sharedMaterials[i]);
                                    newMaterials[i] = null;
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

                            WearableEquipmentOption wearableEquipmentOption = new WearableEquipmentOption(armorSetName + " " + wearableEquipment.name, wearableEquipment.equipmentType);
                            if (!equipmentOptions.Exists(item => item.Equals(wearableEquipmentOption))) { equipmentOptions.Add(wearableEquipmentOption); }

                            string prefabVariantPath = Path.Join(dest, Path.GetFileNameWithoutExtension(modelFilePath) + ".prefab");
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
        #endif
    }
}