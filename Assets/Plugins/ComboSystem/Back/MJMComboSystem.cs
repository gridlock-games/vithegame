namespace MJM
{
    using GameCreator.Melee;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using Unity.Netcode;

    [Serializable]
    public class ComboEvent : UnityEvent<int> { }

    public class MJMComboSystem : NetworkBehaviour
    {
        [SerializeField] private CharacterMelee _characterMelee;
        //UI
        private NetworkVariable<int> comboCount = new NetworkVariable<int>();
        public ComboEvent onCounterUpdate;

        public override void OnNetworkSpawn()
        {
            comboCount.OnValueChanged += OnComboCountChange;
        }

        public override void OnNetworkDespawn()
        {
            comboCount.OnValueChanged -= OnComboCountChange;
        }

        private void OnComboCountChange(int prev, int current)
        {
            onCounterUpdate.Invoke(current);

            if (IsOwner & current == 0)
            {
                GetComponentInChildren<ComboUIAnimator>().HideComboUI();
            }
        }

        public void AddCount(int value)
        {
            if (!IsServer) { return; }

            comboCount.Value += value;
        }

        public void ResetCount()
        {
            if (!IsServer) { return; }

            comboCount.Value = 0;
        }

        public int GetCount()
        {
            return comboCount.Value;
        }
    }
}
