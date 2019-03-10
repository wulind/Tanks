using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class MyNetworkManager : NetworkManager {
    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
        PlayerController[] players = Resources.FindObjectsOfTypeAll<PlayerController>();
        foreach (PlayerController player in players)
        {
            if (player.localPlayerCheck())
            {
                player.gameObject.GetComponent<Health>().killPlayer();
            }
        }
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        Application.Quit();//abandon game
        base.OnServerDisconnect(conn);
    }
}
