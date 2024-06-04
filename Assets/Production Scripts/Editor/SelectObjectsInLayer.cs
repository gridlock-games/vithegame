using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Vi.Editor
{
    public class SelectObjectsInLayer : UnityEditor.Editor
    {
        [MenuItem("Tools/Convert Projectile Layers To Colliders")]
        static void SelectGameObjectsInLayer()
        {
            foreach (GameObject g in FindGameObjectsInLayer(LayerMask.NameToLayer("Projectile")))
            {
                Debug.Log(g);
                g.layer = LayerMask.NameToLayer("ProjectileCollider");
                EditorUtility.SetDirty(g);
            }
        }

        private static GameObject[] FindGameObjectsInLayer(int layer)
        {
            var goArray = FindObjectsOfType(typeof(GameObject)) as GameObject[];
            var goList = new List<GameObject>();
            for (int i = 0; i < goArray.Length; i++)
            {
                if (goArray[i].layer == layer)
                {
                    goList.Add(goArray[i]);
                }
            }
            if (goList.Count == 0)
            {
                return null;
            }
            return goList.ToArray();
        }
    }
}