using UnityEngine;
using System.Collections;

public class FlyCam : MonoBehaviour
{
	public  float lookSensitivity  = 15.0f;   
	public  float moveSensitivity  = 1.0f;   
	public  float minPitch         = -60.0f; // The minimum pitch angle (looking down)
	public  float maxPitch         = 60.0f;  // he maximum pitch angle (looking up).
	public  float lookDamping        = 0.9f;   
	public  float moveDamping        = 0.9f;  
	
	private float   rotationY    = 0.0f;         
	private Vector2 rotationVelocity = Vector2.zero; 
	private Vector2 linearVelocity = Vector2.zero;

	void Update()
	{
		if( Input.GetMouseButton(1) )
		{
			// Add to the look velocity.
			rotationVelocity.x += Input.GetAxisRaw("Mouse X") * lookSensitivity * Time.deltaTime;
			rotationVelocity.y += Input.GetAxisRaw("Mouse Y") * lookSensitivity * Time.deltaTime;
		}
		
		// Calculate and set the values for the new rotation.
		float rotationX = transform.localEulerAngles.y + rotationVelocity.x;
		rotationY += rotationVelocity.y;
		rotationY = Mathf.Clamp(rotationY, minPitch, maxPitch);
		transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0.0f);
		
		// Damp rotation velocity.
		rotationVelocity *= lookDamping;

		linearVelocity.x += Input.GetAxisRaw("Horizontal") * moveSensitivity * Time.deltaTime;
		linearVelocity.y += Input.GetAxisRaw("Vertical")   * moveSensitivity * Time.deltaTime;

		gameObject.transform.Translate(new Vector3(linearVelocity.x, 0.0f, linearVelocity.y), Space.Self);
		
		// Damp linear velocity.
		linearVelocity *= moveDamping;
	}
}
