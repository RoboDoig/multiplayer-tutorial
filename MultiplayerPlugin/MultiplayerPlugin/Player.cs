using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerPlugin
{
    class Player
    {
        public ushort ID { get; set; }
        public string playerName { get; set; }

        public Player(ushort _ID, string _playerName)
        {
            ID = _ID;
            playerName = _playerName;
        }
    }
}
