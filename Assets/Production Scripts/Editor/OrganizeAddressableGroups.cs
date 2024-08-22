using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor;
using System.Linq;

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

            if (!groupToOrganize) { Debug.LogError("No duplicate asset group found!"); return; }

            int entryIndex = 0;
            foreach (AddressableAssetEntry entry in groupToOrganize.entries.ToArray())
            {
                string[] directories = entry.AssetPath.Split('/');
                string targetGroupName = "";
                for (int i = 0; i < directories.Length-1; i++)
                {
                    targetGroupName += directories[i] + " ";
                }
                targetGroupName.Trim();

                AddressableAssetGroup groupToModify = settings.FindGroup(item => item.Name == targetGroupName);

                if (EditorUtility.DisplayCancelableProgressBar("Organizing Addressable Group: " + targetGroupName,
                        (entryIndex + 1).ToString() + " out of " + groupToOrganize.entries.Count.ToString() + " assets " + entry.TargetAsset.name,
                        entryIndex / groupToOrganize.entries.Count))
                { break; }

                if (!groupToModify)
                {
                    groupToModify = settings.CreateGroup(targetGroupName, false, false, false, groupToOrganize.Schemas, groupToOrganize.SchemaTypes.ToArray());
                }
                settings.MoveEntry(entry, groupToModify);
            }
            settings.RemoveGroup(groupToOrganize);
            EditorUtility.ClearProgressBar();
        }
    }
}