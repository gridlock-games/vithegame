using UnityEngine;
using System.Collections;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System.Collections.Generic;
using Unity.Netcode;

namespace Vi.Core
{
    public class ItemShopManager : MonoBehaviour
    {
        public int ViEssenceCount { get; private set; }

        public IEnumerator GetViEssenceOfUser(string userId, System.Action<int> setMyInt)
        {
            UnityWebRequest getRequest = UnityWebRequest.Get(WebRequestManager.Singleton.GetAPIURL(false) + "auth/users/getEssenceForUser/" + userId);
            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in GetViEssenceOfUser() " + getRequest.error);
                getRequest.Dispose();
                ViEssenceCount = 0;
                if (setMyInt != null) { setMyInt(0); }
                yield break;
            }
            else if (int.TryParse(getRequest.downloadHandler.text, out int result))
            {
                ViEssenceCount = result;
                if (setMyInt != null) { setMyInt(ViEssenceCount); }
            }
            else
            {
                Debug.LogWarning("Unable to parse vi essence count from return body: " + getRequest.downloadHandler.text);
                ViEssenceCount = 0;
                if (setMyInt != null) { setMyInt(0); }
            }

            getRequest.Dispose();
        }

        public IEnumerator PurchaseItems(string charId, List<PurchaseItem> itemsToPurchase, System.Action<bool> purchaseSuccessfulCallback)
        {
            if (itemsToPurchase == null) { Debug.LogWarning("Items to purchase is null!"); yield break; }
            if (itemsToPurchase.Count == 0) { Debug.LogWarning("Items to purchase count is 0!"); yield break; }

            PurchaseItemsPostPayload payload = new PurchaseItemsPostPayload(charId, itemsToPurchase);

            string json = JsonConvert.SerializeObject(payload);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(WebRequestManager.Singleton.GetAPIURL(false) + "characters/purchaseItems", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            // Send again to get around unity bug
            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                postRequest = new UnityWebRequest(WebRequestManager.Singleton.GetAPIURL(false) + "characters/purchaseItems", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();
            }

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in PurchaseItems()" + postRequest.error);
                purchaseSuccessfulCallback(false);
            }
            else if (bool.TryParse(postRequest.downloadHandler.text, out bool purchaseSuccessful))
            {
                purchaseSuccessfulCallback(purchaseSuccessful);
            }
            else
            {
                Debug.LogWarning("Unable to parse boolean from purchase items request body " + postRequest.downloadHandler.text);
                purchaseSuccessfulCallback(false);
            }

            postRequest.Dispose();
        }

        private struct PurchaseItemsPostPayload
        {
            public string charId;
            public List<PurchaseItem> items;

            public PurchaseItemsPostPayload(string charId, List<PurchaseItem> items)
            {
                this.charId = charId;
                this.items = items;
            }
        }

        public struct PurchaseItem : INetworkSerializable
        {
            public string itemId;
            public int qty;
            public int cost;

            public PurchaseItem(string itemId, int qty, int cost)
            {
                this.itemId = itemId;
                this.qty = qty;
                this.cost = cost;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref itemId);
                serializer.SerializeValue(ref qty);
                serializer.SerializeValue(ref cost);
            }
        }

        public List<ShopItem> ShopItems { get; private set; } = new List<ShopItem>();

        public IEnumerator GetShopItems()
        {
            UnityWebRequest getRequest = UnityWebRequest.Get(WebRequestManager.Singleton.GetAPIURL(false) + "items/getShopItems");
            yield return getRequest.SendWebRequest();

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Get Request Error in GetShopItems() " + getRequest.error);
                getRequest.Dispose();
                yield break;
            }

            ShopItems = JsonConvert.DeserializeObject<List<ShopItem>>(getRequest.downloadHandler.text);
            ShopItems = ShopItems.FindAll(item => item.enabled);
            getRequest.Dispose();
        }

        public struct ShopItem
        {
            public string _id;
            public string itemId;
            public int cost;
            public int qty;
            public bool enabled;
            public int __v;
        }

#if UNITY_EDITOR
        public IEnumerator InsertItemToStore(string itemId, int cost, int quantity, bool enabled)
        {
            InsertItemToStorePostPayload payload = new InsertItemToStorePostPayload(itemId, cost, quantity, enabled);

            string json = JsonConvert.SerializeObject(payload);
            json = "[" + json + "]";
            Debug.Log(json);
            byte[] jsonData = System.Text.Encoding.UTF8.GetBytes(json);

            UnityWebRequest postRequest = new UnityWebRequest(WebRequestManager.Singleton.GetAPIURL(false) + "items/insertToStore", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
            postRequest.SetRequestHeader("Content-Type", "application/json");
            yield return postRequest.SendWebRequest();

            // Send again to get around unity bug
            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                postRequest = new UnityWebRequest(WebRequestManager.Singleton.GetAPIURL(false) + "items/insertToStore", UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(jsonData));
                postRequest.SetRequestHeader("Content-Type", "application/json");
                yield return postRequest.SendWebRequest();
            }

            if (postRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Post request error in InsertItemToStore()" + postRequest.error);
            }
            else if (bool.TryParse(postRequest.downloadHandler.text, out bool insertSuccess))
            {
                Debug.Log("Insert Success: " + insertSuccess);
            }

            postRequest.Dispose();
        }

        private struct InsertItemToStorePostPayload
        {
            public string itemId;
            public int cost;
            public int qty;
            public bool enabled;

            public InsertItemToStorePostPayload(string itemId, int cost, int qty, bool enabled)
            {
                this.itemId = itemId;
                this.cost = cost;
                this.qty = qty;
                this.enabled = enabled;
            }
        }
#endif
    }
}