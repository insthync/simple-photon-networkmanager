using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using PlayerState = SimplePhotonNetworkManager.PlayerState;

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
    public bool hostAlwaysReady = true;
    public int autoStartWhenPlayersReadyAtLeast = 0;
    public int canStartWhenPlayersReadyAtLeast = 0;
    public int HostPlayerID { get; private set; }

    private readonly Dictionary<int, PhotonPlayer> players = new Dictionary<int, PhotonPlayer>();
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
        SimplePhotonNetworkManager.onMasterClientSwitched += OnMasterClientSwitchedCallback;
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
        SimplePhotonNetworkManager.onMasterClientSwitched -= OnMasterClientSwitchedCallback;
    }

    private void OnDestroy()
    {
        SimplePhotonNetworkManager.onJoinedRoom -= OnJoinedRoomCallback;
        SimplePhotonNetworkManager.onPlayerConnected -= OnPlayerConnectedCallback;
        SimplePhotonNetworkManager.onPlayerDisconnected -= OnPlayerDisconnectedCallback;
        SimplePhotonNetworkManager.onPlayerPropertiesChanged -= OnPlayerPropertiesChangedCallback;
        SimplePhotonNetworkManager.onCustomRoomPropertiesChanged -= OnCustomRoomPropertiesChangedCallback;
        SimplePhotonNetworkManager.onMasterClientSwitched -= OnMasterClientSwitchedCallback;
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

        waitingPlayerListRoot.SetActive(!gameRule.IsTeamGameplay);
        waitingPlayerTeamAListRoot.SetActive(gameRule.IsTeamGameplay);
        waitingPlayerTeamBListRoot.SetActive(gameRule.IsTeamGameplay);

        object botCountObject;
        if (textBotCount != null &&
            customProperties.TryGetValue(BaseNetworkGameManager.CUSTOM_ROOM_GAME_RULE_BOT_COUNT, out botCountObject))
        {
            textBotCount.text = ((int)botCountObject).ToString("N0");
            textBotCount.gameObject.SetActive(gameRule != null && gameRule.HasOptionBotCount);
        }

        object matchTimeObject;
        if (textMatchTime != null &&
            customProperties.TryGetValue(BaseNetworkGameManager.CUSTOM_ROOM_GAME_RULE_MATCH_TIME, out matchTimeObject))
        {
            textMatchTime.text = ((int)matchTimeObject).ToString("N0");
            textMatchTime.gameObject.SetActive(gameRule != null && gameRule.HasOptionMatchTime);
        }

        object matchKillObject;
        if (textMatchKill != null &&
            customProperties.TryGetValue(BaseNetworkGameManager.CUSTOM_ROOM_GAME_RULE_MATCH_KILL, out matchKillObject))
        {
            textMatchKill.text = ((int)matchKillObject).ToString("N0");
            textMatchKill.gameObject.SetActive(gameRule != null && gameRule.HasOptionMatchKill);
        }

        object matchScoreObject;
        if (textMatchScore != null &&
            customProperties.TryGetValue(BaseNetworkGameManager.CUSTOM_ROOM_GAME_RULE_MATCH_SCORE,
                out matchScoreObject))
        {
            textMatchScore.text = ((int)matchScoreObject).ToString("N0");
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
        if (SimplePhotonNetworkManager.Singleton.CountPlayerWithState(PlayerState.Ready) >= canStartWhenPlayersReadyAtLeast)
            SimplePhotonNetworkManager.Singleton.StartGame();
    }

    public virtual void OnClickReady()
    {
        SimplePhotonNetworkManager.Singleton.TogglePlayerState();
    }

    public virtual void OnClickChangeTeam()
    {
        BaseNetworkGameManager.Singleton.ChangePlayerTeam();
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
        players.Clear();
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
        if (PhotonNetwork.player.IsMasterClient && hostAlwaysReady)
            SimplePhotonNetworkManager.Singleton.SetPlayerState(PlayerState.Ready);
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
        players.Remove(id);
    }

    private void CreatePlayerUI(PhotonPlayer player)
    {
        int key = player.ID;
        DestroyPlayerUI(key);

        PunTeams.Team team = player.GetTeam();
        Transform container = waitingPlayerListContainer;
        Dictionary<int, UIPhotonWaitingPlayer> uiDict = waitingPlayers;
        switch (team)
        {
            case PunTeams.Team.red:
                container = waitingPlayerTeamAListContainer;
                uiDict = waitingTeamAPlayers;
                break;
            case PunTeams.Team.blue:
                container = waitingPlayerTeamBListContainer;
                uiDict = waitingTeamBPlayers;
                break;
        }
        UIPhotonWaitingPlayer newEntry = Instantiate(waitingPlayerPrefab, container);
        newEntry.SetData(this, player);
        newEntry.gameObject.SetActive(true);
        uiDict.Add(key, newEntry);

        players[player.ID] = player;
    }

    private void UpdatePlayerUI(PhotonPlayer player)
    {
        PunTeams.Team team = player.GetTeam();
        if (waitingPlayers.ContainsKey(player.ID))
        {
            if (team != PunTeams.Team.none)
            {
                // If player team changed, recreate waiting player UI
                CreatePlayerUI(player);
                return;
            }
            waitingPlayers[player.ID].SetData(this, player);
        }
        if (waitingTeamAPlayers.ContainsKey(player.ID))
        {
            if (team != PunTeams.Team.red)
            {
                // If player team changed, recreate waiting player UI
                CreatePlayerUI(player);
                return;
            }
            waitingTeamAPlayers[player.ID].SetData(this, player);
        }
        if (waitingTeamBPlayers.ContainsKey(player.ID))
        {
            if (team != PunTeams.Team.blue)
            {
                // If player team changed, recreate waiting player UI
                CreatePlayerUI(player);
                return;
            }
            waitingTeamBPlayers[player.ID].SetData(this, player);
        }
    }

    private void OnPlayerConnectedCallback(PhotonPlayer player)
    {
        UpdateRoomData();
        CreatePlayerUI(player);
    }

    private void OnPlayerDisconnectedCallback(PhotonPlayer player)
    {
        UpdateRoomData();
        DestroyPlayerUI(player.ID);
    }

    private void OnPlayerPropertiesChangedCallback(PhotonPlayer player, Hashtable props)
    {
        if (players.ContainsKey(player.ID))
            UpdatePlayerUI(player);
        else
            CreatePlayerUI(player);

        if (PhotonNetwork.isMasterClient && autoStartWhenPlayersReadyAtLeast > 0 && 
            SimplePhotonNetworkManager.Singleton.CountPlayerWithState(PlayerState.Ready) >= autoStartWhenPlayersReadyAtLeast)
        {
            // Start game automatically when ready player reached `autoStartGameWhenPlayersReady` amount
            OnClickStartGame();
        }
    }

    private void OnCustomRoomPropertiesChangedCallback(Hashtable propertiesThatChanged)
    {
        var room = PhotonNetwork.room;
        var customProperties = room.CustomProperties;
        var playerId = (int)customProperties[SimplePhotonNetworkManager.CUSTOM_ROOM_PLAYER_ID];
        if (playerId != HostPlayerID)
        {
            // Update with `OnJoinedRoomCallback` to refresh all data
            OnJoinedRoomCallback();
        }
        else
            UpdateRoomData();
    }

    private void OnMasterClientSwitchedCallback(PhotonPlayer player)
    {
        if (hostAlwaysReady && player.IsLocal)
            SimplePhotonNetworkManager.Singleton.SetPlayerState(PlayerState.Ready);
    }
}
