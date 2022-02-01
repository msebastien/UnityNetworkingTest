using MSebastien.Core.Singletons;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayersManager : NetworkSingleton<PlayersManager>
{
    private NetworkVariable<int> playersInGame = new NetworkVariable<int>();

    public int PlayersInGame
    {
        get
        {
            return playersInGame.Value;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
        {
            if(IsServer)
            {
                Logger.Instance.LogInfo($"Client {id} has connected.");
                playersInGame.Value++;
            }
        };

        NetworkManager.Singleton.OnClientDisconnectCallback += (id) =>
        {
            if (IsServer)
            {
                Logger.Instance.LogInfo($"Client {id} has been disconnected.");
                playersInGame.Value--;
            }
        };
    }
}
