using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class EnemySpawner : NetworkBehaviour {
    public GameObject enemyPrefab;
    public int numberOfEnemies;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public override void OnStartServer() {
        for (int i = 0; i < numberOfEnemies; i++) {
            //randomly generate position
            Vector3 spawnPosition = new Vector3(Random.Range(-8.0f, 8.0f), 0, Random.Range(-8.0f, 8.0f));

            //randomly generate rotation
            Quaternion spawnRotation = Quaternion.Euler(0.0f, Random.Range(0, 180.0f), 0);

            GameObject enemy = (GameObject)Instantiate(enemyPrefab, spawnPosition, spawnRotation);
            NetworkServer.Spawn(enemy);
        }
    }
}
