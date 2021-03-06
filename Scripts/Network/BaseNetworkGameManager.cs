﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[RequireComponent(typeof(PunTeams))]
public abstract class BaseNetworkGameManager : SimplePhotonNetworkManager
{
    public static new BaseNetworkGameManager Singleton
    {
        get { return SimplePhotonNetworkManager.Singleton as BaseNetworkGameManager; }
    }

    public PunTeams Teams { get; private set; }

    public const string CUSTOM_ROOM_GAME_RULE = "G";
    public const string CUSTOM_ROOM_GAME_RULE_BOT_COUNT = "Gbc";
    public const string CUSTOM_ROOM_GAME_RULE_MATCH_TIME = "Gmt";
    public const string CUSTOM_ROOM_GAME_RULE_MATCH_KILL = "Gmk";
    public const string CUSTOM_ROOM_GAME_RULE_MATCH_SCORE = "Gms";

    public BaseNetworkGameRule gameRule;
    protected float updateScoreTime;
    protected float updateMatchTime;
    protected bool startUpdateGameRule;
    public readonly List<BaseNetworkGameCharacter> Characters = new List<BaseNetworkGameCharacter>();
    public float RemainsMatchTime { get; protected set; }
    public bool IsMatchEnded { get; protected set; }
    public float MatchEndedAt { get; protected set; }

    private void Start()
    {
        // Setup required components
        if (Teams == null)
            Teams = GetComponent<PunTeams>();
        if (Teams == null)
            Teams = gameObject.AddComponent<PunTeams>();
    }

    public int CountAliveCharacters()
    {
        var count = 0;
        foreach (var character in Characters)
        {
            if (character == null)
                continue;
            if (!character.IsDead)
                ++count;
        }
        return count;
    }

    public int CountDeadCharacters()
    {
        var count = 0;
        foreach (var character in Characters)
        {
            if (character == null)
                continue;
            if (character.IsDead)
                ++count;
        }
        return count;
    }

    public override void LeaveRoom()
    {
        if (gameRule != null)
            gameRule.OnStopConnection(this);
        base.LeaveRoom();
    }

    public override void Disconnect()
    {
        if (gameRule != null)
            gameRule.OnStopConnection(this);
        base.Disconnect();
    }

    protected override void Update()
    {
        base.Update();
        if (PhotonNetwork.isMasterClient)
            ServerUpdate();
        if (PhotonNetwork.isNonMasterClientInRoom)
            ClientUpdate();
    }

    protected override string[] GetCustomRoomPropertiesForLobby()
    {
        return new string[]
        {
            CUSTOM_ROOM_ROOM_NAME,
            CUSTOM_ROOM_ROOM_PASSWORD,
            CUSTOM_ROOM_PLAYER_ID,
            CUSTOM_ROOM_PLAYER_NAME,
            CUSTOM_ROOM_SCENE_NAME,
            CUSTOM_ROOM_MATCH_MAKE,
            CUSTOM_ROOM_STATE,
            CUSTOM_ROOM_GAME_RULE,
            CUSTOM_ROOM_GAME_RULE_BOT_COUNT,
            CUSTOM_ROOM_GAME_RULE_MATCH_TIME,
            CUSTOM_ROOM_GAME_RULE_MATCH_KILL,
            CUSTOM_ROOM_GAME_RULE_MATCH_SCORE,
        };
    }

    public override void OnReceivedRoomListUpdate()
    {
        var rooms = PhotonNetwork.GetRoomList();
        var foundRooms = new List<NetworkDiscoveryData>();
        foreach (var room in rooms)
        {
            var customProperties = room.CustomProperties;
            if (customProperties.Count == 0)
                continue;
            var isMatchMaking = (bool)customProperties[CUSTOM_ROOM_MATCH_MAKE];
            if (!isMatchMaking)
            {
                var discoveryData = new NetworkDiscoveryData();
                discoveryData.name = room.Name;
                discoveryData.roomName = (string)customProperties[CUSTOM_ROOM_ROOM_NAME];
                discoveryData.roomPassword = (string)customProperties[CUSTOM_ROOM_ROOM_PASSWORD];
                discoveryData.playerId = (string)customProperties[CUSTOM_ROOM_PLAYER_ID];
                discoveryData.playerName = (string)customProperties[CUSTOM_ROOM_PLAYER_NAME];
                discoveryData.sceneName = (string)customProperties[CUSTOM_ROOM_SCENE_NAME];
                discoveryData.state = (byte)customProperties[CUSTOM_ROOM_STATE];
                discoveryData.numPlayers = room.PlayerCount;
                discoveryData.maxPlayers = room.MaxPlayers;
                discoveryData.gameRule = (string)customProperties[CUSTOM_ROOM_GAME_RULE];
                discoveryData.botCount = (int)customProperties[CUSTOM_ROOM_GAME_RULE_BOT_COUNT];
                discoveryData.matchTime = (int)customProperties[CUSTOM_ROOM_GAME_RULE_MATCH_TIME];
                discoveryData.matchKill = (int)customProperties[CUSTOM_ROOM_GAME_RULE_MATCH_KILL];
                discoveryData.matchScore = (int)customProperties[CUSTOM_ROOM_GAME_RULE_MATCH_SCORE];
                foundRooms.Add(discoveryData);
            }
        }
        if (onReceivedRoomListUpdate != null)
            onReceivedRoomListUpdate.Invoke(foundRooms);
    }

    protected virtual void ServerUpdate()
    {
        if (GetRoomState() == RoomState.Waiting)
            return;

        if (gameRule != null && startUpdateGameRule)
            gameRule.OnUpdate();

        if (Time.unscaledTime - updateScoreTime >= 1f)
        {
            if (gameRule != null && !gameRule.IsMatchEnded)
            {
                int length = 0;
                List<object> objects;
                GetSortedScoresAsObjects(out length, out objects);
                if (isConnectOffline)
                    RpcUpdateScores(length, objects.ToArray());
                else
                    photonView.RPC("RpcUpdateScores", PhotonTargets.All, length, objects.ToArray());
            }
            updateScoreTime = Time.unscaledTime;
        }

        if (gameRule != null && Time.unscaledTime - updateMatchTime >= 1f)
        {
            RemainsMatchTime = gameRule.RemainsMatchTime;
            if (isConnectOffline)
                RpcMatchStatus(gameRule.RemainsMatchTime, gameRule.IsMatchEnded);
            else
                photonView.RPC("RpcMatchStatus", PhotonTargets.All, gameRule.RemainsMatchTime, gameRule.IsMatchEnded);

            if (!IsMatchEnded && gameRule.IsMatchEnded)
            {
                IsMatchEnded = true;
                MatchEndedAt = Time.unscaledTime;
            }

            updateMatchTime = Time.unscaledTime;
        }
    }

    protected virtual void ClientUpdate()
    {

    }

    public void SendKillNotify(string killerName, string victimName, string weaponId)
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        photonView.RPC("RpcKillNotify", PhotonTargets.All, killerName, victimName, weaponId);
    }

    public NetworkGameScore[] GetSortedScores()
    {
        for (var i = Characters.Count - 1; i >= 0; --i)
        {
            var character = Characters[i];
            if (character == null)
                Characters.RemoveAt(i);
        }
        Characters.Sort();
        var scores = new NetworkGameScore[Characters.Count];
        for (var i = 0; i < Characters.Count; ++i)
        {
            var character = Characters[i];
            var ranking = new NetworkGameScore();
            ranking.viewId = character.photonView.viewID;
            ranking.playerName = character.playerName;
            ranking.team = character.playerTeam;
            ranking.score = character.Score;
            ranking.killCount = character.KillCount;
            ranking.assistCount = character.AssistCount;
            ranking.dieCount = character.DieCount;
            scores[i] = ranking;
        }
        return scores;
    }

    public void GetSortedScoresAsObjects(out int length, out List<object> objects)
    {
        length = 0;
        objects = new List<object>();
        var sortedScores = GetSortedScores();
        length = (sortedScores.Length);
        foreach (var sortedScore in sortedScores)
        {
            objects.Add(sortedScore.viewId);
            objects.Add(sortedScore.playerName);
            objects.Add(sortedScore.team);
            objects.Add(sortedScore.score);
            objects.Add(sortedScore.killCount);
            objects.Add(sortedScore.assistCount);
            objects.Add(sortedScore.dieCount);
        }
    }

    public void RegisterCharacter(BaseNetworkGameCharacter character)
    {
        if (character == null || Characters.Contains(character))
            return;
        character.RegisterNetworkGameManager(this);
        Characters.Add(character);
    }

    public bool CanCharacterRespawn(BaseNetworkGameCharacter character, params object[] extraParams)
    {
        if (gameRule != null)
            return gameRule.CanCharacterRespawn(character, extraParams);
        return true;
    }

    public bool RespawnCharacter(BaseNetworkGameCharacter character, params object[] extraParams)
    {
        if (gameRule != null)
            return gameRule.RespawnCharacter(character, extraParams);
        return true;
    }

    public virtual void OnScoreIncrease(BaseNetworkGameCharacter character, int increaseAmount)
    {
        if (gameRule != null)
            gameRule.OnScoreIncrease(character, increaseAmount);
    }

    public virtual void OnKillIncrease(BaseNetworkGameCharacter character, int increaseAmount)
    {
        if (gameRule != null)
            gameRule.OnKillIncrease(character, increaseAmount);
    }

    public virtual void OnUpdateCharacter(BaseNetworkGameCharacter character)
    {
        if (gameRule != null)
            gameRule.OnUpdateCharacter(character);
    }

    public void SetGameRule(BaseNetworkGameRule gameRule)
    {
        // If room not created, set data to field to use later
        this.gameRule = gameRule;
        if (PhotonNetwork.inRoom && PhotonNetwork.isMasterClient)
        {
            Hashtable customProperties = new Hashtable();
            customProperties[CUSTOM_ROOM_GAME_RULE] = gameRule == null ? "" : gameRule.name;
            customProperties[CUSTOM_ROOM_GAME_RULE_BOT_COUNT] = gameRule == null ? 0 : gameRule.botCount;
            customProperties[CUSTOM_ROOM_GAME_RULE_MATCH_TIME] = gameRule == null ? 0 : gameRule.matchTime;
            customProperties[CUSTOM_ROOM_GAME_RULE_MATCH_KILL] = gameRule == null ? 0 : gameRule.matchKill;
            customProperties[CUSTOM_ROOM_GAME_RULE_MATCH_SCORE] = gameRule == null ? 0 : gameRule.matchScore;
            PhotonNetwork.room.SetCustomProperties(customProperties);

            foreach (var player in PhotonNetwork.playerList)
            {
                SetPlayerTeam(player);
            }
        }
    }

    public override void OnCreatedRoom()
    {
        // Reset last game/match data
        ResetGame();
        Hashtable customProperties = new Hashtable();
        customProperties[CUSTOM_ROOM_GAME_RULE] = gameRule == null ? "" : gameRule.name;
        customProperties[CUSTOM_ROOM_GAME_RULE_BOT_COUNT] = gameRule == null ? 0 : gameRule.botCount;
        customProperties[CUSTOM_ROOM_GAME_RULE_MATCH_TIME] = gameRule == null ? 0 : gameRule.matchTime;
        customProperties[CUSTOM_ROOM_GAME_RULE_MATCH_KILL] = gameRule == null ? 0 : gameRule.matchKill;
        customProperties[CUSTOM_ROOM_GAME_RULE_MATCH_SCORE] = gameRule == null ? 0 : gameRule.matchScore;
        PhotonNetwork.room.SetCustomProperties(customProperties);
        base.OnCreatedRoom();
    }

    public override void OnOnlineSceneChanged()
    {
        if (isLog) Debug.Log("OnOnlineSceneChanged");
        // Reset last game/match data
        ResetGame();
        // Get game rule to initial client objects
        var customProperties = PhotonNetwork.room.CustomProperties;
        var gameRuleName = (string)customProperties[CUSTOM_ROOM_GAME_RULE];
        BaseNetworkGameRule foundGameRule;
        if (BaseNetworkGameInstance.GameRules.TryGetValue(gameRuleName, out foundGameRule))
        {
            gameRule = foundGameRule;
            gameRule.InitialClientObjects();
            if (PhotonNetwork.isMasterClient && !startUpdateGameRule)
            {
                startUpdateGameRule = true;
                gameRule.OnStartServer(this);
            }
        }
    }

    public override void OnMasterClientSwitched(PhotonPlayer newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);
        if (GetRoomState() == RoomState.Playing)
        {
            Characters.Clear();
            var characters = FindObjectsOfType<BaseNetworkGameCharacter>();
            foreach (var character in characters)
            {
                Characters.Add(character);
            }
            if (gameRule != null)
                gameRule.OnMasterChange(this);
        }
        startUpdateGameRule = true;
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.isMasterClient)
            SetPlayerTeam(PhotonNetwork.player);
        base.OnJoinedRoom();
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        if (PhotonNetwork.isMasterClient)
        {
            if (GetRoomState() == RoomState.Playing && gameRule != null && !gameRule.IsMatchEnded)
            {
                int length = 0;
                List<object> objects;
                GetSortedScoresAsObjects(out length, out objects);
                photonView.RPC("RpcUpdateScores", newPlayer, length, objects.ToArray());
                if (gameRule != null)
                    photonView.RPC("RpcMatchStatus", newPlayer, gameRule.RemainsMatchTime, gameRule.IsMatchEnded);
                // Adjust bots
                gameRule.AdjustBots();
            }
            SetPlayerTeam(newPlayer);
        }
        base.OnPhotonPlayerConnected(newPlayer);
    }

    public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        base.OnPhotonPlayerDisconnected(otherPlayer);
        if (PhotonNetwork.isMasterClient)
        {
            if (GetRoomState() == RoomState.Playing && gameRule != null && !gameRule.IsMatchEnded)
            {
                // Adjust bots
                gameRule.AdjustBots();
            }
        }
    }

    protected void SetPlayerTeam(PhotonPlayer player)
    {
        bool isTeamGameplay = gameRule != null && gameRule.IsTeamGameplay;
        if (!isTeamGameplay)
            player.SetTeam(PunTeams.Team.none);
        else
        {
            if (PunTeams.PlayersPerTeam[PunTeams.Team.red].Count > PunTeams.PlayersPerTeam[PunTeams.Team.blue].Count)
                player.SetTeam(PunTeams.Team.blue);
            else
                player.SetTeam(PunTeams.Team.red);
        }
    }

    public void ChangePlayerTeam()
    {
        photonView.RPC("RpcChangePlayerTeam", PhotonTargets.MasterClient, PhotonNetwork.player.UserId);
    }

    [PunRPC]
    protected void RpcUpdateScores(int length, object[] objects)
    {
        if (length == 0 || objects == null)
            return;
        var scores = new NetworkGameScore[length];
        var j = 0;
        for (var i = 0; i < length; ++i)
        {
            var score = new NetworkGameScore();
            score.viewId = (int)objects[j++];
            score.playerName = (string)objects[j++];
            score.team = (PunTeams.Team)objects[j++];
            score.score = (int)objects[j++];
            score.killCount = (int)objects[j++];
            score.assistCount = (int)objects[j++];
            score.dieCount = (int)objects[j++];
            scores[i] = score;
        }
        UpdateScores(scores);
    }

    [PunRPC]
    protected void RpcMatchStatus(float remainsMatchTime, bool isMatchEnded)
    {
        RemainsMatchTime = remainsMatchTime;
        if (!IsMatchEnded && isMatchEnded)
        {
            IsMatchEnded = true;
            MatchEndedAt = Time.unscaledTime;
        }
    }

    [PunRPC]
    protected void RpcChangePlayerTeam(string id)
    {
        PhotonPlayer foundPlayer = GetPlayerById(id);
        if (foundPlayer != null)
        {
            bool isTeamGameplay = gameRule != null && gameRule.IsTeamGameplay;
            if (!isTeamGameplay)
                foundPlayer.SetTeam(PunTeams.Team.none);
            else
            {
                var maxPlayerEachTeam = PhotonNetwork.room.MaxPlayers / 2;
                switch (foundPlayer.GetTeam())
                {
                    case PunTeams.Team.red:
                        if (PunTeams.PlayersPerTeam[PunTeams.Team.blue].Count < maxPlayerEachTeam)
                            foundPlayer.SetTeam(PunTeams.Team.blue);
                        break;
                    default:
                        if (PunTeams.PlayersPerTeam[PunTeams.Team.red].Count < maxPlayerEachTeam)
                            foundPlayer.SetTeam(PunTeams.Team.red);
                        break;
                }
            }
        }
    }

    [PunRPC]
    protected void RpcKillNotify(string killerName, string victimName, string weaponId)
    {
        KillNotify(killerName, victimName, weaponId);
    }

    protected void ResetGame()
    {
        Characters.Clear();
        updateScoreTime = 0f;
        updateMatchTime = 0f;
        RemainsMatchTime = 0f;
        IsMatchEnded = false;
        MatchEndedAt = 0f;
        startUpdateGameRule = false;
    }

    protected abstract void UpdateScores(NetworkGameScore[] scores);
    protected abstract void KillNotify(string killerName, string victimName, string weaponId);
}
