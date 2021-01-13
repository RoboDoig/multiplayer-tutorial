using System.Net;
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
        UIManager.singleton.SetInputInteractable(false);
    }

    private void OnLoginSuccess(LoginResult result) {
        // If login is a success, attempt to start matchmaking with the client's entity key values
        StartMatchmakingRequest(result.EntityToken.Entity.Id, result.EntityToken.Entity.Type);
    }

    private void StartMatchmakingRequest(string entityID, string entityType) {
        // Create a matchmaking request
        PlayFabMultiplayerAPI.CreateMatchmakingTicket(
            new CreateMatchmakingTicketRequest {
                Creator = new MatchmakingPlayer {
                    Entity = new PlayFab.MultiplayerModels.EntityKey {
                        Id = entityID,
                        Type = entityType
                    },
                    Attributes = new MatchmakingPlayerAttributes {
                        DataObject = new {
                            Latencies = new object[] {
                                new {
                                    region = region,
                                    latency = 100
                                }
                            },
                        },
                    },
                },

                // Cancel matchmaking after this time in seconds with no match found
                GiveUpAfterSeconds = matchmakingTimeout,

                // name of the queue to poll
                QueueName = matchmakingQueue,
            },

            this.OnMatchmakingTicketCreated,
            this.OnPlayFabError
        );
    }

     private void OnMatchmakingTicketCreated(CreateMatchmakingTicketResult createMatchmakingTicketResult) {
        // Now we need to start polling the ticket periodically, using a coroutine
        StartCoroutine(PollMatchmakingTicket(createMatchmakingTicketResult.TicketId));

        // Display progress in UI
        UIManager.singleton.DisplayNetworkMessage("Matchmaking request sent");
    }

    private IEnumerator PollMatchmakingTicket(string ticketId) {
        // Delay ticket request
        yield return new WaitForSeconds(10);

        // Poll the ticket
        PlayFabMultiplayerAPI.GetMatchmakingTicket(
            new GetMatchmakingTicketRequest {
                TicketId = ticketId,
                QueueName = matchmakingQueue
            },

            // callbacks
            this.OnGetMatchmakingTicket,
            this.OnPlayFabError
        );
    }

    private void OnGetMatchmakingTicket(GetMatchmakingTicketResult getMatchmakingTicketResult) {
        // When PlayFab returns our matchmaking ticket

        if (getMatchmakingTicketResult.Status == "Matched") {
            // If we found a match, we then need to access its server
            MatchFound(getMatchmakingTicketResult);
        } else if (getMatchmakingTicketResult.Status == "Canceled") {
            // If the matchmaking ticket was canceled we need to reset the input UI
            UIManager.singleton.SetInputInteractable(true);
            UIManager.singleton.DisplayNetworkMessage("Start Session");
        } else {
            // If we don't have a conclusive matchmaking status, we keep polling the ticket
            StartCoroutine(PollMatchmakingTicket(getMatchmakingTicketResult.TicketId));
        }

        // Display matchmaking status in the UI
        UIManager.singleton.DisplayNetworkMessage(getMatchmakingTicketResult.Status);
    }

    private void MatchFound(GetMatchmakingTicketResult getMatchmakingTicketResult) {
        // When we find a match, we need to request the connection variables to join clients
        PlayFabMultiplayerAPI.GetMatch(
            new GetMatchRequest {
                MatchId = getMatchmakingTicketResult.MatchId,
                QueueName = matchmakingQueue
            },

            this.OnGetMatch,
            this.OnPlayFabError
        );
    }

    private void OnGetMatch(GetMatchResult getMatchResult) {
        // Get the server to join
        string ipString = getMatchResult.ServerDetails.IPV4Address;
        int tcpPort = 0;
        int udpPort = 0;

        // Get the ports and names to join
        foreach (Port port in getMatchResult.ServerDetails.Ports) {
            if (port.Name == playfabTCPPortName)
                tcpPort = port.Num;

            if (port.Name == playfabUDPPortName)
                udpPort = port.Num;
        }

        // Connect and initialize the DarkRiftClient, hand over control to the NetworkManager
        if (tcpPort != 0 && udpPort != 0)
            drClient.ConnectInBackground(IPAddress.Parse(ipString), tcpPort, udpPort, true, null);
    }

    // PlayFab error handling //
    private void OnPlayFabError(PlayFabError error) {
        // Debug log an error report
        Debug.Log(error.GenerateErrorReport());
    }
}
