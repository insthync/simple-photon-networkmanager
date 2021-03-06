﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;

public abstract class BaseNetworkGameCharacter : PunBehaviour, System.IComparable<BaseNetworkGameCharacter>
{
    public static BaseNetworkGameCharacter Local { get; private set; }

    public GameObject[] noTeamObjects;
    public GameObject[] teamAObjects;
    public GameObject[] teamBObjects;

    public abstract bool IsDead { get; }

    protected int _score;
    protected int _killCount;
    protected int _assistCount;
    protected int _dieCount;

    public virtual string playerName
    {
        get { return (photonView != null && photonView.owner != null) ? photonView.owner.NickName : ""; }
        set { if (photonView.isMine) photonView.owner.NickName = value; }
    }
    public virtual PunTeams.Team playerTeam
    {
        get { return (photonView != null && photonView.owner != null) ? photonView.owner.GetTeam() : PunTeams.Team.none; }
        set { if (photonView.isMine) photonView.owner.SetTeam(value); }
    }
    public int score
    {
        get { return _score; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != score)
            {
                if (value > score && NetworkManager != null)
                    NetworkManager.OnScoreIncrease(this, value - score);
                _score = value;
                photonView.RPC("RpcUpdateScore", PhotonTargets.Others, value);
            }
        }
    }
    public int killCount
    {
        get { return _killCount; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != killCount)
            {
                if (value > killCount && NetworkManager != null)
                    NetworkManager.OnKillIncrease(this, value - killCount);
                _killCount = value;
                photonView.RPC("RpcUpdateKillCount", PhotonTargets.Others, value);
            }
        }
    }
    public int assistCount
    {
        get { return _assistCount; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != assistCount)
            {
                _assistCount = value;
                photonView.RPC("RpcUpdateAssistCount", PhotonTargets.Others, value);
            }
        }
    }
    public int dieCount
    {
        get { return _dieCount; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != dieCount)
            {
                _dieCount = value;
                photonView.RPC("RpcUpdateDieCount", PhotonTargets.Others, value);
            }
        }
    }
    public int Score
    {
        get
        {
            if (IsDead && NetworkManager != null && NetworkManager.gameRule != null && NetworkManager.gameRule.ShowZeroScoreWhenDead)
                return 0;
            return score;
        }
    }
    public int KillCount
    {
        get
        {
            if (IsDead && NetworkManager != null && NetworkManager.gameRule != null && NetworkManager.gameRule.ShowZeroKillCountWhenDead)
                return 0;
            return killCount;
        }
    }
    public int AssistCount
    {
        get
        {
            if (IsDead && NetworkManager != null && NetworkManager.gameRule != null && NetworkManager.gameRule.ShowZeroAssistCountWhenDead)
                return 0;
            return assistCount;
        }
    }
    public int DieCount
    {
        get
        {
            if (IsDead && NetworkManager != null && NetworkManager.gameRule != null && NetworkManager.gameRule.ShowZeroDieCountWhenDead)
                return 0;
            return dieCount;
        }
    }

    public BaseNetworkGameManager NetworkManager { get; protected set; }
    public void RegisterNetworkGameManager(BaseNetworkGameManager networkManager)
    {
        NetworkManager = networkManager;
    }

    public virtual bool CanRespawn(params object[] extraParams)
    {
        if (NetworkManager != null)
            return NetworkManager.CanCharacterRespawn(this, extraParams);
        return true;
    }
    
    public virtual bool Respawn(params object[] extraParams)
    {
        if (NetworkManager != null)
            return NetworkManager.RespawnCharacter(this, extraParams);
        return true;
    }

    protected virtual void Init()
    {
        if (!PhotonNetwork.isMasterClient)
            return;
        score = 0;
        killCount = 0;
        assistCount = 0;
        dieCount = 0;
    }

    protected virtual void Awake()
    {
        Init();
    }

    protected virtual void SyncData()
    {
        if (!PhotonNetwork.isMasterClient)
            return;
        photonView.RPC("RpcUpdateScore", PhotonTargets.Others, score);
        photonView.RPC("RpcUpdateKillCount", PhotonTargets.Others, killCount);
        photonView.RPC("RpcUpdateAssistCount", PhotonTargets.Others, assistCount);
        photonView.RPC("RpcUpdateDieCount", PhotonTargets.Others, dieCount);
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        if (!PhotonNetwork.isMasterClient)
            return;
        photonView.RPC("RpcUpdateScore", newPlayer, score);
        photonView.RPC("RpcUpdateKillCount", newPlayer, killCount);
        photonView.RPC("RpcUpdateAssistCount", newPlayer, assistCount);
        photonView.RPC("RpcUpdateDieCount", newPlayer, dieCount);
    }

    protected virtual void OnStartServer()
    {
        SyncData();
    }

    protected virtual void OnStartClient()
    {
    }

    protected virtual void OnStartLocalPlayer()
    {
    }

    protected virtual void Start()
    {
        if (PhotonNetwork.isMasterClient)
            OnStartServer();

        if (photonView.isMine)
        {
            OnStartLocalPlayer();
            SetLocalPlayer();
        }

        OnStartClient();

        NetworkManager = FindObjectOfType<BaseNetworkGameManager>();
    }

    protected virtual void SetLocalPlayer()
    {
        if (Local != null)
            return;

        Local = this;
    }

    protected virtual void Update()
    {
        if (NetworkManager != null)
            NetworkManager.OnUpdateCharacter(this);

        foreach (var obj in noTeamObjects)
        {
            obj.SetActive(playerTeam == PunTeams.Team.none);
        }
        foreach (var obj in teamAObjects)
        {
            obj.SetActive(playerTeam == PunTeams.Team.red);
        }
        foreach (var obj in teamBObjects)
        {
            obj.SetActive(playerTeam == PunTeams.Team.blue);
        }
    }

    public void ResetScore()
    {
        score = 0;
    }

    public void ResetKillCount()
    {
        killCount = 0;
    }

    public void ResetAssistCount()
    {
        assistCount = 0;
    }

    public void ResetDieCount()
    {
        dieCount = 0;
    }

    public int CompareTo(BaseNetworkGameCharacter other)
    {
        return ((-1 * Score.CompareTo(other.Score)) * 10) + photonView.viewID.CompareTo(other.photonView.viewID);
    }

    #region Update RPCs
    [PunRPC]
    protected void RpcUpdateScore(int score)
    {
        _score = score;
    }
    [PunRPC]
    protected void RpcUpdateKillCount(int killCount)
    {
        _killCount = killCount;
    }
    [PunRPC]
    protected void RpcUpdateAssistCount(int assistCount)
    {
        _assistCount = assistCount;
    }
    [PunRPC]
    protected void RpcUpdateDieCount(int dieCount)
    {
        _dieCount = dieCount;
    }
    #endregion
}
