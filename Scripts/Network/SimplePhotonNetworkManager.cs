using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon;

public class SimplePhotonNetworkManager : PunBehaviour
{
    public const int UNIQUE_VIEW_ID = int.MaxValue;
    public const string CUSTOM_ROOM_PLAYER_NAME = "P";
    public const string CUSTOM_ROOM_SCENE_NAME = "S";
    public static SimplePhotonNetworkManager Singleton { get; protected set; }
    public static System.Action<NetworkDiscoveryData> onReceivedBroadcast;

    public bool isLog;
    public SceneNameField offlineScene;
    public SceneNameField onlineScene;
    public GameObject playerPrefab;
    public string gameVersion = "1";
    public string masterAddress = "localhost";
    public int masterPort = 5055;
    public byte maxConnections;
    public string roomName;
    public bool IsJoinedLobby { get; protected set; }
    public AsyncOperation LoadSceneAsyncOp { get; protected set; }
    public SimplePhotonStartPoint[] StartPoints { get; protected set; }

    protected virtual void Awake()
    {
        if (Singleton != null)
        {
            Destroy(gameObject);
            return;
        }
        Singleton = this;
        DontDestroyOnLoad(Singleton);
        StartPoints = new SimplePhotonStartPoint[0];
        // Set unique view id
        PhotonView view = GetComponent<PhotonView>();
        if (view == null)
            view = gameObject.AddComponent<PhotonView>();
        view.viewID = UNIQUE_VIEW_ID;
    }

    protected virtual void Update()
    {
        if (IsJoinedLobby)
        {
            var rooms = PhotonNetwork.GetRoomList();
            foreach (var room in rooms)
            {
                var customProperties = room.CustomProperties;
                var discoveryData = new NetworkDiscoveryData();
                discoveryData.roomName = room.Name;
                discoveryData.playerName = (string)customProperties[CUSTOM_ROOM_PLAYER_NAME];
                discoveryData.sceneName = (string)customProperties[CUSTOM_ROOM_SCENE_NAME];
                discoveryData.numPlayers = room.PlayerCount;
                discoveryData.maxPlayers = room.MaxPlayers;
                if (onReceivedBroadcast != null)
                    onReceivedBroadcast.Invoke(discoveryData);
            }
        }
    }

    public void StartLan()
    {
        PhotonNetwork.autoJoinLobby = false;
        PhotonNetwork.automaticallySyncScene = true;
        PhotonNetwork.ConnectToMaster(masterAddress, masterPort, PhotonNetwork.PhotonServerSettings.AppID, gameVersion);
    }

    public void StartOnline()
    {
        PhotonNetwork.autoJoinLobby = false;
        PhotonNetwork.automaticallySyncScene = true;
        PhotonNetwork.ConnectToBestCloudServer(gameVersion);
    }

    public void CreateRoom()
    {
        var roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxConnections;
        PhotonNetwork.CreateRoom(roomName, roomOptions, null);
    }

    public void JoinRandomRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    /// <summary>
    /// This will be called after function ConnectToMaster called and autoJoinLobby is False
    /// </summary>
    public override void OnConnectedToMaster()
    {
        if (isLog) Debug.Log("OnConnectedToMaster");
        IsJoinedLobby = true;
    }

    public override void OnJoinedLobby()
    {
        if (isLog) Debug.Log("OnJoinedLobby");
        IsJoinedLobby = true;
    }

    public override void OnPhotonRandomJoinFailed(object[] codeAndMsg)
    {
        if (isLog) Debug.Log("OnPhotonRandomJoinFailed");
        CreateRoom();
    }

    public override void OnCreatedRoom()
    {
        if (isLog) Debug.Log("OnCreatedRoom");
        var customProperties = PhotonNetwork.room.CustomProperties;
        customProperties.Add(CUSTOM_ROOM_PLAYER_NAME, PhotonNetwork.playerName);
        customProperties.Add(CUSTOM_ROOM_SCENE_NAME, onlineScene.SceneName);
        PhotonNetwork.room.SetCustomProperties(customProperties);
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
        StartPoints = FindObjectsOfType<SimplePhotonStartPoint>();
    }

    public override void OnJoinedRoom()
    {
        if (isLog) Debug.Log("OnJoinedRoom");
        if (PhotonNetwork.isMasterClient)
            StartCoroutine(WaitOnlineSceneLoaded());
    }

    protected IEnumerator WaitOnlineSceneLoaded()
    {
        while (LoadSceneAsyncOp != null && !LoadSceneAsyncOp.isDone)
        {
            yield return null;
        }
        OnPhotonPlayerConnected(PhotonNetwork.player);
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        if (isLog) Debug.Log("OnPhotonPlayerConnected");
        if (!PhotonNetwork.isMasterClient)
            return;
        photonView.RPC("RpcAddPlayer", newPlayer);
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
    }

    public override void OnDisconnectedFromPhoton()
    {
        if (isLog) Debug.Log("OnDisconnectedFromPhoton");
        if (!SceneManager.GetActiveScene().name.Equals(offlineScene.SceneName))
            SceneManager.LoadScene(offlineScene.SceneName);
    }
}
