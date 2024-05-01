using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Vi.Core;
using System.Linq;
using UnityEngine.InputSystem.OnScreen;
using Newtonsoft.Json;

namespace Vi.UI
{
    public class UIModificationMenu : Menu
    {
        [SerializeField] private PlayerUI playerUIPrefab;
        [SerializeField] private RectTransform UIMimicParent;

        private Dictionary<GameObject, GameObject> prefabCrosswalk = new Dictionary<GameObject, GameObject>();
        private void Start()
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
                GameObject copy = Instantiate(child.gameObject, UIMimicParent);

                Transform[] copyChildren = copy.GetComponentsInChildren<Transform>(true);
                Transform[] originalChildren = child.GetComponentsInChildren<Transform>(true);

                copyChildren = System.Array.FindAll(copyChildren, item => System.Array.Exists(originalChildren, originalItem => item.name.Replace("(Clone)", "") == originalItem.name));

                for (int childIndex = 0; childIndex < copyChildren.Length; childIndex++)
                {
                    prefabCrosswalk.Add(copyChildren[childIndex].gameObject, originalChildren[childIndex].gameObject);

                    if (copyChildren[childIndex].TryGetComponent(out KillFeed killFeed))
                    {
                        killFeed.SetPreviewOn();
                        continue;
                    }
                    if (copyChildren[childIndex].GetComponent<KillFeedElement>()) { continue; }

                    foreach (Behaviour c in copyChildren[childIndex].GetComponents<Behaviour>())
                    {
                        if (c is Graphic) { continue; }
                        if (c.GetType().ToString() == "DuloGames.UI.UIHighlightTransition") { continue; }
                        c.enabled = false;
                    }

                    if (childIndex < originalChildren.Length)
                    {
                        foreach (PlatformUIDefinition.UIDefinition platformUIDefinition in platformUIDefinitions)
                        {
                            foreach (GameObject g in platformUIDefinition.gameObjectsToEnable)
                            {
                                if (g == originalChildren[childIndex].gameObject)
                                {
                                    copyChildren[childIndex].gameObject.SetActive(platformUIDefinition.platforms.Contains(Application.platform));
                                }
                            }

                            foreach (PlatformUIDefinition.MoveUIDefinition moveUIDefinition in platformUIDefinition.objectsToMove)
                            {
                                if (moveUIDefinition.gameObjectToMove == originalChildren[childIndex].gameObject)
                                {
                                    if (platformUIDefinition.platforms.Contains(Application.platform))
                                    {
                                        RectTransform rt = (RectTransform)copyChildren[childIndex];
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
                                if (g == originalChildren[childIndex].gameObject)
                                {
                                    if (platformUIDefinition.platforms.Contains(Application.platform)) { Destroy(copyChildren[childIndex]); }
                                }
                            }
                        }
                    }

                    if (copyChildren[childIndex].GetComponent<Button>() | (copyChildren[childIndex].GetComponent<OnScreenButton>() & !copyChildren[childIndex].GetComponent<CustomOnScreenStick>()))
                    {
                        if (copyChildren[childIndex].gameObject.name == "Ability 1") { Debug.Log(copyChildren[childIndex]); }
                        if (PlayerPrefs.HasKey("UIOverrides"))
                        {
                            List<PlatformUIDefinition.PositionOverrideDefinition> positionOverrideDefinitions = JsonConvert.DeserializeObject<List<PlatformUIDefinition.PositionOverrideDefinition>>(PlayerPrefs.GetString("UIOverrides"));

                            foreach (PlatformUIDefinition.PositionOverrideDefinition positionOverrideDefinition in positionOverrideDefinitions)
                            {
                                GameObject g = platformUIDefinitionComponent.GetGameObjectFromPath(positionOverrideDefinition.gameObjectPath);
                                if (g == originalChildren[childIndex].gameObject)
                                {
                                    ((RectTransform)copyChildren[childIndex]).anchoredPosition = new Vector2(positionOverrideDefinition.newAnchoredX, positionOverrideDefinition.newAnchoredY);
                                }
                            }
                        }

                        DraggableUIObject draggableUIObject = copyChildren[childIndex].gameObject.AddComponent<DraggableUIObject>();
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
            if (PlayerPrefs.HasKey("UIOverrides"))
            {
                overridesList = JsonConvert.DeserializeObject<List<PlatformUIDefinition.PositionOverrideDefinition>>(PlayerPrefs.GetString("UIOverrides"));
            }
            else
            {
                overridesList = new List<PlatformUIDefinition.PositionOverrideDefinition>();
            }

            overridesList.Add(new PlatformUIDefinition.PositionOverrideDefinition
            {
                gameObjectPath = PlatformUIDefinition.GetGameObjectPath(prefabRef),
                newAnchoredX = modifiedRect.anchoredPosition.x,
                newAnchoredY = modifiedRect.anchoredPosition.y
            });
            
            PlayerPrefs.SetString("UIOverrides", JsonConvert.SerializeObject(overridesList));
        }

        public void ResetUI()
        {
            PlayerPrefs.DeleteKey("UIOverrides");
            Start();
        }
    }
}