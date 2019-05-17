using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UINetworkLoading : MonoBehaviour
{
    public static UINetworkLoading Singleton { get; private set; }
    public GameObject connectingToMasterObject;
    public GameObject joiningLobbyObject;
    public GameObject joiningRoomObject;

    private void Awake()
    {
        if (Singleton != null)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        Singleton = this;
        SimplePhotonNetworkManager.onConnectingToMaster += OnConnectingToMaster;
        SimplePhotonNetworkManager.onConnectedToMaster += OnConnectedToMaster;
        SimplePhotonNetworkManager.onJoiningLobby += OnJoiningLobby;
        SimplePhotonNetworkManager.onJoiningRoom += OnJoiningRoom;
        SimplePhotonNetworkManager.onJoinedLobby += OnJoinedLobby;
        SimplePhotonNetworkManager.onJoinedRoom += OnJoinedRoom;
        SimplePhotonNetworkManager.onConnectionError += OnConnectionError;
        SimplePhotonNetworkManager.onRoomConnectError += OnRoomConnectError;
    }

    public void OnConnectingToMaster()
    {
        if (connectingToMasterObject != null)
            connectingToMasterObject.SetActive(true);
    }

    public void OnJoiningLobby()
    {
        if (joiningLobbyObject != null)
            joiningLobbyObject.SetActive(true);
    }

    public void OnJoiningRoom()
    {
        if (joiningRoomObject != null)
            joiningRoomObject.SetActive(true);
    }

    public void OnConnectedToMaster()
    {
        if (connectingToMasterObject != null)
            connectingToMasterObject.SetActive(false);
    }

    public void OnJoinedLobby()
    {
        if (joiningLobbyObject != null)
            joiningLobbyObject.SetActive(false);
    }

    public void OnJoinedRoom()
    {
        if (joiningRoomObject != null)
            joiningRoomObject.SetActive(false);
    }

    public void OnConnectionError(DisconnectCause error)
    {
        if (joiningLobbyObject != null)
            joiningLobbyObject.SetActive(false);

        if (joiningRoomObject != null)
            joiningRoomObject.SetActive(false);
    }

    public void OnRoomConnectError(object[] codeAndMsg)
    {
        if (joiningLobbyObject != null)
            joiningLobbyObject.SetActive(false);

        if (joiningRoomObject != null)
            joiningRoomObject.SetActive(false);
    }
}
