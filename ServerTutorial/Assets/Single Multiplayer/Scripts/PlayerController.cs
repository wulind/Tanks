using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerController : NetworkBehaviour {

    public GameObject bulletPrefab;
    public Transform bulletSpawn;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        if (!isLocalPlayer) {
            return;
        }
        float x = Input.GetAxis("Horizontal") * Time.deltaTime * 150.0f;
        float z = Input.GetAxis("Vertical") * Time.deltaTime * 3.0f;

        transform.Rotate(0, x, 0);
        transform.Translate(0, 0, z);

        if (Input.GetKeyDown(KeyCode.Space)) {
            CmdFire();
        }
    }

   public override void OnStartLocalPlayer() {
        GetComponent<MeshRenderer>().material.color = Color.blue;
    }
    [Command]
    public void CmdFire() {
        //create bullet from prefab
        GameObject bullet = (GameObject)Instantiate(bulletPrefab, bulletSpawn.position, bulletSpawn.rotation);
        
        //add velocity to bullet
        bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * 6.0f;

        //bulletSpawn bullet on client
        NetworkServer.Spawn(bullet);

        //destory bullet after 2 sec
        Destroy(bullet, 2);
    }
}
