using UnityEngine;
using UnityEngine.UI;

namespace Vi.UI
{
    public class ShowAfterPassword : MonoBehaviour
    {
        [SerializeField] private InputField passwordInput;
        [SerializeField] private GameObject passwordSectionParent;
        [SerializeField] private string password;
        [SerializeField] private GameObject objectToConstrain;

        private void OnEnable()
        {
            objectToConstrain.SetActive(false);
            passwordSectionParent.SetActive(true);
            passwordInput.text = "";

            if (Application.isEditor)
            {
                passwordInput.text = password;
            }
        }

        private void OnDisable()
        {
            objectToConstrain.SetActive(false);
            passwordSectionParent.SetActive(true);
            passwordInput.text = "";
        }

        public void OnPasswordChange()
        {
            if (passwordInput.text == password)
            {
                objectToConstrain.SetActive(true);
                passwordSectionParent.SetActive(false);
            }
        }
    }
}