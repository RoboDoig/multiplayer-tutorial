using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

using PlayFab;
using PlayFab.ClientModels;
using PlayFab.MultiplayerModels;

public class NetworkInterface : MonoBehaviour
{
    public static NetworkInterface singleton;
    private UnityClient drClient;

    // PlayFab settings
    public string titleID; // The playfab title ID
    public string region; // The region where we will try to connect
    public string matchmakingQueue; // The name of the matchmaking queue we'll use
    public int matchmakingTimeout; // How long to attempt matchmaking before resetting
    public string playfabTCPPortName; // Playfab's name for the TCP port mapping
    public string playfabUDPPortName; // Playfab's name for the UDP port mapping

    void Awake() {
        if (singleton != null) {
            Destroy(gameObject);
            return;
        }

        singleton = this;
    }

    void Start() {
        drClient = GetComponent<UnityClient>();
    }

    // Connect with local test server //
    public void StartLocalSession() {
        // Connect to local network
        drClient.ConnectInBackground(drClient.Host, drClient.Port, drClient.Port, true, delegate {OnLocalSessionCallback();} );

        // Update UI
        UIManager.singleton.SetInputInteractable(false);
    }

    public void OnLocalSessionCallback() {
        if (drClient.ConnectionState == ConnectionState.Connected) {
            // If connection successful, send any additional player info
            NetworkManager.singleton.SendPlayerInformationMessage(UIManager.singleton.nameInputField.text);

            // Set lobby controls to interactable
            UIManager.singleton.SetLobbyInteractable(true);
        } else {
            // Else reset the input UI
            UIManager.singleton.SetInputInteractable(true);
        }
    }

    public void SetPlayerReady() {
        // Tell the server this player is ready to start game
        NetworkManager.singleton.SendPlayerReadyMessage(true);

        // Update UI
        UIManager.singleton.SetLobbyInteractable(false);
    }

    // PlayFab Connection //
    public void StartSession(string clientName) {
        // Attempt to login to PlayFab
        var request = new LoginWithCustomIDRequest { CustomId = clientName, CreateAccount = true};
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnPlayFabError);

        // Disable input panel
        uiManager.SetInputInteractable(false);
    }
}
