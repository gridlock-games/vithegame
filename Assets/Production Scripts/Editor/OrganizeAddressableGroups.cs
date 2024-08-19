using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor;
using System.IO;

namespace Vi.Editor
{
    public static class OrganizeAddressableGroups
    {
        private static bool IsAssetAddressable(Object obj)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj)));
            return entry != null;
        }

        [MenuItem("Tools/Organize Addressable Groups")]
        private static void OrganizeAddressables()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            AddressableAssetGroup groupToOrganize = settings.FindGroup(item => item.Name == "Duplicate Asset Isolation");

            if (!groupToOrganize) { Debug.LogError("No duplicate asset group found!"); }

            int entryIndex = 0;
            foreach (AddressableAssetEntry entry in groupToOrganize.entries)
            {
                string[] directories = entry.AssetPath.Split('/');
                AddressableAssetGroup groupToModify = settings.FindGroup(item => item.Name == directories[^2]);

                if (EditorUtility.DisplayCancelableProgressBar("Organizing Addressable Group: " + directories[^2],
                        (entryIndex + 1).ToString() + " out of " + groupToOrganize.entries.Count.ToString() + " assets " + entry.TargetAsset.name,
                        entryIndex / groupToOrganize.entries.Count))
                { break; }

                if (!groupToModify)
                {
                    groupToModify = settings.CreateGroup(directories[^2], false, false, false, null, null);
                    Debug.Log(groupToModify);
                }
                //settings.MoveEntry(entry, groupToModify);
            }
            EditorUtility.ClearProgressBar();
        }
    }
}