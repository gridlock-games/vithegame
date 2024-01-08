using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Vi.Core
{
    public abstract class NetworkInteractable : NetworkBehaviour
    {
        public abstract void Interact(GameObject invoker);
    }
}