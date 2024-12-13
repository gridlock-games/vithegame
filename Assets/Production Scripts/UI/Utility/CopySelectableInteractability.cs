using UnityEngine;
using UnityEngine.UI;

namespace Vi.UI
{
    [RequireComponent(typeof(Selectable))]
    public class CopySelectableInteractability : MonoBehaviour
    {
        [SerializeField] private Selectable selectableToCopy;

        private Selectable selectable;
        private void Awake()
        {
            selectable = GetComponent<Selectable>();
        }

        private void Update()
        {
            selectable.interactable = selectableToCopy.interactable;
        }
    }
}