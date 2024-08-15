using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.AddressableAssets.Build.AnalyzeRules
{
    public class UnusedAddressableRule : AnalyzeRule
    {
        private List<GUID> sceneAssets = new List<GUID>();
        private List<GUID> addressableAssets = new List<GUID>();
        private List<SceneAsset> addressableSceneAssets = new List<SceneAsset>();
        private List<GUID> AssetDifference = new List<GUID>();
        private List<string> unusedAssetPaths = new List<string>();
        private Dictionary<GUID, List<GUID>> addressableFoldersData = new Dictionary<GUID, List<GUID>>();

        private List<string> addressableGroupsToIgnore = new List<string>
        {
            "Shaders-Unity-Terrain",
        };

        public override bool CanFix
        {
            get { return true; }
        }

        public override string ruleName
        {
            get { return "Check Unused Addressable Assets"; }
        }

        public override List<AnalyzeResult> RefreshAnalysis(AddressableAssetSettings settings)
        {
            ClearAnalysis();
            return CheckUnusedAddressableAssets(settings);
        }

        protected List<AnalyzeResult> CheckUnusedAddressableAssets(AddressableAssetSettings settings)
        {
            List<AnalyzeResult> results = new List<AnalyzeResult>();
            sceneAssets.Clear();

            GetAddressableSceneAssets(settings);

            var lastActiveScenePath = SceneManager.GetActiveScene().path;
            var scenesProcessed = 0;

            foreach (var sceneAsset in addressableSceneAssets)
            {
                var scenePath = AssetDatabase.GetAssetPath(sceneAsset);

                scenesProcessed++;
                var progress = (float)scenesProcessed / (float)addressableSceneAssets.Count;
                if (EditorUtility.DisplayCancelableProgressBar(
                        $"Analyzing scene ({scenesProcessed}/{addressableSceneAssets.Count})", scenePath, progress))
                {
                    break;
                }

                var scene = EditorSceneManager.OpenScene(scenePath);
                GetSceneAssets(scene);
                GetSceneAssets(scenePath);
            }

            EditorSceneManager.OpenScene(lastActiveScenePath);

            // Remove duplicate entries
            sceneAssets = sceneAssets.Distinct().ToList();

            GetAddressableAssets(settings);
            FilterLists();
            Convert_GUID_ToPath();

            for (int i = 0; i < unusedAssetPaths.Count; i++)
            {
                results.Add(new AnalyzeResult { resultName = unusedAssetPaths[i] + "", severity = MessageType.Warning });
            }

            if (results.Count == 0)
                results.Add(new AnalyzeResult { resultName = ruleName + " - No unused assets found." });

            return results;
        }

        private void GetSceneAssets(Scene scene)
        {
            List<GameObject> rootGameObjects = scene.GetRootGameObjects().ToList();
            List<Object> objects = rootGameObjects.ConvertAll(root => (Object)root);

            Object[] dependencies = EditorUtility.CollectDependencies(objects.ToArray());

            foreach (var obj in dependencies)
            {
                if (obj == null)
                    continue;

                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long _))
                {
                    sceneAssets.Add(new GUID(guid));
                }
            }
        }

        private void GetSceneAssets(string resourcePath)
        {
            string[] dependencies = AssetDatabase.GetDependencies(resourcePath);
            sceneAssets.AddRange(from dependency in dependencies
                                 select new GUID(AssetDatabase.AssetPathToGUID(dependency)));
        }

        private void GetAddressableAssets(AddressableAssetSettings settings)
        {
            addressableAssets.Clear();
            addressableFoldersData.Clear();

            foreach (var aaGroup in settings.groups)
            {
                if (aaGroup == null)
                    continue;

                if (addressableGroupsToIgnore.Contains(aaGroup.Name))
                    continue;

                foreach (var entry in aaGroup.entries)
                {
                    if (entry.AssetPath == "*/Resources/" || entry.AssetPath == "Scenes In Build")
                    {
                        continue;
                    }

                    // Don't add scenes to the addressableAssets list
                    if (entry.IsScene)
                    {
                        continue;
                    }

                    // Scan addressables' sub-assets (inside addressable folders, .fbx models, etc.)
                    var splitPath = entry.AssetPath.Split('/');
                    if (entry.IsFolder || !splitPath.Last().Contains('.'))
                    {
                        string[] folderGuids = AssetDatabase.FindAssets("t:folder", new[] { entry.AssetPath });
                        string[] guids = AssetDatabase.FindAssets("", new[] { entry.AssetPath });
                        List<GUID> filteredGuids = new List<GUID>();

                        filteredGuids.AddRange(from guid in guids
                                               where !folderGuids.Contains(guid) // Don't add folders to the list
                                                     && !AssetDatabase.GUIDToAssetPath(guid).EndsWith(".cs") // Don't add scripts to the list
                                               select new GUID(guid));

                        addressableFoldersData.Add(new GUID(entry.guid), filteredGuids);
                    }
                    else
                    {
                        addressableAssets.Add(new GUID(entry.guid));
                    }
                }
            }
        }

        private void GetAddressableSceneAssets(AddressableAssetSettings settings)
        {
            addressableSceneAssets.Clear();
            addressableSceneAssets = (from aaGroup in settings.groups
                                      where aaGroup != null
                                      from entry in aaGroup.entries
                                      where entry.IsScene
                                      select entry.MainAsset as SceneAsset).ToList();
        }

        private void FilterLists()
        {
            AssetDifference.Clear();
            AssetDifference = addressableAssets.Except(sceneAssets).ToList();

            foreach (var addressableFolderData in addressableFoldersData)
            {
                if (addressableFolderData.Value.Intersect(sceneAssets).Any())
                    continue; // At least one sub-asset from this addressable folder is being used in the game

                // Mark addressable folder as unused, if ALL of its sub-assets are unused
                AssetDifference.Add(addressableFolderData.Key);
            }
        }

        private void Convert_GUID_ToPath()
        {
            unusedAssetPaths.Clear();
            unusedAssetPaths = (from guid in AssetDifference
                                select AssetDatabase.GUIDToAssetPath(guid.ToString())).ToList();
        }

        public override void FixIssues(AddressableAssetSettings settings)
        {
            if (AssetDifference == null)
                CheckUnusedAddressableAssets(settings);

            if (AssetDifference == null || AssetDifference.Count == 0)
                return;

            foreach (var asset in AssetDifference)
                settings.RemoveAssetEntry(asset.ToString(), false);

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
        }
    }

    [InitializeOnLoad]
    class RegisterUnusedAssetRule
    {
        static RegisterUnusedAssetRule()
        {
            AnalyzeSystem.RegisterNewRule<UnusedAddressableRule>();
        }
    }
}