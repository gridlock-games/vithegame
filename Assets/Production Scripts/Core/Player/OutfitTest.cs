using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using Vi.ScriptableObjects;

namespace Vi.Player
{
    public class OutfitTest : MonoBehaviour
    {
        [SerializeField] private int[] equipmentIndexesToAdd;

        private void Start()
        {
            int counter = 0;
            foreach (var w in PlayerDataManager.Singleton.GetCharacterReference().GetWearableEquipmentOptions(CharacterReference.RaceAndGender.HumanMale))
            {
                //Debug.Log(counter + " " + w.wearableEquipmentPrefab.name + " " + w.equipmentType + " " + w.raceAndGender);
                counter++;
            }

            StartCoroutine(Wait());
        }

        private IEnumerator Wait()
        {
            yield return null;
            yield return null;
            yield return null;
            foreach (int i in equipmentIndexesToAdd)
            {
                GetComponent<AnimationHandler>().ApplyWearableEquipment(PlayerDataManager.Singleton.GetCharacterReference().GetWearableEquipmentOptions(CharacterReference.RaceAndGender.HumanMale)[i]);
            }
        }
    }
}