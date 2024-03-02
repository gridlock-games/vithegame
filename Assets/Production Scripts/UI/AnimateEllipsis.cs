using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vi.UI
{
    [RequireComponent(typeof(Text))]
    public class AnimateEllipsis : MonoBehaviour
    {
        private Text text;

        private void Start()
        {
            text = GetComponent<Text>();
        }

        private float lastTextChangeTime;
        private void Update()
        {
            if (text.text == "") { return; }

            if (Time.time - lastTextChangeTime > 0.5f)
            {
                lastTextChangeTime = Time.time;
                switch (text.text.Split(".").Length)
                {
                    case 1:
                        text.text = text.text.Replace(".", "") + ".";
                        break;
                    case 2:
                        text.text = text.text.Replace(".", "") + "..";
                        break;
                    case 3:
                        text.text = text.text.Replace(".", "") + "...";
                        break;
                    case 4:
                        text.text = text.text.Replace(".", "");
                        break;
                }
            }
        }
    }
}