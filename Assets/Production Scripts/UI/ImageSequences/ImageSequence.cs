using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace Vi.UI
{
    [CreateAssetMenu(fileName = "ImageSequence", menuName = "Production/Image Sequence")]
    public class ImageSequence : ScriptableObject
    {
        [SerializeField] private List<Sprite> sprites;

#if UNITY_EDITOR
        [ContextMenu("Find Images")]
        private void FindImages()
        {
            string assetPath = AssetDatabase.GetAssetPath(this);
            string filenameNoExtension = Path.GetFileNameWithoutExtension(assetPath);
            string folderPath = Path.Join(assetPath.Substring(0, assetPath.LastIndexOf('/')), filenameNoExtension);

            if (AssetDatabase.IsValidFolder(folderPath))
            {
                string[] guidList = AssetDatabase.FindAssets("t:Sprite", new string[] { folderPath });

                if (guidList.Length == 0)
                {
                    Debug.LogWarning("No sprites found in folder " + folderPath);
                }
                else
                {
                    sprites.Clear();
                }

                foreach (string guid in guidList)
                {
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
                    if (sprite)
                    {
                        sprites.Add(sprite);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Couldn't find a valid folder at path: " + folderPath);
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            EditorUtility.UnloadUnusedAssetsImmediate();
        }
#endif
    }
}
