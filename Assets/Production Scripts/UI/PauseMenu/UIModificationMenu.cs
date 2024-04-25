using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Vi.Core;
using System.Linq;
using UnityEngine.InputSystem.OnScreen;

namespace Vi.UI
{
    public class UIModificationMenu : Menu
    {
        [SerializeField] private PlayerUI playerUIPrefab;
        [SerializeField] private RectTransform UIMimicParent;

        private Dictionary<GameObject, GameObject> prefabCrosswalk = new Dictionary<GameObject, GameObject>();
        private void Start()
        {
            PlatformUIDefinition.UIDefinition[] platformUIDefinitions = playerUIPrefab.GetComponent<PlatformUIDefinition>().GetPlatformUIDefinitions();

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
                        c.enabled = false;
                    }

                    if (copyChildren[childIndex].GetComponent<OnScreenButton>())
                    {
                        copyChildren[childIndex].gameObject.AddComponent<DraggableUIObject>();
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
                }
            }
        }
    }
}