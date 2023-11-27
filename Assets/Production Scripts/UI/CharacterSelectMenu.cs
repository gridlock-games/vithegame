using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.ScriptableObjects;

namespace Vi.UI
{
    public class CharacterSelectMenu : Menu
    {
        [SerializeField] private Transform characterSelectParent;
        [SerializeField] private CharacterSelectElement characterSelectElement;
        [SerializeField] private CharacterReference characterReference;

        private void Awake()
        {
            CharacterReference.PlayerModelOption[] playerModelOptions = characterReference.GetPlayerModelOptions();
            for (int i = 0; i < playerModelOptions.Length; i++)
            {
                GameObject g = Instantiate(characterSelectElement.gameObject, characterSelectParent);
                g.GetComponent<CharacterSelectElement>().Initialize(playerModelOptions[i]);
            }
        }

        //private float WIDTH = 200;
        //private float HEIGHT = 200;
        //private float X_OFFSET = 0;
        //private void CreateGrid()
        //{
        //    var incrementX = 0;
        //    var amoutToSpawn = WIDTH;

        //    for (int y = 0; y < HEIGHT; y++)
        //    {
        //        for (int x = 0; x < amoutToSpawn; x++)
        //        {
        //            var xPos = (x + incrementX) * X_OFFSET;

        //            if (y % 2 == 1)
        //                xPos += X_OFFSET / 2;

        //            CreateHex(xPos, x + incrementX, y);

        //            if (y > 0)
        //                CreateHex(xPos, x + incrementX, -y);
        //        }

        //        if (y % 2 == 1)
        //            incrementX++;

        //        amoutToSpawn--;
        //    }
        //}
    }
}