# if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LightPat.Editor
{
    [CreateAssetMenu(fileName = "new-build-definition", menuName = "Build Definition")]
    public class BuildDefinition : ScriptableObject
    {
        public SceneAsset[] scenesInBuild;

        [ContextMenu("Apply Build Settings")]
        public BuildPlayerOptions ApplyBuildSettings()
        {
            string[] scenesInBuildPaths = new string[scenesInBuild.Length];
            EditorBuildSettingsScene[] editorScenes = new EditorBuildSettingsScene[scenesInBuild.Length];
            for (int i = 0; i < scenesInBuild.Length; i++)
            {
                string scenePath = AssetDatabase.GetAssetPath(scenesInBuild[i]);
                scenesInBuildPaths[i] = scenePath;
                editorScenes[i] = new EditorBuildSettingsScene(scenePath, true);
            }

            EditorBuildSettings.scenes = editorScenes;

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
            {
                scenes = scenesInBuildPaths
            };

            return buildPlayerOptions;
        }
    }
}
# endif