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
    public Text textGameRule;
    public Text textBotCount;
    public Text textMatchTime;
    public Text textMatchKill;
    public Text textMatchScore;
    public UIPhotonWaitingPlayer waitingPlayerPrefab;
    public GameObject waitingPlayerListRoot;
    public Transform waitingPlayerListContainer;
    public GameObject waitingPlayerTeamAListRoot;
    public Transform waitingPlayerTeamAListContainer;
    public GameObject waitingPlayerTeamBListRoot;
    public Transform waitingPlayerTeamBListContainer;
    public GameObject[] hostObjects;
    public GameObject[] nonHostObjects;
    public int HostPlayerID { get; private set; }

    private readonly Dictionary<int, UIPhotonWaitingPlayer> waitingPlayers = new Dictionary<int, UIPhotonWaitingPlayer>();
    private readonly Dictionary<int, UIPhotonWaitingPlayer> waitingTeamAPlayers = new Dictionary<int, UIPhotonWaitingPlayer>();
    private readonly Dictionary<int, UIPhotonWaitingPlayer> waitingTeamBPlayers = new Dictionary<int, UIPhotonWaitingPlayer>();

    public override void Show()
    {
        base.Show();
        SimplePhotonNetworkManager.onJoinedRoom += OnJoinedRoomCallback;
        SimplePhotonNetworkManager.onPlayerConnected += OnPlayerConnectedCallback;
        SimplePhotonNetworkManager.onPlayerDisconnected += OnPlayerDisconnectedCallback;
        SimplePhotonNetworkManager.onPlayerPropertiesChanged += OnPlayerPropertiesChangedCallback;
        SimplePhotonNetworkManager.onCustomRoomPropertiesChanged += OnCustomRoomPropertiesChangedCallback;
        OnJoinedRoomCallback();
    }

    public override void Hide()
    {
        base.Hide();
        SimplePhotonNetworkManager.onJoinedRoom -= OnJoinedRoomCallback;
        SimplePhotonNetworkManager.onPlayerConnected -= OnPlayerConnectedCallback;
        SimplePhotonNetworkManager.onPlayerDisconnected -= OnPlayerDisconnectedCallback;
        SimplePhotonNetworkManager.onPlayerPropertiesChanged -= OnPlayerPropertiesChangedCallback;
        SimplePhotonNetworkManager.onCustomRoomPropertiesChanged -= OnCustomRoomPropertiesChangedCallback;
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

        object gameRuleObject;
        BaseNetworkGameRule gameRule = null;
        if (textGameRule != null &&
            customProperties.TryGetValue(BaseNetworkGameManager.CUSTOM_ROOM_GAME_RULE, out gameRuleObject) &&
            BaseNetworkGameInstance.GameRules.TryGetValue(gameRuleObject.ToString(), out gameRule))
            textGameRule.text = gameRule == null ? "Unknow" : gameRule.Title;

        object botCountObject;
        if (textBotCount != null &&
            customProperties.TryGetValue(BaseNetworkGameManager.CUSTOM_ROOM_GAME_RULE_BOT_COUNT, out botCountObject))
        {
            textBotCount.text = ((int) botCountObject).ToString("N0");
            textBotCount.gameObject.SetActive(gameRule != null && gameRule.HasOptionBotCount);
        }

        object matchTimeObject;
        if (textMatchTime != null &&
            customProperties.TryGetValue(BaseNetworkGameManager.CUSTOM_ROOM_GAME_RULE_MATCH_TIME, out matchTimeObject))
        {
            textMatchTime.text = ((int) matchTimeObject).ToString("N0");
            textMatchTime.gameObject.SetActive(gameRule != null && gameRule.HasOptionMatchTime);
        }

        object matchKillObject;
        if (textMatchKill != null &&
            customProperties.TryGetValue(BaseNetworkGameManager.CUSTOM_ROOM_GAME_RULE_MATCH_KILL, out matchKillObject))
        {
            textMatchKill.text = ((int) matchKillObject).ToString("N0");
            textMatchKill.gameObject.SetActive(gameRule != null && gameRule.HasOptionMatchKill);
        }

        object matchScoreObject;
        if (textMatchScore != null &&
            customProperties.TryGetValue(BaseNetworkGameManager.CUSTOM_ROOM_GAME_RULE_MATCH_SCORE,
                out matchScoreObject))
        {
            textMatchScore.text = ((int) matchScoreObject).ToString("N0");
            textMatchScore.gameObject.SetActive(gameRule != null && gameRule.HasOptionMatchScore);
        }

        HostPlayerID = playerId;
    }

    public virtual void OnClickLeaveRoom()
    {
        SimplePhotonNetworkManager.Singleton.LeaveRoom();
    }

    public virtual void OnClickStartGame()
    {
        SimplePhotonNetworkManager.Singleton.StartGame();
    }

    public virtual void OnClickReady()
    {
        Hashtable customProperties = PhotonNetwork.player.CustomProperties;
        SimplePhotonNetworkManager.PlayerState state = SimplePhotonNetworkManager.PlayerState.NotReady;
        object stateObj;
        if (customProperties.TryGetValue(SimplePhotonNetworkManager.CUSTOM_PLAYER_STATE, out stateObj))
            state = (SimplePhotonNetworkManager.PlayerState) (byte) stateObj;
        // Toggle state
        if (state == SimplePhotonNetworkManager.PlayerState.NotReady)
            state = SimplePhotonNetworkManager.PlayerState.Ready;
        if (state == SimplePhotonNetworkManager.PlayerState.Ready)
            state = SimplePhotonNetworkManager.PlayerState.NotReady;
        // Set state property
        customProperties[SimplePhotonNetworkManager.CUSTOM_PLAYER_STATE] = (byte)state;
        PhotonNetwork.player.SetCustomProperties(customProperties);
    }

    public virtual void OnClickChangeTeam()
    {

    }

    private void OnJoinedRoomCallback()
    {
        UpdateRoomData();
        // Set waiting player list
        for (var i = waitingPlayerListContainer.childCount - 1; i >= 0; --i)
        {
            var child = waitingPlayerListContainer.GetChild(i);
            Destroy(child.gameObject);
        }

        for (var i = waitingPlayerTeamAListContainer.childCount - 1; i >= 0; --i)
        {
            var child = waitingPlayerTeamAListContainer.GetChild(i);
            Destroy(child.gameObject);
        }

        for (var i = waitingPlayerTeamBListContainer.childCount - 1; i >= 0; --i)
        {
            var child = waitingPlayerTeamBListContainer.GetChild(i);
            Destroy(child.gameObject);
        }

        waitingPlayers.Clear();
        waitingTeamAPlayers.Clear();
        waitingTeamBPlayers.Clear();
        foreach (PhotonPlayer data in PhotonNetwork.playerList)
        {
            CreatePlayerUI(data);
        }
        foreach (var hostObject in hostObjects)
        {
            hostObject.SetActive(HostPlayerID == PhotonNetwork.player.ID);
        }
        foreach (var nonHostObject in nonHostObjects)
        {
            nonHostObject.SetActive(HostPlayerID != PhotonNetwork.player.ID);
        }
    }

    private void DestroyPlayerUI(int id)
    {
        if (waitingPlayers.ContainsKey(id))
        {
            Destroy(waitingPlayers[id].gameObject);
            waitingPlayers.Remove(id);
        }
        if (waitingTeamAPlayers.ContainsKey(id))
        {
            Destroy(waitingTeamAPlayers[id].gameObject);
            waitingTeamAPlayers.Remove(id);
        }
        if (waitingTeamBPlayers.ContainsKey(id))
        {
            Destroy(waitingTeamBPlayers[id].gameObject);
            waitingTeamBPlayers.Remove(id);
        }
    }

    private void CreatePlayerUI(PhotonPlayer player)
    {
        int key = player.ID;
        var newEntry = Instantiate(waitingPlayerPrefab, waitingPlayerListContainer);
        newEntry.SetData(this, player);
        newEntry.gameObject.SetActive(true);
        waitingPlayers.Add(key, newEntry);


    }

    private void OnPlayerConnectedCallback(PhotonPlayer player)
    {
        UpdateRoomData();
        int key = player.ID;
        DestroyPlayerUI(key);
        CreatePlayerUI(player);
    }

    private void OnPlayerDisconnectedCallback(PhotonPlayer player)
    {
        UpdateRoomData();
        int key = player.ID;
        DestroyPlayerUI(key);
    }

    private void OnPlayerPropertiesChangedCallback(object[] playerAndUpdatedProps)
    {
        // TODO: Under testing
        foreach (var playerAndUpdatedProp in playerAndUpdatedProps)
        {
            Debug.LogError(playerAndUpdatedProp.ToString());
        }
    }

    private void OnCustomRoomPropertiesChangedCallback(Hashtable propertiesThatChanged)
    {
        UpdateRoomData();
    }
}
