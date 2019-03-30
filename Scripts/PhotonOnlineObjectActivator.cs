using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotonOnlineObjectActivator : MonoBehaviour
{
    public GameObject[] onlineObjects;
    public GameObject[] offlineObjects;
    
    void Update()
    {
        foreach (var obj in onlineObjects)
        {
            obj.SetActive(!PhotonNetwork.offlineMode);
        }
        foreach (var obj in offlineObjects)
        {
            obj.SetActive(PhotonNetwork.offlineMode);
        }
    }
}
