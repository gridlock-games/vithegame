using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Vi.UI
{
    public class ImageSequencePlayer : MonoBehaviour
    {
        [SerializeField] private AssetReference imageSequence;
    }
}