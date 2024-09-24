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

            // Organize the duplicate asset isolation group into different groups based on asset path
            int entryIndex = 0;
            foreach (AddressableAssetEntry entry in groupToOrganize.entries.ToArray())
            {
                Debug.Log(entryIndex);
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

                try
                {
                    //if (!groupToModify)
                    //{
                    //    groupToModify = settings.CreateGroup(targetGroupName, false, false, false, groupToOrganize.Schemas, groupToOrganize.SchemaTypes.ToArray());
                    //}
                    //settings.MoveEntry(entry, groupToModify);
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                    EditorUtility.ClearProgressBar();
                    return;
                }
                break;
            }

            EditorUtility.ClearProgressBar();

            // Remove groups with 0 entries in them
            foreach (AddressableAssetGroup group in settings.groups.ToArray())
            {
                List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>();
                group.GatherAllAssets(entries, true, true, true);

                if (entries.Count == 0) { Debug.Log("Removing group " + group.name); settings.RemoveGroup(group); }
            }
        }
    }
}