using UnityEngine;

namespace TanksMP
{
	/// <summary>
	/// Camera script for following the player or a different target transform.
	/// </summary>
	public class FollowTarget : MonoBehaviour
	{
		/// <summary>
		/// The camera target to follow.
		/// Automatically picked up in LateUpdate().
		/// </summary>
		public Transform target;

		/// <summary>
		/// The clamped distance in the x-z plane to the target.
        /// Unity world units
		/// </summary>
		public float distance = 10.0f;

        /// <summary>
        /// The clamped height the camera should be above the target.
        /// Unity world units
        /// </summary>
        public float height = 5.0f;

		/// <summary>
		/// Reference to the Camera component.
		/// </summary>
		[HideInInspector]
		public Camera cam;
        
		/// <summary>
		/// Reference to the camera Transform.
		/// </summary>
		[HideInInspector]
		public Transform camTransform;

        //initialize variables
        void Start() {
            cam = GetComponent<Camera>();
            camTransform = transform;

            //AudioListener is a child of the camera because the camera is positioned
            //above the player but the AudioListener needs to consider audio from the position
            //of the player in 3D space.
            Transform listener = GetComponentInChildren<AudioListener>().transform;
            listener.position = transform.position + camTransform.forward * distance;
        }

        /* Position camera in every frame
         */
        void LateUpdate(){
            //cancel if there is no target
            if (!target)
                return;

            //convert the camera's transform angle into a rotation
            //rotates (x, y, z) degrees around x, y, z axis
            //get rotation of camera so that it doesn't change
            Quaternion currentRotation = Quaternion.Euler(0, camTransform.eulerAngles.y, 0);

            //set the position of the camera on the x-z plane to
            //distance units behind the target, height units above the target
            //distance is how far the camera should be from tank
            Vector3 pos = target.position;
            pos -= currentRotation * Vector3.forward * Mathf.Abs(distance);
            pos.y = target.position.y + Mathf.Abs(height);
            transform.position = pos;

            //Look at target
            transform.LookAt(target);

            //clamp distance
            transform.position = target.position - (transform.forward * Mathf.Abs(distance));
        }
	}
}