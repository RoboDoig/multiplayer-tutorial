using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DarkRift;
using DarkRift.Server;

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
    }
}
