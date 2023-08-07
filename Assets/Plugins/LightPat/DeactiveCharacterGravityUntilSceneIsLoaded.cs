using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameCreator.Characters;

namespace LightPat.Core
{
    [RequireComponent(typeof(Character))]
    public class DeactiveCharacterGravityUntilSceneIsLoaded : MonoBehaviour
    {
        public string sceneName;

        private Scene scene;
        private int sceneIndex;
        private Character character;

        private void Start()
        {
            sceneIndex = SceneUtility.GetBuildIndexByScenePath(sceneName);
            if (sceneIndex == -1) { Debug.LogError("Scene not in build settings"); return; }

            scene = SceneManager.GetSceneByBuildIndex(sceneIndex);
            character = GetComponent<Character>();
            character.characterLocomotion.UseGravity(false);
        }

        private void Update()
        {
            if (sceneIndex == -1) { Debug.LogError("Scene not in build settings"); return; }

            if (scene.isLoaded)
            {
                character.characterLocomotion.UseGravity(true);
                Destroy(this);
            }
        }
    }
}