using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DarkRift;

namespace MultiplayerPlugin
{
    class Player : IDarkRiftSerializable
    {
        public ushort ID { get; set; }
        public string playerName { get; set; }
        public bool isReady { get; set; }

        public Player()
        {

        }

        public Player(ushort _ID, string _playerName)
        {
            ID = _ID;
            playerName = _playerName;
            isReady = false;
        }

        public void Deserialize(DeserializeEvent e)
        {
            ID = e.Reader.ReadUInt16();
            playerName = e.Reader.ReadString();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(ID);
            e.Writer.Write(playerName);
        }
    }
}
