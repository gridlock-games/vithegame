using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LightPat.Core
{
    public class CutsceneSkip : MonoBehaviour
    {
        [SerializeField] private string nextSceneName;
        [SerializeField] private KeyCode skipKey;
        [SerializeField] private AudioSource audioSourceToWaitFor;

        private void Update()
        {
            if (Input.GetKeyDown(skipKey))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else if (!audioSourceToWaitFor.isPlaying)
            {
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}