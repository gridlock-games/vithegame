using UnityEngine;

namespace Vi.UI
{
    [CreateAssetMenu(fileName = "ImageSequence", menuName = "Production/Image Sequence")]
    public class ImageSequence : ScriptableObject
    {
        [SerializeField] private Sprite[] sprites;
    }
}
