using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon;

public class UIPhotonStats : UnityEngine.MonoBehaviour
{
    public string formatPing = "{0} ms";
    public string formatCountOfPlayers = "<color=green>ONLINE :</color> {0}";
    public string formatCountOfPlayersInRoom = "<color=green>ONLINE :</color> {0}";
    public string formatCountOfPlayersOnMaster = "<color=green>ONLINE :</color> {0}";
    public string formatCountOfRooms = "<color=green>ROOM :</color> {0}";
    public Text textPing;
    public Text textCountOfPlayers;
    public Text textCountOfPlayersInRoom;
    public Text textCountOfPlayersOnMaster;
    public Text textCountOfRooms;

    private void Update()
    {
        if (textPing != null)
            textPing.text = string.Format(formatPing, PhotonNetwork.GetPing().ToString("N0"));

        if (textCountOfPlayers != null)
            textCountOfPlayers.text = string.Format(formatCountOfPlayers, PhotonNetwork.countOfPlayers.ToString("N0"));

        if (textCountOfPlayersInRoom != null)
            textCountOfPlayersInRoom.text = string.Format(formatCountOfPlayersInRoom, PhotonNetwork.countOfPlayersInRooms.ToString("N0"));

        if (textCountOfPlayersOnMaster != null)
            textCountOfPlayersOnMaster.text = string.Format(formatCountOfPlayersOnMaster, PhotonNetwork.countOfPlayersOnMaster.ToString("N0"));

        if (textCountOfRooms != null)
            textCountOfRooms.text = string.Format(formatCountOfRooms, PhotonNetwork.countOfRooms.ToString("N0"));
    }
}
