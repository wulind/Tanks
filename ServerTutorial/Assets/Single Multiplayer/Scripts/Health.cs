using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Health : NetworkBehaviour {
    public const int maxHealth = 100;
    [SyncVar (hook = "OnChangeHealth")] public int currentHealth = maxHealth;
    public RectTransform healthbar;
    private NetworkStartPosition[] spawnPoints;

    public Text winLose;
    public RestartGame restart;

    // Use this for initialization
    void Start () {

        if (isLocalPlayer) {
            spawnPoints = FindObjectsOfType<NetworkStartPosition>();
        }

        winLose = GameObject.FindObjectOfType<Text>();
        winLose.text = "";

        restart = Object.FindObjectOfType<RestartGame>();
    }
	
	// Update is called once per frame
	void Update () {
        
    }

    public void TakeDamage(int amount) {
        //make clients not run this code
        if (!isServer) {
            return;
        }

        currentHealth -= amount;
        
        if (currentHealth <= 0) {

            if (isLocalPlayer)
                winLose.text = "Lose";
            
            RpcPlayerDead();
            gameObject.SetActive(false);
        }
    }

    [ClientRpc]
    void RpcPlayerDead() {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 2) {

            if (isLocalPlayer)
                winLose.text = "Lose";
            else
                winLose.text = "Win";

            players[0].gameObject.SetActive(false);
            players[1].gameObject.SetActive(false);
        }
        restart.restartButton.gameObject.SetActive(true);
    }

    [ClientRpc]
    public void RpcPlayerRespawn()
    {
        gameObject.GetComponent<Health>().resetHealth();
        gameObject.transform.position = gameObject.GetComponent<PlayerController>().spawnPosition;
        gameObject.SetActive(true);
        gameObject.GetComponent<Health>().winLose.text = "";
    }

    void OnChangeHealth(int health) {
        healthbar.sizeDelta = new Vector2(health * 2, healthbar.sizeDelta.y);
    }

    public void resetHealth() {
        currentHealth = 100;
    }

    public void killPlayer()
    {
        currentHealth = 0;
        Destroy(gameObject);
    }
}
