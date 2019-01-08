using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class UIPhotonWaitingRoom : UIBase
{
    public Text textRoomName;
    public Text textPlayerName;
    public Text textSceneName;
    public Text textPlayerCount;
    public Text textRoomState;
    public string roomStateWaiting = "Waiting";
    public string roomStatePlaying = "Playing";

    private void OnEnable()
    {
        SimplePhotonNetworkManager.onJoinedRoom += OnJoinedRoom;
        SimplePhotonNetworkManager.onPlayerConnected += OnPlayerConnected;
        SimplePhotonNetworkManager.onPlayerDisconnected += OnPlayerDisconnected;
        SimplePhotonNetworkManager.onPlayerPropertiesChanged += OnPlayerPropertiesChanged;
        SimplePhotonNetworkManager.onCustomRoomPropertiesChanged += OnCustomRoomPropertiesChanged;
    }

    private void OnDisable()
    {
        SimplePhotonNetworkManager.onJoinedRoom -= OnJoinedRoom;
        SimplePhotonNetworkManager.onPlayerConnected -= OnPlayerConnected;
        SimplePhotonNetworkManager.onPlayerDisconnected -= OnPlayerDisconnected;
        SimplePhotonNetworkManager.onPlayerPropertiesChanged -= OnPlayerPropertiesChanged;
        SimplePhotonNetworkManager.onCustomRoomPropertiesChanged -= OnCustomRoomPropertiesChanged;
    }

    private void UpdateRoomData()
    {
        var room = PhotonNetwork.room;
        var customProperties = room.CustomProperties;
        var roomName = (string)customProperties[SimplePhotonNetworkManager.CUSTOM_ROOM_ROOM_NAME];
        var playerId = (int)customProperties[SimplePhotonNetworkManager.CUSTOM_ROOM_PLAYER_ID];
        var playerName = (string)customProperties[SimplePhotonNetworkManager.CUSTOM_ROOM_PLAYER_NAME];
        var sceneName = (string)customProperties[SimplePhotonNetworkManager.CUSTOM_ROOM_SCENE_NAME];
        var state = (byte)customProperties[SimplePhotonNetworkManager.CUSTOM_ROOM_STATE];

        if (textRoomName != null)
            textRoomName.text = string.IsNullOrEmpty(roomName) ? "Untitled" : roomName;
        if (textPlayerName != null)
            textPlayerName.text = playerName;
        if (textSceneName != null)
            textSceneName.text = sceneName;
        if (textPlayerCount != null)
            textPlayerCount.text = room.PlayerCount + "/" + room.MaxPlayers;
        if (textRoomState != null)
        {
            switch ((SimplePhotonNetworkManager.RoomState)state)
            {
                case SimplePhotonNetworkManager.RoomState.Waiting:
                    textRoomState.text = roomStateWaiting;
                    break;
                case SimplePhotonNetworkManager.RoomState.Playing:
                    textRoomState.text = roomStatePlaying;
                    break;
            }
        }
    }

    public virtual void OnClickLeaveRoom()
    {
        SimplePhotonNetworkManager.Singleton.LeaveRoom();
    }

    public virtual void OnClickStartGame()
    {
        SimplePhotonNetworkManager.Singleton.StartGame();
    }

    private void OnJoinedRoom()
    {
        UpdateRoomData();
    }

    private void OnPlayerConnected(PhotonPlayer player)
    {
        UpdateRoomData();
    }

    private void OnPlayerDisconnected(PhotonPlayer player)
    {
        UpdateRoomData();
    }

    private void OnPlayerPropertiesChanged(object[] playerAndUpdatedProps)
    {

    }

    private void OnCustomRoomPropertiesChanged(Hashtable propertiesThatChanged)
    {
        UpdateRoomData();
    }
}
