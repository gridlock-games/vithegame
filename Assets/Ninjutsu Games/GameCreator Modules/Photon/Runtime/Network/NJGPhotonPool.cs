using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace NJG.PUN
{
    public class NJGPhotonPool : IPunPrefabPool
    {
        /// <summary>Contains a GameObject per prefabId, to speed up instantiation.</summary>
        public Dictionary<string, GameObject> ResourceCache;

        public NJGPhotonPool(DatabasePhoton db)
        {
            ResourceCache = new Dictionary<string, GameObject>();
            for (int i = 0; i < db.prefabs.Count; i++)
            {
                if (db.prefabs[i] == null) continue;

                if (!ResourceCache.ContainsKey(db.prefabs[i].name))
                {
                    ResourceCache.Add(db.prefabs[i].name, db.prefabs[i]);
                }
            }
        }

        /// <summary>Returns an inactive instance of a networked GameObject, to be used by PUN.</summary>
        /// <param name="prefabId">String identifier for the networked object.</param>
        /// <param name="position">Location of the new object.</param>
        /// <param name="rotation">Rotation of the new object.</param>
        /// <returns></returns>
        public virtual GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
        {
            GameObject res = null;
            bool cached = ResourceCache.TryGetValue(prefabId, out res);
            if (!cached)
            {
                res = (GameObject)Resources.Load(prefabId, typeof(GameObject));
                if (!res)
                {
                    Debug.LogErrorFormat("DefaultPool failed to load \"{0}\" . Make sure it's in a \"Resources\" folder or added in Cached Prefabas in Photon database.", prefabId);
                }
                else
                {
                    ResourceCache.Add(prefabId, res);
                }
            }
            if (res)
            {
                bool wasActive = res.activeSelf;
                if (wasActive) res.SetActive(false);

                GameObject instance = Object.Instantiate(res, position, rotation);

                if (wasActive) res.SetActive(true);
                return instance;
            }
            return null;
        }

        /// <summary>Simply destroys a GameObject.</summary>
        /// <param name="gameObject">The GameObject to get rid of.</param>
        public virtual void Destroy(GameObject gameObject)
        {
            Object.Destroy(gameObject);
        }
    }
}
