using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace TanksMP
{
	/// <summary>
	/// Player class implementing movement control and shooting.
	/// </summary> 
	public class NetworkedPlayer : NetworkBehaviour
	{
		/// <summary>
		/// Player name
		/// </summary>
		[HideInInspector]
		public string myName;
        
		/// <summary>
		/// UI Text displaying the player name.
		/// </summary>
		public Text label;
        
		/// <summary>
		/// Team value assigned by the game manager.
		/// </summary>
		[HideInInspector]
		public int teamIndex;

		/// <summary>
		/// Current health value.
		/// </summary>
		[SyncVar (hook = "OnHealthChange")] public int health = 10;
        
		/// <summary>
		/// Maximum health value at game start.
		/// </summary>
		[HideInInspector]
		public int maxHealth;

		/// <summary>
		/// Current turret rotation and shooting direction.
		/// </summary>
		[HideInInspector]
		public int turretRotation;
        
		/// <summary>
		/// Delay between shots.
		/// </summary>
		public float fireRate = 0.75f;
        
		/// <summary>
		/// Movement speed in all directions.
		/// </summary>
		public float moveSpeed = 8f;

		/// <summary>
		/// UI Slider visualizing health value.
		/// </summary>
		public Slider healthSlider;

		/// <summary>
		/// Clip to play when a shot has been fired.
		/// </summary>
		public AudioClip shotClip;
        
		/// <summary>
		/// Clip to play on player death.
		/// </summary>
		public AudioClip explosionClip;
        
		/// <summary>
		/// Object to spawn on shooting.
		/// </summary>
		public GameObject shotFX;
        
		/// <summary>
		/// Object to spawn on player death.
		/// </summary>
		public GameObject explosionFX;

		/// <summary>
		/// Turret to rotate with look direction.
		/// </summary>
		public Transform turret;
        
		/// <summary>
		/// Position to spawn new bullets at.
		/// </summary>
		public Transform shotPos;
      
		/// <summary>
		/// Array of available bullets for shooting.
		/// </summary>
		public GameObject bullet;
        
		/// <summary>
		/// MeshRenderers that should be highlighted in team color.
		/// </summary>
		public MeshRenderer[] renderers;
        
		/// <summary>
		/// Reference to the camera following component.
		/// </summary>
		[HideInInspector]
		public FollowTarget camFollow;
		
		//timestamp when next shot should happen
		private float nextFire;
        
		//reference to this rigidbody
		private Rigidbody rb;
        
		///Prefab for NetworkedBullet
		public GameObject bulletPrefab;

		///Used to indicate win/lose of player
		Text winLose;

		/// <summary>
		/// Used to prevent player moving whilst respawning
		/// </summary>
		[HideInInspector]
		public bool disableInput;

		void Awake ()
		{
			maxHealth = health;
		}

		void Start ()
		{	
		}

		public override void OnStartLocalPlayer() {
			for (int i = 0; i < renderers.Length; i++)
				renderers [i].material.color = Color.blue;
            
			//set name in label
			label.text = myName;

			// OnHealthChange (health);

            rb = GetComponent<Rigidbody> ();
			camFollow = Camera.main.GetComponent<FollowTarget> ();
			camFollow.target = turret;
        }

		void OnEnable ()
		{
			// OnHealthChange (health);
		}

		void FixedUpdate ()
		{
            if (!isLocalPlayer) {
                return;
            }

			//check for frozen Y position, regardless of other position constraints
			if ((rb.constraints & RigidbodyConstraints.FreezePositionY) != RigidbodyConstraints.FreezePositionY)
			{
				//Y position is not locked and the player is above normal height, apply additional gravity
				if (transform.position.y > 0)
					rb.AddForce (Physics.gravity * 2f, ForceMode.Acceleration);
			}
			
			//movement variables
			Vector2 moveDir;
			Vector2 turnDir;

			//reset moving input when no arrow keys are pressed down
			if (Input.GetAxisRaw ("Horizontal") == 0 && Input.GetAxisRaw ("Vertical") == 0)
			{
				moveDir.x = 0;
				moveDir.y = 0;
			} else
			{
				//read out moving directions and calculate force
				moveDir.x = Input.GetAxis ("Horizontal");
				moveDir.y = Input.GetAxis ("Vertical");
				Move (moveDir);
			}

			//cast a ray on a plane at the mouse position for detecting where to shoot 
			Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			Plane plane = new Plane (Vector3.up, Vector3.up);
			float distance = 0f;
			Vector3 hitPos = Vector3.zero;
			//the hit position determines the mouse position in the scene
			if (plane.Raycast (ray, out distance))
			{
				hitPos = ray.GetPoint (distance) - transform.position;
			}

			//we've converted the mouse position to a direction
			turnDir = new Vector2 (hitPos.x, hitPos.z);

			//rotate turret to look at the mouse direction
			RotateTurret (turnDir);

			//shoot bullet on left mouse click
			if (Input.GetButton ("Fire1"))
				Shoot();

		}
      
        
		//moves rigidbody in the direction passed in
		void Move (Vector2 direction = default(Vector2))
		{
			//if direction is not zero, rotate player in the moving direction relative to camera
			if (direction != Vector2.zero)
				transform.rotation = Quaternion.LookRotation (new Vector3 (direction.x, 0, direction.y))
				* Quaternion.Euler (0, camFollow.camTransform.eulerAngles.y, 0);
            
			//create movement vector based on current rotation and speed
			Vector3 movementDir = transform.forward * moveSpeed * Time.deltaTime;
			//apply vector to rigidbody position
			rb.MovePosition (rb.position + movementDir);
		}
        
		//rotates turret to the direction passed in
		void RotateTurret (Vector2 direction = default(Vector2))
		{
			//don't rotate without values
			if (direction == Vector2.zero)
				return;

			//get rotation value as angle out of the direction we received
			int newRotation = (int)(Quaternion.LookRotation (new Vector3 (direction.x, 0, direction.y)).eulerAngles.y + camFollow.camTransform.eulerAngles.y);
            
			turretRotation = newRotation;

			turret.rotation = Quaternion.Euler (0, newRotation, 0);
		}

		//shoots a bullet in the direction passed in
		//we do not rely on the current turret rotation here, because we send the direction
		//along with the shot request to the server to absolutely ensure a synced shot position
		protected void Shoot (Vector2 direction = default(Vector2))
		{            
			//if shot delay is over
			if (Time.time > nextFire)
			{
				//set next shot timestamp
				nextFire = Time.time + fireRate;

				//create bullet from prefab
				GameObject bullet = (GameObject)Instantiate(bulletPrefab, shotPos.position, turret.rotation);
				
				//add velocity to bullet
				bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * 6.0f;

				//bulletSpawn bullet on client
				NetworkServer.Spawn(bullet);

				//destory bullet after 2 sec
				Destroy(bullet, 2);

				//spawn bullet using pooling, locally
				// GameObject obj = PoolManager.Spawn (bullet, shotPos.position, turret.rotation);
				NetworkedBullet blt = bullet.GetComponent<NetworkedBullet>();
				blt.owner = gameObject;

				if (shotFX)
					PoolManager.Spawn (shotFX, shotPos.position, Quaternion.identity);
				if (shotClip)
					AudioManager.Play3D (shotClip, shotPos.position, 0.1f);
			}
		}
        
		//hook for updating health locally
		protected void OnHealthChange (int value)
		{   
			health = value;
			healthSlider.value = (float)health / maxHealth;
		}

		/// <summary>
		/// Calculate damage to be taken by the Player,
		/// triggers score increase and respawn workflow on death.
		/// </summary>
		public void TakeDamage (NetworkedBullet bullet)
		{
            if (!isServer)
                return;
             
			//substract health by damage
			health -= bullet.damage;
			// OnHealthChange (health);

			//bullet killed the player
			if (health <= 0)
			{
                if (isLocalPlayer)
                    winLose.text = "Lose";
            
                RpcPlayerDead();
                gameObject.SetActive(false);
            }
		}

        /// <summary>
		/// Signals if player wins
		/// </summary>
        [ClientRpc]
        public void RpcPlayerDead() {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            if (players.Length == 2) {

                if (isLocalPlayer)
                    winLose.text = "Lose";
                else
                    winLose.text = "Win";

                players[0].gameObject.SetActive(false);
				players[1].gameObject.SetActive(false);
            }
            GameManager.GetInstance().DisplayRestart();
        }

		/// <summary>
		/// This is when the respawn delay is over
		/// </summary>
		[ClientRpc]
		public void RpcPlayerRespawn ()
		{
			//further changes only affect the local player
			if (!isLocalPlayer)
				return;

			//toggle visibility for player gameobject (on/off)
			gameObject.SetActive (true);
			winLose.text = "";
			health = maxHealth;

			//local player got respawned so reset states
			ResetPosition ();
		}

		/// <summary>
		/// Repositions in team area and resets camera & input variables.
		/// This should only be called for the local player.
		/// </summary>
		public void ResetPosition ()
		{
			if (!isLocalPlayer)
				return;
			//start following the local player again
			camFollow.target = turret;
			disableInput = false;

			//get team area and reposition it there
			transform.position = GameManager.GetInstance().GetSpawnPosition(teamIndex);
            
			//reset forces modified by input
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
			transform.rotation = Quaternion.identity;
		}
        
		/// <summary>
		/// Deletes Player GameObject
		/// </summary>
		public void KillPlayer ()
		{
			if(isLocalPlayer)
				Destroy(gameObject);
		}
        
		//called on game end providing the winning team
		void GameOver (int teamIndex)
		{
			//display game over window
			GameManager.GetInstance().DisplayGameOver (teamIndex);
		}
	}
}