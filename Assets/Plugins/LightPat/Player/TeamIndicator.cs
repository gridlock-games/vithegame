using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightPat.Core;

namespace LightPat.Player
{
    public class TeamIndicator : MonoBehaviour
    {
        public Color glowColor = Color.red;
        public float glowAmount = 2.46f;

        private void Start()
        {
            
        }

        private void Update()
        {
            if (ClientManager.Singleton)
            {

            }
            else
            {

            }
        }
    }
}
