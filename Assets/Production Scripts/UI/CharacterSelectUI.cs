using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Vi.Core;
using Vi.ScriptableObjects;
using UnityEngine.UI;

namespace Vi.UI
{
    public class CharacterSelectUI : MonoBehaviour
    {
        [SerializeField] private CharacterSelectElement characterSelectElement;
        [SerializeField] private Transform characterSelectParent;
        [SerializeField] private Text characterNameText;
        [SerializeField] private Text characterRoleText;

        private readonly float size = 200;
        private readonly int height = 2;

        private void Awake()
        {
            CharacterReference.PlayerModelOption[] playerModelOptions = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions();

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
                    g.GetComponent<CharacterSelectElement>().Initialize(this, playerModelOptions[characterIndex].characterImage, characterIndex, 0);
                    characterIndex++;
                }
            }
        }

        public void StartClient()
        {
            NetworkManager.Singleton.StartClient();
        }

        private int skinIndex;

        public void UpdateCharacterPreview(int characterIndex, int skinIndex)
        {
            Debug.Log(characterIndex + " " + skinIndex);
        }

        public void ResetSkinIndex() { skinIndex = 0; }

        public void ChangeSkin()
        {
            string payload = System.Text.Encoding.ASCII.GetString(NetworkManager.Singleton.NetworkConfig.ConnectionData);
            string[] payloadOptions = payload.Split(PlayerDataManager.payloadParseString);

            string playerName = "Player Name";
            int characterIndex = 0;
            int skinIndex = 0;

            if (payloadOptions.Length > 0) { playerName = payloadOptions[0]; }
            if (payloadOptions.Length > 1) { int.TryParse(payloadOptions[1], out characterIndex); }
            if (payloadOptions.Length > 2) { int.TryParse(payloadOptions[2], out skinIndex); }

            skinIndex += 1;
            if (skinIndex > PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions()[characterIndex].skinOptions.Length - 1) { skinIndex = 0; }

            NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(playerName + PlayerDataManager.payloadParseString + characterIndex + PlayerDataManager.payloadParseString + skinIndex);

            UpdateCharacterPreview(characterIndex, skinIndex);
        }
    }
}