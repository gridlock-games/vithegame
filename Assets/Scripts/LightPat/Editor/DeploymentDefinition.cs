# if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace LightPat.Editor
{
    [CreateAssetMenu(fileName = "new-deployment-definition", menuName = "Deployment Definition")]
    public class DeploymentDefinition : ScriptableObject
    {
        public Build[] buildList;

        [ContextMenu("Create builds")]
        public void CreateBuilds()
        {
            foreach (Build build in buildList)
            {
                if (Directory.Exists(build.locationPathName))
                {
                    // Delete all files inside the directory, while retaining the root directory
                    DirectoryInfo directory = new DirectoryInfo(build.locationPathName);

                    foreach (FileInfo file in directory.GetFiles())
                    {
                        file.Delete();
                    }

                    foreach (DirectoryInfo dir in directory.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                }
                else // If the target directory doesn't exist
                {
                    Debug.LogError(build.locationPathName + " does not exist. Please enter a valid directory on your machine");
                    break;
                }

                string[] scenesInBuildPaths = new string[build.scenesInBuild.Length];
                EditorBuildSettingsScene[] editorScenes = new EditorBuildSettingsScene[build.scenesInBuild.Length];
                for (int i = 0; i < build.scenesInBuild.Length; i++)
                {
                    string scenePath = AssetDatabase.GetAssetPath(build.scenesInBuild[i]);
                    scenesInBuildPaths[i] = scenePath;
                    editorScenes[i] = new EditorBuildSettingsScene(scenePath, true);
                }

                EditorBuildSettings.scenes = editorScenes;

                string fileExtension = "";
                if (build.target == BuildTarget.StandaloneWindows64)
                {
                    fileExtension = ".exe";
                }
                else if (build.target == BuildTarget.StandaloneLinux64)
                {
                    fileExtension = ".x86_64";
                }
                else
                {
                    Debug.LogError("Build targets of type " + build.target + " are not supported");
                    break;
                }

                BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
                {
                    scenes = scenesInBuildPaths,
                    locationPathName = Path.Join(build.locationPathName, PlayerSettings.productName + fileExtension),
                    options = build.options,
                    target = build.target,
                    subtarget = (int)build.subtarget,
                    targetGroup = BuildTargetGroup.Standalone
                };

                var buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
                Debug.Log("Build completed with result of: " + buildReport.summary.result);
            }
        }

        [ContextMenu("Log Current Build Settings")]
        public void LogCurrentBuildSettings()
        {
            // Log some of the current build options retrieved from the Build Settings Window
            BuildPlayerOptions buildPlayerOptions = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(new BuildPlayerOptions());
            Debug.Log("BuildPlayerOptions\n"
                + "Scenes: " + string.Join(",", buildPlayerOptions.scenes) + "\n"
                + "Build location: " + buildPlayerOptions.locationPathName + "\n"
                + "Options: " + buildPlayerOptions.options + "\n"
                + "Target: " + buildPlayerOptions.target + "\n"
                + "Subtarget: " + buildPlayerOptions.subtarget + "\n"
                + "Taget group: " + buildPlayerOptions.targetGroup);
        }
    }

    [System.Serializable]
    public struct Build
    {
        public SceneAsset[] scenesInBuild;
        public string locationPathName;
        public BuildOptions options;
        public BuildTarget target;
        public StandaloneBuildSubtarget subtarget;
    }
}
#endif