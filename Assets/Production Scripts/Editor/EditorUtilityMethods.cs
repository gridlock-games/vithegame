using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vi.Core;
using Vi.Core.VFX;
using Vi.Core.CombatAgents;
using Vi.Core.MeshSlicing;
using Vi.Core.Weapons;
using Vi.ScriptableObjects;
using Vi.Utility;
using UnityEngine.U2D;
using UnityEditor.U2D;

namespace Vi.Editor
{
    public class EditorUtilityMethods : UnityEditor.Editor
    {
        [MenuItem("Tools/Clear Progress Bar")]
        static void ClearProgressBar()
        {
            EditorUtility.ClearProgressBar();
        }

        [MenuItem("Tools/Unload Unused Assets")]
        static void UnloadUnusedAssets()
        {
            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        #region
        private static Attributes[] GetBotAndPlayerPrefabs()
        {
            List<Attributes> returnedList = new List<Attributes>();
            returnedList.Add(AssetDatabase.LoadAssetAtPath<GameObject>(@"Assets\Production\Prefabs\Bot.prefab").GetComponent<Attributes>());
            returnedList.Add(AssetDatabase.LoadAssetAtPath<GameObject>(@"Assets\Production\Prefabs\Player.prefab").GetComponent<Attributes>());
            return returnedList.ToArray();
        }

        private static Mob[] GetMobPrefabs()
        {
            List<Mob> mobList = new List<Mob>();
            foreach (string filePath in Directory.GetFiles(@"Assets\Production\Prefabs\Mobs", "*.prefab", SearchOption.AllDirectories))
            {
                mobList.Add(AssetDatabase.LoadAssetAtPath<GameObject>(filePath).GetComponent<Mob>());
            }
            return mobList.ToArray();
        }

        // Loading Scriptable Object Methods
        private static string networkPrefabListFolderPath = @"Assets\Production";
        static List<NetworkPrefabsList> GetNetworkPrefabsLists()
        {
            List<NetworkPrefabsList> networkPrefabsLists = new List<NetworkPrefabsList>();
            foreach (string listPath in Directory.GetFiles(networkPrefabListFolderPath, "*.asset", SearchOption.AllDirectories))
            {
                var prefabsList = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(listPath);
                if (prefabsList) { networkPrefabsLists.Add(prefabsList); }
            }
            return networkPrefabsLists;
        }

        private static string characterReferenceAssetPath = @"Assets\Production\CharacterReference.asset";
        static CharacterReference GetCharacterReference()
        {
            return AssetDatabase.LoadAssetAtPath<CharacterReference>(characterReferenceAssetPath);
        }

        private static string pooledObjectListAssetPath = @"Assets\Production\PooledObjectList.asset";
        static PooledObjectList GetPooledObjectList()
        {
            return AssetDatabase.LoadAssetAtPath<PooledObjectList>(pooledObjectListAssetPath);
        }

        private static List<ActionClip> GetActionClips()
        {
            List<ActionClip> actionClips = new List<ActionClip>();
            string actionClipFolder = @"Assets/Production/Actions";
            foreach (string actionClipFilePath in Directory.GetFiles(actionClipFolder, "*.asset", SearchOption.AllDirectories))
            {
                ActionClip actionClip = AssetDatabase.LoadAssetAtPath<ActionClip>(actionClipFilePath);
                if (actionClip)
                {
                    actionClips.Add(actionClip);
                    foreach (ActionVFX vfx in actionClip.actionVFXList)
                    {
                        if (!vfx)
                        {
                            Debug.LogError(actionClip + " has a null VFX reference " + actionClipFilePath);
                        }
                    }

                    foreach (ActionClip.StatusPayload statusPayload in actionClip.statusesToApplyToTeammateOnHit)
                    {
                        if (statusPayload.associatedWithCurrentWeapon)
                        {
                            Debug.Log(actionClip + " " + statusPayload.status + " " + actionClipFilePath);
                        }
                    }
                }
            }
            return actionClips;
        }

        [MenuItem("Tools/Mass Edit Action Clips")]
        private static void MassEditActionClips()
        {
            foreach (ActionClip actionClip in GetActionClips())
            {
                actionClip.armorPenetration = 0;
                EditorUtility.SetDirty(actionClip);
            }
            AssetDatabase.SaveAssets();
        }

        #endregion

        #region
        // Build Sanity Check
        [MenuItem("Tools/Production/0.Perform Build Sanity Check")]
        static void PerformBuildSanityCheck()
        {
            SetTextureImportOverrides();
            SetAudioCompressionSettings();
            SetVideoClipImportOverrides();
            GenerateDroppedWeaponVariants();
            RemoveComponentsFromWeaponPreviews();
            SetActionVFXLayers();
            ValidatePooledObjectsList();
            //ValidateNetworkPrefabsLists();
            AssetDatabase.SaveAssets();
            Debug.Log("REMEMBER TO CHECK AND ORGANIZE YOUR ADDRESSABLE GROUPS");
        }

        [MenuItem("Tools/Production/Set Audio Compression Settings")]
        private static void SetAudioCompressionSettings()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(@"Assets/Production/Prefabs/Networking/AudioManager.prefab");

            BuildTargetGroup[] buildTargetGroups = new BuildTargetGroup[]
            {
                BuildTargetGroup.Standalone,
                BuildTargetGroup.Android,
                BuildTargetGroup.iOS
            };

            if (prefab.TryGetComponent(out AudioManager audioManager))
            {
                string[] guids = AssetDatabase.FindAssets("t:audioclip");
                int counter = -1;
                foreach (string guid in guids)
                {
                    counter += 1;
                    if (EditorUtility.DisplayCancelableProgressBar("Setting audio clip compression settings",
                        counter.ToString() + " out of " + guids.Length.ToString() + " audio clips completed",
                        (float)counter / guids.Length))
                    {
                        break;
                    }

                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    AudioImporter audioImporter = AssetImporter.GetAtPath(assetPath) as AudioImporter;
                    if (audioImporter)
                    {
                        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                        bool shouldReimport = false;
                        if (!audioImporter.loadInBackground)
                        {
                            audioImporter.loadInBackground = true;
                            shouldReimport = true;
                        }

                        if (clip)
                        {
                            foreach (BuildTargetGroup buildTargetGroup in buildTargetGroups)
                            {
                                AudioImporterSampleSettings sampleSettings;
                                if (clip.length > 10) // Music/Long Audio Clips
                                {
                                    sampleSettings = new AudioImporterSampleSettings()
                                    {
                                        compressionFormat = AudioCompressionFormat.Vorbis,
                                        loadType = AudioClipLoadType.Streaming,
                                        preloadAudioData = false,
                                        quality = 1,
                                        sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate
                                    };
                                }
                                else if (clip.name.ToUpper().Contains("FOOT")
                                    | clip.name.ToUpper().Contains("IMPACT")
                                    | clip.name.ToUpper().Contains("HIT")
                                    | clip.name.ToUpper().Contains("WEAPON")
                                    | clip.name.ToUpper().Contains("WPN")
                                    | clip.name.ToUpper().Contains("WHOOSH")
                                    | clip.name.ToUpper().Contains("PUNCH")
                                    | clip.name.ToUpper().Contains("ARMOR")
                                    | clip.name.ToUpper().Contains("ARMOUR")) // Frequently played sounds
                                {
                                    sampleSettings = new AudioImporterSampleSettings()
                                    {
                                        compressionFormat = AudioCompressionFormat.ADPCM,
                                        loadType = AudioClipLoadType.DecompressOnLoad,
                                        preloadAudioData = true,
                                        quality = 1,
                                        sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate
                                    };
                                }
                                else
                                {
                                    sampleSettings = new AudioImporterSampleSettings()
                                    {
                                        compressionFormat = AudioCompressionFormat.PCM,
                                        loadType = AudioClipLoadType.DecompressOnLoad,
                                        preloadAudioData = true,
                                        quality = 1,
                                        sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate
                                    };
                                }

                                if (buildTargetGroup == BuildTargetGroup.Android
                                    | buildTargetGroup == BuildTargetGroup.iOS)
                                {
                                    sampleSettings.quality = 0.7f;
                                }

                                AudioImporterSampleSettings existingSettings = audioImporter.GetOverrideSampleSettings(buildTargetGroup);

                                if (existingSettings.compressionFormat != sampleSettings.compressionFormat
                                    | existingSettings.loadType != sampleSettings.loadType
                                    | existingSettings.preloadAudioData != sampleSettings.preloadAudioData
                                    | existingSettings.quality != sampleSettings.quality
                                    | existingSettings.sampleRateSetting != sampleSettings.sampleRateSetting)
                                {
                                    audioImporter.SetOverrideSampleSettings(buildTargetGroup, sampleSettings);
                                    shouldReimport = true;
                                }
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Couldn't get audio clip at path: " + assetPath);
                        }
                        
                        if (shouldReimport)
                        {
                            audioImporter.SaveAndReimport();
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Couldn't get audio importer at path: " + assetPath);
                    }
                }
            }
            else
            {
                Debug.LogError("Audio Manager is null");
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        [MenuItem("Tools/Production/Set Video Clip Import Overrides")]
        static void SetVideoClipImportOverrides()
        {
            string[] videoClips = AssetDatabase.FindAssets("t:VideoClip");
            for (int i = 0; i < videoClips.Length; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Setting video clip import settings",
                    i.ToString() + " out of " + videoClips.Length.ToString() + " video clips completed",
                    (float)i / videoClips.Length))
                {
                    break;
                }

                string assetPath = AssetDatabase.GUIDToAssetPath(videoClips[i]);
                if (assetPath.Contains("com.unity.")) { continue; }
                if (assetPath.Length == 0) { Debug.LogError(videoClips[i] + " not found"); continue; }

                VideoClipImporter importer = (VideoClipImporter)AssetImporter.GetAtPath(assetPath);

                bool shouldReimport = false;
                if (importer.importAudio)
                {
                    importer.importAudio = false;
                    shouldReimport = true;
                }
                
                VideoImporterTargetSettings defaultSettings = importer.defaultTargetSettings;
                if (!defaultSettings.enableTranscoding)
                {
                    defaultSettings.enableTranscoding = true;
                    importer.defaultTargetSettings = defaultSettings;
                    shouldReimport = true;
                }
                
                if (importer.GetTargetSettings("Android").spatialQuality != VideoSpatialQuality.MediumSpatialQuality)
                {
                    VideoImporterTargetSettings androidSettings = defaultSettings;
                    androidSettings.spatialQuality = VideoSpatialQuality.MediumSpatialQuality;
                    importer.SetTargetSettings("Android", androidSettings);
                    shouldReimport = true;
                }

                if (importer.GetTargetSettings("iPhone").spatialQuality != VideoSpatialQuality.MediumSpatialQuality)
                {
                    VideoImporterTargetSettings iPhoneSettings = defaultSettings;
                    iPhoneSettings.spatialQuality = VideoSpatialQuality.MediumSpatialQuality;
                    importer.SetTargetSettings("iPhone", iPhoneSettings);
                    shouldReimport = true;
                }
                
                if (shouldReimport)
                {
                    importer.SaveAndReimport();
                }
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        [MenuItem("Tools/Production/Set Texture Import Overrides")]
        static void SetTextureImportOverrides()
        {
            string[] textures = AssetDatabase.FindAssets("t:Texture");//, new string[] { @"Assets\Production\Images\Map Icons\Mobile version Screenshots" });
            for (int i = 0; i < textures.Length; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Overriding Textures For Android",
                    i.ToString() + " out of " + textures.Length.ToString() + " textures completed",
                    i / (float)textures.Length)) { break; }

                string assetPath = AssetDatabase.GUIDToAssetPath(textures[i]);
                if (assetPath.Contains("com.unity.")) { continue; }
                if (assetPath.Length == 0) { Debug.LogError(textures[i] + " not found"); continue; }

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                bool textureIsInSpriteAtlas = false;
                if (sprite)
                {
                    List<Sprite> allSpritesInAtlases = new List<Sprite>();

                    // Skip textures that are in a sprite atlas, you want to set overrides direclty  on the astlas
                    string[] spriteAtlasPaths = AssetDatabase.FindAssets("t:SpriteAtlas");
                    List<SpriteAtlas> spriteAtlases = new List<SpriteAtlas>();
                    foreach (string spriteAtlasGuid in spriteAtlasPaths)
                    {
                        SpriteAtlas spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(spriteAtlasGuid));
                        spriteAtlases.Add(spriteAtlas);

                        Sprite[] sprites = new Sprite[spriteAtlas.spriteCount];
                        for (int j = 0; j < spriteAtlas.GetSprites(sprites); j++)
                        {
                            allSpritesInAtlases.Add(sprites[j]);
                        }
                    }

                    if (allSpritesInAtlases.Exists(item => item.name.Replace("(Clone)", "") == sprite.name))
                    {
                        textureIsInSpriteAtlas = true;
                    }
                }

                EditorUtility.UnloadUnusedAssetsImmediate();

                if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
                {
                    bool shouldReimport = false;

                    if (textureIsInSpriteAtlas)
                    {
                        var ds = importer.GetDefaultPlatformTextureSettings();

                        if (ds.crunchedCompression)
                        {
                            ds.crunchedCompression = false;
                            shouldReimport = true;
                            importer.SetPlatformTextureSettings(ds);
                        }

                        if (importer.GetPlatformTextureSettings("Android").overridden)
                        {
                            importer.ClearPlatformTextureSettings("Android");
                            shouldReimport = true;
                        }

                        if (importer.GetPlatformTextureSettings("iPhone").overridden)
                        {
                            importer.ClearPlatformTextureSettings("iPhone");
                            shouldReimport = true;
                        }

                        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                        {
                            importer.textureCompression = TextureImporterCompression.Uncompressed;
                            shouldReimport = true;
                        }

                        if (shouldReimport) { importer.SaveAndReimport(); }
                        continue;
                    }

                    TextureImporterPlatformSettings defaultSettings = importer.GetDefaultPlatformTextureSettings();
                    if (!defaultSettings.crunchedCompression & !importer.isReadable)
                    {
                        defaultSettings.compressionQuality = 100;
                        defaultSettings.crunchedCompression = true;
                        importer.SetPlatformTextureSettings(defaultSettings);
                        shouldReimport = true;
                    }

                    bool isEnvironmentAsset = assetPath.Contains("Environment Assets");

                    if (isEnvironmentAsset)
                    {
                        if (defaultSettings.maxTextureSize < 2048)
                        {
                            defaultSettings.maxTextureSize = 2048;
                            defaultSettings.compressionQuality = 100;
                            defaultSettings.crunchedCompression = true;
                            importer.SetPlatformTextureSettings(defaultSettings);
                            shouldReimport = true;
                        }
                    }

                    int targetMobileImportSize = isEnvironmentAsset ? 1024 : 256;
                    if (defaultSettings.maxTextureSize > targetMobileImportSize)
                    {
                        if (!importer.GetPlatformTextureSettings("Android").overridden
                        | importer.GetPlatformTextureSettings("Android").maxTextureSize < targetMobileImportSize)
                        {
                            TextureImporterPlatformSettings androidSettings = new TextureImporterPlatformSettings();
                            androidSettings.name = "Android";
                            androidSettings.overridden = true;
                            androidSettings.maxTextureSize = 256;
                            importer.SetPlatformTextureSettings(androidSettings);
                            shouldReimport = true;
                        }

                        if (!importer.GetPlatformTextureSettings("iPhone").overridden
                            | importer.GetPlatformTextureSettings("iPhone").maxTextureSize != importer.GetPlatformTextureSettings("Android").maxTextureSize)
                        {
                            TextureImporterPlatformSettings iPhoneSettings = new TextureImporterPlatformSettings();
                            iPhoneSettings.name = "iPhone";
                            iPhoneSettings.overridden = true;
                            iPhoneSettings.maxTextureSize = importer.GetPlatformTextureSettings("Android").maxTextureSize;
                            importer.SetPlatformTextureSettings(iPhoneSettings);
                            shouldReimport = true;
                        }
                    }

                    if (shouldReimport) { importer.SaveAndReimport(); }
                }
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            EditorUtility.UnloadUnusedAssetsImmediate();

            //foreach (SpriteAtlas spriteAtlas in spriteAtlases)
            //{
            //    if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(spriteAtlas)) is SpriteAtlasImporter atlasImporter)
            //    {
            //        bool shouldReimport = false;

            //        var standaloneSettings = atlasImporter.GetPlatformSettings("Standalone");
            //        standaloneSettings.crunchedCompression = true;
            //        standaloneSettings.compressionQuality = 100;
            //        standaloneSettings.maxTextureSize = 2048;
            //        standaloneSettings.overridden = true;
            //        atlasImporter.SetPlatformSettings(standaloneSettings);
            //        shouldReimport = true;

            //        var androidSettings = atlasImporter.GetPlatformSettings("Android");
            //        androidSettings.crunchedCompression = true;
            //        androidSettings.compressionQuality = 100;
            //        androidSettings.maxTextureSize = 256;
            //        androidSettings.overridden = true;
            //        atlasImporter.SetPlatformSettings(androidSettings);
            //        shouldReimport = true;

            //        var iPhoneSettings = atlasImporter.GetPlatformSettings("iPhone");
            //        iPhoneSettings.crunchedCompression = true;
            //        iPhoneSettings.compressionQuality = 100;
            //        iPhoneSettings.maxTextureSize = 256;
            //        iPhoneSettings.overridden = true;
            //        atlasImporter.SetPlatformSettings(iPhoneSettings);
            //        shouldReimport = true;

            //        if (shouldReimport) { atlasImporter.SaveAndReimport(); }
            //    }
            //}
        }

        [MenuItem("Tools/Production/Generate Dropped Weapon Variants")]
        static void GenerateDroppedWeaponVariants()
        {
            List<CharacterReference.WeaponOption> weaponOptions = new List<CharacterReference.WeaponOption>();
            weaponOptions.AddRange(GetCharacterReference().GetWeaponOptions());

            string mobFolder = @"Assets/Production/Prefabs/Mobs";
            foreach (string mobFilePath in Directory.GetFiles(mobFolder, "*.prefab", SearchOption.AllDirectories))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(mobFilePath);
                if (prefab)
                {
                    if (prefab.TryGetComponent(out Mob mob))
                    {
                        if (mob.GetWeaponOption() == null) { continue; }
                        if (mob.GetWeaponOption().weapon == null) { continue; }
                        weaponOptions.Add(mob.GetWeaponOption());
                    }
                }
                else
                {
                    Debug.LogWarning("Unable to load prefab at path " + mobFilePath);
                }
            }

            int counter = 0;
            foreach (var weaponOption in weaponOptions)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Creating dropped weapon variants",
                    weaponOption.weapon.name + " | " + counter.ToString() + " out of " + weaponOptions.Count,
                    counter / (float)weaponOptions.Count))
                {
                    break;
                }

                foreach (var weaponModelData in weaponOption.weapon.GetWeaponModelData())
                {
                    foreach (var data in weaponModelData.data)
                    {
                        if (data.weaponPrefab.TryGetComponent(out RuntimeWeapon runtimeWeapon))
                        {
                            runtimeWeapon.CreateDropWeaponPrefabVariant();
                        }
                        else
                        {
                            Debug.LogError(data.weaponPrefab + " doesn't have a runtime weapon");
                        }
                    }
                }
                counter++;
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        [MenuItem("Tools/Production/Remove Components From Weapon Previews")]
        static void RemoveComponentsFromWeaponPreviews()
        {
            int index = 0;
            foreach (var weaponOption in GetCharacterReference().GetWeaponOptions())
            {
                index++;
                if (EditorUtility.DisplayCancelableProgressBar("Removing components from weapon previews",
                    weaponOption.weapon.ToString(),
                    index / (float)GetCharacterReference().GetWeaponOptions().Length))
                {
                    break;
                }

                bool componentDestroyed = false;
                foreach (Component component in weaponOption.weaponPreviewPrefab.GetComponentsInChildren<Component>())
                {
                    if (component is not Transform
                        & component is not Camera
                        & component is not UnityEngine.Rendering.Universal.UniversalAdditionalCameraData
                        & component is not Renderer)
                    {
                        Debug.Log("Destroying " + component + " from weapon preview prefab " + weaponOption.weaponPreviewPrefab);
                        DestroyImmediate(component, true);
                        componentDestroyed = true;
                    }
                }

                if (componentDestroyed) { EditorUtility.SetDirty(weaponOption.weaponPreviewPrefab); }
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        [MenuItem("Tools/Production/Set Action VFX Layers")]
        static void SetActionVFXLayers()
        {
            List<string> files = new List<string>();
            string VFXFolder = @"Assets\Production\Prefabs\VFX";
            files.AddRange(Directory.GetFiles(VFXFolder, "*.prefab", SearchOption.AllDirectories));

            string packagedPrefabsFolder = @"Assets\PackagedPrefabs";
            files.AddRange(Directory.GetFiles(packagedPrefabsFolder, "*.prefab", SearchOption.AllDirectories));

            int counter = 0;
            foreach (string prefabFilePath in files)
            {
                counter++;
                if (EditorUtility.DisplayCancelableProgressBar("Setting Action VFX Layers",
                    counter.ToString() + " out of " + files.Count.ToString(),
                    counter / (float)files.Count))
                {
                    break;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFilePath);
                if (prefab)
                {
                    if (prefab.TryGetComponent(out ActionVFX actionVFX))
                    {
                        actionVFX.SetLayers();
                    }
                }
                else
                {
                    Debug.LogWarning("Could not load prefab at path: " + prefabFilePath);
                }
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        [MenuItem("Tools/Production/Validate Pooled Objects List")]
        static void ValidatePooledObjectsList()
        {
            List<ActionClip> actionClips = GetActionClips();

            PooledObjectList pooledObjectList = GetPooledObjectList();

            List<PooledObject> pooledObjectsThatShouldBeIncluded = new List<PooledObject>();

            Mob[] mobs = GetMobPrefabs();
            Attributes[] players = GetBotAndPlayerPrefabs(); 

            int counter = 0;
            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories));
            foreach (string prefabFilePath in files)
            {
                counter++;
                if (EditorUtility.DisplayCancelableProgressBar("Validating Pooled Objects", counter + " out of " + files.Count, counter / (float)files.Count)) { break; }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFilePath);
                if (prefab)
                {
                    if (prefab.name == "MobBase") { continue; }
                    if (prefab.TryGetComponent(out PooledObject pooledObject))
                    {
                        if (prefab.TryGetComponent(out NetworkObject networkObject))
                        {
                            if (prefab.TryGetComponent(out ActionVFX actionVFX))
                            {
                                bool vfxReferencedInActionClip = false;
                                foreach (ActionClip actionClip in actionClips)
                                {
                                    if (actionClip.actionVFXList.Contains(actionVFX) | actionClip.chargeAttackChargingVFX == actionVFX)
                                    {
                                        vfxReferencedInActionClip = true;
                                        break;
                                    }
                                }

                                if (vfxReferencedInActionClip)
                                {
                                    pooledObjectList.TryAddPooledObject(pooledObject);
                                    pooledObjectsThatShouldBeIncluded.Add(pooledObject);

                                    if (actionVFX is GameInteractiveActionVFX g)
                                    {
                                        foreach (FollowUpVFX followUpVFX in g.GetFollowUpVFX())
                                        {
                                            pooledObjectList.TryAddPooledObject(followUpVFX.GetComponent<PooledObject>());
                                            pooledObjectsThatShouldBeIncluded.Add(followUpVFX.GetComponent<PooledObject>());
                                        }
                                    }
                                }
                                else
                                {
                                    bool broken = false;
                                    foreach (Attributes attributes in players)
                                    {
                                        if (attributes.GetPooledObjectDependencies().Contains(pooledObject))
                                        {
                                            pooledObjectList.TryAddPooledObject(pooledObject);
                                            pooledObjectsThatShouldBeIncluded.Add(pooledObject);
                                            broken = true;
                                            break;
                                        }
                                    }

                                    if (!broken)
                                    {
                                        foreach (Mob mob in mobs)
                                        {
                                            if (mob.GetPooledObjectDependencies().Contains(pooledObject))
                                            {
                                                pooledObjectList.TryAddPooledObject(pooledObject);
                                                pooledObjectsThatShouldBeIncluded.Add(pooledObject);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                pooledObjectList.TryAddPooledObject(pooledObject);
                                pooledObjectsThatShouldBeIncluded.Add(pooledObject);
                            }
                        }
                        else // No Network Object
                        {
                            pooledObjectList.TryAddPooledObject(pooledObject);
                            pooledObjectsThatShouldBeIncluded.Add(pooledObject);
                        }
                    }
                }
                else
                {
                    Debug.LogError("Problem loading prefab at path " + prefabFilePath);
                }
            }
            EditorUtility.ClearProgressBar();

            foreach (PooledObjectReference pooledObjectReference in pooledObjectList.GetPooledObjectReferences())
            {
                PooledObject pl = AssetDatabase.LoadAssetAtPath<PooledObject>(AssetDatabase.GUIDToAssetPath(pooledObjectReference.AssetGUID));
                if (pl)
                {
                    if (!pooledObjectsThatShouldBeIncluded.Contains(pl))
                    {
                        Debug.Log(pl + " is no longer needed in the pooled object list and can be safely removed " + pooledObjectsThatShouldBeIncluded.Count);
                    }
                }
                else
                {
                    Debug.LogWarning("Pooled object is null");
                }
            }

            pooledObjectList.FindDuplicates();

            AssetDatabase.SaveAssets();
            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        [MenuItem("Tools/Production/Organize Addressable Groups")]
        private static void OrganizeAddressables()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            AddressableAssetGroup groupToOrganize = settings.FindGroup(item => item.Name == "Duplicate Asset Isolation");

            if (groupToOrganize)
            {
                // Organize the duplicate asset isolation group into different groups based on asset path
                int originalCount = groupToOrganize.entries.Count;
                foreach (AddressableAssetEntry entry in groupToOrganize.entries.ToArray())
                {
                    string[] directories = entry.AssetPath.Split('/');
                    string targetGroupName = "";
                    for (int i = 0; i < directories.Length - 1; i++)
                    {
                        targetGroupName += directories[i] + " ";
                    }
                    targetGroupName.Trim();

                    AddressableAssetGroup groupToModify = settings.FindGroup(item => item.Name == targetGroupName);

                    if (EditorUtility.DisplayCancelableProgressBar("Organizing Addressable Group: " + targetGroupName,
                            groupToOrganize.entries.Count.ToString() + " assets left - " + entry.TargetAsset.name,
                            groupToOrganize.entries.Count / (float)originalCount))
                    { break; }

                    if (!groupToModify)
                    {
                        groupToModify = settings.CreateGroup(targetGroupName, false, false, false, groupToOrganize.Schemas.ToList(), groupToOrganize.SchemaTypes.ToArray());
                    }
                    settings.MoveEntry(entry, groupToModify);
                }
                EditorUtility.ClearProgressBar();
            }

            // Remove groups with 0 entries in them
            foreach (AddressableAssetGroup group in settings.groups.ToArray())
            {
                List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>();
                group.GatherAllAssets(entries, true, true, true);

                if (entries.Count == 0) { Debug.Log("Removing group " + group.name); settings.RemoveGroup(group); }
            }
        }
        #endregion

        [MenuItem("Tools/Utility/Set GPU Instancing Based On Static Objects")]
        private static void SetGPUInstancingBasedOnStaticObjects()
        {
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.gameObject.isStatic | r.isPartOfStaticBatch)
                    {
                        foreach (Material m in r.materials)
                        {
                            if (m.enableInstancing)
                            {
                                m.enableInstancing = false;
                                EditorUtility.SetDirty(m);
                            }
                        }
                    }
                }
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Utility/Remove Is Readable")]
        private static void RemoveIsReadable()
        {
            List<string> paths = new List<string>();
            paths.AddRange(AssetDatabase.FindAssets("t:Model"));

            for (int i = 0; i < paths.Count; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Removing is readable",
                    i.ToString() + " out of " + paths.Count, (float)i / paths.Count)) { break; }
                
                ModelImporter modelImporter = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(paths[i])) as ModelImporter;
                if (modelImporter)
                {
                    if (modelImporter.isReadable)
                    {
                        modelImporter.isReadable = false;
                        EditorUtility.SetDirty(modelImporter);
                        modelImporter.SaveAndReimport();
                    }
                }
                else
                {
                    Debug.LogWarning("No model importer at " + paths[i]);
                }
                EditorUtility.UnloadUnusedAssetsImmediate();
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Utility/Enable GPU Instancing")]
        private static void EnableGPUInstancing()
        {
            List<string> paths = new List<string>();
            paths.AddRange(Directory.GetFiles(@"Assets\PackagedPrefabs", "*.mat", SearchOption.AllDirectories));
            for (int i = 0; i < paths.Count; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Enabling GPU Instancing",
                    i.ToString() + " out of " + paths.Count, (float)i / paths.Count)) { break; }

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(paths[i]);
                if (mat)
                {
                    if (!mat.enableInstancing)
                    {
                        mat.enableInstancing = true;
                        EditorUtility.SetDirty(mat);
                    }
                }
                EditorUtility.UnloadUnusedAssetsImmediate();
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Utility/Mass Convert Material Shaders")]
        private static void MassConvertMaterialShaders()
        {
            Shader shader = Shader.Find("Shader Graphs/Character and Weapon Shader");
            Mob[] mobs = GetMobPrefabs();
            for (int i = 0; i < mobs.Length; i++)
            {
                Mob mob = mobs[i];

                if (EditorUtility.DisplayCancelableProgressBar("Converting Shaders", i.ToString() + " out of " + mobs.Length, i / (float)mobs.Length))
                { break; }

                foreach (Renderer r in mob.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material mat in r.sharedMaterials)
                    {
                        if (!mat.enableInstancing)
                        {
                            mat.enableInstancing = true;
                            EditorUtility.SetDirty(mat);
                        }
                        
                        if (mat.shader != shader)
                        {
                            if (!mat.shader.name.Contains("Lit"))
                            {
                                Debug.LogWarning("Unknown shader " + mat.shader + " " + mat + " " + mob + " " + r);
                                continue;
                            }

                            if (!mat.HasTexture("_BaseMap"))
                            {
                                Debug.LogWarning("No Base Map " + mat.shader + " " + mat + " " + mob + " " + r);
                                continue;
                            }

                            Texture baseMap = null;
                            if (mat.HasTexture("_BaseMap"))
                            {
                                baseMap = mat.GetTexture("_BaseMap");
                            }

                            //Texture normalMap = mat.GetTexture("_BumpMap");

                            //Texture baseMap = mat.GetTexture("_Base_Color");

                            //if (!baseMap) { Debug.LogWarning("No base map found " + mat); }
                            //if (!normalMap) { Debug.LogWarning("No normal map found " + mat); }

                            mat.shader = shader;

                            mat.SetTexture("_Base_Color", baseMap);
                            //mat.SetTexture("_Normal_Map", normalMap);

                            mat.SetFloat("_Alpha_cut", 1);
                            mat.SetFloat("_Ambient_Strength", 1);
                            mat.SetFloat("_Roughness", 0);

                            EditorUtility.SetDirty(mat);
                        }
                    }
                }
            }
            EditorUtility.ClearProgressBar();
            EditorUtility.UnloadUnusedAssetsImmediate();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Utility/Set Environment Reflections On Weapons And Armor")]
        private static void SetEnvironmentReflectionsOnWeaponsAndArmor()
        {
            string[] paths = Directory.GetFiles(@"Assets\PackagedPrefabs\MODEL_CHAR_StylizedCharacter", "*.mat", SearchOption.AllDirectories);

            int counter = -1;
            foreach (string path in paths)
            {
                counter++;
                if (EditorUtility.DisplayCancelableProgressBar("Setting Environment Reflections: " + path,
                            counter.ToString() + " out of " + paths.Length,
                            counter / (float)paths.Length))
                { break; }

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat)
                {
                    if (!mat.shader.name.Contains("Lit")) { Debug.Log("Skipping " + mat); continue; }

                    if (!mat.IsKeywordEnabled("_SPECULARHIGHLIGHTS_OFF") | mat.GetFloat("_SpecularHighlights") != 0)
                    {
                        mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                        mat.SetFloat("_SpecularHighlights", 0);
                        EditorUtility.SetDirty(mat);
                    }

                    if (mat.IsKeywordEnabled("_ENVIRONMENTREFLECTIONS_OFF") | mat.GetFloat("_EnvironmentReflections") != 1)
                    {
                        mat.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                        mat.SetFloat("_EnvironmentReflections", 1);
                        EditorUtility.SetDirty(mat);
                    }
                }
                else
                {
                    Debug.LogWarning("No material at path " + path);
                }
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Utility/Set Network Object Settings")]
        private static void SetNetworkObjectSettings()
        {
            string[] paths = AssetDatabase.GetAllAssetPaths();
            int counter = -1;
            foreach (string assetPath in paths)
            {
                counter++;
                if (EditorUtility.DisplayCancelableProgressBar("Setting Network Object Settings: " + assetPath,
                            counter.ToString() + " out of " + paths.Length,
                            counter / (float)paths.Length))
                { break; }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab)
                {
                    if (prefab.TryGetComponent(out NetworkObject networkObject))
                    {
                        if (networkObject.AutoObjectParentSync | networkObject.SceneMigrationSynchronization | networkObject.ActiveSceneSynchronization)
                        {
                            networkObject.SceneMigrationSynchronization = false;
                            networkObject.ActiveSceneSynchronization = false;
                            networkObject.AutoObjectParentSync = false;
                            UnityEditor.EditorUtility.SetDirty(networkObject);
                        }
                    }
                }
                EditorUtility.UnloadUnusedAssetsImmediate();
            }
            EditorUtility.ClearProgressBar();
        }

        [MenuItem("Tools/Utility/Set Actions Layer Transition Times On Animation Controller")]
        private static void SetActionsLayerTransitionTimes()
        {
            AnimatorController animatorController = (AnimatorController)Selection.activeObject;
            foreach (AnimatorControllerLayer layer in animatorController.layers)
            {
                if (layer.name != "Actions" & layer.name != "Flinch") { continue; }
                foreach (ChildAnimatorState state in layer.stateMachine.states)
                {
                    foreach (AnimatorStateTransition transition in state.state.transitions)
                    {
                        if (transition.hasExitTime)
                        {
                            transition.exitTime = 0.85f;
                        }
                        transition.duration = 0.15f;
                    }
                }
            }
            EditorUtility.SetDirty(animatorController);
        }

        [MenuItem("Tools/Utility/Remove Missing Scripts From Prefabs")]
        private static void FindAndRemoveMissingInSelected()
        {
            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories));
            List<GameObject> assetList = new List<GameObject>();
            foreach (string file in files)
            {
                assetList.Add(AssetDatabase.LoadAssetAtPath<GameObject>(file));
            }

            // EditorUtility.CollectDeepHierarchy does not include inactive children
            var deeperSelection = assetList.ToArray().SelectMany(go => go.GetComponentsInChildren<Transform>(true))
                .Select(t => t.gameObject);
            var prefabs = new HashSet<Object>();
            int compCount = 0;
            int goCount = 0;
            foreach (var go in deeperSelection)
            {
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (count > 0)
                {
                    if (PrefabUtility.IsPartOfAnyPrefab(go))
                    {
                        RecursivePrefabSource(go, prefabs, ref compCount, ref goCount);
                        count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                        // if count == 0 the missing scripts has been removed from prefabs
                        if (count == 0)
                            continue;
                        // if not the missing scripts must be prefab overrides on this instance
                    }

                    Undo.RegisterCompleteObjectUndo(go, "Remove missing scripts");
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    compCount += count;
                    goCount++;
                }
            }

            Debug.Log($"Found and removed {compCount} missing scripts from {goCount} GameObjects");
        }

        // Prefabs can both be nested or variants, so best way to clean all is to go through them all
        // rather than jumping straight to the original prefab source.
        private static void RecursivePrefabSource(GameObject instance, HashSet<Object> prefabs, ref int compCount, ref int goCount)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
            // Only visit if source is valid, and hasn't been visited before
            if (source == null || !prefabs.Add(source))
                return;

            // go deep before removing, to differantiate local overrides from missing in source
            RecursivePrefabSource(source, prefabs, ref compCount, ref goCount);

            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(source);
            if (count > 0)
            {
                Undo.RegisterCompleteObjectUndo(source, "Remove missing scripts");
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(source);
                compCount += count;
                goCount++;
            }
        }

        [MenuItem("Tools/Utility/Set Network Prefabs As Dirty")]
        static void SetNetworkPrefabsAsDirty()
        {
            foreach (NetworkPrefabsList networkPrefabList in GetNetworkPrefabsLists())
            {
                foreach (NetworkPrefab networkPrefab in networkPrefabList.PrefabList)
                {
                    EditorUtility.SetDirty(networkPrefab.Prefab);
                }
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Utility/Generate Exploded Meshes")]
        static void GenerateExplodedMeshes()
        {
            if (!Selection.activeObject)
            {
                Debug.LogWarning("Please select an object in the project view before generating meshes!");
                return;
            }

            if (Selection.activeObject is not Mesh)
            {
                Debug.LogWarning("Selected object must be a mesh!");
                return;
            }

            Mesh mesh = (Mesh)Selection.activeObject;
            if (!mesh) { Debug.LogError("Please select a mesh to explode"); return; }

            GameObject root = new GameObject("Root");

            SkinnedMeshRenderer skinnedMeshRenderer = root.AddComponent<SkinnedMeshRenderer>();
            skinnedMeshRenderer.sharedMesh = mesh;
            root.AddComponent<Sliceable>();

            List<GameObject> objectsToSlice = Slicer.RandomSlice(root).ToList();
            DestroyImmediate(root);

            bool canceled = false;
            int sliceCount = 5;
            for (int i = 0; i < sliceCount; i++)
            {
                GameObject[] copy = objectsToSlice.ToArray();
                objectsToSlice.Clear();
                int j = 0;
                foreach (GameObject slice in copy)
                {
                    if (EditorUtility.DisplayCancelableProgressBar((i + 1).ToString() + " slices out of " + sliceCount, j.ToString() + " meshes out of " + copy.Length, j / (float)copy.Length))
                    {
                        canceled = true;
                        break;
                    }

                    objectsToSlice.AddRange(Slicer.RandomSlice(slice));
                    DestroyImmediate(slice);
                    j++;
                }

                if (canceled) { break; }

                foreach (GameObject slice in objectsToSlice)
                {
                    slice.name = mesh.name + "_Slice";
                }
            }

            EditorUtility.ClearProgressBar();

            GameObject parent = new GameObject("Delete Me");
            foreach (GameObject slice in objectsToSlice)
            {
                slice.transform.SetParent(parent.transform);
            }

            if (canceled) { return; }

            string meshPath = AssetDatabase.GetAssetPath(mesh);
            meshPath = meshPath.Replace(".fbx", "_Explosion");
            string resultFolderGUID = AssetDatabase.CreateFolder(meshPath.Substring(0, meshPath.LastIndexOf('/')), mesh.name + "_Explosion");

            int counter = 0;
            foreach (GameObject slice in objectsToSlice)
            {
                counter++;
                AssetDatabase.CreateAsset(slice.GetComponent<MeshFilter>().sharedMesh, Path.Join(AssetDatabase.GUIDToAssetPath(resultFolderGUID), slice.name + "_" + counter.ToString() + ".asset"));
            }
        }
    }
}