#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif
using GameCreator.ModuleManager;

using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Realtime;
using UnityEngine;

namespace NJG.PUN
{
    public static class ModuleInitilize
    {
        public const string SYMBOL_STATS = "PHOTON_STATS";
        public const string SYMBOL_PHOTON = "PHOTON_MODULE";
        public const string SYMBOL_NPC = "PHOTON_RPG";
        public const string STATS_PATH = "Assets/Plugins/GameCreator/Stats";
        public const string AI_PATH = "Assets/Ninjutsu Games/GameCreator Modules/RPG";
        //public const string STATS_PATH2 = "Assets/Plugins/GameCreatorData/Stats";

        /// <summary>
        /// Add define symbols as soon as Unity gets done compiling.
        /// </summary>
        [InitializeOnLoadMethod]
        static void Initilize()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
#if PHOTON_UNITY_NETWORKING && !PHOTON_STATS
            var module = ModuleManager.GetAssetModule("com.gamecreator.module.stats");

            bool HasStats = module != null && module.module != null && ModuleManager.IsEnabled(module.module);

            if (HasStats)
            {
                PhotonEditorUtils.AddScriptingDefineSymbolToAllBuildTargetGroups(SYMBOL_STATS);
            }
#endif

#if PHOTON_UNITY_NETWORKING && !PHOTON_RPG
            var module2 = ModuleManager.GetAssetModule("com.ninjutsugames.module.rpg");

            bool HasAI = module2 != null && module2.module != null && ModuleManager.IsEnabled(module2.module);

            if (HasAI)
            {
                PhotonEditorUtils.AddScriptingDefineSymbolToAllBuildTargetGroups(SYMBOL_NPC);
            }
#endif

#if PHOTON_UNITY_NETWORKING && !PHOTON_MODULE
            var module3 = ModuleManager.GetAssetModule("com.ninjutsugames.module.photon");

            bool HasStats3 = module3 != null && module3.module != null && ModuleManager.IsEnabled(module3.module);

            if (HasStats3)
            {
                PhotonEditorUtils.AddScriptingDefineSymbolToAllBuildTargetGroups(SYMBOL_PHOTON);
            }
#endif
            }
        }

        public static void CleanUpDefineSymbols()
        {
            foreach (BuildTarget target in Enum.GetValues(typeof(BuildTarget)))
            {
                BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);

                if (group == BuildTargetGroup.Unknown)
                {
                    continue;
                }

                var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group)
                    .Split(';')
                    .Select(d => d.Trim())
                    .ToList();

                List<string> newDefineSymbols = new List<string>();
                foreach (var symbol in defineSymbols)
                {
                    if (SYMBOL_STATS.Equals(symbol) || symbol.StartsWith(SYMBOL_STATS) || SYMBOL_NPC.Equals(symbol) || symbol.StartsWith(SYMBOL_NPC))
                    {
                        continue;
                    }

                    newDefineSymbols.Add(symbol);
                }

                try
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", newDefineSymbols.ToArray()));
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("Could not set clean up STATS's define symbols for build target: {0} group: {1}, {2}", target, group, e);
                }
            }
        }
    }

    public class CleanUpDefinesOnPunDelete : UnityEditor.AssetModificationProcessor
    {
        public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions rao)
        {
            if (ModuleInitilize.STATS_PATH.Equals(assetPath) || ModuleInitilize.AI_PATH.Equals(assetPath))
            {
                ModuleInitilize.CleanUpDefineSymbols();
            }

            return AssetDeleteResult.DidNotDelete;
        }
    }
}
