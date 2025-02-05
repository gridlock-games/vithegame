using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Vi.Utility;

namespace Vi.UI
{
    [RequireComponent(typeof(Image))]
    public class ImageSequencePlayer : MonoBehaviour
    {
        private ImageSequence imageSequence;
        public void ChangeImageSequence(ImageSequence imageSequence)
        {
            this.imageSequence = imageSequence;
            spriteCounter = -1;
            timer = 0;

            UpdateDisplay();
        }

        private Image image;
        private void Awake()
        {
            image = GetComponent<Image>();
        }

        private void OnEnable()
        {
            imageSequence = null;
            spriteCounter = 0;
            timer = 0;

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (imageSequence == null)
            {
                image.sprite = null;
                timer = 0;
            }
            else
            {
                spriteCounter++;
                image.sprite = imageSequence[spriteCounter % imageSequence.Length];
            }
        }

        private int spriteCounter = -1;
        private float timer;
        private void Update()
        {
            if (imageSequence == null)
            {
                timer = 0;
                spriteCounter = -1;
                return;
            }

            timer += Time.deltaTime;
            if (timer >= (1f / imageSequence.FrameRate))
            {
                UpdateDisplay();
                timer -= 1f / imageSequence.FrameRate;
            }
        }
    }
}