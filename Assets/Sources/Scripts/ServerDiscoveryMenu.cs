using System.Collections.Generic;
using UnityEngine;
using IslandAdventureBattleRoyale;

namespace Mirror.Discovery
{
    [DisallowMultipleComponent]
    [HelpURL("https://mirror-networking.com/docs/Components/NetworkDiscovery.html")]
    [RequireComponent(typeof(NetworkDiscovery))]
    public class ServerDiscoveryMenu : MonoBehaviour
    {
        public GameObject ServerButton;
        public GameObject ServerBrowserWindow;

        public readonly Dictionary<long, ServerResponse> discoveredServers = new Dictionary<long, ServerResponse>();

        public NetworkDiscovery networkDiscovery;


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (networkDiscovery == null)
            {
                networkDiscovery = GetComponent<NetworkDiscovery>();
                UnityEditor.Events.UnityEventTools.AddPersistentListener(networkDiscovery.OnServerFound, OnDiscoveredServer);
                UnityEditor.Undo.RecordObjects(new Object[] { this, networkDiscovery }, "Set NetworkDiscovery");
            }
        }
#endif

        private void Update()
        {
            if (NetworkManager.singleton == null)
                return;

            if (NetworkServer.active || NetworkClient.active)
                return;

            if (!NetworkClient.isConnected && !NetworkServer.active && !NetworkClient.active)
            {
                if (ServerBrowserWindow != null)
                {
                    if (ServerBrowserWindow.transform.childCount < discoveredServers.Count)
                    {
                        ServerButtons();
                    }
                }
            }
        }

        public void StartDiscovery(GameObject ServerListWindow)
        {
            ServerBrowserWindow = ServerListWindow;
            discoveredServers.Clear();
            networkDiscovery.StartDiscovery();
        }

        public void StartHost()
        {
            discoveredServers.Clear();
            NetworkManager.singleton.StartHost();
            networkDiscovery.AdvertiseServer();
        }

        public void StartServer()
        {
            discoveredServers.Clear();
            NetworkManager.singleton.StartServer();
            networkDiscovery.AdvertiseServer();
        }

        void ServerButtons()
        {

            foreach (ServerResponse info in discoveredServers.Values)
            {
                ServerConnectButton SBC = Instantiate(ServerButton, ServerBrowserWindow.transform).GetComponent<ServerConnectButton>();
                SBC.serverID = info.serverId;
                SBC.hostName = info.uri.Host;
            }
        }

        public void OnDiscoveredServer(ServerResponse info)
        {
            // Note that you can check the versioning to decide if you can connect to the server or not using this method
            discoveredServers[info.serverId] = info;
        }
    }
}