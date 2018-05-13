using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UINetworkLoading : MonoBehaviour
{
    public static UINetworkLoading Singleton { get; private set; }
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
        SimplePhotonNetworkManager.onJoiningLobby += OnJoiningLobby;
        SimplePhotonNetworkManager.onJoiningRoom += OnJoiningRoom;
        SimplePhotonNetworkManager.onJoinedLobby += OnJoinedLobby;
        SimplePhotonNetworkManager.onJoinedRoom += OnJoinedRoom;
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
}
