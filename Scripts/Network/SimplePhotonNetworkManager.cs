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

    public const int UNIQUE_VIEW_ID = 999;
    public const string CUSTOM_ROOM_ROOM_NAME = "R";
    public const string CUSTOM_ROOM_PLAYER_NAME = "P";
    public const string CUSTOM_ROOM_SCENE_NAME = "S";
    public const string CUSTOM_ROOM_STATE = "St";
    public static SimplePhotonNetworkManager Singleton { get; protected set; }
    public static event System.Action<List<NetworkDiscoveryData>> onReceivedRoomListUpdate;
    public static event System.Action<DisconnectCause> onConnectionError;
    public static event System.Action<object[]> onRoomConnectError;
    public static event System.Action onJoiningLobby;
    public static event System.Action onJoinedLobby;
    public static event System.Action onJoiningRoom;
    public static event System.Action onJoinedRoom;
    public static event System.Action onLeftRoom;
    public static event System.Action onDisconnected;

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
        roomOptions.CustomRoomProperties = new Hashtable() { { CUSTOM_ROOM_ROOM_NAME, roomName } };
        roomOptions.CustomRoomPropertiesForLobby = new string[] { CUSTOM_ROOM_ROOM_NAME, CUSTOM_ROOM_PLAYER_NAME, CUSTOM_ROOM_SCENE_NAME, CUSTOM_ROOM_STATE };
        roomOptions.MaxPlayers = maxConnections;
        PhotonNetwork.CreateRoom(string.Empty, roomOptions, null);
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
        if (PhotonNetwork.isMasterClient && startGameOnRoomCreated)
        {
            // If master client joined room, wait for scene change if needed
            StartCoroutine(MasterWaitOnlineSceneLoaded());
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
        if (!PhotonNetwork.isMasterClient)
            return;
        photonView.RPC("RpcAddPlayer", newPlayer);
    }

    private void OnSceneChanged(Scene current, Scene next)
    {
        if (next.name == onlineScene.SceneName && PhotonNetwork.inRoom)
        {
            OnOnlineSceneChanged();
        }
    }
    
    [PunRPC]
    protected virtual void RpcAddPlayer()
    {
        var position = Vector3.zero;
        var rotation = Quaternion.identity;
        RandomStartPoint(out position, out rotation);
        PhotonNetwork.Instantiate(playerPrefab.name, position, rotation, 0);
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
