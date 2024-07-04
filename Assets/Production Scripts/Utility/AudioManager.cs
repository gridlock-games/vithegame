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
        private void RegisterAudioSourceToBeAffectedByTimescale(AudioSource audioSource)
        {
            if (!registeredAudioSources.Contains(audioSource)) { registeredAudioSources.Add(audioSource); }
        }

        private const float defaultVolume = 1;
        private const float defaultPitch = 1;
        private const float defaultPanning = 0;
        private const float defaultSpatialBlend = 1;
        private const float defaultMinDistance = 5;
        private const float defaultMaxDistance = 10;

        private void ResetAudioSourceProperties(AudioSource audioSource)
        {
            audioSource.volume = defaultVolume;
            audioSource.pitch = defaultPitch;
            audioSource.panStereo = defaultPanning;
            audioSource.spatialBlend = defaultSpatialBlend;
            audioSource.minDistance = defaultMinDistance;
            audioSource.maxDistance = defaultMaxDistance;
        }

        /// <summary>
        /// Plays a clip in 3D space, pass null for the first parameter if you want this to play completely before destruction
        /// </summary>
        /// <param name="objectToDestroyWith"></param>
        /// <param name="audioClip"></param>
        /// <param name="position"></param>
        /// <param name="volume"></param>
        public AudioSource PlayClipAtPoint(GameObject objectToDestroyWith, AudioClip audioClip, Vector3 position, float volume)
        {
            AudioSource audioSource = ObjectPoolingManager.SpawnObject(audioSourcePrefab, position, Quaternion.identity).GetComponent<AudioSource>();
            ResetAudioSourceProperties(audioSource);
            RegisterAudioSourceToBeAffectedByTimescale(audioSource);
            audioSource.volume = volume;
            audioSource.clip = audioClip;

            if (objectToDestroyWith)
                StartCoroutine(Play3DSoundPrefabWithInvoker(objectToDestroyWith, audioSource, audioClip, volume));
            else
                StartCoroutine(Play3DSoundPrefab(audioSource, audioClip, volume));

            return audioSource;
        }

        private IEnumerator Play3DSoundPrefabWithInvoker(GameObject invoker, AudioSource audioSource, AudioClip audioClip, float volume)
        {
            audioSource.Play();
            while (true)
            {
                if (!invoker) { break; }
                if (!audioSource) { break; }
                if (!audioSource.isPlaying) { break; }
                yield return null;
            }
            if (audioSource) { ObjectPoolingManager.ReturnObjectToPool(audioSource.gameObject); }
        }

        private IEnumerator Play3DSoundPrefab(AudioSource audioSource, AudioClip audioClip, float volume)
        {
            audioSource.Play();
            while (true)
            {
                if (!audioSource) { break; }
                if (!audioSource.isPlaying) { break; }
                yield return null;
            }
            if (audioSource) { ObjectPoolingManager.ReturnObjectToPool(audioSource.gameObject); }
        }

        /// <summary>
        /// Plays an audio clip in 3D sound space while following a transform's position
        /// </summary>
        public AudioSource PlayClipOnTransform(Transform transformToFollow, AudioClip audioClip, bool shouldLoop, float volume)
        {
            AudioSource audioSource = ObjectPoolingManager.SpawnObject(audioSourcePrefab, transformToFollow.position, transformToFollow.rotation).GetComponent<AudioSource>();
            ResetAudioSourceProperties(audioSource);
            RegisterAudioSourceToBeAffectedByTimescale(audioSource);
            audioSource.volume = volume;
            audioSource.clip = audioClip;

            StartCoroutine(Play3DSoundPrefabOnTransform(transformToFollow, audioSource, audioClip, shouldLoop, volume));
            return audioSource;
        }

        private List<(AudioSource, Transform)> audioSourcesFollowingTransforms = new List<(AudioSource, Transform)>();
        private IEnumerator Play3DSoundPrefabOnTransform(Transform transformToFollow, AudioSource audioSource, AudioClip audioClip, bool shouldLoop, float volume)
        {
            audioSourcesFollowingTransforms.Add((audioSource, transformToFollow));
            if (shouldLoop)
            {
                while (true)
                {
                    audioSource.Play();
                    while (true)
                    {
                        yield return null;
                        if (!audioSource) { break; }
                        if (!audioSource.isPlaying) { break; }
                        if (!transformToFollow) { break; }
                        if (!transformToFollow.gameObject.activeInHierarchy) { break; }
                    }
                }
            }
            else
            {
                audioSource.Play();
                while (true)
                {
                    yield return null;
                    if (!audioSource) { break; }
                    if (!audioSource.isPlaying) { break; }
                    if (!transformToFollow) { break; }
                    if (!transformToFollow.gameObject.activeInHierarchy) { break; }
                }
            }
            audioSourcesFollowingTransforms.Remove((audioSource, transformToFollow));

            // Fade audio clip out
            if (audioSource)
            {
                while (true)
                {
                    if (!audioSource) { break; }
                    if (!audioSource.isPlaying) { break; }
                    audioSource.volume = Mathf.MoveTowards(audioSource.volume, 0, Time.deltaTime * audioSourceFadeOutSpeed);
                    if (Mathf.Approximately(0, audioSource.volume)) { break; }
                    yield return null;
                }
            }

            if (audioSource) { ObjectPoolingManager.ReturnObjectToPool(audioSource.gameObject); }
        }

        private const float audioSourceFadeOutSpeed = 1.5f;

        public AudioSource Play2DClip(AudioClip audioClip, float volume)
        {
            AudioSource audioSource = ObjectPoolingManager.SpawnObject(audioSourcePrefab).GetComponent<AudioSource>();
            ResetAudioSourceProperties(audioSource);
            RegisterAudioSourceToBeAffectedByTimescale(audioSource);
            audioSource.spatialBlend = 0;
            audioSource.volume = volume;
            audioSource.clip = audioClip;

            StartCoroutine(Play2DSoundPrefab(audioSource));
            return audioSource;
        }

        private IEnumerator Play2DSoundPrefab(AudioSource audioSource)
        {
            audioSource.Play();
            while (true)
            {
                if (!audioSource) { break; }
                if (!audioSource.isPlaying) { break; }
                yield return null;
            }
            if (audioSource) { ObjectPoolingManager.ReturnObjectToPool(audioSource.gameObject); }
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
                RegisterAudioSourceToBeAffectedByTimescale(audioSource);
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
            List<int> indexesToRemove = new List<int>();
            for (int i = 0; i < audioSourcesFollowingTransforms.Count; i++)
            {
                (AudioSource audioSource, Transform transformToFollow) = audioSourcesFollowingTransforms[i];
                if (!audioSource)
                {
                    Debug.LogWarning("There is a null audio source in the audio source follow list");
                    indexesToRemove.Add(i);
                    continue;
                }
                if (!transformToFollow)
                {
                    Debug.LogWarning("There is a null transform in the audio source follow list");
                    indexesToRemove.Add(i);
                    continue;
                }
                audioSource.transform.position = transformToFollow.position;
                audioSource.transform.rotation = transformToFollow.rotation;
            }

            foreach (int index in indexesToRemove.OrderByDescending(item => item))
            {
                audioSourcesFollowingTransforms.RemoveAt(index);
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