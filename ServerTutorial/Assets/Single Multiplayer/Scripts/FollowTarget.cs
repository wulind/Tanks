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
		/// </summary>
		public float distance = 10.0f;
        
		/// <summary>
		/// The clamped height the camera should be above the target.
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

        void Start()
        {
            cam = GetComponent<Camera>();
            camTransform = transform;
            Transform listener = GetComponentInChildren<AudioListener>().transform;
            listener.position = transform.position + transform.forward * distance;
        }

        void LateUpdate()
        {
            if (!target)
                return;
            Quaternion currentRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            Vector3 pos = target.position;
            pos -= currentRotation * Vector3.forward * Mathf.Abs(distance);
            pos.y = target.position.y + Mathf.Abs(height);
            transform.position = pos;
            transform.LookAt(target);
            transform.position = target.position - (transform.forward * Mathf.Abs(distance));
        }


    }
}