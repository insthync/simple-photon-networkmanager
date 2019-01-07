using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPhotonWaitingRoom : UIPhotonGameCreate
{
    public virtual void OnClickLeaveRoom()
    {
        SimplePhotonNetworkManager.Singleton.LeaveRoom();
    }

    public virtual void OnClickStartGame()
    {
        SimplePhotonNetworkManager.Singleton.StartGame();
    }

    public override void OnClickCreateGame()
    {
        Debug.LogWarning("Cannot create game in waiting room");
    }

    private void Update()
    {
        /* TODO: Implement this
        PhotonNetwork.playerList
        PhotonNetwork.player
        PhotonNetwork.otherPlayers
        */
    }
}
