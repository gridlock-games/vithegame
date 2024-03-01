using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vi.UI
{
    public class AlertBox : MonoBehaviour
    {
        [SerializeField] private Text alertText;
        
        public void SetText(string newText)
        {
            alertText.text = newText;
        }

        public void DestroyAlert()
        {
            Destroy(gameObject);
        }
    }
}