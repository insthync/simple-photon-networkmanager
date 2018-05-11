using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;

public abstract class BaseNetworkGameCharacter : PunBehaviour, System.IComparable<BaseNetworkGameCharacter>
{
    public static BaseNetworkGameCharacter Local { get; private set; }

    #region Sync Vars
    public string playerName;
    public int score;
    public int killCount;
    public int assistCount;
    public int dieCount;
    #endregion

    public abstract bool IsDead { get; }
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

    public bool CanRespawn(params object[] extraParams)
    {
        if (NetworkManager != null)
            return NetworkManager.CanCharacterRespawn(this, extraParams);
        return true;
    }
    
    public bool Respawn(params object[] extraParams)
    {
        if (NetworkManager != null)
            return NetworkManager.RespawnCharacter(this, extraParams);
        return true;
    }

    protected virtual void Start()
    {
        if (Local != null)
            return;

        Local = this;
        NetworkManager = FindObjectOfType<BaseNetworkGameManager>();
    }

    protected virtual void Update()
    {
        if (NetworkManager != null)
            NetworkManager.OnUpdateCharacter(this);
    }

    protected virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.isWriting)
        {
            stream.SendNext(playerName);
            stream.SendNext(score);
            stream.SendNext(killCount);
            stream.SendNext(assistCount);
            stream.SendNext(dieCount);
        }
        else
        {
            playerName = (string)stream.ReceiveNext();
            score = (int)stream.ReceiveNext();
            killCount = (int)stream.ReceiveNext();
            assistCount = (int)stream.ReceiveNext();
            dieCount = (int)stream.ReceiveNext();
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
}
