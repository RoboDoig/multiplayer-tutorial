using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerPlugin
{
    class Tags
    {
        public static readonly ushort PlayerConnectTag = 1000;
        public static readonly ushort PlayerDisconnectTag = 1001;
        public static readonly ushort PlayerInformationTag = 1002;
        public static readonly ushort PlayerSetReadyTag = 1003;
        public static readonly ushort StartGameTag = 1004;
        public static readonly ushort PlayerMoveTag = 1005;
    }
}
