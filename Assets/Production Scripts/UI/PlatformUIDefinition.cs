using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Vi.UI
{
    public class PlatformUIDefinition : MonoBehaviour
    {
        [SerializeField] private UIDefinition[] platformUIDefinitions;

        [System.Serializable]
        private struct UIDefinition
        {
            public RuntimePlatform[] platforms;
            public GameObject[] gameObjectsToEnable;
            public GameObject[] gameObjectsToDestroy;
            public MoveUIDefinition[] objectsToMove;
        }

        [System.Serializable]
        private struct MoveUIDefinition
        {
            public GameObject gameObjectToMove;
            public Vector2 newAnchoredPosition;
        }

        private void Start()
        {
            foreach (UIDefinition platformUIDefinition in platformUIDefinitions)
            {
                foreach (GameObject g in platformUIDefinition.gameObjectsToEnable)
                {
                    g.SetActive(platformUIDefinition.platforms.Contains(Application.platform));
                }

                foreach (MoveUIDefinition moveUIDefinition in platformUIDefinition.objectsToMove)
                {
                    if (platformUIDefinition.platforms.Contains(Application.platform))
                    {
                        moveUIDefinition.gameObjectToMove.GetComponent<RectTransform>().anchoredPosition = moveUIDefinition.newAnchoredPosition;
                    }
                }

                foreach (GameObject g in platformUIDefinition.gameObjectsToDestroy)
                {
                    if (platformUIDefinition.platforms.Contains(Application.platform)) { Destroy(g); }
                }
            }
        }

        private void Update()
        {
            foreach (UIDefinition platformUIDefinition in platformUIDefinitions)
            {
                foreach (MoveUIDefinition moveUIDefinition in platformUIDefinition.objectsToMove)
                {
                    if (platformUIDefinition.platforms.Contains(Application.platform))
                    {
                        moveUIDefinition.gameObjectToMove.GetComponent<RectTransform>().anchoredPosition = moveUIDefinition.newAnchoredPosition;
                    }
                }
            }
        }
    }
}