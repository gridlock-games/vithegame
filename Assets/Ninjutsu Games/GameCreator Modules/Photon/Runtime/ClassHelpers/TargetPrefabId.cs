using System;
using GameCreator.Variables;
using UnityEngine;

[Serializable]
public class PrefabIDAttribute : PropertyAttribute { }

namespace GameCreator.Core
{
    [Serializable]
    public class TargetPrefabId
    {
        public enum Target
        {
            CachedPrefab,
            GameObject
        }

        // PROPERTIES: ----------------------------------------------------------------------------

        public Target target = Target.CachedPrefab;
        public GameObjectProperty gameObject;
        [PrefabID] public string prefab;

        // PUBLIC METHODS: ------------------------------------------------------------------------

        public string GetPrefabId()
        {
            string result = null;

            switch (target)
            {
                case Target.CachedPrefab:
                    result = prefab;
                    break;
                case Target.GameObject:
                    result = gameObject.GetValue(null).name;
                    break;
            }

            return result;
        }

        // UTILITIES: -----------------------------------------------------------------------------

        public override string ToString()
        {
            string result = "(unknown)";
            switch (target)
            {
                case Target.CachedPrefab: result = string.IsNullOrEmpty(prefab) ? "(none)" : prefab; break;
                case Target.GameObject: result = gameObject.ToString(); break;
            }

            return result;
        }
    }
}