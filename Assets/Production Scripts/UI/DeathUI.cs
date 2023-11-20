using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vi.Core;

namespace Vi.UI
{
    public class DeathUI : MonoBehaviour
    {
        [SerializeField] private PlayerCard playerCard;

        public void Initialize(Attributes attributes)
        {
            playerCard.Initialize(attributes);
        }
    }
}