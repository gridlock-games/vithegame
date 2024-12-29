using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vi.Core;
using System.Linq;
using Vi.Utility;
using Newtonsoft.Json;
using Vi.Player;

namespace Vi.UI
{
    public class UIModificationMenu : Menu
    {
        [SerializeField] private PlayerUI playerUIPrefab;
        [SerializeField] private RectTransform UIMimicParent;

        private Dictionary<GameObject, GameObject> prefabCrosswalk = new Dictionary<GameObject, GameObject>();
        private void OnEnable()
        {
            foreach (KeyValuePair<GameObject, GameObject> kvp in prefabCrosswalk)
            {
                Destroy(kvp.Key);
            }
            prefabCrosswalk.Clear();

            PlatformUIDefinition platformUIDefinitionComponent = playerUIPrefab.GetComponent<PlatformUIDefinition>();
            PlatformUIDefinition.UIDefinition[] platformUIDefinitions = platformUIDefinitionComponent.GetPlatformUIDefinitions();

            foreach (Transform child in playerUIPrefab.transform)
            {
                if (child.name == "TextChat") { continue; }

                GameObject copy = Instantiate(child.gameObject, UIMimicParent);

                Transform[] copyChildren = copy.GetComponentsInChildren<Transform>(true);
                Transform[] originalChildren = child.GetComponentsInChildren<Transform>(true);

                foreach (Transform copyChild in copyChildren)
                {
                    int originalIndex = System.Array.FindIndex(originalChildren, originalItem => copyChild.name.Replace("(Clone)", "") == originalItem.name);
                    if (originalIndex != -1)
                    {
                        prefabCrosswalk.Add(copyChild.gameObject, originalChildren[originalIndex].gameObject);
                    }
                }

                foreach (KeyValuePair<GameObject, GameObject> kvp in prefabCrosswalk)
                {
                    GameObject copyChild = kvp.Key;
                    GameObject originalChild = kvp.Value;

                    if (copyChild.TryGetComponent(out KillFeed killFeed))
                    {
                        killFeed.SetPreviewOn();
                        continue;
                    }
                    if (copyChild.TryGetComponent(out RuntimeWeaponCard weaponCard))
                    {
                        weaponCard.SetPreviewOn(weaponCard.name.Contains("Primary") ? LoadoutManager.WeaponSlotType.Primary : LoadoutManager.WeaponSlotType.Secondary);
                        continue;
                    }
                    if (copyChild.TryGetComponent(out AbilityCard abilityCard)) { abilityCard.SetPreviewOn(); }
                    if (copyChild.TryGetComponent(out PotionCard potionCard)) { potionCard.SetPreviewOn(); }
                    if (copyChild.GetComponent<KillFeedElement>()) { continue; }

                    foreach (Behaviour c in copyChild.GetComponents<Behaviour>())
                    {
                        if (c is Graphic) { continue; }
                        if (c is Canvas) { continue; }
                        if (c is GraphicRaycaster) { continue; }
                        if (c is LayoutGroup) { continue; }
                        if (c is ContentSizeFitter) { continue; }
                        if (c.GetType().ToString() == "DuloGames.UI.UIHighlightTransition") { continue; }
                        c.enabled = false;
                    }

                    foreach (PlatformUIDefinition.UIDefinition platformUIDefinition in platformUIDefinitions)
                    {
                        foreach (GameObject g in platformUIDefinition.gameObjectsToEnable)
                        {
                            if (g == originalChild)
                            {
                                copyChild.gameObject.SetActive(platformUIDefinition.platforms.Contains(Application.platform));
                            }
                        }

                        foreach (GameObject g in platformUIDefinition.gameObjectsToDisable)
                        {
                            if (g == originalChild)
                            {
                                copyChild.SetActive(!platformUIDefinition.platforms.Contains(Application.platform));
                            }
                        }

                        foreach (PlatformUIDefinition.MoveUIDefinition moveUIDefinition in platformUIDefinition.objectsToMove)
                        {
                            if (moveUIDefinition.gameObjectToMove == originalChild.gameObject)
                            {
                                if (platformUIDefinition.platforms.Contains(Application.platform))
                                {
                                    RectTransform rt = (RectTransform)copyChild.transform;
                                    if (moveUIDefinition.shouldOverrideAnchors)
                                    {
                                        rt.anchorMin = moveUIDefinition.anchorMinOverride;
                                        rt.anchorMax = moveUIDefinition.anchorMaxOverride;
                                        rt.pivot = moveUIDefinition.pivotOverride;
                                    }
                                    rt.anchoredPosition = moveUIDefinition.newAnchoredPosition;
                                }
                            }
                        }

                        foreach (GameObject g in platformUIDefinition.gameObjectsToDestroy)
                        {
                            if (g == originalChild.gameObject)
                            {
                                if (platformUIDefinition.platforms.Contains(Application.platform)) { Destroy(copyChild.gameObject); }
                            }
                        }
                    }

                    if (PlatformUIDefinition.UIElementIsAbleToBeModified(copyChild.gameObject))
                    {
                        if (FasterPlayerPrefs.Singleton.HasString("UIOverrides"))
                        {
                            List<PlatformUIDefinition.PositionOverrideDefinition> positionOverrideDefinitions = JsonConvert.DeserializeObject<List<PlatformUIDefinition.PositionOverrideDefinition>>(FasterPlayerPrefs.Singleton.GetString("UIOverrides"));
                            foreach (PlatformUIDefinition.PositionOverrideDefinition positionOverrideDefinition in positionOverrideDefinitions)
                            {
                                GameObject g = platformUIDefinitionComponent.GetGameObjectFromPath(positionOverrideDefinition.gameObjectPath);
                                if (g == originalChild)
                                {
                                    ((RectTransform)copyChild.transform).anchoredPosition = new Vector2(positionOverrideDefinition.newAnchoredX, positionOverrideDefinition.newAnchoredY);
                                }
                            }
                        }
                        DraggableUIObject draggableUIObject = copyChild.AddComponent<DraggableUIObject>();
                        draggableUIObject.Initialize(this);
                    }
                }
            }
        }

        public void OnDraggableUIObject(DraggableUIObject draggableUIObject)
        {
            RectTransform modifiedRect = (RectTransform)draggableUIObject.transform;
            GameObject prefabRef = prefabCrosswalk[draggableUIObject.gameObject];

            List<PlatformUIDefinition.PositionOverrideDefinition> overridesList;
            if (FasterPlayerPrefs.Singleton.HasString("UIOverrides"))
            {
                overridesList = JsonConvert.DeserializeObject<List<PlatformUIDefinition.PositionOverrideDefinition>>(FasterPlayerPrefs.Singleton.GetString("UIOverrides"));
            }
            else
            {
                overridesList = new List<PlatformUIDefinition.PositionOverrideDefinition>();
            }

            string path = PlatformUIDefinition.GetGameObjectPath(prefabRef);
            int overrideIndex = overridesList.FindIndex(item => item.gameObjectPath == path);
            if (overrideIndex == -1)
            {
                overridesList.Add(new PlatformUIDefinition.PositionOverrideDefinition
                {
                    gameObjectPath = path,
                    newAnchoredX = modifiedRect.anchoredPosition.x,
                    newAnchoredY = modifiedRect.anchoredPosition.y
                });
            }
            else
            {
                overridesList[overrideIndex] = new PlatformUIDefinition.PositionOverrideDefinition
                {
                    gameObjectPath = path,
                    newAnchoredX = modifiedRect.anchoredPosition.x,
                    newAnchoredY = modifiedRect.anchoredPosition.y
                };
            }

            FasterPlayerPrefs.Singleton.SetString("UIOverrides", JsonConvert.SerializeObject(overridesList));
        }

        public void ResetUI()
        {
            FasterPlayerPrefs.Singleton.DeleteKey("UIOverrides");
            OnEnable();
        }

        public void CloseUI()
        {
            gameObject.SetActive(false);
        }
    }
}