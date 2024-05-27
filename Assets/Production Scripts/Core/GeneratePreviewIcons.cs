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
        [SerializeField] private bool shouldProcessWeapons;
        [SerializeField] private WeaponPositioningData[] weaponsToGenerate;
        [SerializeField] private bool shouldProcessEquipmentOptions;
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
        private Vector3 startPosition;

        private void Start()
        {
            cam = GetComponent<Camera>();
            startPosition = transform.position;
            if (shouldProcessWeapons) { StartCoroutine(TakeScreenshotsOfWeapons()); }
            if (shouldProcessEquipmentOptions) { StartCoroutine(TakeScreenshotsOfEquipment()); }
        }

        private IEnumerator TakeScreenshotsOfWeapons()
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

        private static readonly Dictionary<CharacterReference.EquipmentType, Vector3> equipmentTypeRotations = new Dictionary<CharacterReference.EquipmentType, Vector3>()
        {
            { CharacterReference.EquipmentType.Beard, new Vector3(0, -90, 0) },
            { CharacterReference.EquipmentType.Belt, new Vector3(0, -90, 0) },
            { CharacterReference.EquipmentType.Boots, new Vector3(45, -90, 0) },
            { CharacterReference.EquipmentType.Brows, new Vector3(0, -90, 0) },
            { CharacterReference.EquipmentType.Cape, new Vector3(0, -90, 0) },
            { CharacterReference.EquipmentType.Chest, new Vector3(0, -90, 0) },
            { CharacterReference.EquipmentType.Gloves, new Vector3(0, -90, 0) },
            { CharacterReference.EquipmentType.Hair, new Vector3(0, -90, 0) },
            { CharacterReference.EquipmentType.Helm, new Vector3(0, -90, 0) },
            { CharacterReference.EquipmentType.Pants, new Vector3(0, -90, 0) },
            { CharacterReference.EquipmentType.Robe, new Vector3(0, -90, 0) },
            { CharacterReference.EquipmentType.Shoulders, new Vector3(0, -90, 0) }
        };

        private IEnumerator TakeScreenshotsOfEquipment()
        {
            foreach (CharacterReference.RaceAndGender raceAndGender in System.Enum.GetValues(typeof(CharacterReference.RaceAndGender)))
            {
                foreach (CharacterReference.WearableEquipmentOption equipmentOption in characterReference.GetArmorEquipmentOptions(raceAndGender))
                {
                    WearableEquipment model = equipmentOption.GetModel(raceAndGender, null);
                    if (model)
                    {
                        if (!model.GetComponentInChildren<SkinnedMeshRenderer>()) { Debug.LogError(model.name + " has no skinned mesh renderer"); continue; }

                        cam.clearFlags = CameraClearFlags.Skybox;

                        GameObject instance = Instantiate(model.gameObject);
                        SkinnedMeshRenderer skinnedMeshRenderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();
                        instance.transform.localScale = new Vector3(instance.transform.localScale.x / skinnedMeshRenderer.transform.localScale.x, instance.transform.localScale.y / skinnedMeshRenderer.transform.localScale.y, instance.transform.localScale.z / skinnedMeshRenderer.transform.localScale.z);
                        instance.transform.localScale = Vector3.ClampMagnitude(instance.transform.localScale, 173);
                        instance.transform.eulerAngles = equipmentTypeRotations[equipmentOption.equipmentType];

                        float multiplierOffset = 0;

                        yield return new WaitUntil(() => !Keyboard.current.spaceKey.isPressed);
                        while (true)
                        {
                            if (Keyboard.current.spaceKey.isPressed) { break; }

                            if (Keyboard.current.wKey.isPressed) { multiplierOffset -= 1; if (!Keyboard.current.shiftKey.isPressed) yield return new WaitForSeconds(0.1f); }
                            if (Keyboard.current.sKey.isPressed) { multiplierOffset += 1; if (!Keyboard.current.shiftKey.isPressed) yield return new WaitForSeconds(0.1f); }

                            if (Keyboard.current.upArrowKey.isPressed) { instance.transform.eulerAngles += new Vector3(45, 0, 0); }
                            yield return new WaitUntil(() => !Keyboard.current.upArrowKey.isPressed);
                            if (Keyboard.current.downArrowKey.isPressed) { instance.transform.eulerAngles += new Vector3(-45, 0, 0); }
                            yield return new WaitUntil(() => !Keyboard.current.downArrowKey.isPressed);

                            if (Keyboard.current.leftArrowKey.isPressed) { instance.transform.eulerAngles += new Vector3(0, 45, 0); }
                            yield return new WaitUntil(() => !Keyboard.current.leftArrowKey.isPressed);
                            if (Keyboard.current.rightArrowKey.isPressed) { instance.transform.eulerAngles += new Vector3(0, -45, 0); }
                            yield return new WaitUntil(() => !Keyboard.current.rightArrowKey.isPressed);

                            Vector3 camOffset = new Vector3(skinnedMeshRenderer.bounds.center.x, skinnedMeshRenderer.bounds.center.y, instance.transform.position.z);
                            camOffset.x -= Mathf.Max(Mathf.Max(skinnedMeshRenderer.bounds.extents.x, skinnedMeshRenderer.bounds.extents.y), skinnedMeshRenderer.bounds.extents.z) * (2 + multiplierOffset);

                            transform.position = startPosition + camOffset;

                            yield return null;
                        }

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
                        string destinationPath = Path.Join("Assets/Production/Images/Equipment Icons", equipmentOption.name + "-" + raceAndGender.ToString() + ".png");
                        File.WriteAllBytes(destinationPath, byteArray);

                        Destroy(instance);

                        yield return null;
                    }
                }

                foreach (CharacterReference.WearableEquipmentOption equipmentOption in characterReference.GetCharacterEquipmentOptions(raceAndGender))
                {
                    WearableEquipment model = equipmentOption.GetModel(raceAndGender, null);
                    if (model)
                    {
                        if (!model.GetComponentInChildren<SkinnedMeshRenderer>()) { Debug.LogError(model.name + " has no skinned mesh renderer"); continue; }

                        cam.clearFlags = CameraClearFlags.Skybox;

                        GameObject instance = Instantiate(model.gameObject);
                        SkinnedMeshRenderer skinnedMeshRenderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();
                        instance.transform.localScale = new Vector3(instance.transform.localScale.x / skinnedMeshRenderer.transform.localScale.x, instance.transform.localScale.y / skinnedMeshRenderer.transform.localScale.y, instance.transform.localScale.z / skinnedMeshRenderer.transform.localScale.z);
                        instance.transform.localScale = Vector3.ClampMagnitude(instance.transform.localScale, 173);
                        instance.transform.eulerAngles = equipmentTypeRotations[equipmentOption.equipmentType];

                        float multiplierOffset = 0;

                        yield return new WaitUntil(() => !Keyboard.current.spaceKey.isPressed);
                        while (true)
                        {
                            if (Keyboard.current.spaceKey.isPressed) { break; }

                            if (Keyboard.current.wKey.isPressed) { multiplierOffset -= 1; if (!Keyboard.current.shiftKey.isPressed) yield return new WaitForSeconds(0.1f); }
                            if (Keyboard.current.sKey.isPressed) { multiplierOffset += 1; if (!Keyboard.current.shiftKey.isPressed) yield return new WaitForSeconds(0.1f); }

                            if (Keyboard.current.upArrowKey.isPressed) { instance.transform.eulerAngles += new Vector3(45, 0, 0); }
                            yield return new WaitUntil(() => !Keyboard.current.upArrowKey.isPressed);
                            if (Keyboard.current.downArrowKey.isPressed) { instance.transform.eulerAngles += new Vector3(-45, 0, 0); }
                            yield return new WaitUntil(() => !Keyboard.current.downArrowKey.isPressed);

                            if (Keyboard.current.leftArrowKey.isPressed) { instance.transform.eulerAngles += new Vector3(0, 45, 0); }
                            yield return new WaitUntil(() => !Keyboard.current.leftArrowKey.isPressed);
                            if (Keyboard.current.rightArrowKey.isPressed) { instance.transform.eulerAngles += new Vector3(0, -45, 0); }
                            yield return new WaitUntil(() => !Keyboard.current.rightArrowKey.isPressed);

                            Vector3 camOffset = new Vector3(skinnedMeshRenderer.bounds.center.x, skinnedMeshRenderer.bounds.center.y, instance.transform.position.z);
                            camOffset.x -= Mathf.Max(Mathf.Max(skinnedMeshRenderer.bounds.extents.x, skinnedMeshRenderer.bounds.extents.y), skinnedMeshRenderer.bounds.extents.z) * (2 + multiplierOffset);

                            transform.position = startPosition + camOffset;

                            yield return null;
                        }

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
                        string destinationPath = Path.Join("Assets/Production/Images/Equipment Icons", equipmentOption.name + "-" + raceAndGender.ToString() + ".png");
                        File.WriteAllBytes(destinationPath, byteArray);

                        Destroy(instance);

                        yield return null;
                    }
                }
            }

            transform.position = startPosition;
        }
    }
}