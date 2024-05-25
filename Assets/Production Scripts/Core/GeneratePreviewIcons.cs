using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using Vi.ScriptableObjects;
using UnityEngine.InputSystem;

namespace Vi.Editor
{
    public class GeneratePreviewIcons : MonoBehaviour
    {
        [SerializeField] private WeaponPositioningData[] weaponsToGenerate;
        [SerializeField] private CharacterReference characterReference;

        [System.Serializable]
        private class WeaponPositioningData
        {
            public Weapon weapon;
            public List<TransformData> transformDatas;
        }

        [System.Serializable]
        private class TransformData
        {
            public Vector3 position;
            public Vector3 rotation;
        }

        private Camera cam;

        private void Start()
        {
            cam = GetComponent<Camera>();
            StartCoroutine(TakeScreenshot());
        }

        private IEnumerator TakeScreenshot()
        {
            int weaponIndex = 0;
            foreach (WeaponPositioningData weaponPositioningData in weaponsToGenerate)
            {
                cam.clearFlags = CameraClearFlags.Skybox;
                Dictionary<GameObject, int> transformCrosswalk = new Dictionary<GameObject, int>();

                foreach (var data in weaponPositioningData.weapon.GetWeaponModelData())
                {
                    if (data.skinPrefab.name == "Human_Male")
                    {
                        int i = 0;
                        foreach (var d in data.data)
                        {
                            if (!d.weaponPrefab.GetComponentInChildren<Renderer>()) { continue; }

                            Vector3 pos = Vector3.zero;
                            Quaternion rot = Quaternion.identity;

                            if (i < weaponPositioningData.transformDatas.Count)
                            {
                                pos = weaponPositioningData.transformDatas[i].position;
                                rot = Quaternion.Euler(weaponPositioningData.transformDatas[i].rotation);
                            }

                            transformCrosswalk.Add(Instantiate(d.weaponPrefab, pos, rot), i);
                            i++;
                        }
                    }
                }

                yield return new WaitUntil(() => !Keyboard.current.spaceKey.isPressed);
                yield return new WaitUntil(() => Keyboard.current.spaceKey.isPressed);
                yield return new WaitUntil(() => !Keyboard.current.spaceKey.isPressed);

                cam.clearFlags = CameraClearFlags.Depth;

                yield return new WaitForEndOfFrame();

                int width = Screen.width;
                int height = Screen.height;
                Texture2D screenshotTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
                Rect rect = new Rect(0, 0, width, height);
                screenshotTexture.ReadPixels(rect, 0, 0);
                screenshotTexture.Apply();

                byte[] byteArray = screenshotTexture.EncodeToPNG();
                string destinationPath = Path.Join("Assets/Production/Images/Weapon Icons", weaponPositioningData.weapon.name + ".png");
                File.WriteAllBytes(destinationPath, byteArray);

                foreach (KeyValuePair<GameObject, int> kvp in transformCrosswalk)
                {
                    var posData = weaponsToGenerate[weaponIndex];

                    var transformData = posData.transformDatas.Count > kvp.Value ? posData.transformDatas[kvp.Value] : new TransformData();
                    transformData.position = kvp.Key.transform.position;
                    transformData.rotation = kvp.Key.transform.eulerAngles;

                    if (posData.transformDatas.Count > kvp.Value)
                        posData.transformDatas[kvp.Value] = transformData;
                    else
                        posData.transformDatas.Add(transformData);

                    weaponsToGenerate[weaponIndex] = posData;
                    Destroy(kvp.Key);
                }

                yield return null;
                weaponIndex++;
            }
        }
    }
}