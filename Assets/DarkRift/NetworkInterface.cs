using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

public class NetworkInterface : MonoBehaviour
{
    public static NetworkInterface singleton;
    private UnityClient drClient;

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
}
