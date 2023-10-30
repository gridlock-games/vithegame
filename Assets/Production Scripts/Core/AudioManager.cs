using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vi.Core
{
    public class AudioManager : MonoBehaviour
    {
        public GameObject audioSourcePrefab;
        public float initialVolume = 1;

        private static List<AudioSource> audioSources = new List<AudioSource>();
        private static AudioManager _singleton;

        public static AudioManager Singleton
        {
            get
            {
                if (_singleton == null)
                {
                    Debug.Log("Audio Manager is Null");
                }

                return _singleton;
            }
        }

        public void RegisterAudioSource(AudioSource audioSource)
        {
            audioSource.spatialBlend = 1;
            audioSource.minDistance = 5;
            audioSources.Add(audioSource);
        }

        public void PlayClipAtPoint(AudioClip audioClip, Vector3 position, float volume = 1)
        {
            GameObject g = Instantiate(audioSourcePrefab, position, Quaternion.identity);
            StartCoroutine(Play3DSoundPrefab(g.GetComponent<AudioSource>(), audioClip, volume));
        }

        private IEnumerator Play3DSoundPrefab(AudioSource audioSouce, AudioClip audioClip, float volume = 1)
        {
            RegisterAudioSource(audioSouce);
            audioSouce.PlayOneShot(audioClip, volume);
            yield return new WaitUntil(() => !audioSouce.isPlaying);
            Destroy(audioSouce.gameObject);
        }

        public void Play2DClip(AudioClip audioClip, float volume = 1)
        {
            GameObject g = Instantiate(audioSourcePrefab);
            StartCoroutine(Play2DSoundPrefab(g.GetComponent<AudioSource>(), audioClip, volume));
        }

        private IEnumerator Play2DSoundPrefab(AudioSource audioSouce, AudioClip audioClip, float volume = 1)
        {
            audioSouce.spatialBlend = 0;
            audioSouce.PlayOneShot(audioClip, volume);
            yield return new WaitUntil(() => !audioSouce.isPlaying);
            Destroy(audioSouce.gameObject);
        }

        private void Awake()
        {
            _singleton = this;
        }

        private void Start()
        {
            foreach (AudioSource audioSouce in FindObjectsOfType<AudioSource>())
            {
                RegisterAudioSource(audioSouce);
            }
            AudioListener.volume = initialVolume;

            // This is for music
            if (TryGetComponent(out AudioSource audioSource))
                audioSource.spatialBlend = 0;
        }

        private void Update()
        {
            audioSources.RemoveAll(item => item == null);
            foreach (AudioSource audioSource in audioSources)
            {
                audioSource.pitch = Time.timeScale;
            }
        }
    }
}