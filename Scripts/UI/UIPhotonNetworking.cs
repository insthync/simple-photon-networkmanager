using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPhotonNetworking : UIBase
{
    public UIPhotonNetworkingEntry entryPrefab;
    public Transform gameListContainer;
    private readonly Dictionary<string, UIPhotonNetworkingEntry> entries = new Dictionary<string, UIPhotonNetworkingEntry>();

    private void OnEnable()
    {
        SimplePhotonNetworkManager.onReceivedBroadcast += OnReceivedBroadcast;
    }

    private void OnDisable()
    {
        SimplePhotonNetworkManager.onReceivedBroadcast -= OnReceivedBroadcast;
    }

    private void OnDestroy()
    {
        SimplePhotonNetworkManager.onReceivedBroadcast -= OnReceivedBroadcast;
    }

    private void OnReceivedBroadcast(NetworkDiscoveryData discoveryData)
    {
        var key = discoveryData.roomName + "-" + discoveryData.playerName + "-" + discoveryData.sceneName;
        if (!entries.ContainsKey(key))
        {
            var newEntry = Instantiate(entryPrefab, gameListContainer);
            newEntry.SetData(discoveryData);
            newEntry.gameObject.SetActive(true);
            entries.Add(key, newEntry);
        }
    }

    public virtual void OnClickConnectToMaster()
    {
        var networkManager = SimplePhotonNetworkManager.Singleton;
        networkManager.ConnectToMaster();
    }

    public virtual void OnClickConnectToBestCloudServer()
    {
        var networkManager = SimplePhotonNetworkManager.Singleton;
        networkManager.ConnectToBestCloudServer();
    }

    public virtual void OnClickConnectToRegion()
    {
        var networkManager = SimplePhotonNetworkManager.Singleton;
        networkManager.ConnectToRegion();
    }

    public virtual void OnClickJoinRandomRoom()
    {
        var networkManager = SimplePhotonNetworkManager.Singleton;
        networkManager.JoinRandomRoom();
    }
}
