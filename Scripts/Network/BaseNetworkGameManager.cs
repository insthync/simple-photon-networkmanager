﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;

public abstract class BaseNetworkGameManager : SimplePhotonNetworkManager
{
    public const string CUSTOM_ROOM_GAME_RULE = "G";
    public static event System.Action<DisconnectCause> onClientError;

    public BaseNetworkGameRule gameRule;
    protected float updateScoreTime;
    protected float updateMatchTime;
    protected bool startUpdateGameRule;
    public readonly List<BaseNetworkGameCharacter> Characters = new List<BaseNetworkGameCharacter>();
    public float RemainsMatchTime { get; protected set; }
    public bool IsMatchEnded { get; protected set; }
    public float MatchEndedAt { get; protected set; }

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

    protected override void Update()
    {
        base.Update();
        if (PhotonNetwork.isMasterClient)
            ServerUpdate();
        if (PhotonNetwork.isNonMasterClientInRoom)
            ClientUpdate();
    }

    protected virtual void ServerUpdate()
    {
        if (gameRule != null && startUpdateGameRule)
            gameRule.OnUpdate();

        if (Time.unscaledTime - updateScoreTime >= 1f)
        {
            photonView.RPC("RpcUpdateScores", PhotonTargets.All, GetSortedScores());
            updateScoreTime = Time.unscaledTime;
        }

        if (gameRule != null && Time.unscaledTime - updateMatchTime >= 1f)
        {
            RemainsMatchTime = gameRule.RemainsMatchTime;
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
            ranking.score = character.Score;
            ranking.killCount = character.KillCount;
            ranking.assistCount = character.AssistCount;
            ranking.dieCount = character.DieCount;
            scores[i] = ranking;
        }
        return scores;
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

    public void OnUpdateCharacter(BaseNetworkGameCharacter character)
    {
        if (gameRule != null)
            gameRule.OnUpdateCharacter(character);
    }

    public override void OnFailedToConnectToPhoton(DisconnectCause cause)
    {
        if (onClientError != null)
            onClientError(cause);
    }
    
    public override void OnConnectionFail(DisconnectCause cause)
    {
        if (onClientError != null)
            onClientError(cause);
    }

    public override void OnCreatedRoom()
    {
        ResetGame();
        var customProperties = PhotonNetwork.room.CustomProperties;
        customProperties[CUSTOM_ROOM_GAME_RULE] = gameRule == null ? "" : gameRule.name;
        PhotonNetwork.room.SetCustomProperties(customProperties);
        base.OnCreatedRoom();
    }

    public override void OnJoinedRoom()
    {
        ResetGame();
        base.OnJoinedRoom();
    }

    public override void OnMasterClientSwitched(PhotonPlayer newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);

        Characters.Clear();
        var characters = FindObjectsOfType<BaseNetworkGameCharacter>();
        foreach (var character in characters)
        {
            Characters.Add(character);
        }
        startUpdateGameRule = true;
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        if (!startUpdateGameRule)
        {
            startUpdateGameRule = true;
            var customProperties = PhotonNetwork.room.CustomProperties;
            var gameRuleName = (string)PhotonNetwork.room.CustomProperties[CUSTOM_ROOM_GAME_RULE];
            BaseNetworkGameRule foundGameRule;
            if (BaseNetworkGameInstance.GameRules.TryGetValue(gameRuleName, out foundGameRule))
            {
                gameRule = foundGameRule;
                gameRule.InitialClientObjects();
            }
        }

        photonView.RPC("RpcUpdateScores", newPlayer, GetSortedScores());
        if (gameRule != null)
            photonView.RPC("RpcMatchStatus", newPlayer, gameRule.RemainsMatchTime, gameRule.IsMatchEnded);
    }

    [PunRPC]
    protected void RpcUpdateScores(NetworkGameScore[] scores)
    {
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
}
