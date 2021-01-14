using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkEntity : MonoBehaviour
{
    public ushort networkID {get; private set;}
    public string playerName {get; private set;}

    public void SetNetworkID (ushort _networkID) {
        networkID = _networkID;
    }

    public void SetPlayerName(string _playerName) {
        playerName = _playerName;
    }
}
