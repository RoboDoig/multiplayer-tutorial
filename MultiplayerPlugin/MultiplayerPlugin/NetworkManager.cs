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

        public NetworkManager(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {

        }
    }
}
