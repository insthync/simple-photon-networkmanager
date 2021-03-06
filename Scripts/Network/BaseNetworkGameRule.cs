﻿using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public abstract class BaseNetworkGameRule : ScriptableObject
{
    public const string MatchTimeCountdownKey = "rCD";
    public const string BotAddedKey = "rBA";
    public const string IsMatchEndedKey = "rMN";
    public const string BotCountKey = "rBC";
    public const string MatchTimeKey = "rMT";
    public const string MatchKillKey = "rMK";
    public const string MatchScoreKey = "rMS";
    public const string TeamScoreAKey = "tSA";
    public const string TeamScoreBKey = "tSB";
    public const string TeamKillAKey = "tKA";
    public const string TeamKillBKey = "tKB";

    [SerializeField]
    private string title;
    [SerializeField, TextArea]
    private string description;
    [SerializeField]
    private int defaultBotCount;
    [HideInInspector]
    public int botCount;
    [SerializeField, Tooltip("Time in seconds, 0 = Unlimit")]
    private int defaultMatchTime;
    [HideInInspector]
    public int matchTime;
    [SerializeField, Tooltip("Match kill limit, 0 = Unlimit")]
    private int defaultMatchKill;
    [HideInInspector]
    public int matchKill;
    [SerializeField, Tooltip("Match score limit, 0 = Unlimit")]
    private int defaultMatchScore;
    [HideInInspector]
    public int matchScore;
    protected BaseNetworkGameManager networkManager;
    public string Title { get { return title; } }
    public string Description { get { return description; } }
    protected abstract BaseNetworkGameCharacter NewBot();
    protected abstract void EndMatch();
    public int DefaultBotCount { get { return defaultBotCount; } }
    public int DefaultMatchTime { get { return defaultMatchTime; } }
    public int DefaultMatchKill { get { return defaultMatchKill; } }
    public int DefaultMatchScore { get { return defaultMatchScore; } }
    public virtual bool HasOptionBotCount { get { return false; } }
    public virtual bool HasOptionMatchTime { get { return false; } }
    public virtual bool HasOptionMatchKill { get { return false; } }
    public virtual bool HasOptionMatchScore { get { return false; } }
    public virtual bool IsTeamGameplay { get { return false; } }
    public virtual bool ShowZeroScoreWhenDead { get { return false; } }
    public virtual bool ShowZeroKillCountWhenDead { get { return false; } }
    public virtual bool ShowZeroAssistCountWhenDead { get { return false; } }
    public virtual bool ShowZeroDieCountWhenDead { get { return false; ; } }
    public abstract bool CanCharacterRespawn(BaseNetworkGameCharacter character, params object[] extraParams);
    public abstract bool RespawnCharacter(BaseNetworkGameCharacter character, params object[] extraParams);

    protected readonly List<BaseNetworkGameCharacter> Bots = new List<BaseNetworkGameCharacter>();
    protected readonly Dictionary<int, int> CharacterCollectedScore = new Dictionary<int, int>();
    protected readonly Dictionary<int, int> CharacterCollectedKill = new Dictionary<int, int>();

    public float RemainsMatchTime
    {
        get
        {
            if (HasOptionMatchTime && MatchTime > 0 && MatchTimeCountdown > 0 && !IsMatchEnded)
                return MatchTimeCountdown;
            return 0f;
        }
    }

    public float MatchTimeCountdown
    {
        get { try { return (float)PhotonNetwork.room.CustomProperties[MatchTimeCountdownKey]; } catch { } return 0f; }
        protected set { if (PhotonNetwork.isMasterClient) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { MatchTimeCountdownKey, value } }); }
    }
    public bool IsBotAdded
    {
        get { try { return (bool)PhotonNetwork.room.CustomProperties[BotAddedKey]; } catch { } return false; }
        protected set { if (PhotonNetwork.isMasterClient) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { BotAddedKey, value } }); }
    }
    public bool IsMatchEnded
    {
        get { try { return (bool)PhotonNetwork.room.CustomProperties[IsMatchEndedKey]; } catch { } return false; }
        protected set { if (PhotonNetwork.isMasterClient) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { IsMatchEndedKey, value } }); }
    }
    public int BotCount
    {
        get { try { return (int)PhotonNetwork.room.CustomProperties[BotCountKey]; } catch { } return 0; }
        protected set { if (PhotonNetwork.isMasterClient) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { BotCountKey, value } }); }
    }
    public int MatchTime
    {
        get { try { return (int)PhotonNetwork.room.CustomProperties[MatchTimeKey]; } catch { } return 0; }
        protected set { if (PhotonNetwork.isMasterClient) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { MatchTimeKey, value } }); }
    }
    public int MatchKill
    {
        get { try { return (int)PhotonNetwork.room.CustomProperties[MatchKillKey]; } catch { } return 0; }
        protected set { if (PhotonNetwork.isMasterClient) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { MatchKillKey, value } }); }
    }
    public int MatchScore
    {
        get { try { return (int)PhotonNetwork.room.CustomProperties[MatchScoreKey]; } catch { } return 0; }
        protected set { if (PhotonNetwork.isMasterClient) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { MatchScoreKey, value } }); }
    }
    public int TeamScoreA
    {
        get { try { return (int)PhotonNetwork.room.CustomProperties[TeamScoreAKey]; } catch { } return 0; }
        protected set { if (PhotonNetwork.isMasterClient) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { TeamScoreAKey, value } }); }
    }
    public int TeamScoreB
    {
        get { try { return (int)PhotonNetwork.room.CustomProperties[TeamScoreBKey]; } catch { } return 0; }
        protected set { if (PhotonNetwork.isMasterClient) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { TeamScoreBKey, value } }); }
    }
    public int TeamKillA
    {
        get { try { return (int)PhotonNetwork.room.CustomProperties[TeamKillAKey]; } catch { } return 0; }
        protected set { if (PhotonNetwork.isMasterClient) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { TeamKillAKey, value } }); }
    }
    public int TeamKillB
    {
        get { try { return (int)PhotonNetwork.room.CustomProperties[TeamKillBKey]; } catch { } return 0; }
        protected set { if (PhotonNetwork.isMasterClient) PhotonNetwork.room.SetCustomProperties(new Hashtable() { { TeamKillBKey, value } }); }
    }

    private float matchTimeReduceTimer;
    private PunTeams.Team tempTeam;

    public virtual void AddBots()
    {
        if (!HasOptionBotCount)
            return;
        int addAmount = BotCount;
        Bots.Clear();
        // Adjust bot count
        int maxPlayers = PhotonNetwork.room.MaxPlayers;
        if (networkManager.isConnectOffline)
            maxPlayers = networkManager.maxConnections;
        if (PhotonNetwork.room.PlayerCount + addAmount > maxPlayers)
            addAmount = maxPlayers - PhotonNetwork.room.PlayerCount;
        for (var i = 0; i < addAmount; ++i)
        {
            var character = NewBot();
            if (IsTeamGameplay)
                character.playerTeam = tempTeam = (tempTeam == PunTeams.Team.red ? PunTeams.Team.blue : PunTeams.Team.red);
            networkManager.RegisterCharacter(character);
            Bots.Add(character);
        }
    }

    public virtual void AdjustBots()
    {
        if (!HasOptionBotCount)
            return;
        // Add bots if needed
        int maxPlayers = PhotonNetwork.room.MaxPlayers;
        if (networkManager.isConnectOffline)
            maxPlayers = networkManager.maxConnections;
        if (Bots.Count < BotCount && PhotonNetwork.room.PlayerCount + Bots.Count < maxPlayers)
        {
            int addAmount = BotCount;
            // Adjust bot count
            if (PhotonNetwork.room.PlayerCount + addAmount > maxPlayers)
                addAmount = maxPlayers - PhotonNetwork.room.PlayerCount;
            for (var i = 0; i < addAmount; ++i)
            {
                var character = NewBot();
                if (IsTeamGameplay)
                    character.playerTeam = tempTeam = (tempTeam == PunTeams.Team.red ? PunTeams.Team.blue : PunTeams.Team.red);
                networkManager.RegisterCharacter(character);
                Bots.Add(character);
            }
        }
        // Remove bots if needed
        while (PhotonNetwork.room.PlayerCount + Bots.Count > maxPlayers)
        {
            int index = Bots.Count - 1;
            BaseNetworkGameCharacter botCharacter = Bots[index];
            PhotonNetwork.Destroy(botCharacter.photonView);
            Bots.RemoveAt(index);
        }
    }

    public virtual void OnStartServer(BaseNetworkGameManager manager)
    {
        networkManager = manager;
        BotCount = botCount;
        MatchTime = matchTime;
        MatchKill = matchKill;
        MatchScore = matchScore;
        MatchTimeCountdown = MatchTime;
        IsBotAdded = false;
        IsMatchEnded = false;
    }

    public virtual void OnStopConnection(BaseNetworkGameManager manager)
    {

    }

    public virtual void OnMasterChange(BaseNetworkGameManager manager)
    {
        networkManager = manager;
    }

    public virtual void OnUpdate()
    {
        if (!IsBotAdded)
        {
            AddBots();
            IsBotAdded = true;
        }

        // Make match time reduce every seconds (not every loops)
        matchTimeReduceTimer += Time.unscaledDeltaTime;
        if (matchTimeReduceTimer >= 1)
        {
            matchTimeReduceTimer = 0;
            MatchTimeCountdown -= 1f;
        }

        if (HasOptionMatchTime && MatchTime > 0 && MatchTimeCountdown <= 0 && !IsMatchEnded)
        {
            IsMatchEnded = true;
            EndMatch();
        }
    }

    public virtual void OnScoreIncrease(BaseNetworkGameCharacter character, int increaseAmount)
    {
        if (!CharacterCollectedScore.ContainsKey(character.photonView.viewID))
            CharacterCollectedScore[character.photonView.viewID] = increaseAmount;
        else
            CharacterCollectedScore[character.photonView.viewID] += increaseAmount;

        if (IsTeamGameplay)
        {
            switch (character.playerTeam)
            {
                case PunTeams.Team.red:
                    TeamScoreA += increaseAmount;
                    break;
                case PunTeams.Team.blue:
                    TeamScoreB += increaseAmount;
                    break;
            }
        }
    }

    public virtual void OnKillIncrease(BaseNetworkGameCharacter character, int increaseAmount)
    {
        if (!CharacterCollectedKill.ContainsKey(character.photonView.viewID))
            CharacterCollectedKill[character.photonView.viewID] = increaseAmount;
        else
            CharacterCollectedKill[character.photonView.viewID] += increaseAmount;

        if (IsTeamGameplay)
        {
            switch (character.playerTeam)
            {
                case PunTeams.Team.red:
                    TeamKillA += increaseAmount;
                    break;
                case PunTeams.Team.blue:
                    TeamKillB += increaseAmount;
                    break;
            }
        }
    }

    public virtual void OnUpdateCharacter(BaseNetworkGameCharacter character)
    {
        int checkScore = character.Score;
        int checkKill = character.KillCount;
        if (IsTeamGameplay)
        {
            // Use team score / kill as checker
            switch (character.playerTeam)
            {
                case PunTeams.Team.red:
                    checkScore = TeamScoreA;
                    checkKill = TeamKillA;
                    break;
                case PunTeams.Team.blue:
                    checkScore = TeamScoreB;
                    checkKill = TeamKillB;
                    break;
            }
        }

        if (HasOptionMatchScore && MatchScore > 0 && checkScore >= MatchScore)
        {
            IsMatchEnded = true;
            EndMatch();
        }

        if (HasOptionMatchKill && MatchKill > 0 && checkKill >= MatchKill)
        {
            IsMatchEnded = true;
            EndMatch();
        }
    }

    public abstract void InitialClientObjects();
}
