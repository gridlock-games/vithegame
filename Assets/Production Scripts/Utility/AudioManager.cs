using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Linq;

namespace Vi.Utility
{
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private GameObject audioSourcePrefab;
        [SerializeField] private MusicClip[] musicClips;

        [System.Serializable]
        private class MusicClip
        {
            public string[] sceneNamesToPlay;
            public AudioClip[] songs;
        }

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

        List<AudioSource> registeredAudioSources = new List<AudioSource>();
        public void RegisterAudioSource(AudioSource audioSource)
        {
            audioSource.spatialBlend = 1;
            audioSource.minDistance = 5;
            registeredAudioSources.Add(audioSource);
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
        public void PlayClipOnTransform(Transform transformToFollow, AudioClip audioClip, bool shouldLoop, float volume = 1)
        {
            GameObject g = ObjectPoolingManager.SpawnObject(audioSourcePrefab, transformToFollow);
            StartCoroutine(Play3DSoundPrefabOnTransform(g.GetComponent<AudioSource>(), audioClip, shouldLoop, volume));
        }

        private IEnumerator Play3DSoundPrefabOnTransform(AudioSource audioSource, AudioClip audioClip, bool shouldLoop, float volume = 1)
        {
            RegisterAudioSource(audioSource);
            if (shouldLoop)
            {
                while (true)
                {
                    if (!audioSource.isPlaying) { audioSource.PlayOneShot(audioClip, volume); }
                    yield return null;
                }
            }
            else
            {
                audioSource.PlayOneShot(audioClip, volume);
                yield return new WaitUntil(() => !audioSource.isPlaying);
            }
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
            SceneManager.sceneLoaded += OnSceneLoad;
            SceneManager.sceneUnloaded += OnSceneUnload;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoad;
            SceneManager.sceneUnloaded -= OnSceneUnload;
        }

        private AudioSource musicSource;
        private void Start()
        {
            RefreshStatus();

            // This is for music
            if (TryGetComponent(out musicSource))
            {
                musicSource.volume = musicVolume;
                musicSource.spatialBlend = 0;
            }

            foreach (AudioSource audioSource in FindObjectsOfType<AudioSource>())
            {
                if (audioSource == musicSource) { continue; }
                RegisterAudioSource(audioSource);
            }

            RefreshMusicClip();
        }

        private float musicVolume = 1;
        private void RefreshStatus()
        {
            musicVolume = FasterPlayerPrefs.Singleton.GetFloat("MusicVolume");
        }

        private const float musicFadeSpeed = 0.5f;

        private float lastTimeScale = 1;

        private void Update()
        {
            if (FasterPlayerPrefs.Singleton.PlayerPrefsWasUpdatedThisFrame) { RefreshStatus(); }

            if (Time.timeScale != lastTimeScale)
            {
                registeredAudioSources.RemoveAll(item => item == null);
                foreach (AudioSource audioSource in registeredAudioSources)
                {
                    audioSource.pitch = Time.timeScale;
                }
            }

            if (!musicSource.isPlaying & musicSource.clip) { musicSource.Play(); }

            if (currentMusicClip == null)
            {
                musicSource.volume = Mathf.MoveTowards(musicSource.volume, 0, Time.deltaTime * musicFadeSpeed);
            }
            else if (!isCrossfading)
            {
                musicSource.volume = Mathf.MoveTowards(musicSource.volume, musicVolume, Time.deltaTime * musicFadeSpeed);
            }

            lastTimeScale = Time.timeScale;
        }

        private bool isCrossfading;
        private Coroutine crossFadeCoroutine;
        private IEnumerator CrossFadeBetweenSongs()
        {
            isCrossfading = true;

            while (true)
            {
                musicSource.volume = Mathf.MoveTowards(musicSource.volume, 0, Time.deltaTime * musicFadeSpeed);
                if (musicSource.volume == 0) { break; }
                yield return null;
            }

            if (currentMusicClip == null) { isCrossfading = false; yield break; }
            musicSource.clip = currentMusicClip.songs.Length == 0 ? null : currentMusicClip.songs[Random.Range(0, currentMusicClip.songs.Length)];

            while (true)
            {
                musicSource.volume = Mathf.MoveTowards(musicSource.volume, musicVolume, Time.deltaTime * musicFadeSpeed);
                if (Mathf.Approximately(musicSource.volume, musicVolume)) { break; }
                yield return null;
            }

            isCrossfading = false;
        }

        private void OnSceneLoad(Scene scene, LoadSceneMode loadSceneMode)
        {
            RefreshMusicClip();
        }

        private void OnSceneUnload(Scene scene)
        {
            RefreshMusicClip();
        }

        private MusicClip currentMusicClip;
        private void RefreshMusicClip()
        {
            bool musicClipFound = false;
            foreach (MusicClip musicClip in musicClips)
            {
                foreach (string sceneName in musicClip.sceneNamesToPlay)
                {
                    Scene scene = SceneManager.GetSceneByName(sceneName);
                    if (!scene.IsValid()) { continue; }

                    if (scene.isLoaded)
                    {
                        musicClipFound = true;
                        currentMusicClip = musicClip;

                        // If the clip we are changing to is not the same as the previous clip, and there is already a clip assigned to the music source
                        int randomIndex = Random.Range(0, currentMusicClip.songs.Length);
                        if (musicSource.clip != (currentMusicClip.songs.Length == 0 ? null : currentMusicClip.songs[randomIndex]) & musicSource.clip)
                        {
                            if (crossFadeCoroutine != null) { StopCoroutine(crossFadeCoroutine); }
                            crossFadeCoroutine = StartCoroutine(CrossFadeBetweenSongs());
                        }
                        else
                        {
                            musicSource.clip = currentMusicClip.songs.Length == 0 ? null : currentMusicClip.songs[randomIndex];
                        }
                        break;
                    }
                }
            }

            if (!musicClipFound) { currentMusicClip = null; }
        }
    }
}