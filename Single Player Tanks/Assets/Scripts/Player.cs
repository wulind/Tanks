using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace TanksMP
{
	/// <summary>
	/// Player class implementing movement control and shooting.
	/// </summary> 
	public class Player : MonoBehaviour
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
		public int health = 10;
        
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
		/// Last player gameobject that killed this one.
		/// </summary>
		[HideInInspector]
		public GameObject killedBy;
        
		/// <summary>
		/// Reference to the camera following component.
		/// </summary>
		[HideInInspector]
		public FollowTarget camFollow;
		
		//timestamp when next shot should happen
		private float nextFire;
        
		//reference to this rigidbody
		private Rigidbody rb;

		/// <summary>
		/// Used to prevent player moving whilst respawning
		/// </summary>
		[HideInInspector]
		public bool disableInput;

		private bool isLocalPlayer;

		void Awake ()
		{
			maxHealth = health;
		}

		void Start ()
		{

			//get corresponding team and colorise renderers in team color
			Team team = GameManager.GetInstance ().teams [teamIndex];
			for (int i = 0; i < renderers.Length; i++)
				renderers [i].material = team.material;
            
			//set name in label
			label.text = myName;

			OnHealthChange (health);

			if (GameManager.GetInstance ().localPlayer != null)
				return;

			//set a global reference to the local player
			GameManager.GetInstance ().localPlayer = this;
			isLocalPlayer = true;

			//get components and set camera target
			rb = GetComponent<Rigidbody> ();
			camFollow = Camera.main.GetComponent<FollowTarget> ();
			camFollow.target = turret;
		}

		void OnEnable ()
		{
			OnHealthChange (health);
		}

		void FixedUpdate ()
		{
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
				Shoot ();

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

				//spawn bullet using pooling, locally
				GameObject obj = PoolManager.Spawn (bullet, shotPos.position, turret.rotation);
				Bullet blt = obj.GetComponent<Bullet> ();
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
		public void TakeDamage (Bullet bullet)
		{
			//substract health by damage
			health -= bullet.damage;
			OnHealthChange (health);

			//bullet killed the player
			if (health <= 0)
			{
				//the game is already over so don't do anything
				if (GameManager.GetInstance ().gameOver)
					return;
				
				//get killer and increase score for that team
				Player other = bullet.owner.GetComponent<Player> ();
				GameManager.GetInstance ().score [other.teamIndex]++;
				GameManager.GetInstance ().ui.OnTeamScoreChanged (other.teamIndex);
				//the maximum score has been reached now
				if (GameManager.GetInstance ().IsGameOver ())
				{
					//tell client the winning team
					GameOver (other.teamIndex);
					return;
				}

				//the game is not over yet, reset runtime values
				health = maxHealth;
				OnHealthChange (health);
				Respawn ();
			}
		}

		/// <summary>
		/// This is when the respawn delay is over
		/// </summary>
		public virtual void Respawn ()
		{
			//toggle visibility for player gameobject (on/off)
			gameObject.SetActive (!gameObject.activeInHierarchy);
			bool isActive = gameObject.activeInHierarchy;
            
			//the player has been killed
			if (!isActive)
			{
				//detect whether the current user was responsible for the kill
				//yes, that's my kill: increase local kill counter
				if (killedBy == GameManager.GetInstance ().localPlayer.gameObject)
				{
					GameManager.GetInstance ().ui.killCounter [0].text = (int.Parse (GameManager.GetInstance ().ui.killCounter [0].text) + 1).ToString ();
					GameManager.GetInstance ().ui.killCounter [0].GetComponent<Animator> ().Play ("Animation");
				}

				if (explosionFX)
				{
					//spawn death particles locally using pooling and colorize them in the player's team color
					GameObject particle = PoolManager.Spawn (explosionFX, transform.position, transform.rotation);
					ParticleColor pColor = particle.GetComponent<ParticleColor> ();
					if (pColor)
						pColor.SetColor (GameManager.GetInstance ().teams [teamIndex].material.color);
				}
				
				//play sound clip on player death
				if (explosionClip)
					AudioManager.Play3D (explosionClip, transform.position);
			}

			//further changes only affect the local player
			if (!isLocalPlayer)
				return;

			//local player got respawned so reset states
			if (isActive == true)
				ResetPosition ();
			else
			{
				//local player was killed, set camera to follow the killer
				camFollow.target = killedBy.transform;
				//disable input
				disableInput = true;
				//display respawn window (only for local player)
				GameManager.GetInstance ().DisplayDeath ();
			} 
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
			transform.position = GameManager.GetInstance ().GetSpawnPosition (teamIndex);
            
			//reset forces modified by input
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
			transform.rotation = Quaternion.identity;
		}
        
		//called on game end providing the winning team
		void GameOver (int teamIndex)
		{
			//display game over window
			GameManager.GetInstance ().DisplayGameOver (teamIndex);
		}
	}
}