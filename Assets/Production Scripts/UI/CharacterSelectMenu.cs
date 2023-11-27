using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.ScriptableObjects;
using UnityEngine.UI;

namespace Vi.UI
{
    public class CharacterSelectMenu : Menu
    {
        [SerializeField] private Transform characterSelectParent;
        [SerializeField] private CharacterSelectElement characterSelectElement;
        [SerializeField] private CharacterReference characterReference;

        private float size = 200;
        private int height = 2;

        private void Awake()
        {
            CharacterReference.PlayerModelOption[] playerModelOptions = characterReference.GetPlayerModelOptions();

            Quaternion rotation = Quaternion.Euler(0, 0, -45);
            int characterIndex = 0;
            for (int x = 0; x < playerModelOptions.Length; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (characterIndex >= playerModelOptions.Length) { return; }

                    Vector3 pos = new Vector3(x * size, y * size, 0);
                    GameObject g = Instantiate(characterSelectElement.gameObject, characterSelectParent);
                    g.transform.localPosition = rotation * pos;
                    g.GetComponent<CharacterSelectElement>().Initialize(playerModelOptions[characterIndex].characterImage, characterIndex, 0);
                    characterIndex++;
                }
            }
        }
    }
}