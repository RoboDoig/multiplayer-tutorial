using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DarkRift;
using DarkRift.Server;

using Microsoft.Playfab.Gaming.GSDK.CSharp;

namespace MultiplayerPlugin
{
    class NetworkManager : Plugin
    {
        public override bool ThreadSafe => false;
        public override Version Version => new Version(1, 0, 0);
        Dictionary<IClient, Player> players = new Dictionary<IClient, Player>();

        public NetworkManager(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            ClientManager.ClientConnected += ClientConnected;
            ClientManager.ClientDisconnected += ClientDisconnected;

            GameserverSDK.RegisterShutdownCallback(OnShutdown);
            GameserverSDK.RegisterHealthCallback(OnHealthCheck);

            // Connect to PlayFab agent
            GameserverSDK.Start();
            if (GameserverSDK.ReadyForPlayers())
            {
                // returns true on allocation call, player about to connect
            }
            else
            {
                // returns false when server is being terminated
            }
        }

        void OnShutdown()
        {

        }

        bool OnHealthCheck()
        {
            return true;
        }

        void ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            // When client connects, generate new player data
            Player newPlayer = new Player(e.Client.ID, "default");
            players.Add(e.Client, newPlayer);

            // Write player data and tell other connected clients about this player
            using (DarkRiftWriter newPlayerWriter = DarkRiftWriter.Create())
            {
                newPlayerWriter.Write(newPlayer);

                using (Message newPlayerMessage = Message.Create(Tags.PlayerConnectTag, newPlayerWriter))
                {
                    foreach (IClient client in ClientManager.GetAllClients().Where(x => x != e.Client))
                    {
                        client.SendMessage(newPlayerMessage, SendMode.Reliable);
                    }
                }
            }

            // Tell the client player about all connected players
            foreach (Player player in players.Values)
            {
                Message playerMessage = Message.Create(Tags.PlayerConnectTag, player);
                e.Client.SendMessage(playerMessage, SendMode.Reliable);
            }


            // Set client message callbacks
            e.Client.MessageReceived += OnPlayerInformationMessage;
            e.Client.MessageReceived += OnPlayerReadyMessage;
            e.Client.MessageReceived += OnPlayerMoveMessage;
        }

        void ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            // Remove player from connected players
            players.Remove(e.Client);

            // Tell all clients about player disconnection
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                writer.Write(e.Client.ID);

                using (Message message = Message.Create(Tags.PlayerDisconnectTag, writer))
                {
                    foreach (IClient client in ClientManager.GetAllClients())
                    {
                        client.SendMessage(message, SendMode.Reliable);
                    }
                }
            }
        }

        void OnPlayerInformationMessage(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                if (message.Tag == Tags.PlayerInformationTag)
                {
                    using (DarkRiftReader reader = message.GetReader())
                    {
                        string playerName = reader.ReadString();

                        // Update player information
                        players[e.Client].playerName = playerName;

                        // Update all players
                        using (DarkRiftWriter writer = DarkRiftWriter.Create())
                        {
                            writer.Write(e.Client.ID);
                            writer.Write(playerName);

                            message.Serialize(writer);
                        }

                        foreach (IClient client in ClientManager.GetAllClients())
                        {
                            client.SendMessage(message, e.SendMode);
                        }
                    }
                }
            }
        }

        void OnPlayerReadyMessage(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                if (message.Tag == Tags.PlayerSetReadyTag)
                {
                    using (DarkRiftReader reader = message.GetReader())
                    {
                        bool isReady = reader.ReadBoolean();

                        // Update player ready status and check if all players are ready
                        players[e.Client].isReady = isReady;
                        CheckAllReady();

                    }
                }
            }
        }

        void CheckAllReady()
        {
            // Check all clients, if any not ready, then return
            foreach (IClient client in ClientManager.GetAllClients())
            {
                if (!players[client].isReady)
                {
                    return;
                }
            }

            // If all are ready, broadcast start game to all clients
            using (DarkRiftWriter writer = DarkRiftWriter.Create())
            {
                using (Message message = Message.Create(Tags.StartGameTag, writer))
                {
                    foreach (IClient client in ClientManager.GetAllClients())
                    {
                        client.SendMessage(message, SendMode.Reliable);
                    }
                }
            }
        }

        void OnPlayerMoveMessage(object sender, MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                if (message.Tag == Tags.PlayerMoveTag)
                {
                    using (DarkRiftReader reader = message.GetReader())
                    {
                        float newX = reader.ReadSingle();
                        float newY = reader.ReadSingle();
                        float newZ = reader.ReadSingle();

                        Player player = players[e.Client];

                        player.X = newX;
                        player.Y = newY;
                        player.Z = newZ;

                        // send this player's updated position back to all clients except the client that sent the message
                        using (DarkRiftWriter writer = DarkRiftWriter.Create())
                        {
                            writer.Write(player.ID);
                            writer.Write(player.X);
                            writer.Write(player.Y);
                            writer.Write(player.Z);

                            message.Serialize(writer);
                        }

                        foreach (IClient client in ClientManager.GetAllClients().Where(x => x != e.Client))
                            client.SendMessage(message, e.SendMode);
                    }
                }
            }
        }
    }
}
