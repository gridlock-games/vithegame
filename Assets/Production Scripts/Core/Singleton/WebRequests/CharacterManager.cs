using UnityEngine;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using Vi.Utility;
using System.Text.RegularExpressions;
using Vi.ScriptableObjects;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;

namespace Vi.Core
{
    public class CharacterManager : MonoBehaviour
    {
        public void StopCharacterRefresh()
        {
            if (characterGetRequestRoutine != null)
            {
                StopCoroutine(characterGetRequestRoutine);
            }
            IsRefreshingCharacters = false;
        }

        public List<Character> Characters { get; private set; } = new List<Character>();

        public bool IsRefreshingCharacters { get; private set; }
        private Coroutine characterGetRequestRoutine;
        public void RefreshCharacters() { characterGetRequestRoutine = StartCoroutine(CharacterGetRequest()); }
        private IEnumerator CharacterGetRequest()
        {
            if (IsRefreshingCharacters) { yield break; }
            IsRefreshingCharacters = true;

            if (FasterPlayerPrefs.InternetReachability == NetworkReachability.NotReachable)
            {
                Characters.Clear();
                yield return new WaitUntil(() => PlayerDataManager.DoesExist());
                yield return new WaitUntil(() => PlayerDataManager.IsCharacterReferenceLoaded());
                for (int i = 0; i < 5; i++)
                {
                    Character character = GetRandomizedCharacter(false);
                    character._id = i.ToString();
                    character.name = PlayerDataManager.Singleton.GetRandomPlayerName(character.raceAndGender);
                    Characters.Add(character);
                }
            }
            else
            {
                UnityWebRequest getRequest = UnityWebRequest.Get(WebRequestManager.Singleton.GetAPIURL(false) + "characters/" + WebRequestManager.Singleton.UserManager.CurrentlyLoggedInUserId);
                yield return getRequest.SendWebRequest();

                Characters.Clear();
                if (getRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Get Request Error in WebRequestManager.CharacterGetRequest() " + WebRequestManager.Singleton.GetAPIURL(false) + "characters/" + WebRequestManager.Singleton.UserManager.CurrentlyLoggedInUserId);
                    getRequest.Dispose();
                    yield break;
                }
                string json = getRequest.downloadHandler.text;
                try
                {
                    try
                    {
                        foreach (CharacterJson jsonStruct in CharacterJson.DeserializeJsonList(json))
                        {
                            Characters.Add(jsonStruct.ToCharacter());
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError(e);
                        foreach (CharacterJson jsonStruct in JsonConvert.DeserializeObject<List<CharacterJson>>(json))
                        {
                            Characters.Add(jsonStruct.ToCharacter());
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                }

                getRequest.Dispose();
            }

            List<int> indexesThatFailedValidation = new List<int>();
            for (int i = 0; i < Characters.Count; i++)
            {
                yield return GetCharacterInventory(Characters[i]);

                if (!ValidateCharacterLoadouts(Characters[i]))
                {
                    indexesThatFailedValidation.Add(i);
                }
                else
                {
                    yield return GetCharacterAttributes(Characters[i]._id.ToString());
                    if (!getCharacterAttributesWasSuccessful) { indexesThatFailedValidation.Add(i); }
                }
            }

            for (int i = indexesThatFailedValidation.Count - 1; i >= 0; i--)
            {
                yield return CharacterDisableRequest(Characters[indexesThatFailedValidation[i]]._id.ToString());
                Characters.RemoveAt(indexesThatFailedValidation[i]);
            }

            // This adds all weapons to the inventory if we're in the editor
#if UNITY_EDITOR
            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
            foreach (Character character in Characters)
            {
                foreach (CharacterReference.WeaponOption weaponOption in weaponOptions)
                {
                    if (!IsItemInInventory(character._id.ToString(), weaponOption.itemWebId))
                    {
                        yield return AddItemToInventory(character._id.ToString(), weaponOption.itemWebId);
                    }
                }
            }

            foreach (Character character in Characters)
            {
                yield return GetCharacterInventory(character);
            }
#endif

            IsRefreshingCharacters = false;
        }

        private bool ValidateCharacterLoadouts(Character characterToValidate)
        {
            if (!HasCharacterInventory(characterToValidate._id.ToString())) { Debug.LogWarning("Calling Validate Character Loadouts but we don't have this character's inventory yet! " + characterToValidate._id); return false; }

            foreach (Loadout loadout in characterToValidate.GetLoadouts())
            {
                foreach (FixedString64Bytes inventoryItemId in loadout.GetLoadoutItemIDsAsArray())
                {
                    if (string.IsNullOrWhiteSpace(inventoryItemId.ToString())) { continue; }

                    if (!HasInventoryItem(characterToValidate._id.ToString(), inventoryItemId.ToString()))
                    {
                        Debug.LogWarning("Character loadout is invalid id: " + characterToValidate._id + " loadout item id: " + inventoryItemId);
                        return false;
                    }
                }

                //foreach (KeyValuePair<CharacterReference.EquipmentType, FixedString64Bytes> inventoryItemId in loadout.GetLoadoutArmorPiecesAsDictionary())
                //{
                //    if (string.IsNullOrWhiteSpace(inventoryItemId.ToString()))
                //    {
                //        if (!NullableEquipmentTypes.Contains(inventoryItemId.Key))
                //        {
                //            Debug.LogWarning("Character loadout is invalid id: " + characterToValidate._id + " loadout item id: " + inventoryItemId);
                //            return false;
                //        }
                //    }
                //}

                //if (string.IsNullOrWhiteSpace(loadout.weapon1ItemId.ToString()))
                //{
                //    Debug.LogWarning("Character loadout is invalid id: " + characterToValidate._id + " loadout item id: " + loadout.weapon1ItemId);
                //    return false;
                //}

                //if (string.IsNullOrWhiteSpace(loadout.weapon2ItemId.ToString()))
                //{
                //    Debug.LogWarning("Character loadout is invalid id: " + characterToValidate._id + " loadout item id: " + loadout.weapon2ItemId);
                //    return false;
                //}
            }

            return true;
        }

        public bool IsGettingCharacterById { get; private set; }
        public bool LastCharacterByIdWasSuccessful { get; private set; }
        public Character CharacterById { get; private set; }
        public void GetCharacterById(string characterId) { StartCoroutine(CharacterByIdGetRequest(characterId)); }

        private IEnumerator CharacterByIdGetRequest(string characterId)
        {
            if (FasterPlayerPrefs.InternetReachability == NetworkReachability.NotReachable)
            {
                CharacterById = Characters.Find(item => item._id == characterId);
                LastCharacterByIdWasSuccessful = true;
            }
            else
            {
                if (IsGettingCharacterById) { yield break; }
                IsGettingCharacterById = true;
                UnityWebRequest getRequest = UnityWebRequest.Get(WebRequestManager.Singleton.GetAPIURL(false) + "characters/" + "getCharacter/" + characterId);
                yield return getRequest.SendWebRequest();

                if (getRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Get Request Error in WebRequestManager.CharacterByIdGetRequest() " + WebRequestManager.Singleton.GetAPIURL(false) + "characters/" + "getCharacter/" + characterId);
                    getRequest.Dispose();
                    LastCharacterByIdWasSuccessful = false;
                    IsGettingCharacterById = false;
                    yield break;
                }

                string json = getRequest.downloadHandler.text;

                if (json == "[]")
                {
                    getRequest.Dispose();
                    LastCharacterByIdWasSuccessful = false;
                    IsGettingCharacterById = false;
                    yield break;
                }

                try
                {
                    CharacterJson characterJson = JsonConvert.DeserializeObject<CharacterJson>(json);

                    if (!characterJson.enabled)
                    {
                        getRequest.Dispose();
                        LastCharacterByIdWasSuccessful = false;
                        IsGettingCharacterById = false;
                        yield break;
                    }

                    CharacterById = characterJson.ToCharacter();

                    if (!CharacterById.HasActiveLoadout())
                    {
                        getRequest.Dispose();
                        LastCharacterByIdWasSuccessful = false;
                        IsGettingCharacterById = false;
                        yield break;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Get Character by ID Exception " + e);
                    CharacterById = GetDefaultCharacter(CharacterReference.RaceAndGender.HumanMale);

                    getRequest.Dispose();
                    LastCharacterByIdWasSuccessful = false;
                    IsGettingCharacterById = false;
                    yield break;
                }

                getRequest.Dispose();

                int index = Characters.FindIndex(item => item._id == CharacterById._id);
                if (index == -1)
                {
                    Characters.Add(CharacterById);
                }
                else
                {
                    Characters[index] = CharacterById;
                }

                yield return GetCharacterInventory(CharacterById._id.ToString());

                if (!ValidateCharacterLoadouts(CharacterById))
                {
                    LastCharacterByIdWasSuccessful = false;
                    IsGettingCharacterById = false;
                    yield break;
                }

                yield return GetCharacterAttributes(CharacterById._id.ToString());

                LastCharacterByIdWasSuccessful = getCharacterAttributesWasSuccessful;
                IsGettingCharacterById = false;
            }
        }

        public IEnumerator UpdateCharacterCosmetics(Character character)
        {
            CharacterCosmeticPutPayload payload = new CharacterCosmeticPutPayload(character._id.ToString(), character.slot, character.eyeColor.ToString(), character.hair.ToString(),
            character.bodyColor.ToString(), character.beard.ToString(), character.brows.ToString(), character.name.ToString(), character.model.ToString());

            string json = JsonUtility.ToJson(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest putRequest = UnityWebRequest.Put(WebRequestManager.Singleton.GetAPIURL(false) + "characters/" + "updateCharacterCosmetic", jsonData);
            putRequest.SetRequestHeader("Content-Type", "application/json");
            yield return putRequest.SendWebRequest();

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                putRequest = UnityWebRequest.Put(WebRequestManager.Singleton.GetAPIURL(false) + "characters/" + "updateCharacterCosmetic", jsonData);
                putRequest.SetRequestHeader("Content-Type", "application/json");
                yield return putRequest.SendWebRequest();
            }

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.UpdateCharacterCosmetics()" + putRequest.error);
            }
            putRequest.Dispose();
        }

        //public List<InventoryItem> GetInventoryItems(string characterId)
        //{
        //    if (characterId == null) { characterId = ""; }

        //    if (InventoryItems.TryGetValue(characterId, out List<InventoryItem> result))
        //    {
        //        return result;
        //    }
        //    return new List<InventoryItem>();
        //}

        public static bool IsItemInInventory(string characterId, string itemWebId)
        {
            if (characterId == null) { characterId = ""; }
            if (itemWebId == null) { itemWebId = ""; }

            if (HasCharacterInventory(characterId))
            {
                List<InventoryItem> inventoryItems = GetInventory(characterId);
                return inventoryItems.Exists(item => item.itemId._id == itemWebId);
            }

            return false;
        }

        public static CharacterReference.WeaponOption GetWeaponOption(InventoryItem inventoryItem)
        {
            CharacterReference.WeaponOption[] options = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
            int index = System.Array.FindIndex(options, item => item.itemWebId == inventoryItem.itemId._id);

            if (index == -1) { return null; }

            return options[index];
        }

        public static CharacterReference.WearableEquipmentOption GetEquipmentOption(InventoryItem inventoryItem, CharacterReference.RaceAndGender raceAndGender)
        {
            List<CharacterReference.WearableEquipmentOption> options = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(raceAndGender);
            int index = options.FindIndex(item => item.itemWebId == inventoryItem.itemId._id);

            if (index == -1) { return null; }

            return options[index];
        }

        public static bool TryGetInventoryItem(string characterId, string inventoryItemId, out InventoryItem resultingInventoryItem)
        {
            if (characterId == null) { characterId = ""; }
            if (inventoryItemId == null) { inventoryItemId = ""; }

            if (inventoryItems.TryGetValue(characterId, out List<InventoryItem> items))
            {
                int index = items.FindIndex(item => item.id == inventoryItemId);
                if (index != -1)
                {
                    resultingInventoryItem = items[index];
                    return true;
                }
            }

            resultingInventoryItem = InventoryItem.GetEmptyInventoryItem();
            return false;
        }

        public static bool HasCharacterInventory(string characterId)
        {
            if (characterId == null) { characterId = ""; }
            return inventoryItems.ContainsKey(characterId);
        }

        public static bool HasInventoryItem(string characterId, string inventoryItemId)
        {
            if (characterId == null) { characterId = ""; }
            if (inventoryItemId == null) { inventoryItemId = ""; }

            if (inventoryItems.TryGetValue(characterId, out List<InventoryItem> items))
            {
                int index = items.FindIndex(item => item.id == inventoryItemId);
                if (index != -1)
                {
                    return true;
                }
            }
            return false;
        }

        public static List<InventoryItem> GetInventory(string characterId)
        {
            if (characterId == null) { characterId = ""; }

            if (HasCharacterInventory(characterId))
            {
                return inventoryItems[characterId];
            }
            return new List<InventoryItem>();
        }

        private static Dictionary<string, List<InventoryItem>> inventoryItems = new Dictionary<string, List<InventoryItem>>();
        public IEnumerator GetCharacterInventory(Character character)
        {
            string characterId = character._id.ToString();
            if (FasterPlayerPrefs.InternetReachability == NetworkReachability.NotReachable)
            {
                List<CharacterReference.WearableEquipmentOption> armorOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(character.raceAndGender);
                CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();

                List<InventoryItem> allItems = new List<InventoryItem>();
                foreach (var option in armorOptions)
                {
                    allItems.Add(new InventoryItem(characterId, new List<int>(), option.itemWebId, true, option.itemWebId));
                }

                foreach (var option in weaponOptions)
                {
                    allItems.Add(new InventoryItem(characterId, new List<int>(), option.itemWebId, true, option.itemWebId));
                }

                if (!inventoryItems.ContainsKey(characterId))
                    inventoryItems.Add(characterId, allItems);
                else
                    inventoryItems[characterId] = allItems;
            }
            else
            {
                UnityWebRequest getRequest = UnityWebRequest.Get(WebRequestManager.Singleton.GetAPIURL(false) + "characters/" + "getInventory/" + characterId);
                yield return getRequest.SendWebRequest();

                if (getRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Get Request Error in WebRequestManager.GetCharacterInventory()");
                    getRequest.Dispose();
                    yield break;
                }
                string json = getRequest.downloadHandler.text;

                if (!inventoryItems.ContainsKey(characterId))
                    inventoryItems.Add(characterId, JsonConvert.DeserializeObject<List<InventoryItem>>(json));
                else
                    inventoryItems[characterId] = JsonConvert.DeserializeObject<List<InventoryItem>>(json);

                getRequest.Dispose();
            }
        }

        public IEnumerator GetCharacterInventory(string characterId)
        {
            UnityWebRequest getRequest = UnityWebRequest.Get(WebRequestManager.Singleton.GetAPIURL(false) + "characters/" + "getInventory/" + characterId);
            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.GetCharacterInventory()");
                getRequest.Dispose();
                yield break;
            }
            string json = getRequest.downloadHandler.text;

            if (!inventoryItems.ContainsKey(characterId))
                inventoryItems.Add(characterId, JsonConvert.DeserializeObject<List<InventoryItem>>(json));
            else
                inventoryItems[characterId] = JsonConvert.DeserializeObject<List<InventoryItem>>(json);

            getRequest.Dispose();
        }

        public struct ModelNames
        {
            public Human human;
            public Orc orc;

            public ModelNames(string humanMaleModelName, string humanFemaleModelName, string orcMaleModelName, string orcFemaleModelName)
            {
                human = new Human()
                {
                    m = humanMaleModelName,
                    f = humanFemaleModelName
                };
                orc = new Orc()
                {
                    m = orcMaleModelName,
                    f = orcFemaleModelName
                };
            }
        }

        public struct Human
        {
            public string m;
            public string f;
        }

        public struct Orc
        {
            public string m;
            public string f;
        }

        public struct ItemAttributes
        {
            public int strength;
            public int vitality;
            public int agility;
            public int dexterity;
            public int intelligence;
            public int? attack;
            public int? defense;
        }

        public struct ItemId
        {
            public string _id;
            public string @class;
            public string name;
            public string equipmentType;
            public int weight;
            public ItemAttributes attributes;
            public bool isCraftOnly;
            public bool isCashExclusive;
            public bool isPassExclusive;
            public bool isBasicGear;
            public ModelNames modelNames;
            public int __v;
            public string id;

            public ItemId(string itemId)
            {
                _id = itemId;
                @class = "";
                name = "";
                equipmentType = "";
                weight = 0;
                attributes = new ItemAttributes();
                isCraftOnly = false;
                isCashExclusive = false;
                isPassExclusive = false;
                isBasicGear = false;
                modelNames = new ModelNames();
                __v = 0;
                id = itemId;
            }
        }

        public struct InventoryItem
        {
            public string charId;
            public List<int> loadoutSlot;
            public ItemId itemId;
            public bool enabled;
            public string id;

            public InventoryItem(string charId, List<int> loadoutSlot, string itemId, bool enabled, string id)
            {
                this.charId = charId;
                this.loadoutSlot = loadoutSlot;
                this.itemId = new ItemId(itemId);
                this.enabled = enabled;
                this.id = id;
            }

            public static InventoryItem GetEmptyInventoryItem()
            {
                return new InventoryItem("", null, "", false, "");
            }
        }

#if UNITY_EDITOR
        public bool InventoryAddWasSuccessful { get; private set; }
        private IEnumerator AddItemToInventory(string charId, string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) { Debug.LogWarning("You are trying to add an item to a character's inventory that has an id of null or whitespace"); yield break; }

            AddCharacterInventoryPayload payload = new AddCharacterInventoryPayload(charId, itemId);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(WebRequestManager.Singleton.GetAPIURL(false) + "characters/" + "setInventory", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                postRequest = new UnityWebRequest(WebRequestManager.Singleton.GetAPIURL(false) + "characters/" + "setInventory", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();
            }

            InventoryAddWasSuccessful = true;
            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.AddItemToInventory()" + postRequest.error);
                InventoryAddWasSuccessful = false;
            }

            if (postRequest.downloadHandler.text == "false")
            {
                InventoryAddWasSuccessful = false;
                yield break;
            }

            postRequest.Dispose();
        }

        private struct AddCharacterInventoryPayload
        {
            public string charId;
            public string itemId;

            public AddCharacterInventoryPayload(string charId, string itemId)
            {
                this.charId = charId;
                this.itemId = itemId;
            }
        }
#endif

        public IEnumerator UpdateCharacterLoadout(string characterId, Loadout newLoadout, bool callUseLoadout)
        {
            if (!int.TryParse(newLoadout.loadoutSlot.ToString(), out int result))
            {
                Debug.LogWarning("Could not parse loadout slot from new loadout! " + newLoadout.loadoutSlot);
            }

            yield return GetCharacterInventory(characterId.ToString());

            newLoadout.helmGearItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.helmGearItemId | item.id == newLoadout.helmGearItemId).id ?? "";
            newLoadout.capeGearItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.capeGearItemId | item.id == newLoadout.capeGearItemId).id ?? "";
            newLoadout.pantsGearItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.pantsGearItemId | item.id == newLoadout.pantsGearItemId).id ?? "";
            newLoadout.shouldersGearItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.shouldersGearItemId | item.id == newLoadout.shouldersGearItemId).id ?? "";
            newLoadout.chestArmorGearItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.chestArmorGearItemId | item.id == newLoadout.chestArmorGearItemId).id ?? "";
            newLoadout.glovesGearItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.glovesGearItemId | item.id == newLoadout.glovesGearItemId).id ?? "";
            newLoadout.beltGearItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.beltGearItemId | item.id == newLoadout.beltGearItemId).id ?? "";
            newLoadout.robeGearItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.robeGearItemId | item.id == newLoadout.robeGearItemId).id ?? "";
            newLoadout.bootsGearItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.bootsGearItemId | item.id == newLoadout.bootsGearItemId).id ?? "";
            newLoadout.weapon1ItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.weapon1ItemId | item.id == newLoadout.weapon1ItemId).id ?? "";
            newLoadout.weapon2ItemId = inventoryItems[characterId].Find(item => item.itemId._id == newLoadout.weapon2ItemId | item.id == newLoadout.weapon2ItemId).id ?? "";

            // Uncomment this to test invalid loadout strings
            //Debug.Log(newLoadout.ToString());
            //foreach (FixedString64Bytes id in newLoadout.GetLoadoutAsList())
            //{
            //    Debug.Log(id + " " + HasInventoryItem(characterId, id.ToString()));
            //}

            CharacterLoadoutPutPayload payload = new CharacterLoadoutPutPayload(characterId, newLoadout);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest putRequest = UnityWebRequest.Put(WebRequestManager.Singleton.GetAPIURL(false) + "characters/" + "saveLoadOut", jsonData);
            putRequest.SetRequestHeader("Content-Type", "application/json");
            yield return putRequest.SendWebRequest();

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                putRequest = UnityWebRequest.Put(WebRequestManager.Singleton.GetAPIURL(false) + "characters/" + "saveLoadOut", jsonData);
                putRequest.SetRequestHeader("Content-Type", "application/json");
                yield return putRequest.SendWebRequest();
            }

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.UpdateCharacterLoadout()" + putRequest.error);
            }
            putRequest.Dispose();

            if (callUseLoadout)
            {
                yield return UseCharacterLoadout(characterId, newLoadout.loadoutSlot.ToString());
            }
        }

        public IEnumerator UseCharacterLoadout(string characterId, string loadoutSlot)
        {
            if (!int.TryParse(loadoutSlot.ToString(), out int result))
            {
                Debug.LogWarning("Could not parse loadout slot from new loadout! " + loadoutSlot);
            }

            UseCharacterLoadoutPayload payload = new UseCharacterLoadoutPayload(characterId, loadoutSlot);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest putRequest = UnityWebRequest.Put(WebRequestManager.Singleton.GetAPIURL(false) + "characters/" + "useLoadOut", jsonData);
            putRequest.SetRequestHeader("Content-Type", "application/json");
            yield return putRequest.SendWebRequest();

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                putRequest = UnityWebRequest.Put(WebRequestManager.Singleton.GetAPIURL(false) + "characters/" + "useLoadOut", jsonData);
                putRequest.SetRequestHeader("Content-Type", "application/json");
                yield return putRequest.SendWebRequest();
            }

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.UseCharacterLoadout()" + putRequest.error);
            }

            putRequest.Dispose();
        }

        private struct UseCharacterLoadoutPayload
        {
            public string charId;
            public string loadoutSlot;

            public UseCharacterLoadoutPayload(string charId, string loadoutSlot)
            {
                this.charId = charId;
                this.loadoutSlot = loadoutSlot;
            }
        }

        public string CharacterCreationError { get; private set; } = "";
        public IEnumerator CharacterPostRequest(Character character)
        {
            CharacterCreationError = "";
            CharacterPostPayload payload = new CharacterPostPayload(character);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(WebRequestManager.Singleton.GetAPIURL(false) + "characters/" + "createCharacterCosmetic", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                postRequest = new UnityWebRequest(WebRequestManager.Singleton.GetAPIURL(false) + "characters/" + "createCharacterCosmetic", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();
            }

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in WebRequestManager.CharacterPostRequest()" + postRequest.error);
                CharacterCreationError = "Server Error";
                yield break;
            }

            // TODO account for web request failure in UI
            if (postRequest.downloadHandler.text == "false")
            {
                CharacterCreationError = "Failed To Create Character";
                yield break;
            }

            character._id = postRequest.downloadHandler.text;

            yield return UpdateCharacterLoadout(postRequest.downloadHandler.text, GetDefaultDisplayLoadout(character.raceAndGender), true);

            postRequest.Dispose();
        }

        public IEnumerator CharacterDisableRequest(string characterId)
        {
            CharacterDisablePayload payload = new CharacterDisablePayload(characterId);

            string json = JsonUtility.ToJson(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest putRequest = UnityWebRequest.Put(WebRequestManager.Singleton.GetAPIURL(false) + "characters/" + "disableCharacter", jsonData);
            putRequest.SetRequestHeader("Content-Type", "application/json");
            yield return putRequest.SendWebRequest();

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                putRequest = UnityWebRequest.Put(WebRequestManager.Singleton.GetAPIURL(false) + "characters/" + "disableCharacter", jsonData);
                putRequest.SetRequestHeader("Content-Type", "application/json");
                yield return putRequest.SendWebRequest();
            }

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.CharacterDisableRequest()" + putRequest.error);
            }
            putRequest.Dispose();
        }

        public Character GetDefaultCharacter(CharacterReference.RaceAndGender raceAndGender)
        {
            switch (raceAndGender)
            {
                case CharacterReference.RaceAndGender.HumanMale:
                    return new Character("", "Human_Male", "", 0,
                        PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender).First(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Body).material.name,
                        PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender).First(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Eyes).material.name,
                        "null", "null", "null", 1,
                        new CharacterAttributes(1, 1, 1, 1, 1),
                        GetDefaultDisplayLoadout(CharacterReference.RaceAndGender.HumanMale),
                        GetDefaultDisplayLoadout(CharacterReference.RaceAndGender.HumanMale),
                        GetDefaultDisplayLoadout(CharacterReference.RaceAndGender.HumanMale),
                        GetDefaultDisplayLoadout(CharacterReference.RaceAndGender.HumanMale),
                        CharacterReference.RaceAndGender.HumanMale);
                case CharacterReference.RaceAndGender.HumanFemale:
                    return new Character("", "Human_Female", "", 0,
                        PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender).First(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Body).material.name,
                        PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender).First(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Eyes).material.name,
                        "null",
                        PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender).First(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Brows).material.name,
                        "null", 1,
                        new CharacterAttributes(1, 1, 1, 1, 1),
                        GetDefaultDisplayLoadout(CharacterReference.RaceAndGender.HumanMale),
                        GetDefaultDisplayLoadout(CharacterReference.RaceAndGender.HumanMale),
                        GetDefaultDisplayLoadout(CharacterReference.RaceAndGender.HumanMale),
                        GetDefaultDisplayLoadout(CharacterReference.RaceAndGender.HumanMale),
                        CharacterReference.RaceAndGender.HumanFemale);
                default:
                    Debug.LogError("Unsure how to handle race and gennder " + raceAndGender);
                    return default;
            }
        }

        public Character GetRandomizedCharacter(bool useDefaultPrimaryWeapon)
        {
            List<CharacterReference.RaceAndGender> raceAndGenderList = new List<CharacterReference.RaceAndGender>()
            {
                CharacterReference.RaceAndGender.HumanMale,
                CharacterReference.RaceAndGender.HumanFemale
            };

            CharacterReference.RaceAndGender raceAndGender = raceAndGenderList[Random.Range(0, raceAndGenderList.Count)];
            var model = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterModel(raceAndGender);

            var characterMaterialOptions = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterMaterialOptions(raceAndGender);
            var equipmentOptions = PlayerDataManager.Singleton.GetCharacterReference().GetCharacterEquipmentOptions(raceAndGender);

            return new Character("", model.model.name,
                "Name", 0,
                characterMaterialOptions.FindAll(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Body).Random().material.name,
                characterMaterialOptions.FindAll(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Eyes).Random().material.name,

                raceAndGender != CharacterReference.RaceAndGender.HumanMale
                    ? "null"
                    : equipmentOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Beard).Random().GetModel(raceAndGender, null).name,

                raceAndGender == CharacterReference.RaceAndGender.HumanFemale
                    ? characterMaterialOptions.FindAll(item => item.materialApplicationLocation == CharacterReference.MaterialApplicationLocation.Brows).Random().material.name
                    : equipmentOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Brows).Random().GetModel(raceAndGender, null).name,

                equipmentOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Hair).Random().GetModel(raceAndGender, null).name,
                1,
                new CharacterAttributes(1, 1, 1, 1, 1),
                GetRandomizedLoadout(raceAndGender, useDefaultPrimaryWeapon),
                GetRandomizedLoadout(raceAndGender),
                GetRandomizedLoadout(raceAndGender),
                GetRandomizedLoadout(raceAndGender),
                raceAndGender);
        }

        public static CharacterReference.WeaponOption GetDefaultPrimaryWeapon()
        {
            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
            return System.Array.Find(weaponOptions, item => item.name == "Flintblade");
        }

        public static CharacterReference.WeaponOption GetDefaultSecondaryWeapon()
        {
            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();
            return System.Array.Find(weaponOptions, item => item.name == "Sylvan Sentinel");
        }

        public static Loadout GetDefaultDisplayLoadout(CharacterReference.RaceAndGender raceAndGender)
        {
            List<CharacterReference.WearableEquipmentOption> armorOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(raceAndGender);

            return new Loadout("1",
                "",
                armorOptions.Find(item => item.isBasicGear & item.equipmentType == CharacterReference.EquipmentType.Chest & item.groupName == "Winterstalk").itemWebId,
                "",
                armorOptions.Find(item => item.isBasicGear & item.equipmentType == CharacterReference.EquipmentType.Boots & item.groupName == "Winterstalk").itemWebId,
                armorOptions.Find(item => item.isBasicGear & item.equipmentType == CharacterReference.EquipmentType.Pants & item.groupName == "Winterstalk").itemWebId,
                "",
                "",
                "",
                "",
                GetDefaultPrimaryWeapon().itemWebId,
                GetDefaultSecondaryWeapon().itemWebId,
                true);
        }

        public static readonly List<CharacterReference.EquipmentType> NullableEquipmentTypes = new List<CharacterReference.EquipmentType>()
        {
            CharacterReference.EquipmentType.Belt,
            CharacterReference.EquipmentType.Cape,
            CharacterReference.EquipmentType.Gloves,
            CharacterReference.EquipmentType.Helm,
            CharacterReference.EquipmentType.Shoulders,
            CharacterReference.EquipmentType.Robe
        };

        public Loadout GetRandomizedLoadout(CharacterReference.RaceAndGender raceAndGender, bool useDefaultPrimaryWeapon = false)
        {
            List<CharacterReference.WearableEquipmentOption> armorOptions = PlayerDataManager.Singleton.GetCharacterReference().GetArmorEquipmentOptions(raceAndGender);
            CharacterReference.WeaponOption[] weaponOptions = PlayerDataManager.Singleton.GetCharacterReference().GetWeaponOptions();

            var helmOptions = armorOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Helm);
            var chestOptions = armorOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Chest);
            var shoulderOptions = armorOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Shoulders);
            var bootsOptions = armorOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Boots);
            var pantsOptions = armorOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Pants);
            var beltOptions = armorOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Belt);
            var gloveOptions = armorOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Gloves);
            var capeOptions = armorOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Cape);
            var robeOptions = armorOptions.FindAll(item => item.equipmentType == CharacterReference.EquipmentType.Robe);

            int helmIndex = Random.Range(NullableEquipmentTypes.Contains(CharacterReference.EquipmentType.Helm) ? -1 : 0, helmOptions.Count);
            int chestIndex = Random.Range(NullableEquipmentTypes.Contains(CharacterReference.EquipmentType.Chest) ? -1 : 0, chestOptions.Count);
            int shoulderIndex = Random.Range(NullableEquipmentTypes.Contains(CharacterReference.EquipmentType.Shoulders) ? -1 : 0, shoulderOptions.Count);
            int bootsIndex = Random.Range(NullableEquipmentTypes.Contains(CharacterReference.EquipmentType.Boots) ? -1 : 0, bootsOptions.Count);
            int pantsIndex = Random.Range(NullableEquipmentTypes.Contains(CharacterReference.EquipmentType.Pants) ? -1 : 0, pantsOptions.Count);
            int beltIndex = Random.Range(NullableEquipmentTypes.Contains(CharacterReference.EquipmentType.Belt) ? -1 : 0, beltOptions.Count);
            int gloveIndex = Random.Range(NullableEquipmentTypes.Contains(CharacterReference.EquipmentType.Gloves) ? -1 : 0, gloveOptions.Count);
            int capeIndex = Random.Range(NullableEquipmentTypes.Contains(CharacterReference.EquipmentType.Cape) ? -1 : 0, capeOptions.Count);
            int robeIndex = Random.Range(NullableEquipmentTypes.Contains(CharacterReference.EquipmentType.Robe) ? -1 : 0, robeOptions.Count);

            int weapon1Index = useDefaultPrimaryWeapon ? 1 : Random.Range(0, weaponOptions.Length);

            var weaponOptionsOfDifferentClass = System.Array.FindAll(weaponOptions, item => item.weapon.GetWeaponClass() != weaponOptions[weapon1Index].weapon.GetWeaponClass());
            int weapon2Index = Random.Range(0, weaponOptionsOfDifferentClass.Length);
            weapon2Index = System.Array.FindIndex(weaponOptions, item => item.itemWebId == weaponOptionsOfDifferentClass[weapon2Index].itemWebId);

            return new Loadout("1",
                helmOptions.Count == 0 ? "" : (helmIndex == -1 ? "" : helmOptions[helmIndex].itemWebId),
                chestOptions.Count == 0 ? "" : (chestIndex == -1 ? "" : chestOptions[chestIndex].itemWebId),
                shoulderOptions.Count == 0 ? "" : (shoulderIndex == -1 ? "" : shoulderOptions[shoulderIndex].itemWebId),
                bootsOptions.Count == 0 ? "" : (bootsIndex == -1 ? "" : bootsOptions[bootsIndex].itemWebId),
                pantsOptions.Count == 0 ? "" : (pantsIndex == -1 ? "" : pantsOptions[pantsIndex].itemWebId),
                beltOptions.Count == 0 ? "" : (beltIndex == -1 ? "" : beltOptions[beltIndex].itemWebId),
                gloveOptions.Count == 0 ? "" : (gloveIndex == -1 ? "" : gloveOptions[gloveIndex].itemWebId),
                capeOptions.Count == 0 ? "" : (capeIndex == -1 ? "" : capeOptions[capeIndex].itemWebId),
                robeOptions.Count == 0 ? "" : (robeIndex == -1 ? "" : robeOptions[robeIndex].itemWebId),
                weaponOptions[weapon1Index].itemWebId,
                weaponOptions[weapon2Index].itemWebId,
                true);
        }

        private CharacterJson ToCharacterJson(Character character)
        {
            string[] raceAndGenderStrings = Regex.Matches(character.raceAndGender.ToString(), @"([A-Z][a-z]+)").Cast<Match>().Select(m => m.Value).ToArray();
            string race = raceAndGenderStrings[0];
            string gender = raceAndGenderStrings[1];

            return new CharacterJson()
            {
                _id = character._id.ToString(),
                name = character.name.ToString(),
                model = character.model.ToString(),
                bodyColor = character.bodyColor.ToString(),
                eyeColor = character.eyeColor.ToString(),
                beard = character.beard.ToString(),
                brows = character.brows.ToString(),
                hair = character.hair.ToString(),
                attributes = new CharacterAttributes(1, 1, 1, 1, 1),
                loadOuts = new List<LoadoutJson>(),
                userId = character.userId.ToString(),
                slot = character.slot,
                level = character.level,
                experience = character.experience,
                race = race,
                gender = gender
            };
        }

        public struct Character : INetworkSerializable
        {
            public FixedString64Bytes _id;
            public FixedString64Bytes name;
            public FixedString64Bytes model;
            public FixedString64Bytes bodyColor;
            public FixedString64Bytes eyeColor;
            public FixedString64Bytes beard;
            public FixedString64Bytes brows;
            public FixedString64Bytes hair;
            public CharacterAttributes attributes;
            public Loadout loadoutPreset1;
            public Loadout loadoutPreset2;
            public Loadout loadoutPreset3;
            public Loadout loadoutPreset4;
            public FixedString64Bytes userId;
            public int slot;
            public int level;
            public int experience;
            public CharacterReference.RaceAndGender raceAndGender;

            public Character(string _id, string model, string name, int experience, string bodyColor, string eyeColor, string beard, string brows, string hair, int level,
                CharacterAttributes characterAttributes,
                Loadout loadoutPreset1, Loadout loadoutPreset2, Loadout loadoutPreset3, Loadout loadoutPreset4, CharacterReference.RaceAndGender raceAndGender)
            {
                loadoutPreset1.loadoutSlot = "1";
                loadoutPreset2.loadoutSlot = "2";
                loadoutPreset3.loadoutSlot = "3";
                loadoutPreset4.loadoutSlot = "4";

                slot = 0;
                this._id = _id;
                this.model = model;
                this.name = name;
                this.experience = experience;
                this.bodyColor = bodyColor;
                this.eyeColor = eyeColor;
                this.beard = beard;
                this.brows = brows;
                this.hair = hair;
                attributes = characterAttributes;
                userId = WebRequestManager.Singleton.UserManager.CurrentlyLoggedInUserId;
                this.level = level;
                this.loadoutPreset1 = loadoutPreset1;
                this.loadoutPreset2 = loadoutPreset2;
                this.loadoutPreset3 = loadoutPreset3;
                this.loadoutPreset4 = loadoutPreset4;
                this.raceAndGender = raceAndGender;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref _id);
                serializer.SerializeValue(ref name);
                serializer.SerializeValue(ref model);
                serializer.SerializeValue(ref bodyColor);
                serializer.SerializeValue(ref eyeColor);
                serializer.SerializeValue(ref beard);
                serializer.SerializeValue(ref brows);
                serializer.SerializeValue(ref hair);
                serializer.SerializeNetworkSerializable(ref attributes);
                serializer.SerializeNetworkSerializable(ref loadoutPreset1);
                serializer.SerializeNetworkSerializable(ref loadoutPreset2);
                serializer.SerializeNetworkSerializable(ref loadoutPreset3);
                serializer.SerializeNetworkSerializable(ref loadoutPreset4);
                serializer.SerializeValue(ref userId);
                serializer.SerializeValue(ref slot);
                serializer.SerializeValue(ref level);
                serializer.SerializeValue(ref experience);
                serializer.SerializeValue(ref raceAndGender);
            }

            public Loadout[] GetLoadouts()
            {
                return new Loadout[]
                {
                    loadoutPreset1,
                    loadoutPreset2,
                    loadoutPreset3,
                    loadoutPreset4
                };
            }

            public Loadout GetLoadoutFromSlot(int loadoutSlot)
            {
                switch (loadoutSlot)
                {
                    case 0:
                        return loadoutPreset1;
                    case 1:
                        return loadoutPreset2;
                    case 2:
                        return loadoutPreset3;
                    case 3:
                        return loadoutPreset4;
                    default:
                        Debug.LogError("You haven't associated a loadout property to the following loadout slot: " + loadoutSlot);
                        return WebRequestManager.Singleton.CharacterManager.GetRandomizedLoadout(raceAndGender);
                }
            }

            public bool IsSlotActive(int loadoutSlot)
            {
                switch (loadoutSlot)
                {
                    case 0:
                        return loadoutPreset1.active;
                    case 1:
                        return loadoutPreset2.active;
                    case 2:
                        return loadoutPreset3.active;
                    case 3:
                        return loadoutPreset4.active;
                    default:
                        Debug.LogError("You haven't associated a loadout property to the following loadout slot: " + loadoutSlot);
                        return false;
                }
            }

            public bool HasActiveLoadout()
            {
                return loadoutPreset1.active | loadoutPreset2.active | loadoutPreset3.active | loadoutPreset4.active;
            }

            public Loadout GetActiveLoadout()
            {
                if (loadoutPreset1.active) { return loadoutPreset1; }
                if (loadoutPreset2.active) { return loadoutPreset2; }
                if (loadoutPreset3.active) { return loadoutPreset3; }
                if (loadoutPreset4.active) { return loadoutPreset4; }
                Debug.LogWarning("Character has no active loadout! " + _id);
                return loadoutPreset1;
            }

            public Character ChangeLoadoutFromSlot(int loadoutSlot, Loadout newLoadout)
            {
                Character copy = this;
                switch (loadoutSlot)
                {
                    case 0:
                        newLoadout.loadoutSlot = "1";
                        copy.loadoutPreset1 = newLoadout;
                        break;
                    case 1:
                        newLoadout.loadoutSlot = "2";
                        copy.loadoutPreset2 = newLoadout;
                        break;
                    case 2:
                        newLoadout.loadoutSlot = "3";
                        copy.loadoutPreset3 = newLoadout;
                        break;
                    case 3:
                        newLoadout.loadoutSlot = "4";
                        copy.loadoutPreset4 = newLoadout;
                        break;
                    default:
                        Debug.LogError("You haven't associated a loadout property to the following loadout slot: " + loadoutSlot);
                        break;
                }
                return copy;
            }

            public Character ChangeActiveLoadoutFromSlot(int loadoutSlot)
            {
                Character copy = this;

                copy.loadoutPreset1 = new Loadout(loadoutPreset1.loadoutSlot, loadoutPreset1.helmGearItemId,
                    loadoutPreset1.chestArmorGearItemId, loadoutPreset1.shouldersGearItemId, loadoutPreset1.bootsGearItemId,
                    loadoutPreset1.pantsGearItemId, loadoutPreset1.beltGearItemId, loadoutPreset1.glovesGearItemId,
                    loadoutPreset1.capeGearItemId, loadoutPreset1.robeGearItemId, loadoutPreset1.weapon1ItemId,
                    loadoutPreset1.weapon2ItemId, loadoutSlot == 0);

                copy.loadoutPreset2 = new Loadout(loadoutPreset2.loadoutSlot, loadoutPreset2.helmGearItemId,
                    loadoutPreset2.chestArmorGearItemId, loadoutPreset2.shouldersGearItemId, loadoutPreset2.bootsGearItemId,
                    loadoutPreset2.pantsGearItemId, loadoutPreset2.beltGearItemId, loadoutPreset2.glovesGearItemId,
                    loadoutPreset2.capeGearItemId, loadoutPreset2.robeGearItemId, loadoutPreset2.weapon1ItemId,
                    loadoutPreset2.weapon2ItemId, loadoutSlot == 1);

                copy.loadoutPreset3 = new Loadout(loadoutPreset3.loadoutSlot, loadoutPreset3.helmGearItemId,
                    loadoutPreset3.chestArmorGearItemId, loadoutPreset3.shouldersGearItemId, loadoutPreset3.bootsGearItemId,
                    loadoutPreset3.pantsGearItemId, loadoutPreset3.beltGearItemId, loadoutPreset3.glovesGearItemId,
                    loadoutPreset3.capeGearItemId, loadoutPreset3.robeGearItemId, loadoutPreset3.weapon1ItemId,
                    loadoutPreset3.weapon2ItemId, loadoutSlot == 2);

                copy.loadoutPreset4 = new Loadout(loadoutPreset4.loadoutSlot, loadoutPreset4.helmGearItemId,
                    loadoutPreset4.chestArmorGearItemId, loadoutPreset4.shouldersGearItemId, loadoutPreset4.bootsGearItemId,
                    loadoutPreset4.pantsGearItemId, loadoutPreset4.beltGearItemId, loadoutPreset4.glovesGearItemId,
                    loadoutPreset4.capeGearItemId, loadoutPreset4.robeGearItemId, loadoutPreset4.weapon1ItemId,
                    loadoutPreset4.weapon2ItemId, loadoutSlot == 3);

                return copy;
            }

            public enum AttributeType
            {
                Strength,
                Vitality,
                Agility,
                Dexterity,
                Intelligence
            }

            public int GetStat(AttributeType attributeType)
            {
                switch (attributeType)
                {
                    case AttributeType.Strength:
                        return attributes.strength;
                    case AttributeType.Vitality:
                        return attributes.vitality;
                    case AttributeType.Agility:
                        return attributes.agility;
                    case AttributeType.Dexterity:
                        return attributes.dexterity;
                    case AttributeType.Intelligence:
                        return attributes.intelligence;
                    default:
                        Debug.LogError("Unsure how to handle attribute type " + attributeType);
                        break;
                }
                return 0;
            }

            public Character SetStat(AttributeType attributeType, int newValue)
            {
                Character copy = this;
                switch (attributeType)
                {
                    case AttributeType.Strength:
                        copy.attributes.strength = newValue;
                        break;
                    case AttributeType.Vitality:
                        copy.attributes.vitality = newValue;
                        break;
                    case AttributeType.Agility:
                        copy.attributes.agility = newValue;
                        break;
                    case AttributeType.Dexterity:
                        copy.attributes.dexterity = newValue;
                        break;
                    case AttributeType.Intelligence:
                        copy.attributes.intelligence = newValue;
                        break;
                    default:
                        Debug.LogError("Unsure how to handle attribute type " + attributeType);
                        break;
                }
                return copy;
            }
        }

        public struct Loadout : INetworkSerializable, System.IEquatable<Loadout>
        {
            public FixedString64Bytes loadoutSlot;
            public FixedString64Bytes helmGearItemId;
            public FixedString64Bytes chestArmorGearItemId;
            public FixedString64Bytes shouldersGearItemId;
            public FixedString64Bytes bootsGearItemId;
            public FixedString64Bytes pantsGearItemId;
            public FixedString64Bytes beltGearItemId;
            public FixedString64Bytes glovesGearItemId;
            public FixedString64Bytes capeGearItemId;
            public FixedString64Bytes robeGearItemId;
            public FixedString64Bytes weapon1ItemId;
            public FixedString64Bytes weapon2ItemId;
            public bool active;

            public Loadout(FixedString64Bytes loadoutSlot, FixedString64Bytes helmGearItemId, FixedString64Bytes chestArmorGearItemId, FixedString64Bytes shouldersGearItemId, FixedString64Bytes bootsGearItemId, FixedString64Bytes pantsGearItemId, FixedString64Bytes beltGearItemId, FixedString64Bytes glovesGearItemId, FixedString64Bytes capeGearItemId, FixedString64Bytes robeGearItemId, FixedString64Bytes weapon1ItemId, FixedString64Bytes weapon2ItemId, bool active)
            {
                this.loadoutSlot = loadoutSlot;
                this.helmGearItemId = helmGearItemId;
                this.chestArmorGearItemId = chestArmorGearItemId;
                this.shouldersGearItemId = shouldersGearItemId;
                this.bootsGearItemId = bootsGearItemId;
                this.pantsGearItemId = pantsGearItemId;
                this.beltGearItemId = beltGearItemId;
                this.glovesGearItemId = glovesGearItemId;
                this.capeGearItemId = capeGearItemId;
                this.robeGearItemId = robeGearItemId;
                this.weapon1ItemId = weapon1ItemId;
                this.weapon2ItemId = weapon2ItemId;
                this.active = active;
            }

            public override string ToString()
            {
                return "Slot: " + loadoutSlot + " Active: " + active
                    + "\nHelm Id: " + helmGearItemId
                    + "\nChest Id: " + chestArmorGearItemId
                    + "\nShoulders Id: " + shouldersGearItemId
                    + "\nBoots Id: " + bootsGearItemId
                    + "\nPants Id: " + pantsGearItemId
                    + "\nBelt Id: " + beltGearItemId
                    + "\nGloves Id: " + glovesGearItemId
                    + "\nCape Id: " + capeGearItemId
                    + "\nRobe Id: " + robeGearItemId
                    + "\nWeapon 1 Id: " + weapon1ItemId
                    + "\nWeapon 2 Id: " + weapon2ItemId;
            }

            public static Loadout GetEmptyLoadout()
            {
                return new Loadout();
            }

            public Loadout Copy()
            {
                return new Loadout(loadoutSlot, helmGearItemId, chestArmorGearItemId, shouldersGearItemId, bootsGearItemId, pantsGearItemId,
                    beltGearItemId, glovesGearItemId, capeGearItemId, robeGearItemId, weapon1ItemId, weapon2ItemId, active);
            }

            public List<FixedString64Bytes> GetLoadoutAsList()
            {
                return new List<FixedString64Bytes>()
                {
                    helmGearItemId,
                    chestArmorGearItemId,
                    shouldersGearItemId,
                    bootsGearItemId,
                    pantsGearItemId,
                    beltGearItemId,
                    glovesGearItemId,
                    capeGearItemId,
                    robeGearItemId,
                    weapon1ItemId,
                    weapon2ItemId
                };
            }

            public Dictionary<CharacterReference.EquipmentType, FixedString64Bytes> GetLoadoutArmorPiecesAsDictionary()
            {
                return new Dictionary<CharacterReference.EquipmentType, FixedString64Bytes>()
                {
                    { CharacterReference.EquipmentType.Helm, helmGearItemId },
                    { CharacterReference.EquipmentType.Chest, chestArmorGearItemId },
                    { CharacterReference.EquipmentType.Shoulders, shouldersGearItemId },
                    { CharacterReference.EquipmentType.Boots, bootsGearItemId },
                    { CharacterReference.EquipmentType.Pants, pantsGearItemId },
                    { CharacterReference.EquipmentType.Belt, beltGearItemId },
                    { CharacterReference.EquipmentType.Gloves, glovesGearItemId },
                    { CharacterReference.EquipmentType.Cape, capeGearItemId },
                    { CharacterReference.EquipmentType.Robe, robeGearItemId }
                };
            }

            public string[] GetLoadoutItemIDsAsArray()
            {
                return new string[]
                {
                    helmGearItemId.ToString(),
                    chestArmorGearItemId.ToString(),
                    shouldersGearItemId.ToString(),
                    bootsGearItemId.ToString(),
                    pantsGearItemId.ToString(),
                    beltGearItemId.ToString(),
                    glovesGearItemId.ToString(),
                    capeGearItemId.ToString(),
                    robeGearItemId.ToString(),
                    weapon1ItemId.ToString(),
                    weapon2ItemId.ToString()
                };
            }

            public bool IsValid()
            {
                if (string.IsNullOrWhiteSpace(weapon1ItemId.ToString())) { return false; }
                if (string.IsNullOrWhiteSpace(weapon2ItemId.ToString())) { return false; }

                foreach (KeyValuePair<CharacterReference.EquipmentType, FixedString64Bytes> kvp in GetLoadoutArmorPiecesAsDictionary())
                {
                    if (!NullableEquipmentTypes.Contains(kvp.Key))
                    {
                        if (string.IsNullOrWhiteSpace(kvp.Value.ToString()))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }

            public Loadout GetValidCopy(CharacterReference.RaceAndGender raceAndGender)
            {
                if (IsValid()) { return this; }

                Loadout validCopy = Copy();
                Loadout defaultLoadout = GetDefaultDisplayLoadout(raceAndGender);

                if (string.IsNullOrWhiteSpace(validCopy.weapon1ItemId.ToString())) { validCopy.weapon1ItemId = defaultLoadout.weapon1ItemId; }
                if (string.IsNullOrWhiteSpace(validCopy.weapon2ItemId.ToString())) { validCopy.weapon2ItemId = defaultLoadout.weapon2ItemId; }

                if (string.IsNullOrWhiteSpace(validCopy.helmGearItemId.ToString())) { validCopy.helmGearItemId = defaultLoadout.helmGearItemId; }
                if (string.IsNullOrWhiteSpace(validCopy.chestArmorGearItemId.ToString())) { validCopy.chestArmorGearItemId = defaultLoadout.chestArmorGearItemId; }
                if (string.IsNullOrWhiteSpace(validCopy.shouldersGearItemId.ToString())) { validCopy.shouldersGearItemId = defaultLoadout.shouldersGearItemId; }
                if (string.IsNullOrWhiteSpace(validCopy.bootsGearItemId.ToString())) { validCopy.bootsGearItemId = defaultLoadout.bootsGearItemId; }
                if (string.IsNullOrWhiteSpace(validCopy.pantsGearItemId.ToString())) { validCopy.pantsGearItemId = defaultLoadout.pantsGearItemId; }
                if (string.IsNullOrWhiteSpace(validCopy.beltGearItemId.ToString())) { validCopy.beltGearItemId = defaultLoadout.beltGearItemId; }
                if (string.IsNullOrWhiteSpace(validCopy.glovesGearItemId.ToString())) { validCopy.glovesGearItemId = defaultLoadout.glovesGearItemId; }
                if (string.IsNullOrWhiteSpace(validCopy.capeGearItemId.ToString())) { validCopy.capeGearItemId = defaultLoadout.capeGearItemId; }
                if (string.IsNullOrWhiteSpace(validCopy.robeGearItemId.ToString())) { validCopy.robeGearItemId = defaultLoadout.robeGearItemId; }

                return validCopy;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref loadoutSlot);
                serializer.SerializeValue(ref helmGearItemId);
                serializer.SerializeValue(ref chestArmorGearItemId);
                serializer.SerializeValue(ref shouldersGearItemId);
                serializer.SerializeValue(ref bootsGearItemId);
                serializer.SerializeValue(ref pantsGearItemId);
                serializer.SerializeValue(ref beltGearItemId);
                serializer.SerializeValue(ref glovesGearItemId);
                serializer.SerializeValue(ref capeGearItemId);
                serializer.SerializeValue(ref robeGearItemId);
                serializer.SerializeValue(ref weapon1ItemId);
                serializer.SerializeValue(ref weapon2ItemId);
                serializer.SerializeValue(ref active);
            }

            public bool Equals(Loadout other)
            {
                return loadoutSlot == other.loadoutSlot & helmGearItemId == other.helmGearItemId & chestArmorGearItemId == other.chestArmorGearItemId
                    & shouldersGearItemId == other.shouldersGearItemId & bootsGearItemId == other.bootsGearItemId & pantsGearItemId == other.pantsGearItemId
                    & beltGearItemId == other.beltGearItemId & glovesGearItemId == other.glovesGearItemId & capeGearItemId == other.capeGearItemId
                    & robeGearItemId == other.robeGearItemId & weapon1ItemId == other.weapon1ItemId & weapon2ItemId == other.weapon2ItemId;
            }

            public bool EqualsIgnoringSlot(Loadout other)
            {
                return helmGearItemId == other.helmGearItemId & chestArmorGearItemId == other.chestArmorGearItemId
                    & shouldersGearItemId == other.shouldersGearItemId & bootsGearItemId == other.bootsGearItemId & pantsGearItemId == other.pantsGearItemId
                    & beltGearItemId == other.beltGearItemId & glovesGearItemId == other.glovesGearItemId & capeGearItemId == other.capeGearItemId
                    & robeGearItemId == other.robeGearItemId & weapon1ItemId == other.weapon1ItemId & weapon2ItemId == other.weapon2ItemId;
            }
        }

        private struct CharacterJson
        {
            public string _id;
            public int slot;
            public string name;
            public string model;
            public int experience;
            public string bodyColor;
            public string eyeColor;
            public string beard;
            public string brows;
            public string hair;
            public string gender;
            public string race;
            public string dateCreated;
            public CharacterAttributes attributes;
            public List<LoadoutJson> loadOuts;
            public bool enabled;
            public string userId;
            public int level;
            public double attack;
            public double defense;
            public double hp;
            public double stamina;
            public double critChance;
            public double crit;
            public string id;

            public static CharacterJson DeserializeJson(string json)
            {
                List<CharacterJson> list = DeserializeJsonList('[' + json + ']');
                if (list.Count == 0)
                {
                    Debug.LogError("Unable to deserialize json body " + json);
                    return new CharacterJson();
                }
                else
                {
                    if (list.Count > 1) { Debug.LogError("There should only be one character in this body! " + json); }
                    return list[0];
                }
            }

            public static List<CharacterJson> DeserializeJsonList(string json)
            {
                List<CharacterJson> parsedElements = new List<CharacterJson>();

                List<string> splitStrings = new List<string>();
                string splitToAppend = "";
                char charToSplitOn = '"';
                for (int i = 0; i < json.Length; i++)
                {
                    if (json[i] == charToSplitOn)
                    {
                        if (splitToAppend.Length > 0)
                        {
                            if (splitToAppend[^1] == '\\')
                            {
                                splitToAppend = splitToAppend[0..^1] + json[i];
                                continue;
                            }
                        }

                        splitStrings.Add(splitToAppend);
                        splitToAppend = "";
                    }
                    else
                    {
                        splitToAppend += json[i];
                    }
                }

                CharacterJson element = new CharacterJson();
                element.loadOuts = new List<LoadoutJson>();
                for (int i = 0; i < splitStrings.Count; i++)
                {
                    switch (splitStrings[i])
                    {
                        case "_id":
                            element = new CharacterJson();
                            element.loadOuts = new List<LoadoutJson>();
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("_id can't find a : in between the value!");
                                }
                                element._id = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property _id!");
                            }
                            break;
                        case "userId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("userId can't find a : in between the value!");
                                }
                                element.userId = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property userId!");
                            }
                            break;
                        case "slot":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (int.TryParse(splitStrings[i + 1][1..^1], out int result))
                                {
                                    element.slot = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing slot property! " + splitStrings[i + 1][1..^1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property _id!");
                            }
                            break;
                        case "name":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("name can't find a : in between the value!");
                                }
                                element.name = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property name!");
                            }
                            break;
                        case "model":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("model can't find a : in between the value!");
                                }
                                element.model = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property model!");
                            }
                            break;
                        case "experience":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (int.TryParse(splitStrings[i + 1][1..^1], out int result))
                                {
                                    element.experience = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing experience property! " + splitStrings[i + 1][1..^1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property experience!");
                            }
                            break;
                        case "bodyColor":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("bodyColor can't find a : in between the value!");
                                }
                                element.bodyColor = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property bodyColor!");
                            }
                            break;
                        case "eyeColor":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("eyeColor can't find a : in between the value!");
                                }
                                element.eyeColor = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property eyeColor!");
                            }
                            break;
                        case "beard":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("beard can't find a : in between the value!");
                                }
                                element.beard = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property beard!");
                            }
                            break;
                        case "brows":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("brows can't find a : in between the value!");
                                }
                                element.brows = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property brows!");
                            }
                            break;
                        case "hair":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("hair can't find a : in between the value!");
                                }
                                element.hair = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property hair!");
                            }
                            break;
                        case "gender":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("gender can't find a : in between the value!");
                                }
                                element.gender = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property gender!");
                            }
                            break;
                        case "race":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("race can't find a : in between the value!");
                                }
                                element.race = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property race!");
                            }
                            break;
                        case "dateCreated":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("dateCreated can't find a : in between the value!");
                                }
                                element.dateCreated = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property dateCreated!");
                            }
                            break;
                        case "attributes":
                            List<string> splitAttributesStrings = new List<string>();
                            for (int j = i; j < splitStrings.Count; j++)
                            {
                                splitAttributesStrings.Add(splitStrings[j]);
                                if (splitStrings[j].Contains("},")) { break; }
                            }
                            element.attributes = CharacterAttributes.FromCharacterJsonExtract(splitAttributesStrings);
                            break;
                        case "loadoutSlot":
                            List<string> splitLoadoutStrings = new List<string>();
                            for (int j = i; j < splitStrings.Count; j++)
                            {
                                splitLoadoutStrings.Add(splitStrings[j]);
                                if (splitStrings[j].Contains("}")) { break; }
                            }
                            element.loadOuts.Add(LoadoutJson.FromCharacterJsonExtract(splitLoadoutStrings));
                            break;
                        case "level":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (int.TryParse(splitStrings[i + 1][1..^1], out int result))
                                {
                                    element.level = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing level property! " + splitStrings[i + 1][1..^1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property level!");
                            }
                            break;
                        case "attack":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (float.TryParse(splitStrings[i + 1][1..^1], out float result))
                                {
                                    element.attack = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing attack property! " + splitStrings[i + 1][1..^1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property attack!");
                            }
                            break;
                        case "defense":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (float.TryParse(splitStrings[i + 1][1..^1], out float result))
                                {
                                    element.defense = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing defense property! " + splitStrings[i + 1][1..^1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property defense!");
                            }
                            break;
                        case "hp":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (float.TryParse(splitStrings[i + 1][1..^1], out float result))
                                {
                                    element.hp = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing hp property! " + splitStrings[i + 1][1..^1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property hp!");
                            }
                            break;
                        case "stamina":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (float.TryParse(splitStrings[i + 1][1..^1], out float result))
                                {
                                    element.stamina = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing hp property! " + splitStrings[i + 1][1..^1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property hp!");
                            }
                            break;
                        case "critChance":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (float.TryParse(splitStrings[i + 1][1..^1], out float result))
                                {
                                    element.critChance = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing critChance property! " + splitStrings[i + 1][1..^1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property critChance!");
                            }
                            break;
                        case "crit":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (float.TryParse(splitStrings[i + 1][1..^1], out float result))
                                {
                                    element.crit = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing crit property! " + splitStrings[i + 1][1..^1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property crit!");
                            }
                            break;
                        case "id":
                            parsedElements.Add(element);
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("id can't find a : in between the value!");
                                }
                                element.id = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property id!");
                            }
                            break;
                    }
                }
                return parsedElements;
            }

            public Character ToCharacter()
            {
                CharacterReference.RaceAndGender raceAndGender = System.Enum.Parse<CharacterReference.RaceAndGender>(char.ToUpper(race[0]) + race[1..].ToLower() + char.ToUpper(gender[0]) + gender[1..].ToLower());
                int loadout1Index = loadOuts.FindIndex(item => item.loadoutSlot == "1");
                int loadout2Index = loadOuts.FindIndex(item => item.loadoutSlot == "2");
                int loadout3Index = loadOuts.FindIndex(item => item.loadoutSlot == "3");
                int loadout4Index = loadOuts.FindIndex(item => item.loadoutSlot == "4");

                // Index values of -1 mean that this loadout doesn't exist in the API yet

                //return new Character(_id, model, name, experience, bodyColor, eyeColor, beard, brows, hair, level,
                //    loadout1Index == -1 ? Singleton.GetDefaultDisplayLoadout(raceAndGender) : loadOuts[loadout1Index].ToLoadout(),
                //    loadout2Index == -1 ? Singleton.GetDefaultDisplayLoadout(raceAndGender) : loadOuts[loadout2Index].ToLoadout(),
                //    loadout3Index == -1 ? Singleton.GetDefaultDisplayLoadout(raceAndGender) : loadOuts[loadout3Index].ToLoadout(),
                //    loadout4Index == -1 ? Singleton.GetDefaultDisplayLoadout(raceAndGender) : loadOuts[loadout4Index].ToLoadout(),
                //    raceAndGender);

                return new Character(_id, model, name, experience, bodyColor, eyeColor, beard, brows, hair, level, attributes,
                    loadout1Index == -1 ? Loadout.GetEmptyLoadout() : loadOuts[loadout1Index].ToLoadout(),
                    loadout2Index == -1 ? Loadout.GetEmptyLoadout() : loadOuts[loadout2Index].ToLoadout(),
                    loadout3Index == -1 ? Loadout.GetEmptyLoadout() : loadOuts[loadout3Index].ToLoadout(),
                    loadout4Index == -1 ? Loadout.GetEmptyLoadout() : loadOuts[loadout4Index].ToLoadout(),
                    raceAndGender);
            }
        }

        private struct LoadoutJson
        {
            public string loadoutSlot;
            public string helmGearItemId;
            public string chestArmorGearItemId;
            public string shouldersGearItemId;
            public string bootsGearItemId;
            public string pantsGearItemId;
            public string beltGearItemId;
            public string glovesGearItemId;
            public string capeGearItemId;
            public string robeGearItemId;
            public string weapon1ItemId;
            public string weapon2ItemId;
            public bool active;

            public LoadoutJson(string loadoutSlot, string helmGearItemId, string chestArmorGearItemId, string shouldersGearItemId, string bootsGearItemId, string pantsGearItemId, string beltGearItemId, string glovesGearItemId, string capeGearItemId, string robeGearItemId, string weapon1ItemId, string weapon2ItemId, bool active)
            {
                this.loadoutSlot = loadoutSlot;
                this.helmGearItemId = helmGearItemId;
                this.chestArmorGearItemId = chestArmorGearItemId;
                this.shouldersGearItemId = shouldersGearItemId;
                this.bootsGearItemId = bootsGearItemId;
                this.pantsGearItemId = pantsGearItemId;
                this.beltGearItemId = beltGearItemId;
                this.glovesGearItemId = glovesGearItemId;
                this.capeGearItemId = capeGearItemId;
                this.robeGearItemId = robeGearItemId;
                this.weapon1ItemId = weapon1ItemId;
                this.weapon2ItemId = weapon2ItemId;
                this.active = active;
            }

            public static LoadoutJson FromCharacterJsonExtract(List<string> splitStrings)
            {
                LoadoutJson loadoutJson = new LoadoutJson();
                for (int i = 0; i < splitStrings.Count; i++)
                {
                    string split = splitStrings[i].Replace("},", ",");

                    switch (split)
                    {
                        case "loadoutSlot":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    Debug.LogWarning("Loadout slot should not be null!");
                                }

                                if (splitStrings[i + 1] != ":")
                                {
                                    Debug.LogError("loadoutSlot can't find a : in between the value!");
                                }
                                loadoutJson.loadoutSlot = splitStrings[i + 2];
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property loadoutSlot!");
                            }
                            break;
                        case "helmGearItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.helmGearItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("helmGearItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.helmGearItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property helmGearItemId!");
                            }
                            break;
                        case "chestArmorGearItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.chestArmorGearItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("chestArmorGearItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.chestArmorGearItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property chestArmorGearItemId!");
                            }
                            break;
                        case "shouldersGearItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.shouldersGearItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("shouldersGearItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.shouldersGearItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property shouldersGearItemId!");
                            }
                            break;
                        case "bootsGearItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.bootsGearItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("bootsGearItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.bootsGearItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property bootsGearItemId!");
                            }
                            break;
                        case "pantsGearItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.pantsGearItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("pantsGearItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.pantsGearItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property pantsGearItemId!");
                            }
                            break;
                        case "beltGearItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.beltGearItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("beltGearItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.beltGearItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property beltGearItemId!");
                            }
                            break;
                        case "glovesGearItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.glovesGearItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("glovesGearItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.glovesGearItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property glovesGearItemId!");
                            }
                            break;
                        case "capeGearItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.capeGearItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("capeGearItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.capeGearItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property capeGearItemId!");
                            }
                            break;
                        case "robeGearItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.robeGearItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("robeGearItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.robeGearItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property robeGearItemId!");
                            }
                            break;
                        case "weapon1ItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.weapon1ItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("weapon1ItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.weapon1ItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property weapon1ItemId!");
                            }
                            break;
                        case "weapon2ItemId":
                            if (i + 2 < splitStrings.Count)
                            {
                                if (splitStrings[i + 1].Contains("null"))
                                {
                                    loadoutJson.weapon2ItemId = null;
                                }
                                else
                                {
                                    if (splitStrings[i + 1] != ":")
                                    {
                                        Debug.LogError("weapon2ItemId can't find a : in between the value!");
                                    }
                                    loadoutJson.weapon2ItemId = splitStrings[i + 2];
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property weapon2ItemId!");
                            }
                            break;
                        case "active":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (bool.TryParse(splitStrings[i + 1][1..^3], out bool result))
                                {
                                    loadoutJson.active = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing " + splitStrings[i + 1][1..^3]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property weapon2ItemId!");
                            }
                            break;
                    }
                }
                return loadoutJson;
            }

            public Loadout ToLoadout()
            {
                return new Loadout(loadoutSlot, helmGearItemId ?? "", chestArmorGearItemId ?? "", shouldersGearItemId ?? "", bootsGearItemId ?? "",
                    pantsGearItemId ?? "", beltGearItemId ?? "", glovesGearItemId ?? "", capeGearItemId ?? "", robeGearItemId ?? "", weapon1ItemId ?? "",
                    weapon2ItemId ?? "", active);
            }
        }

        public struct CharacterAttributes : INetworkSerializable, System.IEquatable<CharacterAttributes>
        {
            public int strength;
            public int vitality;
            public int agility;
            public int dexterity;
            public int intelligence;

            public CharacterAttributes(int strength, int vitality, int agility, int dexterity, int intelligence)
            {
                this.strength = strength;
                this.vitality = vitality;
                this.agility = agility;
                this.dexterity = dexterity;
                this.intelligence = intelligence;
            }

            public bool AreAnyValuesInvalid()
            {
                if (strength <= 0) { return true; }
                if (vitality <= 0) { return true; }
                if (agility <= 0) { return true; }
                if (dexterity <= 0) { return true; }
                if (intelligence <= 0) { return true; }
                return false;
            }

            public override string ToString()
            {
                //return strength + " " + vitality + " " + agility + " " + dexterity + " " + intelligence;
                return "Strength: " + strength
                    + "\nVitality: " + vitality
                    + "\nAgility: " + agility
                    + "\nDexterity: " + dexterity
                    + "\nIntelligence: " + intelligence;
            }

            public static CharacterAttributes FromCharacterJsonExtract(List<string> splitStrings)
            {
                CharacterAttributes attributes = new CharacterAttributes(1, 1, 1, 1, 1);
                for (int i = 0; i < splitStrings.Count; i++)
                {
                    string split = splitStrings[i].Replace("},", ",");
                    switch (split)
                    {
                        case "strength":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (int.TryParse(splitStrings[i + 1][1..^1], out int result))
                                {
                                    attributes.strength = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing strength property! " + splitStrings[i + 1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property strength!");
                            }
                            break;
                        case "vitality":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (int.TryParse(splitStrings[i + 1][1..^1], out int result))
                                {
                                    attributes.vitality = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing vitality property! " + splitStrings[i + 1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property vitality!");
                            }
                            break;
                        case "agility":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (int.TryParse(splitStrings[i + 1][1..^1], out int result))
                                {
                                    attributes.agility = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing agility property! " + splitStrings[i + 1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property agility!");
                            }
                            break;
                        case "dexterity":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (int.TryParse(splitStrings[i + 1][1..^1], out int result))
                                {
                                    attributes.dexterity = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing dexterity property! " + splitStrings[i + 1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property dexterity!");
                            }
                            break;
                        case "intelligence":
                            if (i + 1 < splitStrings.Count)
                            {
                                if (int.TryParse(splitStrings[i + 1][1..^2], out int result))
                                {
                                    attributes.intelligence = result;
                                }
                                else
                                {
                                    Debug.LogError("Error while parsing intelligence property! " + splitStrings[i + 1][1..^1]);
                                }
                            }
                            else
                            {
                                Debug.LogError("Could not find value for property intelligence!");
                            }
                            break;
                    }
                }
                return attributes;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref strength);
                serializer.SerializeValue(ref vitality);
                serializer.SerializeValue(ref agility);
                serializer.SerializeValue(ref dexterity);
                serializer.SerializeValue(ref intelligence);
            }

            public bool Equals(CharacterAttributes other)
            {
                return strength == other.strength
                    & vitality == other.vitality
                    & agility == other.agility
                    & dexterity == other.dexterity
                    & intelligence == other.intelligence;
            }
        }

        private struct CharacterPostPayload
        {
            public string userId;
            public NestedCharacter character;

            public CharacterPostPayload(Character character)
            {
                userId = character.userId.ToString();

                string[] raceAndGenderStrings = Regex.Matches(character.raceAndGender.ToString(), @"([A-Z][a-z]+)").Cast<Match>().Select(m => m.Value).ToArray();

                this.character = new NestedCharacter()
                {
                    slot = character.slot,
                    eyeColor = character.eyeColor.ToString(),
                    hair = string.IsNullOrEmpty(character.hair.ToString()) ? "null" : character.hair.ToString(),
                    bodyColor = string.IsNullOrEmpty(character.bodyColor.ToString()) ? "null" : character.bodyColor.ToString(),
                    beard = string.IsNullOrEmpty(character.beard.ToString()) ? "null" : character.beard.ToString(),
                    brows = string.IsNullOrEmpty(character.brows.ToString()) ? "null" : character.brows.ToString(),
                    name = string.IsNullOrEmpty(character.name.ToString()) ? "null" : character.name.ToString(),
                    model = string.IsNullOrEmpty(character.model.ToString()) ? "null" : character.model.ToString(),
                    race = string.IsNullOrEmpty(raceAndGenderStrings[0].ToUpper()) ? "null" : raceAndGenderStrings[0].ToUpper(),
                    gender = string.IsNullOrEmpty(raceAndGenderStrings[1].ToUpper()) ? "null" : raceAndGenderStrings[1].ToUpper()
                };
            }

            public struct NestedCharacter
            {
                public int slot;
                public string eyeColor;
                public string hair;
                public string bodyColor;
                public string beard;
                public string brows;
                public string name;
                public string model;
                public string race;
                public string gender;
            }
        }

        private struct CharacterCosmeticPutPayload
        {
            public string id;
            public int slot;
            public string eyeColor;
            public string hair;
            public string bodyColor;
            public string beard;
            public string brows;
            public string name;
            public string model;

            public CharacterCosmeticPutPayload(string id, int slot, string eyeColor, string hair, string bodyColor, string beard, string brows, string name, string model)
            {
                this.id = id;
                this.slot = slot;
                this.eyeColor = eyeColor;
                this.hair = hair;
                this.bodyColor = bodyColor;
                this.beard = beard;
                this.brows = brows;
                this.name = name;
                this.model = model;
            }
        }

        private struct CharacterLoadoutPutPayload
        {
            public string charId;
            public NestedCharacterLoadoutPutPayload loadout;

            public CharacterLoadoutPutPayload(string charId, Loadout loadout)
            {
                this.charId = charId;
                this.loadout = new NestedCharacterLoadoutPutPayload()
                {
                    loadoutSlot = StringUtility.EvaluateFixedString(loadout.loadoutSlot),
                    helmGearItemId = StringUtility.EvaluateFixedString(loadout.helmGearItemId),
                    chestArmorGearItemId = StringUtility.EvaluateFixedString(loadout.chestArmorGearItemId),
                    shouldersGearItemId = StringUtility.EvaluateFixedString(loadout.shouldersGearItemId),
                    bootsGearItemId = StringUtility.EvaluateFixedString(loadout.bootsGearItemId),
                    pantsGearItemId = StringUtility.EvaluateFixedString(loadout.pantsGearItemId),
                    beltGearItemId = StringUtility.EvaluateFixedString(loadout.beltGearItemId),
                    glovesGearItemId = StringUtility.EvaluateFixedString(loadout.glovesGearItemId),
                    capeGearItemId = StringUtility.EvaluateFixedString(loadout.capeGearItemId),
                    robeGearItemId = StringUtility.EvaluateFixedString(loadout.robeGearItemId),
                    weapon1ItemId = StringUtility.EvaluateFixedString(loadout.weapon1ItemId),
                    weapon2ItemId = StringUtility.EvaluateFixedString(loadout.weapon2ItemId)
                };
            }

            public struct NestedCharacterLoadoutPutPayload
            {
                public string loadoutSlot;
                public string helmGearItemId;
                public string chestArmorGearItemId;
                public string shouldersGearItemId;
                public string bootsGearItemId;
                public string pantsGearItemId;
                public string beltGearItemId;
                public string glovesGearItemId;
                public string capeGearItemId;
                public string robeGearItemId;
                public string weapon1ItemId;
                public string weapon2ItemId;
            }
        }

        private struct CharacterDisablePayload
        {
            public string characterId;

            public CharacterDisablePayload(string characterId)
            {
                this.characterId = characterId;
            }
        }

        public struct CharacterStats
        {
            public int level;
            public float currentExp;
            public float expToNextLv;
            public int nextStatPointRwd;
            public float attack;
            public float mattack;
            public float defense;
            public float mdefense;
            public float hp;
            public float stamina;
            public float critChance;
            public float crit;
            public float baseHP;
            public float baseST;
            public float baseAtk;
            public float baseMatk;
            public float weaponABaseAtk;
            public float weaponBBaseAtk;

            public CharacterStats(int level, float currentExp, float expToNextLv, int nextStatPointRwd, float attack, float mattack, float defense, float mdefense, float hp, float stamina, float critChance, float crit, float baseHP, float baseST, float baseAtk, float baseMatk, float weaponABaseAtk, float weaponBBaseAtk)
            {
                this.level = level;
                this.currentExp = currentExp;
                this.expToNextLv = expToNextLv;
                this.nextStatPointRwd = nextStatPointRwd;
                this.attack = attack;
                this.mattack = mattack;
                this.defense = defense;
                this.mdefense = mdefense;
                this.hp = hp;
                this.stamina = stamina;
                this.critChance = critChance;
                this.crit = crit;
                this.baseHP = baseHP;
                this.baseST = baseST;
                this.baseAtk = baseAtk;
                this.baseMatk = baseMatk;
                this.weaponABaseAtk = weaponABaseAtk;
                this.weaponBBaseAtk = weaponBBaseAtk;
            }

            public static CharacterStats GetDefaultStats()
            {
                return new CharacterStats(1, 0, 250, 5, 6, 5, 36, 33, 210, 120, 0.15f, 8.625f, 170, 105, 1.5f, 1.7f, 1, 1);
            }

            public int GetAvailableSkillPoints(CharacterAttributes characterAttributes)
            {
                int value = nextStatPointRwd;
                value -= characterAttributes.strength;
                value -= characterAttributes.vitality;
                value -= characterAttributes.agility;
                value -= characterAttributes.dexterity;
                value -= characterAttributes.intelligence;
                value = Mathf.Max(value, 0);
                return value;
            }
        }

        public bool TryGetCharacterStats(string characterId, out CharacterStats characterStats)
        {
            if (string.IsNullOrWhiteSpace(characterId)) // Bot character
            {
                // TODO change this to use bot profiles, as indicated by the character attributes?
                characterStats = CharacterStats.GetDefaultStats();
                return true;
            }
            else if (characterAttributesLookup.TryGetValue(characterId, out characterStats))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool getCharacterAttributesWasSuccessful;
        private Dictionary<string, CharacterStats> characterAttributesLookup = new Dictionary<string, CharacterStats>();
        public IEnumerator GetCharacterAttributes(string characterId)
        {
            getCharacterAttributesWasSuccessful = false;

            UnityWebRequest getRequest = UnityWebRequest.Get(WebRequestManager.Singleton.GetAPIURL(false) + "characters/" + "getCharacterAttribute/" + characterId);
            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in WebRequestManager.GetCharacterAttributes() " + getRequest.error + WebRequestManager.Singleton.GetAPIURL(false) + "characters/" + "getCharacterAttribute/" + characterId);
                getRequest.Dispose();
                yield break;
            }

            string json = getRequest.downloadHandler.text;
            if (json == "{}")
            {
                Debug.LogWarning("Character Stats Response Body is empty! " + characterId);
            }
            else if (characterAttributesLookup.ContainsKey(characterId))
            {
                characterAttributesLookup[characterId] = JsonConvert.DeserializeObject<CharacterStats>(json);
                getCharacterAttributesWasSuccessful = true;
            }
            else
            {
                characterAttributesLookup.Add(characterId, JsonConvert.DeserializeObject<CharacterStats>(json));
                getCharacterAttributesWasSuccessful = true;
            }

            getRequest.Dispose();
        }

        public IEnumerator UpdateCharacterExp(string characterId, float charExpToAdd)
        {
            UpdateCharacterExpPutPayload payload = new UpdateCharacterExpPutPayload()
            {
                charId = characterId,
                charExp = charExpToAdd
            };

            string json = JsonUtility.ToJson(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest putRequest = UnityWebRequest.Put(WebRequestManager.Singleton.GetAPIURL(false) + "characters/updateCharacterExp", jsonData);
            putRequest.SetRequestHeader("Content-Type", "application/json");
            yield return putRequest.SendWebRequest();

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                putRequest = UnityWebRequest.Put(WebRequestManager.Singleton.GetAPIURL(false) + "characters/updateCharacterExp", jsonData);
                putRequest.SetRequestHeader("Content-Type", "application/json");
                yield return putRequest.SendWebRequest();
            }

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.UpdateCharacterExp()" + putRequest.error);
            }
            else
            {
                UpdateCharacterExpResponse response = JsonConvert.DeserializeObject<UpdateCharacterExpResponse>(putRequest.downloadHandler.text);
                if (characterAttributesLookup.TryGetValue(characterId, out CharacterStats stats))
                {
                    stats.level = response.currentLv;
                    stats.expToNextLv = response.expToNextLv;
                    stats.nextStatPointRwd = response.nextStatPointRwd;
                    characterAttributesLookup[characterId] = stats;
                }
            }

            putRequest.Dispose();
        }

        private struct UpdateCharacterExpPutPayload
        {
            public string charId;
            public float charExp;
        }

        private struct UpdateCharacterExpResponse
        {
            public string status;
            public float expToNextLv;
            public int nextStatPointRwd;
            public int currentLv;
        }

        public IEnumerator UpdateCharacterAttributes(string characterId, CharacterAttributes newAttributes)
        {
            if (newAttributes.AreAnyValuesInvalid())
            {
                Debug.LogWarning("Attributes is invalid! " + characterId + "\n" + newAttributes.ToString());
                yield break;
            }

            UpdateCharacterAttributesPutPayload payload = new UpdateCharacterAttributesPutPayload()
            {
                charId = characterId,
                attributes = newAttributes
            };

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest putRequest = UnityWebRequest.Put(WebRequestManager.Singleton.GetAPIURL(false) + "characters/setCharAttribute", jsonData);
            putRequest.SetRequestHeader("Content-Type", "application/json");
            yield return putRequest.SendWebRequest();

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                putRequest = UnityWebRequest.Put(WebRequestManager.Singleton.GetAPIURL(false) + "characters/setCharAttribute", jsonData);
                putRequest.SetRequestHeader("Content-Type", "application/json");
                yield return putRequest.SendWebRequest();
            }

            if (putRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Put request error in WebRequestManager.UpdateCharacterAttributes()" + putRequest.error);
            }
            else
            {
                if (characterAttributesLookup.ContainsKey(characterId))
                {
                    characterAttributesLookup[characterId] = JsonConvert.DeserializeObject<CharacterStats>(putRequest.downloadHandler.text);
                }
                else
                {
                    characterAttributesLookup.Add(characterId, JsonConvert.DeserializeObject<CharacterStats>(putRequest.downloadHandler.text));
                }
            }

            putRequest.Dispose();

            yield return GetCharacterAttributes(characterId);
        }

        private struct UpdateCharacterAttributesPutPayload
        {
            public string charId;
            public CharacterAttributes attributes;
        }
    }
}