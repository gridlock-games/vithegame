using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using LightPat.Core;

namespace LightPat.UI
{
    public class Scoreboard : MonoBehaviour
    {
        [SerializeField] private Transform dataParent;
        [SerializeField] private GameObject dataPrefab;
        [SerializeField] private int dataPrefabSpacing = 20;

        private void Update()
        {
            foreach (Transform child in dataParent)
            {
                Destroy(child.gameObject);
            }

            int i = 0;
            foreach (KeyValuePair<ulong, ClientData> clientData in ClientManager.Singleton.GetClientDataDictionary())
            {
                GameObject dataInstance = Instantiate(dataPrefab, dataParent);
                TextMeshProUGUI playerName = dataInstance.transform.Find("Player Name").GetComponent<TextMeshProUGUI>();
                playerName.SetText(clientData.Value.clientName);

                Color color = Color.black;
                if (clientData.Value.team == Team.Red)
                {
                    color = Color.red;
                }
                else if (clientData.Value.team == Team.Blue)
                {
                    color = Color.blue;
                }

                playerName.color = color;
                dataInstance.transform.Find("Kills").GetComponent<TextMeshProUGUI>().SetText(clientData.Value.kills.ToString());
                dataInstance.transform.Find("Deaths").GetComponent<TextMeshProUGUI>().SetText(clientData.Value.deaths.ToString());
                dataInstance.transform.Find("Damage Dealt").GetComponent<TextMeshProUGUI>().SetText(clientData.Value.damageDealt.ToString());
                dataInstance.transform.localPosition -= new Vector3(0, dataPrefabSpacing * i, 0);
                i++;
            }
        }
    }
}