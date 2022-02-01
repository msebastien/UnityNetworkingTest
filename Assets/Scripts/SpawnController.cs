using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MSebastien.Core.Singletons;
using Unity.Netcode;

public class SpawnController : NetworkSingleton<SpawnController>
{
    [SerializeField]
    private GameObject objectPrefab;

    [SerializeField]
    private int maxObjectInstanceCount = 3;

    /*private void Awake()
    {
        NetworkManager.Singleton.OnServerStarted += () =>
        {
            if (!IsServer) return;
            NetworkObjectPool.Instance.InitializePool();
        };
    }*/

    // Spawn objects (server-side)
    public void SpawnObjects()
    {
        if (!IsServer) return;

        for(int i = 0; i < maxObjectInstanceCount; i++)
        {
            //GameObject go = Instantiate(objectPrefab, 
            //    new Vector3(Random.Range(-4, 4), 2.0f, Random.Range(-4, 4)), Quaternion.identity); // on server
            GameObject go = NetworkObjectPool.Instance.GetNetworkObject(objectPrefab).gameObject;
            go.transform.position = new Vector3(Random.Range(-4, 4), 2.0f, Random.Range(-4, 4));
            
            go.GetComponent<NetworkObject>().Spawn(); // notify clients to instantiate it as well
        }
    }
}
