using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

namespace TanksMP
{
    public class MyNetworkManager : NetworkManager {
        public override void OnClientDisconnect(NetworkConnection conn)
        {
            base.OnClientDisconnect(conn);
            NetworkedPlayer[] players = Resources.FindObjectsOfTypeAll<NetworkedPlayer>();
            foreach (NetworkedPlayer player in players)
            {
                player.KillPlayer();
            }
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            Application.Quit();//abandon game
            base.OnServerDisconnect(conn);
        }
    }
}