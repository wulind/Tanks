using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RestartGame : MonoBehaviour
{
    public Button restartButton;

    private void Awake()
    {
        restartButton.gameObject.SetActive(false);
    }

    public void Restart()
    {
        restartButton.gameObject.SetActive(false);
        Debug.Log("Restarting Game");
        PlayerController[] players = Resources.FindObjectsOfTypeAll<PlayerController>();

        foreach (PlayerController player in players)
        {
            player.gameObject.GetComponent<Health>().RpcPlayerRespawn();
        }
    }
}
