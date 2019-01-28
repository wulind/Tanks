using UnityEngine;

namespace TanksMP
{
	/// <summary>
	/// Projectile script for player shots with collision/hit logic.
	/// </summary>
	public class Bullet : MonoBehaviour
	{
		/// <summary>
		/// Projectile travel speed in units.
		/// </summary>
		public float speed = 10;
        
		/// <summary>
		/// Damage to cause on a player that gets hit.
		/// </summary>
		public int damage = 3;
        
		/// <summary>
		/// Delay until despawned automatically when nothing gets hit.
		/// </summary>
		public float despawnDelay = 1f;
        
		/// <summary>
		/// Clip to play when this projectile gets despawned.
		/// </summary>
		public AudioClip explosionClip;
        
		/// <summary>
		/// Object to spawn when this projectile gets despawned.
		/// </summary>
		public GameObject explosionFX;

		//reference to rigidbody component
		private Rigidbody myRigidbody;
		//reference to collider component
		private SphereCollider sphereCol;

		//get component references
		void Awake ()
		{
			myRigidbody = GetComponent<Rigidbody> ();
			sphereCol = GetComponent<SphereCollider> ();
		}

        //set initial travelling velocity
        void OnSpawn()
        {
            myRigidbody.velocity = speed * transform.forward;
        }

        //check what was hit on collisions
        void onTriggerEnter(Collider col)
        {
            //despawn gameobject
            PoolManager.Despawn(gameObject);
        }

        //set despawn effects and reset variables
        void OnDespawn()
        {
            //create clips and particles on despawn
            if (explosionFX)
                PoolManager.Spawn(explosionFX, transform.position, transform.rotation);
            if (explosionClip)
                AudioManager.Play3D(explosionClip, transform.position);

            //reset modified variables to the initial state
            myRigidbody.velocity = Vector3.zero;
            myRigidbody.angularVelocity = Vector3.zero;
        }

	}
}