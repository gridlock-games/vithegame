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
        private LobbyUI lobbyUI;
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

        public void Initialize(LobbyUI lobbyUI, Sprite characterImage, int characterIndex, int skinIndex)
        {
            this.lobbyUI = lobbyUI;
            characterIconImage.sprite = characterImage;
            this.characterIndex = characterIndex;
            this.skinIndex = skinIndex;
        }

        public void ChangeCharacter()
        {
            KeyValuePair<int, Attributes> localPlayerKvp = PlayerDataManager.Singleton.GetLocalPlayerObject();
            if (PlayerDataManager.Singleton.ContainsId(localPlayerKvp.Key))
            {
                localPlayerKvp.Value.GetComponent<AnimationHandler>().SetCharacter(characterIndex, skinIndex);
            }
            else
            {
                PlayerDataManager.ParsedConnectionData parsedConnectionData = PlayerDataManager.ParseConnectionData(NetworkManager.Singleton.NetworkConfig.ConnectionData);
                parsedConnectionData.characterIndex = characterIndex;
                parsedConnectionData.skinIndex = skinIndex;
                PlayerDataManager.SetConnectionData(parsedConnectionData);
            }

            if (characterSelectMenu) { characterSelectMenu.ResetSkinIndex(); }
            if (characterSelectUI) { characterSelectUI.UpdateCharacterPreview(characterIndex, skinIndex); }
            if (lobbyUI) { lobbyUI.UpdateCharacterPreview(characterIndex, skinIndex); }
        }

        bool isInteractable = true;
        public void SetButtonInteractability(bool isInteractable)
        {
            this.isInteractable = isInteractable;
        }

        private void Update()
        {
            if (!isInteractable) { button.interactable = false; return; }

            KeyValuePair<int, Attributes> localPlayerKvp = PlayerDataManager.Singleton.GetLocalPlayerObject();
            if (PlayerDataManager.Singleton.ContainsId(localPlayerKvp.Key))
            {
                button.interactable = PlayerDataManager.Singleton.GetPlayerData(localPlayerKvp.Key).characterIndex != characterIndex;
            }
            else
            {
                PlayerDataManager.ParsedConnectionData parsedConnectionData = PlayerDataManager.ParseConnectionData(NetworkManager.Singleton.NetworkConfig.ConnectionData);
                button.interactable = characterIndex != parsedConnectionData.characterIndex;
            }
        }
    }
}