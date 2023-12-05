using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Core;
using UnityEngine.UI;
using Unity.Netcode;

namespace Vi.UI
{
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private CharacterSelectElement characterSelectElement;
        [SerializeField] private Transform characterSelectGridParent;
        [SerializeField] private Text characterNameText;
        [SerializeField] private Text characterRoleText;
        [SerializeField] private Vector3 previewCharacterPosition;
        [SerializeField] private Vector3 previewCharacterRotation;

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

                    Vector3 pos = new Vector3(x * size - size, y * size, 0);
                    GameObject g = Instantiate(characterSelectElement.gameObject, characterSelectGridParent);
                    g.transform.localPosition = rotation * pos;
                    g.GetComponent<CharacterSelectElement>().Initialize(this, playerModelOptions[characterIndex].characterImage, characterIndex, 0);
                    characterIndex++;
                }
            }
        }

        private void Start()
        {
            UpdateCharacterPreview(0, 0);
        }

        private GameObject previewObject;
        public void UpdateCharacterPreview(int characterIndex, int skinIndex)
        {
            if (previewObject) { Destroy(previewObject); }

            CharacterReference.PlayerModelOption playerModelOption = PlayerDataManager.Singleton.GetCharacterReference().GetPlayerModelOptions()[characterIndex];
            previewObject = Instantiate(playerModelOption.playerPrefab, previewCharacterPosition, Quaternion.Euler(previewCharacterRotation));
            previewObject.GetComponent<AnimationHandler>().SetCharacter(characterIndex, skinIndex);
            characterNameText.text = playerModelOption.name;
            characterRoleText.text = playerModelOption.role;
        }

        public void LockCharacter()
        {
            foreach (Transform child in characterSelectGridParent)
            {
                if (child.TryGetComponent(out CharacterSelectElement characterSelectElement))
                {
                    characterSelectElement.SetButtonInteractability(false);
                }
            }
        }
    }
}