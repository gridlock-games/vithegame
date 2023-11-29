using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using Vi.Core;

namespace Vi.UI
{
    public class CharacterSelectElement : MonoBehaviour
    {
        [SerializeField] private Image characterIconImage;
        [SerializeField] private Button button;

        private CharacterSelectMenu characterSelectMenu;
        private CharacterSelectUI characterSelectUI;
        private int characterIndex;
        private int skinIndex;
        public void Initialize(CharacterSelectMenu characterSelectMenu, Sprite characterImage, int characterIndex, int skinIndex)
        {
            this.characterSelectMenu = characterSelectMenu;
            characterIconImage.sprite = characterImage;
            this.characterIndex = characterIndex;
            this.skinIndex = skinIndex;
        }

        public void Initialize(CharacterSelectUI characterSelectUI, Sprite characterImage, int characterIndex, int skinIndex)
        {
            this.characterSelectUI = characterSelectUI;
            characterIconImage.sprite = characterImage;
            this.characterIndex = characterIndex;
            this.skinIndex = skinIndex;
        }

        public void ChangeCharacter()
        {
            KeyValuePair<int, Attributes> localPlayerKvp = PlayerDataManager.Singleton.GetLocalPlayer();
            if (PlayerDataManager.Singleton.ContainsId(localPlayerKvp.Key))
            {
                localPlayerKvp.Value.GetComponent<AnimationHandler>().SetCharacter(characterIndex, skinIndex);
            }
            else
            {
                string payload = System.Text.Encoding.ASCII.GetString(NetworkManager.Singleton.NetworkConfig.ConnectionData);
                string[] payloadOptions = payload.Split(PlayerDataManager.payloadParseString);

                string playerName = "Player Name";

                if (payloadOptions.Length > 0) { playerName = payloadOptions[0]; }

                NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(playerName + PlayerDataManager.payloadParseString + characterIndex + PlayerDataManager.payloadParseString + skinIndex);
            }

            if (characterSelectMenu) { characterSelectMenu.ResetSkinIndex(); }
            if (characterSelectUI) { characterSelectUI.UpdateCharacterPreview(characterIndex, skinIndex); }
        }

        private void Update()
        {
            KeyValuePair<int, Attributes> localPlayerKvp = PlayerDataManager.Singleton.GetLocalPlayer();
            if (PlayerDataManager.Singleton.ContainsId(localPlayerKvp.Key))
            {
                button.interactable = PlayerDataManager.Singleton.GetPlayerData(localPlayerKvp.Key).characterIndex != characterIndex;
            }
            else
            {
                string payload = System.Text.Encoding.ASCII.GetString(NetworkManager.Singleton.NetworkConfig.ConnectionData);
                string[] payloadOptions = payload.Split(PlayerDataManager.payloadParseString);

                int characterIndex = 0;

                if (payloadOptions.Length > 1) { int.TryParse(payloadOptions[1], out characterIndex); }

                button.interactable = this.characterIndex != characterIndex;
            }
        }
    }
}