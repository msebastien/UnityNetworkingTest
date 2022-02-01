using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using TMPro;

public class PlayerHud : NetworkBehaviour
{
    private NetworkVariable<NetworkString> playerName = new NetworkVariable<NetworkString>();

    private bool overlaySet = false;
    
    public override void OnNetworkSpawn()
    {
        if(IsServer)
        {
            playerName.Value = $"Player {OwnerClientId}";
        }
    }

    public void SetOverlay()
    {
        var localPlayerOverlay = GetComponentInChildren<TextMeshProUGUI>();
        localPlayerOverlay.text = playerName.Value;
    }

    private void Update()
    {
        if(!overlaySet && !string.IsNullOrEmpty(playerName.Value))
        {
            SetOverlay();
            overlaySet = true;
        }
    }
}


