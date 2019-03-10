using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RestartGame : MonoBehaviour {
    public Button restartButton;
    
    // Use this for initialization
    void Start () {
    }

    // Update is called once per frame
    void Update () {
    }

    private void Awake() {
        restartButton.gameObject.SetActive(false);
    }

    public void Restart()
    {
        restartButton.gameObject.SetActive(false);
        PlayerController[] players = Resources.FindObjectsOfTypeAll<PlayerController>();

        Debug.Log("Restarting Game");

        foreach (PlayerController player in players) {
            if (player.localPlayerCheck()) {
                player.gameObject.GetComponent<Health>().resetHealth();
                player.gameObject.transform.position = player.spawnPosition;
                player.gameObject.SetActive(true);
                player.gameObject.GetComponent<Health>().winLose.text = "";
            }
        }
    }
}
