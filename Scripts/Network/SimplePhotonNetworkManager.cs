using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class SimplePhotonNetworkManager : PunBehaviour
{
    public enum RoomState : byte
    {
        Waiting,
        Playing,
    }

    public enum PlayerState : byte
    {
        NotReady,
        Ready,
    }

    public const int UNIQUE_VIEW_ID = 999;
    public const string CUSTOM_ROOM_ROOM_NAME = "R";
    public const string CUSTOM_ROOM_PLAYER_ID = "Id";
    public const string CUSTOM_ROOM_PLAYER_NAME = "P";
    public const string CUSTOM_ROOM_SCENE_NAME = "S";
    public const string CUSTOM_ROOM_STATE = "St";
    public const string CUSTOM_PLAYER_STATE = "St";
    public static SimplePhotonNetworkManager Singleton { get; protected set; }
    public static System.Action<List<NetworkDiscoveryData>> onReceivedRoomListUpdate;
    public static System.Action<DisconnectCause> onConnectionError;
    public static System.Action<object[]> onRoomConnectError;
    public static System.Action onJoiningLobby;
    public static System.Action onJoinedLobby;
    public static System.Action onJoiningRoom;
    public static System.Action onJoinedRoom;
    public static System.Action onLeftRoom;
    public static System.Action onDisconnected;
    public static System.Action<PhotonPlayer> onPlayerConnected;
    public static System.Action<PhotonPlayer> onPlayerDisconnected;
    public static System.Action<PhotonPlayer, Hashtable> onPlayerPropertiesChanged;
    public static System.Action<Hashtable> onCustomRoomPropertiesChanged;

    public bool isLog;
    public SceneNameField offlineScene;
    public SceneNameField onlineScene;
    public GameObject playerPrefab;
    public string gameVersion = "1";
    public string masterAddress = "localhost";
    public int masterPort = 5055;
    public CloudRegionCode region;
    public int sendRate = 20;
    public int sendRateOnSerialize = 10;
    public byte maxConnections;
    public string roomName;
    public AsyncOperation LoadSceneAsyncOp { get; protected set; }
    public SimplePhotonStartPoint[] StartPoints { get; protected set; }
    public bool isConnectOffline { get; protected set; }
    private bool startGameOnRoomCreated;

    protected virtual void Awake()
    {
        if (Singleton != null)
        {
            Destroy(gameObject);
            return;
        }
        Singleton = this;
        DontDestroyOnLoad(Singleton);
        PhotonNetwork.sendRate = sendRate;
        PhotonNetwork.sendRateOnSerialize = sendRateOnSerialize;
        StartPoints = new SimplePhotonStartPoint[0];
        // Set unique view id
        PhotonView view = GetComponent<PhotonView>();
        if (view == null)
            view = gameObject.AddComponent<PhotonView>();
        view.viewID = UNIQUE_VIEW_ID;
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    protected virtual void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    public virtual void ConnectToMaster()
    {
        PhotonNetwork.autoJoinLobby = true;
        PhotonNetwork.automaticallySyncScene = true;
        PhotonNetwork.PhotonServerSettings.HostType = ServerSettings.HostingOption.SelfHosted;
        PhotonNetwork.ConnectToMaster(masterAddress, masterPort, PhotonNetwork.PhotonServerSettings.AppID, gameVersion);
        if (onJoiningLobby != null)
            onJoiningLobby.Invoke();
    }

    public virtual void ConnectToBestCloudServer()
    {
        isConnectOffline = false;
        PhotonNetwork.autoJoinLobby = true;
        PhotonNetwork.automaticallySyncScene = true;
        PhotonNetwork.PhotonServerSettings.HostType = ServerSettings.HostingOption.BestRegion;
        PhotonNetwork.offlineMode = false;
        PhotonNetwork.ConnectToBestCloudServer(gameVersion);
        if (onJoiningLobby != null)
            onJoiningLobby.Invoke();
    }

    public virtual void ConnectToRegion()
    {
        isConnectOffline = false;
        PhotonNetwork.autoJoinLobby = true;
        PhotonNetwork.automaticallySyncScene = true;
        PhotonNetwork.PhotonServerSettings.HostType = ServerSettings.HostingOption.PhotonCloud;
        PhotonNetwork.offlineMode = false;
        PhotonNetwork.ConnectToRegion(region, gameVersion);
        if (onJoiningLobby != null)
            onJoiningLobby.Invoke();
    }

    public virtual void PlayOffline()
    {
        isConnectOffline = true;
        PhotonNetwork.autoJoinLobby = true;
        PhotonNetwork.automaticallySyncScene = true;
        if (onJoinedLobby != null)
            onJoinedLobby.Invoke();
    }

    public void CreateRoom()
    {
        if (isConnectOffline)
        {
            PhotonNetwork.offlineMode = true;
            return;
        }
        SetupAndCreateRoom();
        startGameOnRoomCreated = true;
    }

    public void CreateWaitingRoom()
    {
        if (isConnectOffline)
        {
            PhotonNetwork.offlineMode = true;
            return;
        }
        SetupAndCreateRoom();
        startGameOnRoomCreated = false;
    }

    private void SetupAndCreateRoom()
    {
        var roomOptions = new RoomOptions();
        roomOptions.CustomRoomPropertiesForLobby = GetCustomRoomPropertiesForLobby();
        roomOptions.MaxPlayers = maxConnections;
        PhotonNetwork.CreateRoom(string.Empty, roomOptions, null);
    }

    protected virtual string[] GetCustomRoomPropertiesForLobby()
    {
        return new string[]
        {
            CUSTOM_ROOM_ROOM_NAME,
            CUSTOM_ROOM_PLAYER_ID,
            CUSTOM_ROOM_PLAYER_NAME,
            CUSTOM_ROOM_SCENE_NAME,
            CUSTOM_ROOM_STATE
        };
    }

    public void SetRoomName(string roomName)
    {
        // If room not created, set data to field to use later
        this.roomName = roomName;
        if (PhotonNetwork.inRoom && PhotonNetwork.isMasterClient)
        {
            var customProperties = PhotonNetwork.room.CustomProperties;
            customProperties[CUSTOM_ROOM_ROOM_NAME] = roomName;
            PhotonNetwork.room.SetCustomProperties(customProperties);
        }
    }

    public void SetRoomOnlineScene(SceneNameField onlineScene)
    {
        // If room not created, set data to field to use later
        this.onlineScene = onlineScene;
        if (PhotonNetwork.inRoom && PhotonNetwork.isMasterClient)
        {
            var customProperties = PhotonNetwork.room.CustomProperties;
            customProperties[CUSTOM_ROOM_SCENE_NAME] = onlineScene.SceneName;
            PhotonNetwork.room.SetCustomProperties(customProperties);
        }
    }

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
        if (onJoiningRoom != null)
            onJoiningRoom.Invoke();
    }

    public void JoinRandomRoom()
    {
        PhotonNetwork.JoinRandomRoom();
        if (onJoiningRoom != null)
            onJoiningRoom.Invoke();
    }

    public virtual void LeaveRoom()
    {
        if (isConnectOffline)
            PhotonNetwork.Disconnect();
        else
            PhotonNetwork.LeaveRoom();
    }

    public virtual void Disconnect()
    {
        if (isConnectOffline)
        {
            if (onDisconnected != null)
                onDisconnected.Invoke();
        }
        else
            PhotonNetwork.Disconnect();
    }

    public override void OnReceivedRoomListUpdate()
    {
        var rooms = PhotonNetwork.GetRoomList();
        var foundRooms = new List<NetworkDiscoveryData>();
        foreach (var room in rooms)
        {
            var customProperties = room.CustomProperties;
            var discoveryData = new NetworkDiscoveryData();
            discoveryData.name = room.Name;
            discoveryData.roomName = (string)customProperties[CUSTOM_ROOM_ROOM_NAME];
            discoveryData.playerId = (int)customProperties[CUSTOM_ROOM_PLAYER_ID];
            discoveryData.playerName = (string)customProperties[CUSTOM_ROOM_PLAYER_NAME];
            discoveryData.sceneName = (string)customProperties[CUSTOM_ROOM_SCENE_NAME];
            discoveryData.state = (byte)customProperties[CUSTOM_ROOM_STATE];
            discoveryData.numPlayers = room.PlayerCount;
            discoveryData.maxPlayers = room.MaxPlayers;
            foundRooms.Add(discoveryData);
        }
        if (onReceivedRoomListUpdate != null)
            onReceivedRoomListUpdate.Invoke(foundRooms);
    }

    public override void OnFailedToConnectToPhoton(DisconnectCause cause)
    {
        if (isLog) Debug.Log("OnFailedToConnectToPhoton " + cause.ToString());
        if (onConnectionError != null)
            onConnectionError(cause);
    }

    public override void OnConnectionFail(DisconnectCause cause)
    {
        if (isLog) Debug.Log("OnConnectionFail " + cause.ToString());
        if (onConnectionError != null)
            onConnectionError(cause);
    }

    public override void OnPhotonCreateRoomFailed(object[] codeAndMsg)
    {
        if (isLog) Debug.Log("OnPhotonCreateRoomFailed " + codeAndMsg[0].ToString() + " " + codeAndMsg[1].ToString());
        if (onRoomConnectError != null)
            onRoomConnectError(codeAndMsg);
    }

    public override void OnPhotonRandomJoinFailed(object[] codeAndMsg)
    {
        if (isLog) Debug.Log("OnPhotonRandomJoinFailed " + codeAndMsg[0].ToString() + " " + codeAndMsg[1].ToString());
        if (onRoomConnectError != null)
            onRoomConnectError(codeAndMsg);
    }

    public override void OnJoinedLobby()
    {
        if (isLog) Debug.Log("OnJoinedLobby");
        if (onJoinedLobby != null)
            onJoinedLobby.Invoke();
    }

    public override void OnCreatedRoom()
    {
        if (isLog) Debug.Log("OnCreatedRoom");
        // Set room information
        var customProperties = PhotonNetwork.room.CustomProperties;
        customProperties[CUSTOM_ROOM_ROOM_NAME] = roomName;
        customProperties[CUSTOM_ROOM_PLAYER_ID] = PhotonNetwork.player.ID;
        customProperties[CUSTOM_ROOM_PLAYER_NAME] = PhotonNetwork.playerName;
        customProperties[CUSTOM_ROOM_SCENE_NAME] = onlineScene.SceneName;
        customProperties[CUSTOM_ROOM_STATE] = (byte) RoomState.Waiting;
        PhotonNetwork.room.SetCustomProperties(customProperties);
        if (startGameOnRoomCreated)
            StartGame();
    }

    public void StartGame()
    {
        if (!PhotonNetwork.inRoom)
        {
            Debug.LogError("Player not joined room, cannot start game");
            return;
        }

        if (!PhotonNetwork.isMasterClient)
        {
            Debug.LogError("Player is not master client, cannot start game");
            return;
        }

        StartCoroutine(LoadOnlineScene());
    }

    protected IEnumerator LoadOnlineScene()
    {
        if (LoadSceneAsyncOp == null || LoadSceneAsyncOp.isDone)
        {
            LoadSceneAsyncOp = PhotonNetwork.LoadLevelAsync(onlineScene.SceneName);
            while (!LoadSceneAsyncOp.isDone)
            {
                yield return null;
            }
        }
        // Change room state to playing
        var customProperties = PhotonNetwork.room.CustomProperties;
        customProperties[CUSTOM_ROOM_STATE] = (byte)RoomState.Playing;
        PhotonNetwork.room.SetCustomProperties(customProperties);
        // Setup start points for master client
        StartPoints = FindObjectsOfType<SimplePhotonStartPoint>();
    }

    public override void OnJoinedRoom()
    {
        if (isLog) Debug.Log("OnJoinedRoom");
        if (PhotonNetwork.isMasterClient)
        {
            if (startGameOnRoomCreated)
            {
                // If master client joined room, wait for scene change if needed
                StartCoroutine(MasterWaitOnlineSceneLoaded());
            }

            // Set player state to ready (master client always ready)
            var customProperties = PhotonNetwork.player.CustomProperties;
            customProperties[CUSTOM_PLAYER_STATE] = (byte)PlayerState.Ready;
            PhotonNetwork.player.SetCustomProperties(customProperties);
        }
        else
        {
            // Set player state to not ready
            var customProperties = PhotonNetwork.player.CustomProperties;
            customProperties[CUSTOM_PLAYER_STATE] = (byte)PlayerState.NotReady;
            PhotonNetwork.player.SetCustomProperties(customProperties);
        }
        if (onJoinedRoom != null)
            onJoinedRoom.Invoke();
    }

    protected IEnumerator MasterWaitOnlineSceneLoaded()
    {
        while (LoadSceneAsyncOp != null && !LoadSceneAsyncOp.isDone)
        {
            yield return null;
        }
        OnPhotonPlayerConnected(PhotonNetwork.player);
    }

    public override void OnConnectedToMaster()
    {
        if (isLog) Debug.Log("OnConnectedToMaster");
        if (isConnectOffline)
            PhotonNetwork.JoinRandomRoom();
    }

    /// <summary>
    /// Override this to initialize something after scene changed
    /// </summary>
    public virtual void OnOnlineSceneChanged()
    {
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        if (isLog) Debug.Log("OnPhotonPlayerConnected");
        if (onPlayerConnected != null)
            onPlayerConnected.Invoke(newPlayer);
    }

    public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        if (isLog) Debug.Log("OnPhotonPlayerDisconnected");
        if (onPlayerDisconnected != null)
            onPlayerDisconnected.Invoke(otherPlayer);
    }

    public override void OnPhotonPlayerPropertiesChanged(object[] playerAndUpdatedProps)
    {
        if (isLog) Debug.Log("OnPhotonPlayerPropertiesChanged");
        if (onPlayerPropertiesChanged != null)
            onPlayerPropertiesChanged.Invoke((PhotonPlayer)playerAndUpdatedProps[0], (Hashtable)playerAndUpdatedProps[1]);
    }

    public override void OnPhotonCustomRoomPropertiesChanged(Hashtable propertiesThatChanged)
    {
        if (isLog) Debug.Log("OnPhotonCustomRoomPropertiesChanged");
        if (onCustomRoomPropertiesChanged != null)
            onCustomRoomPropertiesChanged.Invoke(propertiesThatChanged);
    }

    private void OnSceneChanged(Scene current, Scene next)
    {
        if (next.name == onlineScene.SceneName && PhotonNetwork.inRoom)
        {
            // Send client ready to spawn player at master client
            OnOnlineSceneChanged();
            photonView.RPC("RpcPlayerSceneChanged", PhotonTargets.MasterClient, PhotonNetwork.player.ID);
        }
    }

    public void TogglePlayerReady()
    {
        if (!PhotonNetwork.inRoom)
        {
            Debug.LogError("Cannot toggle ready state because you are not in room");
            return;
        }

        Hashtable customProperties = PhotonNetwork.player.CustomProperties;
        PlayerState state = PlayerState.NotReady;
        object stateObj;
        if (customProperties.TryGetValue(CUSTOM_PLAYER_STATE, out stateObj))
            state = (PlayerState)(byte)stateObj;
        // Toggle state
        if (state == PlayerState.NotReady)
            state = PlayerState.Ready;
        else if (state == PlayerState.Ready)
            state = PlayerState.NotReady;
        // Set state property
        customProperties[CUSTOM_PLAYER_STATE] = (byte)state;
        PhotonNetwork.player.SetCustomProperties(customProperties);
    }

    public PhotonPlayer GetPlayerById(int id)
    {
        PhotonPlayer foundPlayer = null;
        foreach (var player in PhotonNetwork.playerList)
        {
            if (player.ID == id)
            {
                foundPlayer = player;
                break;
            }
        }
        return foundPlayer;
    }

    [PunRPC]
    protected virtual void RpcAddPlayer()
    {
        Vector3 position = Vector3.zero;
        var rotation = Quaternion.identity;
        RandomStartPoint(out position, out rotation);
        PhotonNetwork.Instantiate(playerPrefab.name, position, rotation, 0);
    }

    [PunRPC]
    protected virtual void RpcPlayerSceneChanged(int id)
    {
        PhotonPlayer foundPlayer = GetPlayerById(id);
        if (foundPlayer != null)
            photonView.RPC("RpcAddPlayer", foundPlayer);
    }

    public bool RandomStartPoint(out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        if (StartPoints == null || StartPoints.Length <= 0)
            return false;
        var point = StartPoints[Random.Range(0, StartPoints.Length)];
        position = point.position;
        rotation = point.rotation;
        return true;
    }

    public override void OnLeftRoom()
    {
        if (isLog) Debug.Log("OnLeftRoom");
        if (!SceneManager.GetActiveScene().name.Equals(offlineScene.SceneName))
            SceneManager.LoadScene(offlineScene.SceneName);
        if (onLeftRoom != null)
            onLeftRoom.Invoke();
    }

    public override void OnDisconnectedFromPhoton()
    {
        if (isLog) Debug.Log("OnDisconnectedFromPhoton");
        if (!SceneManager.GetActiveScene().name.Equals(offlineScene.SceneName))
            SceneManager.LoadScene(offlineScene.SceneName);
        if (onDisconnected != null)
            onDisconnected.Invoke();
    }
}
