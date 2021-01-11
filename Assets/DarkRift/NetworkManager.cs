using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager singleton;
    private UnityClient drClient;

    public Dictionary<ushort, NetworkEntity> networkPlayers = new Dictionary<ushort, NetworkEntity>();

    // Player prefabs
    public GameObject localPlayerPrefab;
    public GameObject networkPlayerPrefab;

    void Awake() {
        if (singleton != null) {
            Destroy(gameObject);
            return;
        }

        singleton = this;

        drClient = GetComponent<UnityClient>();
        drClient.MessageReceived += MessageReceived;
    }

    void MessageReceived(object sender, MessageReceivedEventArgs e) {
        using (Message message = e.GetMessage() as Message) {
            if (message.Tag == Tags.PlayerConnectTag) {
                PlayerConnect(sender, e);
            } else if (message.Tag == Tags.PlayerDisconnectTag) {
                PlayerDisconnect(sender, e);
            } else if (message.Tag == Tags.PlayerInformationTag) {
                PlayerInformation(sender, e);
            } else if (message.Tag == Tags.StartGameTag) {
                StartGame(sender, e);
            }
        }

        // Update the UI with connected players
        UIManager.singleton.PopulateConnectedPlayers(networkPlayers);
    }

    void PlayerConnect(object sender, MessageReceivedEventArgs e) {
        using (Message message = e.GetMessage()) {
            using (DarkRiftReader reader = message.GetReader()) {
                // Read the message data
                ushort ID = reader.ReadUInt16();
                string playerName = reader.ReadString();

                // Player / Network Player Spawn
                GameObject obj;
                if (ID == drClient.ID) {
                    // If this ID corresponds to this client, spawn the controllable player prefab
                    obj = Instantiate(localPlayerPrefab, new Vector3(0f, 1f, 0f), Quaternion.identity) as GameObject;
                } else {
                    // Else we spawn a network prefab, non-controllable
                    obj = Instantiate(networkPlayerPrefab, new Vector3(0f, 1f, 0f), Quaternion.identity) as GameObject;
                }

                // Get network entity data of prefab and add to network players store
                networkPlayers.Add(ID, obj.GetComponent<NetworkEntity>());

                // Update player name
                networkPlayers[ID].SetPlayerName(playerName);
            }
        }
    }

    void PlayerDisconnect(object sender, MessageReceivedEventArgs e) {
        using (Message message = e.GetMessage()) {
            using (DarkRiftReader reader = message.GetReader()) {
                ushort ID = reader.ReadUInt16();
                Destroy(networkPlayers[ID].gameObject);
                networkPlayers.Remove(ID);
            }
        }
    }

    void PlayerInformation(object sender, MessageReceivedEventArgs e) {
        using (Message message = e.GetMessage()) {
            using (DarkRiftReader reader = message.GetReader()) {
                PlayerInformationMessage playerInformationMessage = reader.ReadSerializable<PlayerInformationMessage>();

                networkPlayers[playerInformationMessage.id].SetPlayerName(playerInformationMessage.playerName);
            }
        }
    }

    void StartGame(object sender, MessageReceivedEventArgs e) {
        UIManager.singleton.CloseUI();

        // Set the local player to be controllable
        foreach (KeyValuePair<ushort, NetworkEntity> networkPlayer in networkPlayers) {
            Player player = networkPlayer.Value.GetComponent<Player>();
            if (player != null) {
                player.controllable = true;
            }
        }
    }

    // Network Messages
    // Message for updating player information
    private class PlayerInformationMessage : IDarkRiftSerializable {
        public ushort id {get; set;}
        public string playerName {get; set;}

        public PlayerInformationMessage() {

        }

        public PlayerInformationMessage(string _playerName) {
            playerName = _playerName;
        }

        public void Deserialize(DeserializeEvent e) {
            id  = e.Reader.ReadUInt16();
            playerName = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e) {
            e.Writer.Write(playerName);
        }
    }

    public void SendPlayerInformationMessage(string playerName) {
        using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
            writer.Write(new PlayerInformationMessage(playerName));
            using (Message message = Message.Create(Tags.PlayerInformationTag, writer)) {
                drClient.SendMessage(message, SendMode.Reliable);
            }
        }
    }

    // Message for telling the server a player is ready
    private class PlayerReadyMessage : IDarkRiftSerializable {
        public ushort id {get; set;}
        public bool isReady {get; set;}

        public PlayerReadyMessage() {

        }

        public PlayerReadyMessage(bool _isReady) {
            isReady = _isReady;
        }

        public void Deserialize(DeserializeEvent e) {
            id = e.Reader.ReadUInt16();
            isReady = e.Reader.ReadBoolean();
        }

        public void Serialize(SerializeEvent e) {
            e.Writer.Write(isReady);
        }
    }

    public void SendPlayerReadyMessage(bool isReady) {
        using (DarkRiftWriter writer = DarkRiftWriter.Create()) {
            writer.Write(new PlayerReadyMessage(isReady));
            using (Message message = Message.Create(Tags.PlayerSetReadyTag, writer)) {
                drClient.SendMessage(message, SendMode.Reliable);
            }
        }
    }
}
