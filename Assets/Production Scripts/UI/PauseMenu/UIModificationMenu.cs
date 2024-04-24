using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Vi.Core;

namespace Vi.UI
{
    public class UIModificationMenu : Menu
    {
        [SerializeField] private PlayerUI playerUIPrefab;
        [SerializeField] private RectTransform UIMimicParent;

        private void Start()
        {
            foreach (Transform child in playerUIPrefab.transform)
            {
                GameObject g = Instantiate(child.gameObject, UIMimicParent);

                foreach (Transform nestedChild in g.GetComponentsInChildren<Transform>())
                {
                    if (nestedChild.TryGetComponent(out KillFeed killFeed))
                    {
                        killFeed.SetPreviewOn();
                        continue;
                    }
                    if (nestedChild.GetComponent<KillFeedElement>()) { continue; }

                    foreach (Behaviour c in nestedChild.GetComponents<Behaviour>())
                    {
                        if (c is Graphic) { continue; }
                        c.enabled = false;
                    }
                }
            }
        }
    }
}