using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayerState = SimplePhotonNetworkManager.PlayerState;

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
        PlayerState state = PlayerState.NotReady;
        if (data.CustomProperties.ContainsKey(SimplePhotonNetworkManager.CUSTOM_PLAYER_STATE))
            state = (PlayerState)(byte)data.CustomProperties[SimplePhotonNetworkManager.CUSTOM_PLAYER_STATE];

        if (textPlayerName != null)
            textPlayerName.text = data.NickName;

        if (textPlayerState != null)
        {
            switch (state)
            {
                case PlayerState.Ready:
                    textPlayerState.text = playerStateReady;
                    break;
                case PlayerState.NotReady:
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
