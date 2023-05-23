using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovement : MonoBehaviour {

	public float speed = 7.0f; //player's movement speed
	public float gravity = 10.0f; //amount of gravitational force applied to the player
	private CharacterController controller; //player's CharacterController component
	private Vector3 moveDirection = new Vector3();

	void Start () {
		controller = transform.GetComponent<CharacterController>();
	}

	void Update () {
		//APPLY GRAVITY
		if(moveDirection.y > gravity * -1) {
			moveDirection.y -= gravity * Time.deltaTime;
		}
		controller.Move(moveDirection * Time.deltaTime);
		Vector3 left = transform.TransformDirection(Vector3.left);

		if(controller.isGrounded) {
			if(Input.GetKeyDown(KeyCode.Space)) {
				moveDirection.y = speed;
			}
			else if(Input.GetKey("w")) {
				controller.SimpleMove(transform.forward * speed);
			}
			else if(Input.GetKey("s")) {
				controller.SimpleMove(transform.forward * -speed);
			}
			else if(Input.GetKey("a")) {
				controller.SimpleMove(left * speed);
			}
			else if(Input.GetKey("d")) {
				controller.SimpleMove(left * -speed);
			}
		}
		else {
			if(Input.GetKey("w")) {
				Vector3 relative;
				relative = transform.TransformDirection(0,0,1);
				controller.Move(relative * Time.deltaTime * speed / 1.5f);
			}
		}
	}
}
