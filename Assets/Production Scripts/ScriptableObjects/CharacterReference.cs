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
            public GameObject playerPrefab;
            public GameObject botPrefab;
            public GameObject[] skinOptions;
        }

        [System.Serializable]
        public class WeaponOption
        {
            public Sprite weaponIcon;
            public RuntimeAnimatorController animationController;
            public Weapon weapon;
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

        [System.Serializable]
        public class WearableEquipmentOption
        {
            public RaceAndGender raceAndGender;
            public EquipmentType equipmentType;
            public WearableEquipment wearableEquipmentPrefab;
            public Color averageTextureColor;

            public WearableEquipmentOption(WearableEquipment wearableEquipmentPrefab, Color averageTextureColor)
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
                this.averageTextureColor = averageTextureColor;
            }

            public WearableEquipmentOption(EquipmentType equipmentType, Color averageTextureColor)
            {
                this.equipmentType = equipmentType;
                raceAndGender = RaceAndGender.HumanMale;
                wearableEquipmentPrefab = null;
                this.averageTextureColor = averageTextureColor;
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

        public List<WearableEquipmentOption> GetWearableEquipmentOptions(RaceAndGender raceAndGender) { return equipmentOptions.FindAll(item => item.raceAndGender == raceAndGender | item.raceAndGender == RaceAndGender.Universal); }

        public List<CharacterMaterial> GetCharacterMaterialOptions(RaceAndGender raceAndGender) { return characterMaterialOptions.FindAll(item => item.raceAndGender == raceAndGender | item.raceAndGender == RaceAndGender.Universal); }

        # if UNITY_EDITOR
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

            equipmentOptions.Clear();
            filepaths = Directory.GetFiles(@"Assets\PackagedPrefabs\StylizedCharacter\Prefabs", "*.prefab", SearchOption.AllDirectories);
            foreach (string filepath in filepaths)
            {
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

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(filepath);
                if (!prefab.GetComponentInChildren<SkinnedMeshRenderer>()) { continue; }

                // Add wearable equipment component to prefab here if necessary
                if (!prefab.GetComponent<WearableEquipment>())
                {
                    Debug.Log(Path.GetFileNameWithoutExtension(filepath));
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
                    equipmentOptions.Add(wearableEquipmentOption);
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