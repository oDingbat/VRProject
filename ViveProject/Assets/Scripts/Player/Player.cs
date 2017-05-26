using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using UnityEngine.PostProcessing;

[RequireComponent (typeof (CharacterController)), RequireComponent(typeof(Entity))]
public class Player : MonoBehaviour {

	public LayerMask				hmdLayerMask;	// The layerMask for the hmd, defines objects which the player cannot put their head through
	public LayerMask				characterControllerLayerMask;
	public LayerMask				grabLayerMask;

	public PostProcessingProfile	postProcessingProfile;
	public Entity					entity;

	// The SteamVR Tracked Objects, used to get the position and rotation of the controllers and head
	public SteamVR_TrackedObject	controllerLeft;
	public SteamVR_TrackedController cLeft;
	public SteamVR_TrackedObject	controllerRight;
	public SteamVR_TrackedObject	hmd;

	// The SteamVR Controller Devices for the left & right controllers; Used to get input
	SteamVR_Controller.Device		controllerDeviceLeft;
	SteamVR_Controller.Device		controllerDeviceRight;

	// The GameObjects for the hands, head, & rig
	public GameObject				handLeft;
	public GameObject				handRight;
	public Rigidbody				handRigidbodyLeft;
	public Rigidbody				handRigidbodyRight;
	public GameObject				head;
	public GameObject				rig;
	Vector3							handRigidbodyPositionOffset = new Vector3(-0.02f, -0.025f, -0.075f);

	// Audio Sources
	public AudioSource				windLoop;
	public AudioSource				footstep;
	public Vector3					positionLastFootstepPlayed;

	// States for the hands
	public Transform				climbableGrabbedLeft;
	public Transform				climbableGrabbedRight;
	public Rigidbody				grabbedItemLeft;
	public Rigidbody				grabbedItemRight;
	public Quaternion				grabRotationLeft;
	public Quaternion				grabRotationRight;
	public Quaternion				grabRotationLastFrameLeft;
	public Quaternion				grabRotationLastFrameRight;
	public bool						wasClimbingLastFrame;
	public Vector3					grabOffsetLeft;
	public Vector3					grabOffsetRight;
	public Vector3					grabCCOffsetLeft;
	public Vector3					grabCCOffsetRight;
	float							grabRayLength = 0.15f;

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
	Vector3							ccPositionLastFrame;
	Vector3							platformMovementsAppliedLastFrame = Vector3.zero;
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
		characterController = GetComponent<CharacterController>();          // Grab the character controller at the start
		handRigidbodyLeft = handLeft.GetComponent<Rigidbody>();
		handRigidbodyRight = handRight.GetComponent<Rigidbody>();
		entity = GetComponent<Entity>();
		Debug.Log("Vitals");
		StartCoroutine(UpdateVitals());
	}

	void Update () {
		CheckSetControllers();
		UpdateControllerInput();
		UpdatePlayerMovement();

		windLoop.volume = Mathf.Lerp(windLoop.volume, Mathf.Clamp01(((Vector3.Distance(ccPositionLastFrame, characterController.transform.position) + platformMovementsAppliedLastFrame.magnitude) / Time.deltaTime) / 75) - 0.15f, 50 * Time.deltaTime);
		ccPositionLastFrame = characterController.transform.position;
		platformMovementsAppliedLastFrame = Vector3.zero;

		if (grounded == true && Vector3.Distance(new Vector3(characterController.transform.position.x, 0, characterController.transform.position.z), positionLastFootstepPlayed) > 2f) {
			PlayFootstepSound();
		}
	}

	void FixedUpdate () {
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

	void PlayFootstepSound () {
		footstep.volume = Mathf.Clamp01(velocityCurrent.magnitude / 40f);
		footstep.Play();
		positionLastFootstepPlayed = new Vector3(characterController.transform.position.x, 0, characterController.transform.position.z);
	}

	void UpdateControllerInput () {
		if (controllerDeviceLeft.index != 0 && controllerDeviceRight.index != 0) {

			if (controllerDeviceLeft.GetPress(Valve.VR.EVRButtonId.k_EButton_Grip)) {
				GrabLeft();
			}

			if (controllerDeviceLeft.GetPressUp(Valve.VR.EVRButtonId.k_EButton_Grip)) {
				ReleaseLeft();
			}

			if (controllerDeviceRight.GetPress(Valve.VR.EVRButtonId.k_EButton_Grip)) {
				GrabRight();
			}

			if (controllerDeviceRight.GetPressUp(Valve.VR.EVRButtonId.k_EButton_Grip)) {
				ReleaseRight();
			}

			bool padPressed = controllerDeviceLeft.GetPress(SteamVR_Controller.ButtonMask.Touchpad);
			Debug.Log(padPressed);

			float slopeSpeed = ((-slopeHighest + 45) / (characterController.slopeLimit * 2)) + 0.5f;
			moveSpeedCurrent = Mathf.Lerp(moveSpeedCurrent, (heightCurrent > heightCutoffStanding ? (padPressed ? moveSpeedRunning : moveSpeedStanding) : (heightCurrent > heightCutoffCrouching ? moveSpeedCrouching : moveSpeedLaying)) * slopeSpeed, 5 * Time.deltaTime);
			velocityDesired = new Vector3(controllerDeviceLeft.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad).x, velocityDesired.y, controllerDeviceLeft.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad).y) * moveSpeedCurrent;
			velocityDesired = Quaternion.LookRotation(new Vector3(controllerLeft.transform.forward.x, 0, controllerLeft.transform.forward.z), Vector3.up) * velocityDesired;
			if (climbableGrabbedLeft == null && climbableGrabbedRight == null) {
				wasClimbingLastFrame = false;
				if (grounded == true) {
					velocityCurrent = Vector3.Lerp(velocityCurrent, new Vector3(velocityDesired.x, velocityCurrent.y, velocityDesired.z), 25 * Time.deltaTime);
				} else {
					if (controllerDeviceLeft.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad) != Vector2.zero) {
						Vector3 normalizedVelocityDesired = new Vector3(velocityDesired.x, 0, velocityDesired.z).normalized * (Mathf.Clamp01(new Vector3(velocityDesired.x, 0, velocityDesired.z).magnitude) * Mathf.Clamp(new Vector3(velocityCurrent.x, 0, velocityCurrent.z).magnitude, moveSpeedCurrent, Mathf.Infinity));
						velocityCurrent = Vector3.Lerp(velocityCurrent, new Vector3(normalizedVelocityDesired.x, velocityCurrent.y, normalizedVelocityDesired.z), 1 * Time.deltaTime);
					}
				}
			} else {
				Vector3 combinedClimbPositions = Vector3.zero;
				Quaternion climbRotationLeft = Quaternion.Euler(0, 0, 0);
				Quaternion climbRotationRight = Quaternion.Euler(0, 0, 0);
				int climbCount = 0;

				if (climbableGrabbedLeft == true) {
					climbRotationLeft = Quaternion.Inverse(grabRotationLeft) * climbableGrabbedLeft.rotation;
				}

				if (climbableGrabbedRight == true) {
					climbRotationRight = Quaternion.Inverse(grabRotationRight) * climbableGrabbedRight.rotation;
				}

				if (climbableGrabbedLeft == true) {
					combinedClimbPositions += (climbableGrabbedLeft.position + climbRotationLeft * grabOffsetLeft) + (climbRotationLeft * grabCCOffsetLeft - (controllerLeft.transform.position - characterController.transform.position));
					climbCount++;
				}

				if (climbableGrabbedRight == true) {
					combinedClimbPositions += (climbableGrabbedRight.position + climbRotationRight * grabOffsetRight) + (climbRotationRight * grabCCOffsetRight - (controllerRight.transform.position - characterController.transform.position));
					climbCount++;
				}

				combinedClimbPositions = combinedClimbPositions / climbCount;

				velocityCurrent = Vector3.Lerp(velocityCurrent, (combinedClimbPositions - characterController.transform.position) / Time.deltaTime, 25 * Time.deltaTime);

				if (wasClimbingLastFrame == true) {
					if (climbableGrabbedLeft) {
						if (climbableGrabbedRight) {
							RotatePlayer(Quaternion.Slerp(Quaternion.Inverse(grabRotationLastFrameLeft) * climbRotationLeft, Quaternion.Inverse(grabRotationLastFrameRight) * climbRotationRight, 0.5f));
						} else {
							RotatePlayer(Quaternion.Inverse(grabRotationLastFrameLeft) * climbRotationLeft);
						}
					} else {
						if (climbableGrabbedRight) {
							RotatePlayer(Quaternion.Inverse(grabRotationLastFrameRight) * climbRotationRight);
						} else {
							Debug.LogWarning("Why are we here?");
						}
					}
				}

				grabRotationLastFrameLeft = climbRotationLeft;
				grabRotationLastFrameRight = climbRotationRight;
				wasClimbingLastFrame = true;
			}
		}
	}

	void RotatePlayer (Quaternion rot) {
		Vector3 rigOffset = (rig.transform.position - hmd.transform.position);
		rigOffset.y = 0;
		rig.transform.position = new Vector3(hmd.transform.position.x, rig.transform.position.y, hmd.transform.position.z) + (rot * rigOffset);
		rig.transform.rotation = rig.transform.rotation * rot;
	}

	void GrabLeft () {
		if (climbableGrabbedLeft == null && grabbedItemLeft == null) {
			// Try and grab something
			Vector3 originPosition = controllerLeft.transform.position + (controllerLeft.transform.rotation * new Vector3(handRigidbodyPositionOffset.x, handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z));
			Collider[] itemColliders = Physics.OverlapSphere(originPosition, 0.2f, grabLayerMask);
			foreach (Collider hitItem in itemColliders) {
				if (hitItem.transform.gameObject.layer == LayerMask.NameToLayer("Item")) {
					grabOffsetLeft = Quaternion.Inverse(controllerLeft.transform.rotation) * (hitItem.transform.position - controllerLeft.transform.position);
					grabRotationLeft = Quaternion.Inverse(controllerLeft.transform.rotation) * hitItem.transform.rotation;
					grabbedItemLeft = hitItem.transform.GetComponent<Rigidbody>();
					return;
				}
			}

			Collider[] climbColliders = Physics.OverlapSphere(originPosition, 0.065f, grabLayerMask);
			foreach (Collider hitClimb in climbColliders) {
				if (hitClimb.transform.gameObject.layer == LayerMask.NameToLayer("Climbable")) {
					grabOffsetLeft = characterController.transform.position - hitClimb.transform.position;
					grabRotationLeft = hitClimb.transform.rotation;
					grabCCOffsetLeft = controllerLeft.transform.position - characterController.transform.position;
					climbableGrabbedLeft = hitClimb.transform;
					StartCoroutine(TriggerHapticFeedback(controllerDeviceLeft, 0.1f));
					return;
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
		grabbedItemLeft = null;
		if (climbableGrabbedRight == null) {
			velocityCurrent = Vector3.ClampMagnitude(velocityCurrent, 6);
		}
	}

	void GrabRight () {
		if (climbableGrabbedRight == null && grabbedItemRight == null) {
			// Try and grab something
			Vector3 originPosition = controllerRight.transform.position + (controllerRight.transform.rotation * new Vector3(-handRigidbodyPositionOffset.x, handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z));
			Collider[] itemColliders = Physics.OverlapSphere(originPosition, 0.2f, grabLayerMask);
			foreach (Collider hitItem in itemColliders) {
				if (hitItem.transform.gameObject.layer == LayerMask.NameToLayer("Item")) {
					grabOffsetRight = Quaternion.Inverse(controllerRight.transform.rotation) * (hitItem.transform.position - controllerRight.transform.position);
					grabRotationRight = Quaternion.Inverse(controllerRight.transform.rotation) * hitItem.transform.rotation;
					grabbedItemRight = hitItem.transform.GetComponent<Rigidbody>();
					return;
				}
			}

			Collider[] climbColliders = Physics.OverlapSphere(originPosition, 0.065f, grabLayerMask);
			foreach (Collider hitClimb in climbColliders) {
				if (hitClimb.transform.gameObject.layer == LayerMask.NameToLayer("Climbable")) {
					grabOffsetRight = characterController.transform.position - hitClimb.transform.position;
					grabRotationRight = hitClimb.transform.rotation;
					grabCCOffsetRight = controllerRight.transform.position - characterController.transform.position;
					climbableGrabbedRight = hitClimb.transform;
					StartCoroutine(TriggerHapticFeedback(controllerDeviceRight, 0.1f));
					return;
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
		grabbedItemRight = null;
		if (climbableGrabbedRight == null) {
			velocityCurrent = Vector3.ClampMagnitude(velocityCurrent, 6);
		}
	}

	IEnumerator TriggerHapticFeedback(SteamVR_Controller.Device device, float duration) {
		for (float i = 0; i <= duration; i += 0.01f) {
			device.TriggerHapticPulse(3999);
			yield return new WaitForSeconds(0.01f);
		}
	}

	void UpdatePlayerMovement() {
		ccPositionLastFrame = characterController.transform.position;

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
					if (grounded == false) { PlayFootstepSound(); }
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
				if (grounded == false) { PlayFootstepSound(); }
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

	public void MovePlayer (Vector3 deltaPosition) {
		Debug.Log("Move Player");
		Vector3 ccPositionBefore = characterController.transform.position;
		characterController.Move(deltaPosition);
		Vector3 netCCMovement = (characterController.transform.position - ccPositionBefore);
		rig.transform.position += netCCMovement;
		platformMovementsAppliedLastFrame += netCCMovement;
	}

	void SetCharacterControllerHeight (float desiredHeight) {
		characterController.transform.position = new Vector3(characterController.transform.position.x, rig.transform.position.y + (Mathf.Clamp(desiredHeight, characterController.radius * 2f, Mathf.Infinity) / 2), characterController.transform.position.z);
		characterController.height = Mathf.Clamp(desiredHeight, characterController.radius * 2f, Mathf.Infinity);
		characterController.stepOffset = characterController.height / 4f;
	}

	void UpdateHandPhysics () {
		// Left Hand
		handRigidbodyLeft.velocity = ((controllerLeft.transform.position + controllerLeft.transform.rotation * new Vector3(handRigidbodyPositionOffset.x, handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z)) - handLeft.transform.position) / Time.fixedDeltaTime;
		Quaternion rotationDeltaLeft = (Quaternion.AngleAxis(30, controllerLeft.transform.right) * controllerLeft.transform.rotation) * Quaternion.Inverse(handLeft.transform.rotation);
		float angleLeft;
		Vector3 axisLeft;
		rotationDeltaLeft.ToAngleAxis(out angleLeft, out axisLeft);
		if (angleLeft > 180) {
			angleLeft -= 360;
		}

		if (angleLeft != float.NaN) {
			handRigidbodyLeft.maxAngularVelocity = Mathf.Infinity;
			handRigidbodyLeft.angularVelocity = (angleLeft * axisLeft);
		}

		// Right Hand
		handRigidbodyRight.velocity = ((controllerRight.transform.position + controllerRight.transform.rotation * new Vector3(-handRigidbodyPositionOffset.x, handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z)) - handRight.transform.position) / Time.fixedDeltaTime;
		Quaternion rotationDeltaRight = (Quaternion.AngleAxis(30, controllerRight.transform.right) * controllerRight.transform.rotation) * Quaternion.Inverse(handRight.transform.rotation);
		float angleRight;
		Vector3 axisRight;
		rotationDeltaRight.ToAngleAxis(out angleRight, out axisRight);
		if (angleRight > 180) {
			angleRight -= 360;
		}

		if (angleRight != float.NaN) {
			handRigidbodyRight.maxAngularVelocity = Mathf.Infinity;
			handRigidbodyRight.angularVelocity = (angleRight * axisRight);
		}



		// Item Physics
		if (grabbedItemLeft != null && grabbedItemLeft == grabbedItemRight) {
			Vector3 combinedPositions = ((controllerLeft.transform.position + (controllerLeft.transform.rotation * grabOffsetLeft)) + (controllerRight.transform.position + (controllerRight.transform.rotation * grabOffsetRight))) / 2;
			grabbedItemLeft.velocity = (combinedPositions - grabbedItemLeft.transform.position) / Time.fixedDeltaTime;
			Quaternion rotationDeltaItem = Quaternion.Slerp(controllerLeft.transform.rotation * grabRotationLeft, controllerRight.transform.rotation * grabRotationRight, 0.5f) * Quaternion.Inverse(grabbedItemLeft.transform.rotation);
			float angleItem;
			Vector3 axisItem;
			rotationDeltaItem.ToAngleAxis(out angleItem, out axisItem);
			if (angleItem > 180) {
				angleItem -= 360;
			}

			if (angleItem != float.NaN) {
				grabbedItemLeft.maxAngularVelocity = Mathf.Infinity;
				grabbedItemLeft.angularVelocity = (angleItem * axisItem);
			}
		} else {
			if (grabbedItemLeft != null) {
				grabbedItemLeft.velocity = ((controllerLeft.transform.position + (controllerLeft.transform.rotation * grabOffsetLeft)) - grabbedItemLeft.transform.position) / Time.fixedDeltaTime;
				Quaternion rotationDeltaItemLeft = (controllerLeft.transform.rotation * grabRotationLeft) * Quaternion.Inverse(grabbedItemLeft.transform.rotation);
				float angleItemLeft;
				Vector3 axisItemLeft;
				rotationDeltaItemLeft.ToAngleAxis(out angleItemLeft, out axisItemLeft);
				if (angleItemLeft > 180) {
					angleItemLeft -= 360;
				}

				if (angleItemLeft != float.NaN) {
					grabbedItemLeft.maxAngularVelocity = Mathf.Infinity;
					grabbedItemLeft.angularVelocity = (angleItemLeft * axisItemLeft);
				}
			}

			// Item Physics
			if (grabbedItemRight != null) {
				grabbedItemRight.velocity = ((controllerRight.transform.position + (controllerRight.transform.rotation * grabOffsetRight)) - grabbedItemRight.transform.position) / Time.fixedDeltaTime;
				Quaternion rotationDeltaItemRight = (controllerRight.transform.rotation * grabRotationRight) * Quaternion.Inverse(grabbedItemRight.transform.rotation);
				float angleItemRight;
				Vector3 axisItemRight;
				rotationDeltaItemRight.ToAngleAxis(out angleItemRight, out axisItemRight);
				if (angleItemRight > 180) {
					angleItemRight -= 360;
				}

				if (angleItemRight != float.NaN) {
					grabbedItemRight.maxAngularVelocity = Mathf.Infinity;
					grabbedItemRight.angularVelocity = (angleItemRight * axisItemRight);
				}
			}
		}

	}

	void UpdateHeadPhysics () {

	}

	public void TakeDamage (int damage) {
		entity.vitals.healthCurrent = Mathf.Clamp(entity.vitals.healthCurrent - damage, 0, entity.vitals.healthMax);
		float desiredIntensity = Mathf.Abs(Mathf.Clamp((float)entity.vitals.healthCurrent * (100 / 75), 0, 100) - 100) / 100;
		UpdateDamageVignette(desiredIntensity);
	}

	IEnumerator UpdateVitals () {
		while (true) {
			entity.vitals.healthCurrent = Mathf.Clamp(entity.vitals.healthCurrent + 1, 0, entity.vitals.healthMax);
			float desiredIntensity = Mathf.Abs(Mathf.Clamp((float)entity.vitals.healthCurrent * (100 / 75), 0, 100) - 100) / 100;
			UpdateDamageVignette(desiredIntensity);
			yield return new WaitForSeconds(0.2f);
		}
	}

	void UpdateDamageVignette (float intensity) {
		VignetteModel.Settings newVignetteSettings = postProcessingProfile.vignette.settings;
		newVignetteSettings.intensity = intensity;
		postProcessingProfile.vignette.settings = newVignetteSettings;
	}

}

