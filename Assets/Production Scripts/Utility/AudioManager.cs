using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Vi.Utility
{
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private GameObject audioSourcePrefab;
        [SerializeField] private MusicClip[] musicClips;

        [System.Serializable]
        private struct MusicClip
        {
            public string sceneNameToPlay;
            public AudioClip song;
        }

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

        /// <summary>
        /// Plays a clip in 3D space, pass null for the first parameter if you want this to play completely before destruction
        /// </summary>
        /// <param name="objectToDestroyWith"></param>
        /// <param name="audioClip"></param>
        /// <param name="position"></param>
        /// <param name="volume"></param>
        public void PlayClipAtPoint(GameObject objectToDestroyWith, AudioClip audioClip, Vector3 position, float volume = 1)
        {
            GameObject g = ObjectPoolingManager.SpawnObject(audioSourcePrefab, position, Quaternion.identity);
            if (objectToDestroyWith)
                StartCoroutine(Play3DSoundPrefabWithInvoker(objectToDestroyWith, g.GetComponent<AudioSource>(), audioClip, volume));
            else
                StartCoroutine(Play3DSoundPrefab(g.GetComponent<AudioSource>(), audioClip, volume));
        }

        private IEnumerator Play3DSoundPrefabWithInvoker(GameObject invoker, AudioSource audioSource, AudioClip audioClip, float volume = 1)
        {
            RegisterAudioSource(audioSource);
            audioSource.PlayOneShot(audioClip, volume);
            yield return new WaitUntil(() => !audioSource.isPlaying | !invoker);
            ObjectPoolingManager.ReturnObjectToPool(audioSource.gameObject);
        }

        private IEnumerator Play3DSoundPrefab(AudioSource audioSource, AudioClip audioClip, float volume = 1)
        {
            RegisterAudioSource(audioSource);
            audioSource.PlayOneShot(audioClip, volume);
            yield return new WaitUntil(() => !audioSource.isPlaying);
            ObjectPoolingManager.ReturnObjectToPool(audioSource.gameObject);
        }

        /// <summary>
        /// Plays an audio clip in 3D sound space while following a transform's position
        /// </summary>
        public void PlayClipOnTransform(Transform transformToFollow, AudioClip audioClip, float volume = 1)
        {
            GameObject g = ObjectPoolingManager.SpawnObject(audioSourcePrefab, transformToFollow);
            StartCoroutine(Play3DSoundPrefabOnTransform(g.GetComponent<AudioSource>(), audioClip, volume));
        }

        private IEnumerator Play3DSoundPrefabOnTransform(AudioSource audioSource, AudioClip audioClip, float volume = 1)
        {
            RegisterAudioSource(audioSource);
            audioSource.PlayOneShot(audioClip, volume);
            yield return new WaitUntil(() => !audioSource.isPlaying);
            ObjectPoolingManager.ReturnObjectToPool(audioSource.gameObject);
        }

        public void Play2DClip(AudioClip audioClip, float volume = 1)
        {
            GameObject g = ObjectPoolingManager.SpawnObject(audioSourcePrefab);
            StartCoroutine(Play2DSoundPrefab(g.GetComponent<AudioSource>(), audioClip, volume));
        }

        private IEnumerator Play2DSoundPrefab(AudioSource audioSource, AudioClip audioClip, float volume = 1)
        {
            audioSource.spatialBlend = 0;
            audioSource.PlayOneShot(audioClip, volume);
            yield return new WaitUntil(() => !audioSource.isPlaying);
            ObjectPoolingManager.ReturnObjectToPool(audioSource.gameObject);
        }

        private void Awake()
        {
            _singleton = this;
        }

        private AudioSource musicSource;
        private void Start()
        {
            foreach (AudioSource audioSouce in FindObjectsOfType<AudioSource>())
            {
                RegisterAudioSource(audioSouce);
            }

            // This is for music
            if (TryGetComponent(out musicSource))
            {
                musicSource.volume = FasterPlayerPrefs.Singleton.GetFloat("MusicVolume");
                musicSource.spatialBlend = 0;
            }
        }

        private const float musicFadeTime = 0.5f;

        private void Update()
        {
            audioSources.RemoveAll(item => item == null);
            foreach (AudioSource audioSource in audioSources)
            {
                audioSource.pitch = Time.timeScale;
            }

            if (musicSource)
            {
                if (!musicSource.isPlaying) { musicSource.Play(); }
                musicSource.volume = Mathf.MoveTowards(musicSource.volume, NetworkManager.Singleton.IsConnectedClient ? 0 : FasterPlayerPrefs.Singleton.GetFloat("MusicVolume"), Time.deltaTime * musicFadeTime);
            }
        }
    }
}