using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPhotonWaitingRoom : UIPhotonGameCreate
{
    public override void Show()
    {
        base.Show();
        SimplePhotonNetworkManager.onJoinedRoom -= OnJoinedRoom;
    }

    public virtual void OnClickCreateWaitingRoom()
    {
        SimplePhotonNetworkManager.Singleton.CreateWaitingRoom();
        SimplePhotonNetworkManager.onJoinedRoom += OnJoinedRoom;
    }

    public override void OnClickCreateGame()
    {
        SimplePhotonNetworkManager.Singleton.StartGame();
    }

    private void OnJoinedRoom()
    {
        Show();
    }
}
