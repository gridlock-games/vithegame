using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;
using UnityEngine.UI;
using TMPro;

namespace Vi.UI
{
    public class AudioSettingsMenu : Menu
    {
        [SerializeField] private Text errorText;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Dropdown speakerModeDropdown;
        [SerializeField] private TMP_Dropdown dspBufferSizeDropdown;
        [SerializeField] private TMP_Dropdown sampleRateDropdown;
        [SerializeField] private TMP_Dropdown numRealVoicesDropdown;
        [SerializeField] private TMP_Dropdown numVirtualVoicesDropdown;

        private const string errorMessage = "Failed To Apply Audio Settings";

        public void ChangeMasterVolume()
        {
            AudioListener.volume = volumeSlider.value;
            PersistentLocalObjects.Singleton.SetFloat("MasterVolume", AudioListener.volume);
        }

        public void ChangeSpeakerMode()
        {
            AudioConfiguration audioConfiguration = AudioSettings.GetConfiguration();
            audioConfiguration.speakerMode = validSpeakerModes[speakerModeDropdown.value];
            if (!AudioSettings.Reset(audioConfiguration)) { errorText.text = errorMessage; }
        }

        public void ChangeDSPBufferSize()
        {
            AudioConfiguration audioConfiguration = AudioSettings.GetConfiguration();
            audioConfiguration.dspBufferSize = validDSPBufferSizes[dspBufferSizeDropdown.value];
            if (!AudioSettings.Reset(audioConfiguration)) { errorText.text = errorMessage; }
        }

        public void ChangeSampleRate()
        {
            AudioConfiguration audioConfiguration = AudioSettings.GetConfiguration();
            audioConfiguration.sampleRate = validDSPBufferSizes[sampleRateDropdown.value];
            if (!AudioSettings.Reset(audioConfiguration)) { errorText.text = errorMessage; }
        }

        public void ChangeNumRealVoices()
        {
            AudioConfiguration audioConfiguration = AudioSettings.GetConfiguration();
            audioConfiguration.numRealVoices = validNumRealVoices[numRealVoicesDropdown.value];
            if (!AudioSettings.Reset(audioConfiguration)) { errorText.text = errorMessage; }
        }

        public void ChangeNumVirtualVoices()
        {
            AudioConfiguration audioConfiguration = AudioSettings.GetConfiguration();
            audioConfiguration.numVirtualVoices = validNumVirtualVoices[numVirtualVoicesDropdown.value];
            if (!AudioSettings.Reset(audioConfiguration)) { errorText.text = errorMessage; }
        }

        private void Start()
        {
            errorText.text = "";
            volumeSlider.value = PersistentLocalObjects.Singleton.GetFloat("MasterVolume");

            AudioConfiguration currentAudioConfiguration = AudioSettings.GetConfiguration();

            List<string> speakerModeOptions = new List<string>();
            int currentIndex = 0;
            for (int i = 0; i < validSpeakerModes.Length; i++)
            {
                AudioSpeakerMode audioSpeakerMode = validSpeakerModes[i];
                string speakerModeName = audioSpeakerMode.ToString();
                if (audioSpeakerMode == AudioSpeakerMode.Mode5point1)
                {
                    speakerModeName = "5.1";
                }
                else if (audioSpeakerMode == AudioSpeakerMode.Mode7point1)
                {
                    speakerModeName = "7.1";
                }
                speakerModeOptions.Add(speakerModeName);
                if (audioSpeakerMode == currentAudioConfiguration.speakerMode) { currentIndex = i; }
            }
            speakerModeDropdown.ClearOptions();
            speakerModeDropdown.AddOptions(speakerModeOptions);
            speakerModeDropdown.value = currentIndex;

            List<string> dspBufferSizeOptions = new List<string>();
            currentIndex = 0;
            for (int i = 0; i < validDSPBufferSizes.Length; i++)
            {
                dspBufferSizeOptions.Add(validDSPBufferSizes[i].ToString());
                if (validDSPBufferSizes[i] == currentAudioConfiguration.dspBufferSize) { currentIndex = i; }
            }
            dspBufferSizeDropdown.ClearOptions();
            dspBufferSizeDropdown.AddOptions(dspBufferSizeOptions);
            dspBufferSizeDropdown.value = currentIndex;

            List<string> sampleRateOptions = new List<string>();
            currentIndex = 0;
            for (int i = 0; i < validSampleRates.Length; i++)
            {
                sampleRateOptions.Add(validSampleRates[i].ToString());
                if (validSampleRates[i] == currentAudioConfiguration.dspBufferSize) { currentIndex = i; }
            }
            sampleRateDropdown.ClearOptions();
            sampleRateDropdown.AddOptions(sampleRateOptions);
            sampleRateDropdown.value = currentIndex;

            List<string> numRealVoicesOptions = new List<string>();
            currentIndex = 0;
            for (int i = 0; i < validNumRealVoices.Length; i++)
            {
                numRealVoicesOptions.Add(validNumRealVoices[i].ToString());
                if (validNumRealVoices[i] == currentAudioConfiguration.numRealVoices) { currentIndex = i; }
            }
            numRealVoicesDropdown.ClearOptions();
            numRealVoicesDropdown.AddOptions(sampleRateOptions);
            numRealVoicesDropdown.value = currentIndex;

            List<string> numVirtualVoicesOptions = new List<string>();
            currentIndex = 0;
            for (int i = 0; i < validNumVirtualVoices.Length; i++)
            {
                numVirtualVoicesOptions.Add(validNumVirtualVoices[i].ToString());
                if (validNumVirtualVoices[i] == currentAudioConfiguration.numVirtualVoices) { currentIndex = i; }
            }
            numVirtualVoicesDropdown.ClearOptions();
            numVirtualVoicesDropdown.AddOptions(sampleRateOptions);
            numVirtualVoicesDropdown.value = currentIndex;
        }

        static AudioSpeakerMode[] validSpeakerModes =
        {
            AudioSpeakerMode.Mono,
            AudioSpeakerMode.Stereo,
            AudioSpeakerMode.Quad,
            AudioSpeakerMode.Surround,
            AudioSpeakerMode.Mode5point1,
            AudioSpeakerMode.Mode7point1
        };

        static int[] validDSPBufferSizes =
        {
            32, 64, 128, 256, 340, 480, 512, 1024, 2048, 4096, 8192
        };

        static int[] validSampleRates =
        {
            11025, 22050, 44100, 48000, 88200, 96000,
        };

        static int[] validNumRealVoices =
        {
            1, 2, 4, 8, 16, 32, 50, 64, 100, 128, 256, 512,
        };

        static int[] validNumVirtualVoices =
        {
            1, 2, 4, 8, 16, 32, 50, 64, 100, 128, 256, 512,
        };
    }
}
