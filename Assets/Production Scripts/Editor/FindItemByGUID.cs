using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Vi.Editor
{
    public class FindItemByGUID : UnityEditor.Editor
    {
        [MenuItem("Tools/Find Item By GUID")]
        static void FindItemByGUIDMethod()
        {
            string guid = "56f1fae43c882434d94c645713a29ec6";
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.Length == 0) p = "not found";
            Debug.Log(p);
        }
    }
}