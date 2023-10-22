using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;

namespace Vi.UI
{
    public class PlayerUI : MonoBehaviour
    {
        [SerializeField] private PlayerCard playerCard;

        private void Start()
        {
            playerCard.Initialize(GetComponentInParent<Attributes>());
        }
    }
}