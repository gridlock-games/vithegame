using System.IO;
using GameCreator.Core;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;

namespace NJG.PUN
{
    public class NJGUtilities : MonoBehaviour
    {
        public static string TRAVERSAL_PATH = "Assets/Plugins/GameCreator/Traversal/Prefabs";
        public static string TRAVERSAL_NETWORK_PATH = "Assets/Ninjutsu Games/GameCreator Modules/PhotonTraversal/Prefabs";
        public static T ReplacePrefab<T>(MonoBehaviour target, string targetPath) where T : MonoBehaviour
        {
            if (target == null) return null;
            if (target.gameObject == null) return null;
            
            string prefabPath = null;
            GameObject root = null;
            
            Debug.LogWarningFormat("IsPrefab: {0} isInstance: {1}", PrefabUtility.IsPartOfAnyPrefab(target.gameObject), PrefabUtility.IsPartOfPrefabInstance(target.gameObject));
            if (PrefabUtility.IsPartOfAnyPrefab(target.gameObject) && PrefabUtility.IsPartOfPrefabInstance(target.gameObject))
            {
                root = PrefabUtility.GetOutermostPrefabInstanceRoot(target);
                prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot,
                    InteractionMode.AutomatedAction);
            }

            //ComponentUtility.CopyComponent(target);
            var tmpGO = new GameObject("tempOBJ");
            tmpGO.hideFlags = HideFlags.DontSave;
            var inst = tmpGO.AddComponent<T>();
            EditorUtility.CopySerialized(target, inst);
            MonoScript yourReplacementScript = MonoScript.FromMonoBehaviour(inst);

            SerializedObject so = new SerializedObject(target);
            SerializedProperty scriptProperty = so.FindProperty("m_Script");
            so.Update();
            scriptProperty.objectReferenceValue = yourReplacementScript;
            so.ApplyModifiedProperties();
            
            DestroyImmediate(tmpGO);

            T c = so.targetObject as T;
            //ComponentUtility.PasteComponentValues(c);

            if (!string.IsNullOrEmpty(prefabPath))
            {
                string finalPath = string.Format("{0}/{1}", targetPath, Path.GetFileName(prefabPath));
                // Debug.LogWarningFormat("prefabPath {0} to {1}", prefabPath, finalPath);
                PrefabUtility.SaveAsPrefabAssetAndConnect(root, finalPath, InteractionMode.AutomatedAction);
            }

            return c;
        }
        public static T ReplaceComponent<T>(MonoBehaviour target) where T : MonoBehaviour
        {
            if (target == null) return null;
            if (target.gameObject == null) return null;
            
            string prefabPath = null;
            GameObject root = null;
            Debug.LogWarningFormat("IsPrefab: {0} isInstance: {1}", PrefabUtility.IsPartOfAnyPrefab(target.gameObject), PrefabUtility.IsPartOfPrefabInstance(target.gameObject));
            if (PrefabUtility.IsPartOfAnyPrefab(target.gameObject) && PrefabUtility.IsPartOfPrefabInstance(target.gameObject))
            {
                root = PrefabUtility.GetOutermostPrefabInstanceRoot(target);
                prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot,
                    InteractionMode.AutomatedAction);
            }

            //ComponentUtility.CopyComponent(target);
            var tmpGO = new GameObject("tempOBJ");
            tmpGO.hideFlags = HideFlags.DontSave;
            var inst = tmpGO.AddComponent<T>();
            EditorUtility.CopySerialized(target, inst);
            MonoScript yourReplacementScript = MonoScript.FromMonoBehaviour(inst);

            SerializedObject so = new SerializedObject(target);
            SerializedProperty scriptProperty = so.FindProperty("m_Script");
            so.Update();
            scriptProperty.objectReferenceValue = yourReplacementScript;
            so.ApplyModifiedProperties();
            
            DestroyImmediate(tmpGO);

            T c = so.targetObject as T;
            //ComponentUtility.PasteComponentValues(c);

            if (!string.IsNullOrEmpty(prefabPath))
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);
            }

            return c;
        }
        
        public class Section
        {
            private const float ANIM_BOOL_SPEED = 3f;
            public GUIContent name;
            public AnimBool state;

            private string KEY_STATE = "network-{0}-section-{1}";
            private string key;

            public Section(string name, Texture2D icon, UnityAction repaint, string key)
            {
                this.name = new GUIContent($" {name}", icon);
                state = new AnimBool(GetState());
                state.speed = ANIM_BOOL_SPEED;
                state.valueChanged.AddListener(repaint);
                this.key = key;
            }

            public void PaintSection()
            {
                GUIStyle buttonStyle = (state.target
                        ? CoreGUIStyles.GetToggleButtonNormalOn()
                        : CoreGUIStyles.GetToggleButtonNormalOff()
                    );

                if (GUILayout.Button(name, buttonStyle))
                {
                    state.target = !state.target;
                    string key = string.Format(KEY_STATE, this.key, name.text.GetHashCode());
                    EditorPrefs.SetBool(key, state.target);
                }
            }

            private bool GetState()
            {
                string key = string.Format(KEY_STATE, this.key, name.text.GetHashCode());
                return EditorPrefs.GetBool(key, true);
            }
        }
    }
}