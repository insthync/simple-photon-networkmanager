using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPhotonWaitingPlayer : MonoBehaviour
{
    public Text textPlayerName;
    public Text textPlayerState;
    public string playerStateReady = "Ready";
    public string playerStateNotReady = "Not Ready";
    public GameObject[] hostObjects;
    public GameObject[] owningObjects;
    public UIPhotonWaitingRoom Room { get; private set; }
    public PhotonPlayer Data { get; private set; }

    public void SetData(UIPhotonWaitingRoom room, PhotonPlayer data)
    {
        Room = room;
        Data = data;
        var state = (byte) data.CustomProperties[SimplePhotonNetworkManager.CUSTOM_PLAYER_STATE];

        if (textPlayerName != null)
            textPlayerName.text = data.NickName;

        if (textPlayerState != null)
        {
            switch ((SimplePhotonNetworkManager.PlayerState) state)
            {
                case SimplePhotonNetworkManager.PlayerState.Ready:
                    textPlayerState.text = playerStateReady;
                    break;
                case SimplePhotonNetworkManager.PlayerState.NotReady:
                    textPlayerState.text = playerStateNotReady;
                    break;
            }
        }

        foreach (var hostObject in hostObjects)
        {
            hostObject.SetActive(room.HostPlayerID == data.ID);
        }

        foreach (var owningObject in owningObjects)
        {
            owningObject.SetActive(PhotonNetwork.player.ID == data.ID);
        }
    }
}
