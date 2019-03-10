using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RestartGame : MonoBehaviour
{
    public Button restartButton;

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

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
