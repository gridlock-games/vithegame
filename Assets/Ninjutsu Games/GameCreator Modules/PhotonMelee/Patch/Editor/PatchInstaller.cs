using System.IO;
using GameCreator.Update;
using Photon.Realtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NJG.PUN.Melee
{
    public class PatchInstaller : MonoBehaviour
    {
        #if !PHOTON_MELEE
        public const string SYMBOL = "PHOTON_MELEE";
        public const string ASSETS_PATH = "Assets/";
        public const string PATCH_PATH = "Ninjutsu Games/GameCreator Modules/PhotonMelee/Patch/";
        private static bool installed;
        
        [InitializeOnLoadMethod]
        static void OnInitializeInstall()
        {
            Debug.LogWarningFormat("Patch isPlaying: {0} compiling: {1} updating: {2} willChange: {3}", 
                EditorApplication.isPlaying, EditorApplication.isCompiling, EditorApplication.isUpdating, 
                EditorApplication.isPlayingOrWillChangePlaymode);
            if (EditorApplication.isPlaying) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!installed) EditorApplication.update += PrepareInstallUpdate;
            else CompleteInstallation();
        }
        
        private static void PrepareInstallUpdate()
        {
            if (EditorApplication.isCompiling) return;
            if (EditorApplication.isUpdating) return;

            EditorApplication.update -= PrepareInstallUpdate;

            InstallPatch();
        }

        private static void CompleteInstallation(){
            if (EditorApplication.isCompiling) return;
            if (EditorApplication.isUpdating) return;
            
            EditorApplication.update -= CompleteInstallation;
            
            PhotonEditorUtils.AddScriptingDefineSymbolToAllBuildTargetGroups(SYMBOL);
            Debug.LogFormat("Melee Patch installed!");
        }

        private static void InstallPatch()
        {
            Debug.LogFormat("Installing Melee Patch...");
            
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            SceneSetup[] scenesSetup = EditorSceneManager.GetSceneManagerSetup();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            AssetDatabase.ImportPackage(
                Path.Combine(ASSETS_PATH, PATCH_PATH + "Patch.unitypackage"),
                false
            );
            
            if (scenesSetup != null && scenesSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(scenesSetup);
            }
            
            // string assetPath = Path.Combine(Application.dataPath, PATCH_PATH);
            // if (File.Exists(assetPath) || Directory.Exists(assetPath))
            // {
            //     #if NJG_TEST
            //     Debug.LogFormat("Delete: {0}", assetPath);
            //     #else 
            //     FileUtil.DeleteFileOrDirectory(assetPath);
            //     #endif
            // }
            installed = true;
            Debug.LogFormat("Melee Patch processed!");
            EditorApplication.update += CompleteInstallation;
        }
        #endif
    }
}