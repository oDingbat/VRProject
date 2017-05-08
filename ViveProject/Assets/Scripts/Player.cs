using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

[RequireComponent (typeof (CharacterController))]
public class Player : MonoBehaviour {

	public LayerMask				hmdLayerMask;	// The layerMask for the hmd, defines objects which the player cannot put their head through

	// The SteamVR Tracked Objects, used to get the position and rotation of the controllers and head
	public SteamVR_TrackedObject	controllerLeft;
	public SteamVR_TrackedObject	controllerRight;
	public SteamVR_TrackedObject	hmd;

	// The SteamVR Controller Devices for the left & right controllers; Used to get input
	SteamVR_Controller.Device		controllerDeviceLeft;
	SteamVR_Controller.Device		controllerDeviceRight;

	// The GameObjects for the hands, head, & rig
	public GameObject				handLeft;
	public GameObject				handRight;
	public GameObject				head;
	public GameObject				rig;

	// The player's characterController, used to move the player
	public CharacterController		characterController;
	public Vector3					characterControllerPositionLastFrame;
	public Vector3					hmdPositionLastFrame;
	public float					leanDistance;
	float							headRadius = 0.15f;

	public Vector3					velocityCurrent;		// The current velocity of the player
	public Vector3					velocityDesired;        // The desired velocity of the player

	/*	Concept for step by step player movement process:
	 * step 1: Get change in hmd move position
	 * step 2: Adjust the rig position to account for hmd collision
	 * step 3: Attempt to move the characterController to that position horizontally
	 *	(ignore vertical positionChange; hmd move does not move camera rig)
	 * step 4: If hmd leanDistance is too far from the character controller and we cannot move the character controller there, then pull back the hmd
	 * (unless there is a large enough gap for the player to move, so teleport the characterController through the gap)
	 * step 5: Record hmd leanDistance as horizontal distance from center of character controller
	 * step 6: Get secondary controller trackpad input as character controller velocity
	 * step 7: Move character controller to velocity
	 * step 8: Check again if we can compensate for leanDistance
	 * step 9: Move camera rig to compensate
	 */

	void Start () {
		characterController = GetComponent<CharacterController>();			// Grab the character controller at the start
			// Define the controllerDevices
		controllerDeviceLeft = SteamVR_Controller.Input((int)controllerLeft.index);
		controllerDeviceRight = SteamVR_Controller.Input((int)controllerRight.index);
	}

	void Update () {
		UpdateControllerInput();
		UpdatePlayerMovement();
	}

	void UpdateControllerInput () {
		velocityDesired = new Vector3(controllerDeviceLeft.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad).x, 0, controllerDeviceLeft.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad).y);
	}

	void UpdatePlayerMovement () {

		//characterController.Move(velocityCurrent * Time.deltaTime);

		RaycastHit hit;
		
		// Step 1: HMD movement	
		float furthestHeadCollisionDistance = 0;

		for (int i = 0; i < 15; i++) {			// Vertical Slices
			for (int j = 0; j < 15; j++) {      // Rings
				Vector3 origin = hmdPositionLastFrame + Quaternion.Euler(0, (360 / 15) * i, 0) * Quaternion.Euler((360 / 15) * j, 0, 0) * new Vector3(0, 0, headRadius / 2);
				Vector3 endingPos = hmd.transform.position + Quaternion.Euler(0, (360 / 15) * i, 0) * Quaternion.Euler((360 / 15) * j, 0, 0) * new Vector3(0, 0, headRadius / 2);
				//Debug.DrawLine(origin, origin + new Vector3(0, 0.001f, 0), Color.red);
				if (Physics.Raycast(origin, (hmd.transform.position - hmdPositionLastFrame).normalized, out hit, Vector3.Distance(hmd.transform.position, hmdPositionLastFrame), hmdLayerMask)) {
					if (hit.transform) {
						float headCollisionDistance = Vector3.Distance(hit.point, endingPos);
						if (headCollisionDistance > furthestHeadCollisionDistance) {
							furthestHeadCollisionDistance = headCollisionDistance;
						}
					}
				}
			}
		}

		if (furthestHeadCollisionDistance > 0) {
			Vector3 direction = (hmdPositionLastFrame - hmd.transform.position);
			direction = new Vector3(direction.x, 0, direction.z).normalized;
			rig.transform.position += direction * (furthestHeadCollisionDistance + 0.001f);
		}

		Vector3 netHmdMovement = (hmd.transform.position - hmdPositionLastFrame);
		netHmdMovement = new Vector3(netHmdMovement.x, 0, netHmdMovement.z);
		characterController.Move(netHmdMovement);

		Debug.DrawLine(hmd.transform.position, new Vector3(characterController.transform.position.x, hmd.transform.position.y, characterController.transform.position.z), Color.red, 0, false);


		hmdPositionLastFrame = hmd.transform.position;
		characterControllerPositionLastFrame = transform.position;
	}

}

