using UnityEngine;
using Vi.ScriptableObjects;
using Vi.Utility;
using System.Collections;

namespace Vi.Core
{
    [RequireComponent(typeof(PooledObject))]
    public class StatusVFX : MonoBehaviour
    {
        [Header("Transform")]
        [SerializeField] private Vector3 positionOffset;
        [SerializeField] private Vector3 rotationOffset;
        [SerializeField] private Vector3 scale = new Vector3(1, 1, 1);
        [Header("Audio")]
        [SerializeField] protected AudioClip audioClipToPlayOnAwake;
        [SerializeField] protected float awakeAudioClipDelay;
        [SerializeField] protected float awakeAudioClipStartTime;

        private PooledObject pooledObject;
        private void Awake()
        {
            pooledObject = GetComponent<PooledObject>();
        }

        private void OnEnable()
        {
            if (pooledObject.IsPrewarmObject()) { return; }

            if (transform.parent == null) { Debug.LogWarning("Status VFX parent is null! " + this); }

            transform.localRotation = Quaternion.Euler(rotationOffset);
            transform.localPosition = transform.localRotation * positionOffset;
            transform.localScale = scale;

            if (audioClipToPlayOnAwake) { StartCoroutine(PlayAwakeAudioClip()); }
        }

        protected const float actionVFXSoundEffectVolume = 0.7f;

        private IEnumerator PlayAwakeAudioClip()
        {
            yield return new WaitForSeconds(awakeAudioClipDelay);
            AudioSource audioSource = AudioManager.Singleton.PlayClipOnTransform(transform, audioClipToPlayOnAwake, false, actionVFXSoundEffectVolume);
            if (audioSource)
            {
                audioSource.time = awakeAudioClipStartTime;
            }
        }
    }
}