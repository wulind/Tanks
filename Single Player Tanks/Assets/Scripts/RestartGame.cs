using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TanksMP
{
    public class RestartGame : MonoBehaviour
    {
        // Start is called before the first frame update
        void Awake()
        {
            gameObject.SetActive(false);
        }

        public void Restart()
        {
            gameObject.SetActive(false);

            NetworkedPlayer[] players = Resources.FindObjectsOfTypeAll<NetworkedPlayer>();
            foreach (NetworkedPlayer player in players){
                player.RpcPlayerRespawn();
            }
        }
    }
}

