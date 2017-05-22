﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

[RequireComponent (typeof (CharacterController))]
public class Player : MonoBehaviour {

	public LayerMask				hmdLayerMask;	// The layerMask for the hmd, defines objects which the player cannot put their head through
	public LayerMask				characterControllerLayerMask;
	public LayerMask				grabLayerMask;

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

	// States for the hands
	public Transform				climbableGrabbedLeft;
	public Transform				climbableGrabbedRight;
	public Vector3					grabOffsetLeft;
	public Vector3					grabOffsetRight;
	public Vector3					grabCCOffsetLeft;
	public Vector3					grabCCOffsetRight;

	// The player's characterController, used to move the player
	public CharacterController		characterController;
	public CharacterController		headCC;
	public GameObject				verticalPusher;
	public Vector3					hmdNeckPosition;
	public Transform				neck;
	public float					leanDistance;
	public float					currentVerticalHeadOffset;
	float							maxLeanDistance = 0.5f;
	public float					heightCurrent;
	float							heightCutoffStanding = 1f;			// If the player is standing above this height, they are considered standing
	float							heightCutoffCrouching = 0.5f;		// If the player is standing avove this height but below the standingCutoff, they are crouching
																		// If the player is below the crouchingCutoff then they are laying

	public Vector3					velocityCurrent;		// The current velocity of the player
	public Vector3					velocityDesired;        // The desired velocity of the player
	float							moveSpeedRunning = 6f;
	float							moveSpeedStanding = 3f;
	float							moveSpeedCrouching = 1.5f;
	float							moveSpeedLaying = 0.75f;
	float							moveSpeedCurrent;
	float							slopeHighest;
	float							slopeLowest;
	public bool						grounded = false;

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
	 * 
	 * 
	 */

	void Start () {
		characterController = GetComponent<CharacterController>();			// Grab the character controller at the start
			// Define the controllerDevices
		controllerDeviceLeft = SteamVR_Controller.Input((int)controllerLeft.index);
		controllerDeviceRight = SteamVR_Controller.Input((int)controllerRight.index);
	}

	void Update () {
		CheckSetControllers();
		UpdateControllerInput();
		UpdatePlayerMovement();
		UpdateHandPhysics();
		UpdateHeadPhysics();
	}

	void CheckSetControllers () {
		if (controllerDeviceLeft == null) {
			controllerDeviceLeft = SteamVR_Controller.Input((int)controllerLeft.index);
		}

		if (controllerDeviceRight == null) {
			controllerDeviceRight = SteamVR_Controller.Input((int)controllerRight.index);
		}
	}

	void UpdateControllerInput () {
		if (controllerDeviceLeft.index != 0 && controllerDeviceRight.index != 0) {

			if (controllerDeviceLeft.GetPress(Valve.VR.EVRButtonId.k_EButton_Grip)) {
				Debug.DrawRay(controllerLeft.transform.position, controllerLeft.transform.forward, Color.blue);
				Debug.DrawRay(controllerLeft.transform.position, controllerLeft.transform.up, Color.green);
				GrabLeft();
			}

			if (controllerDeviceLeft.GetPressUp(Valve.VR.EVRButtonId.k_EButton_Grip)) {
				ReleaseLeft();
			}

			if (controllerDeviceRight.GetPress(Valve.VR.EVRButtonId.k_EButton_Grip)) {
				Debug.DrawRay(controllerRight.transform.position, controllerRight.transform.forward, Color.blue);
				Debug.DrawRay(controllerRight.transform.position, controllerRight.transform.up, Color.green);
				GrabRight();
			}

			if (controllerDeviceRight.GetPressUp(Valve.VR.EVRButtonId.k_EButton_Grip)) {
				ReleaseRight();
			}

			bool padPressed = controllerDeviceLeft.GetPress(Valve.VR.EVRButtonId.k_EButton_DPad_Down) || controllerDeviceLeft.GetPress(Valve.VR.EVRButtonId.k_EButton_DPad_Up) || controllerDeviceLeft.GetPress(Valve.VR.EVRButtonId.k_EButton_DPad_Left) || controllerDeviceLeft.GetPress(Valve.VR.EVRButtonId.k_EButton_DPad_Right);
			float slopeSpeed = ((-slopeHighest + 45) / (characterController.slopeLimit * 2)) + 0.5f;
			moveSpeedCurrent = Mathf.Lerp(moveSpeedCurrent, (heightCurrent > heightCutoffStanding ? (padPressed ? moveSpeedRunning : moveSpeedStanding) : (heightCurrent > heightCutoffCrouching ? moveSpeedCrouching : moveSpeedLaying)) * slopeSpeed, 5 * Time.deltaTime);
			velocityDesired = new Vector3(controllerDeviceLeft.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad).x, velocityDesired.y, controllerDeviceLeft.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad).y) * moveSpeedCurrent;
			velocityDesired = Quaternion.LookRotation(new Vector3(controllerLeft.transform.forward.x, 0, controllerLeft.transform.forward.z), Vector3.up) * velocityDesired;
			if (climbableGrabbedLeft == null && climbableGrabbedRight == null) {
				velocityCurrent = Vector3.Lerp(velocityCurrent, new Vector3(velocityDesired.x, velocityCurrent.y, velocityDesired.z), (grounded ? 25 : 1) * Time.deltaTime);
			} else {
				Vector3 combinedClimbPositions = Vector3.zero;
				int climbCount = 0;

				if (climbableGrabbedLeft == true) {
					combinedClimbPositions += (climbableGrabbedLeft.position + grabOffsetLeft) + (grabCCOffsetLeft - (controllerLeft.transform.position - characterController.transform.position));
					climbCount++;
				}

				if (climbableGrabbedRight == true) {
					combinedClimbPositions += (climbableGrabbedRight.position + grabOffsetRight) + (grabCCOffsetRight - (controllerRight.transform.position - characterController.transform.position));
					climbCount++;
				}

				combinedClimbPositions = combinedClimbPositions / climbCount;

				velocityCurrent = Vector3.Lerp(velocityCurrent, (combinedClimbPositions - characterController.transform.position) / Time.deltaTime, 25 * Time.deltaTime);
			}
		}
	}

	void GrabLeft () {
		Debug.Log("Attempt");
		if (climbableGrabbedLeft == null) {
			// Try and grab something
			RaycastHit basicHit;
			if (Physics.Raycast(controllerLeft.transform.position + controllerLeft.transform.forward * -0.3f, controllerLeft.transform.forward, out basicHit, 0.6f, grabLayerMask)) {
				if (basicHit.transform.gameObject.layer == LayerMask.NameToLayer("Climbable")) {
					grabOffsetLeft = characterController.transform.position - basicHit.transform.position;
					grabCCOffsetLeft = controllerLeft.transform.position - characterController.transform.position;
					climbableGrabbedLeft = basicHit.transform;
					Debug.Log("Grabbed");
				}
			}
		} else {
			// Do nothing?
		}
	}

	void ReleaseLeft () {
		grabOffsetLeft = Vector3.zero;
		grabCCOffsetLeft = Vector3.zero;
		climbableGrabbedLeft = null;
	}

	void GrabRight () {
		Debug.Log("Attempt");
		if (climbableGrabbedRight == null) {
			// Try and grab something
			RaycastHit basicHit;
			if (Physics.Raycast(controllerRight.transform.position + controllerRight.transform.forward * -0.3f, controllerRight.transform.forward, out basicHit, 0.6f, grabLayerMask)) {
				if (basicHit.transform.gameObject.layer == LayerMask.NameToLayer("Climbable")) {
					grabOffsetRight = characterController.transform.position - basicHit.transform.position;
					grabCCOffsetRight = controllerRight.transform.position - characterController.transform.position;
					climbableGrabbedRight = basicHit.transform;
					Debug.Log("Grabbed");
				}
			}
		} else {
			// Do nothing?
		}
	}

	void ReleaseRight () {
		grabOffsetRight = Vector3.zero;
		grabCCOffsetRight = Vector3.zero;
		climbableGrabbedRight = null;
	}

	void UpdatePlayerMovement() {

		//velocityDesired += new Vector3(0, -9 * Time.deltaTime, 0);

		RaycastHit hit;

		// Step 1: HMD movement
		Vector3 hmdPosDelta = ((hmd.transform.position - verticalPusher.transform.localPosition) - headCC.transform.position);
		headCC.Move(hmdPosDelta);       // attempt to move the headCC to the new hmdPosition
		Vector3 hmdOffsetHorizontal = headCC.transform.position - hmd.transform.position;
		verticalPusher.transform.position += new Vector3(0, (headCC.transform.position - hmd.transform.position).y, 0);
		hmdOffsetHorizontal.y = 0;

		rig.transform.position += new Vector3(hmdOffsetHorizontal.x, 0, hmdOffsetHorizontal.z);

		// Step 3: Attempt to move the characterController to the HMD
		Vector3 neckOffset = hmd.transform.forward + hmd.transform.up;
		neckOffset.y = 0;
		neckOffset = neckOffset.normalized * -0.15f;
		Vector3 hmdCCDifference = ((hmd.transform.position + neckOffset) - characterController.transform.position);
		hmdCCDifference = new Vector3(hmdCCDifference.x, 0, hmdCCDifference.z);
		characterController.Move(hmdCCDifference);

		// Lean Distance Debug
		Debug.DrawLine(hmd.transform.position, new Vector3(characterController.transform.position.x, hmd.transform.position.y, characterController.transform.position.z), Color.red, 0, false);

		// Step 4: If HMD leanDistance is too far (greater than maxLeanDistance) then pull back the HMD (by moving the camera rig)
		leanDistance = Vector3.Distance(new Vector3(hmd.transform.position.x, 0, hmd.transform.position.z) + neckOffset, new Vector3(characterController.transform.position.x, 0, characterController.transform.position.z));

		if (leanDistance > maxLeanDistance) {
			Vector3 leanPullBack = (characterController.transform.position - hmd.transform.position); // The direction to pull the hmd back
			leanPullBack = new Vector3(leanPullBack.x, 0, leanPullBack.z).normalized;
			rig.transform.position += leanPullBack * (leanDistance - maxLeanDistance);
		}

		SetCharacterControllerHeight((hmd.transform.position.y - (rig.transform.position.y)) - 0.25f);

		// Step ?: Gravity
		if (climbableGrabbedLeft == null && climbableGrabbedRight == null) {
			velocityCurrent += new Vector3(0, -9.81f * Time.deltaTime, 0);          // Add Gravity this frame
		}
		
		slopeHighest = 0;
		slopeLowest = 180;

		float closestGroundDistance = Mathf.Infinity;
		float furthestGrounedDistance = 0;
		for (int k = 0; k < 15; k++) {          // Vertical Slices
			for (int l = 0; l < 15; l++) {      // Rings
				Vector3 origin = characterController.transform.position + new Vector3(0, 0.004f, 0) + new Vector3(0, (Mathf.Clamp(characterController.height, characterController.radius * 2, Mathf.Infinity) / -2) + characterController.radius, 0) + Quaternion.Euler(0, (360 / 15) * k, 0) * Quaternion.Euler((180 / 15) * l, 0, 0) * new Vector3(0, 0, characterController.radius);
				Debug.DrawLine(origin, origin + Vector3.down * 0.005f, Color.blue, 0, false);
				if (Physics.Raycast(origin, Vector3.down, out hit, Mathf.Infinity, characterControllerLayerMask)) {
					if (hit.transform) {
						float groundCollisionDistance = Vector3.Distance(hit.point, origin);
						if (groundCollisionDistance < closestGroundDistance) {
							closestGroundDistance = groundCollisionDistance;
							float normalAngle = Vector3.Angle(hit.normal, Vector3.up);
							slopeHighest = ((normalAngle > slopeHighest) ? normalAngle : slopeHighest);
							slopeLowest = ((normalAngle < slopeLowest) ? normalAngle : slopeLowest);
						}
						if (groundCollisionDistance > furthestGrounedDistance) {
							furthestGrounedDistance = groundCollisionDistance;
						}
					}
				}
			}
		}

		if (closestGroundDistance != Mathf.Infinity) {
			if (closestGroundDistance < 0.15f) {
				if (climbableGrabbedLeft == null && climbableGrabbedRight == null) {
					grounded = true;
					velocityCurrent.y = -closestGroundDistance;
					if (furthestGrounedDistance < 0.35f) {
						Vector3 ccStart = characterController.transform.position;
						characterController.Move(new Vector3(0, -closestGroundDistance + characterController.skinWidth, 0));
						Vector3 ccDelta = (characterController.transform.position - ccStart);
						rig.transform.position += ccDelta;
					}
				}
			} else if (-closestGroundDistance > velocityCurrent.y * Time.deltaTime) {
				grounded = true;
				velocityCurrent.y = -closestGroundDistance;
				// Fell to ground
				// Take damage?
			} else {
				grounded = false;
			}
		} else {
			grounded = false;
		}

		// Step 6: Get secondary controller trackpad input as character controller velocity
		Vector3 ccPositionBeforePad = characterController.transform.position;
		characterController.Move(velocityCurrent * Time.deltaTime);
		Vector3 netCCMovement = (characterController.transform.position - ccPositionBeforePad);
		rig.transform.position += netCCMovement;

		heightCurrent = hmd.transform.position.y - (rig.transform.position.y);

	}

	void SetCharacterControllerHeight (float desiredHeight) {
		characterController.transform.position = new Vector3(characterController.transform.position.x, rig.transform.position.y + (Mathf.Clamp(desiredHeight, characterController.radius * 2f, Mathf.Infinity) / 2), characterController.transform.position.z);
		characterController.height = Mathf.Clamp(desiredHeight, characterController.radius * 2f, Mathf.Infinity);
		characterController.stepOffset = characterController.height / 4f;
	}

	void UpdateHandPhysics () {
		//handLeft.GetComponent<Rigidbody>().velocity = Vector3.Lerp(handLeft.GetComponent<Rigidbody>().velocity, controllerLeft.transform.position - handLeft.transform.position, 50 * Time.deltaTime);
		//handRight.GetComponent<Rigidbody>().velocity = Vector3.Lerp(handRight.GetComponent<Rigidbody>().velocity, controllerRight.transform.position - handRight.transform.position, 50 * Time.deltaTime);
		handLeft.GetComponent<Rigidbody>().velocity = Vector3.Lerp(handLeft.GetComponent<Rigidbody>().velocity, (controllerLeft.transform.position - handLeft.transform.position) / Time.deltaTime, 50 * Time.deltaTime);
		handRight.GetComponent<Rigidbody>().velocity = Vector3.Lerp(handLeft.GetComponent<Rigidbody>().velocity, (controllerRight.transform.position - handRight.transform.position) / Time.deltaTime, 50 * Time.deltaTime);
		handLeft.transform.rotation = controllerLeft.transform.rotation;
		handRight.transform.rotation = controllerRight.transform.rotation;
		handLeft.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
		handRight.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

	}

	void UpdateHeadPhysics () {

	}

}

