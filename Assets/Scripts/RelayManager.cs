using MSebastien.Core.Singletons;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public class RelayManager : Singleton<RelayManager>
{
    [SerializeField]
    private string environment = "production"; // used by Unity Gaming Services

    [SerializeField]
    private int maxConnections = 10; // number of max concurrent connections

    public bool IsRelayServerEnabled => Transport != null && 
        Transport.Protocol == UnityTransport.ProtocolType.RelayUnityTransport;

    public UnityTransport Transport => NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>();

    /// <summary>
    /// HostGame allocate a Relay server and returns needed data to host the game
    /// /!\ Need to be called right before StartHost(). Calling this method in a Awake()/Start() method won't work.
    /// </summary>
    public async Task<RelayHostData> SetupRelay()
    {
        Logger.Instance.LogInfo($"Relay Server starting with max connections {maxConnections}");
        // Set environment used (production,development,QA...)
        InitializationOptions options = new InitializationOptions()
            .SetEnvironmentName(environment);

        // Initialize the Unity Services engine
        await UnityServices.InitializeAsync(options);

        // If not already logged, log the user in
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        // Ask Unity Services to allocate a Relay server
        Allocation allocation = await Relay.Instance.CreateAllocationAsync(maxConnections);

        // Populate hosting data
        RelayHostData data = new RelayHostData
        {
            Key = allocation.Key,
            Port = (ushort)allocation.RelayServer.Port,
            AllocationID = allocation.AllocationId,
            AllocationIDBytes = allocation.AllocationIdBytes,
            ConnectionData = allocation.ConnectionData,
            IPv4Address = allocation.RelayServer.IpV4
        };

        // Retrieve the Relay join code for our clients to join our party
        data.JoinCode = await Relay.Instance.GetJoinCodeAsync(data.AllocationID);

        Transport.SetRelayServerData(data.IPv4Address, data.Port, data.AllocationIDBytes, data.Key, data.ConnectionData);

        Logger.Instance.LogInfo($"Relay Server generated a join code : {data.JoinCode}");

        return data;
    }

    /// <summary>
    /// Join a Relay server based on the JoinCode received from the Host or Server
    /// </summary>
    public async Task<RelayJoinData> JoinRelay(string joinCode)
    {
        // Set environment used (production,development,QA...)
        InitializationOptions options = new InitializationOptions()
            .SetEnvironmentName(environment);

        // Initialize the Unity Services engine
        await UnityServices.InitializeAsync(options);

        // If not already logged, log the user in
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        // Ask Unity Services for allocation data based on a join code
        JoinAllocation allocation = await Relay.Instance.JoinAllocationAsync(joinCode);

        // Populate joining data
        RelayJoinData data = new RelayJoinData
        {
            Key = allocation.Key,
            Port = (ushort)allocation.RelayServer.Port,
            AllocationID = allocation.AllocationId,
            AllocationIDBytes = allocation.AllocationIdBytes,
            ConnectionData = allocation.ConnectionData,
            HostConnectionData = allocation.HostConnectionData,
            IPv4Address = allocation.RelayServer.IpV4
        };

        Transport.SetRelayServerData(data.IPv4Address, data.Port, data.AllocationIDBytes, data.Key, data.ConnectionData, data.HostConnectionData);

        Logger.Instance.LogInfo($"Client joined game with join code {joinCode}");
        return data;
    }
}
