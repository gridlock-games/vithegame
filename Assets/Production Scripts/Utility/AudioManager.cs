using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        private void RegisterAudioSource(AudioSource audioSource)
        {
            audioSource.spatialBlend = 1;
            audioSource.minDistance = 5;
            if (!registeredAudioSources.Contains(audioSource)) { registeredAudioSources.Add(audioSource); }
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
            audioSource.volume = volume;
            audioSource.clip = audioClip;
            audioSource.Play();
            yield return new WaitUntil(() => !audioSource.isPlaying | !invoker);
            ObjectPoolingManager.ReturnObjectToPool(audioSource.gameObject);
        }

        private IEnumerator Play3DSoundPrefab(AudioSource audioSource, AudioClip audioClip, float volume = 1)
        {
            RegisterAudioSource(audioSource);
            audioSource.volume = volume;
            audioSource.clip = audioClip;
            audioSource.Play();
            yield return new WaitUntil(() => !audioSource.isPlaying);
            ObjectPoolingManager.ReturnObjectToPool(audioSource.gameObject);
        }

        /// <summary>
        /// Plays an audio clip in 3D sound space while following a transform's position
        /// </summary>
        public void PlayClipOnTransform(Transform transformToFollow, AudioClip audioClip, bool shouldLoop, float volume = 1)
        {
            GameObject g = ObjectPoolingManager.SpawnObject(audioSourcePrefab, transformToFollow.position, transformToFollow.rotation);
            StartCoroutine(Play3DSoundPrefabOnTransform(transformToFollow, g.GetComponent<AudioSource>(), audioClip, shouldLoop, volume));
        }

        private List<(AudioSource, Transform)> audioSourcesFollowingTransforms = new List<(AudioSource, Transform)>();
        private IEnumerator Play3DSoundPrefabOnTransform(Transform transformToFollow, AudioSource audioSource, AudioClip audioClip, bool shouldLoop, float volume = 1)
        {
            RegisterAudioSource(audioSource);
            audioSourcesFollowingTransforms.Add((audioSource, transformToFollow));
            if (shouldLoop)
            {
                while (true)
                {
                    audioSource.volume = volume;
                    audioSource.clip = audioClip;
                    audioSource.Play();
                    while (true)
                    {
                        yield return null;
                        if (!audioSource.isPlaying) { break; }
                        if (!transformToFollow) { break; }
                        if (!transformToFollow.gameObject.activeInHierarchy) { break; }
                    }
                }
            }
            else
            {
                audioSource.volume = volume;
                audioSource.clip = audioClip;
                audioSource.Play();
                while (true)
                {
                    yield return null;
                    if (!audioSource.isPlaying) { break; }
                    if (!transformToFollow) { break; }
                    if (!transformToFollow.gameObject.activeInHierarchy) { break; }
                }
            }
            audioSourcesFollowingTransforms.Remove((audioSource, transformToFollow));

            // Fade audio clip out
            while (true)
            {
                if (!audioSource.isPlaying) { break; }
                audioSource.volume = Mathf.MoveTowards(audioSource.volume, 0, Time.deltaTime * audioSourceFadeOutSpeed);
                if (Mathf.Approximately(0, audioSource.volume)) { break; }
                yield return null;
            }

            ObjectPoolingManager.ReturnObjectToPool(audioSource.gameObject);
        }

        private const float audioSourceFadeOutSpeed = 1.5f;

        public void Play2DClip(AudioClip audioClip, float volume = 1)
        {
            GameObject g = ObjectPoolingManager.SpawnObject(audioSourcePrefab);
            StartCoroutine(Play2DSoundPrefab(g.GetComponent<AudioSource>(), audioClip, volume));
        }

        private IEnumerator Play2DSoundPrefab(AudioSource audioSource, AudioClip audioClip, float volume = 1)
        {
            audioSource.spatialBlend = 0;
            audioSource.volume = volume;
            audioSource.clip = audioClip;
            audioSource.Play();
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

        private void LateUpdate()
        {
            foreach ((AudioSource audioSource, Transform transformToFollow) in audioSourcesFollowingTransforms)
            {
                if (!audioSource) { Debug.LogError("There is a null audio source in the audio source follow list"); continue; }
                if (!transformToFollow) { Debug.LogError("There is a null transform in the audio source follow list"); continue; }
                audioSource.transform.position = transformToFollow.position;
                audioSource.transform.rotation = transformToFollow.rotation;
            }
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
            if (System.Array.Exists(musicClips, item => item.sceneNamesToPlay.Contains(scene.name)))
            {
                RefreshMusicClip();
            }
        }

        private void OnSceneUnload(Scene scene)
        {
            if (System.Array.Exists(musicClips, item => item.sceneNamesToPlay.Contains(scene.name)))
            {
                RefreshMusicClip();
            }
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