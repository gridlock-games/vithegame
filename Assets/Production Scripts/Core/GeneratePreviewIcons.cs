using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using Vi.ScriptableObjects;

namespace Vi.Editor
{
    public class GeneratePreviewIcons : MonoBehaviour
    {
        [SerializeField] private Weapon[] weaponsToGenerate;

        private Camera cam;

        private void Start()
        {
            cam = GetComponent<Camera>();
            StartCoroutine(TakeScreenshot());
        }

        private IEnumerator TakeScreenshot()
        {
            foreach (Weapon weapon in weaponsToGenerate)
            {
                cam.clearFlags = CameraClearFlags.Skybox;
                List<GameObject> instanceList = new List<GameObject>();

                foreach (var data in weapon.GetWeaponModelData())
                {
                    if (data.skinPrefab.name.Contains("Male"))
                    {
                        foreach (var d in data.data)
                        {
                            instanceList.Add(Instantiate(d.weaponPrefab));
                        }
                    }
                }

                yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

                cam.clearFlags = CameraClearFlags.Depth;

                yield return new WaitForEndOfFrame();

                int width = Screen.width;
                int height = Screen.height;
                Texture2D screenshotTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
                Rect rect = new Rect(0, 0, width, height);
                screenshotTexture.ReadPixels(rect, 0, 0);
                screenshotTexture.Apply();

                byte[] byteArray = screenshotTexture.EncodeToPNG();
                string destinationPath = Path.Join("Assets/Production/Images/Weapon Icons", weapon.name + ".png");
                File.WriteAllBytes(destinationPath, byteArray);

                foreach (GameObject instance in instanceList)
                {
                    Destroy(instance);
                }

                yield return null;
            }
        }
    }
}