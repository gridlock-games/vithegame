namespace GameCreator.Core
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [AddComponentMenu("")]
    public class IgniterKeyDownTwice : Igniter
    {
        public KeyCode targetKey = KeyCode.Space;
        public float doublePressInterval = 0.3f;

        private float lastKeyPressTime = 0f;
        private bool keyPressedTwice = false;

#if UNITY_EDITOR
        public new static string NAME = "Input/On Key Down Twice";
#endif

        void Update()
        {
            if (Input.GetKeyDown(targetKey))
            {
                if (Time.time - lastKeyPressTime <= doublePressInterval)
                {
                    keyPressedTwice = true;
                }
                else
                {
                    keyPressedTwice = false;
                }
                lastKeyPressTime = Time.time;
            }

            if (keyPressedTwice && Input.GetMouseButtonDown(0))
            {
                this.ExecuteTrigger(gameObject);
                keyPressedTwice = false;
            }
        }
    }

}