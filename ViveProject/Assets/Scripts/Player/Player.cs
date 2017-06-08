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
	public GrabNode					grabGrabNodeLeft;
	public GrabNode					grabGrabNodeRight;
	public Quaternion				grabRotationLeft;
	public Quaternion				grabRotationRight;
	public Quaternion				grabRotationLastFrameLeft;
	public Quaternion				grabRotationLastFrameRight;
	public bool						wasClimbingLastFrame;
	public Vector3					grabOffsetLeft;
	public Vector3					grabOffsetRight;
	public Vector3					grabCCOffsetLeft;
	public Vector3					grabCCOffsetRight;
	public Vector3					grabDualWieldDirection;             // The vector3 direction from handDominant to handNonDominant (ie: hand right to left direction normalized)
	public string					grabDualWieldDominantHand;
	public Quaternion				grabDualWieldDominantStartRotation;
	Vector3							handPosLastFrameLeft;
	Vector3							handPosLastFrameRight;
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
	bool							padPressed = false;

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
				handLeft.GetComponent<BoxCollider>().enabled = false;
			}

			if (controllerDeviceLeft.GetPressUp(Valve.VR.EVRButtonId.k_EButton_Grip)) {
				ReleaseLeft();
				handLeft.GetComponent<BoxCollider>().enabled = true;
			}

			if (controllerDeviceRight.GetPress(Valve.VR.EVRButtonId.k_EButton_Grip)) {
				GrabRight();
				handRight.GetComponent<BoxCollider>().enabled = false;
			}

			if (controllerDeviceRight.GetPressUp(Valve.VR.EVRButtonId.k_EButton_Grip)) {
				ReleaseRight();
				handRight.GetComponent<BoxCollider>().enabled = true;
			}

			if (padPressed == true) {
				if (!controllerDeviceLeft.GetPress(SteamVR_Controller.ButtonMask.Touchpad) && controllerDeviceLeft.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad) == Vector2.zero) {
					padPressed = false;
				}
			} else {
				if (controllerDeviceLeft.GetPress(SteamVR_Controller.ButtonMask.Touchpad)) {
					padPressed = true;
				}
			}

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
						velocityCurrent = Vector3.Lerp(velocityCurrent, new Vector3(normalizedVelocityDesired.x, velocityCurrent.y, normalizedVelocityDesired.z), 2.5f * Time.deltaTime);
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
							//RotatePlayer(Quaternion.Slerp(Quaternion.Inverse(grabRotationLastFrameLeft) * climbRotationLeft, Quaternion.Inverse(grabRotationLastFrameRight) * climbRotationRight, 0.5f));
						} else {
							//RotatePlayer(Quaternion.Inverse(grabRotationLastFrameLeft) * climbRotationLeft);
						}
					} else {
						if (climbableGrabbedRight) {
							//RotatePlayer(Quaternion.Inverse(grabRotationLastFrameRight) * climbRotationRight);
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

		handPosLastFrameLeft = controllerLeft.transform.position;
		handPosLastFrameRight = controllerRight.transform.position;

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
				if (hitItem.transform.gameObject.layer == LayerMask.NameToLayer("Item") || hitItem.transform.gameObject.layer == LayerMask.NameToLayer("Heavy Item")) {
					if (hitItem.transform.GetComponent<Rigidbody>()) {
						grabbedItemLeft = hitItem.transform.GetComponent<Rigidbody>();
					} else if (hitItem.transform.parent.GetComponent<Rigidbody>()) {
						grabbedItemLeft = hitItem.transform.parent.GetComponent<Rigidbody>();
					} else if (hitItem.transform.parent.parent.GetComponent<Rigidbody>()) {
						grabbedItemLeft = hitItem.transform.parent.parent.GetComponent<Rigidbody>();
					}

					if (grabbedItemLeft.transform.gameObject.layer == LayerMask.NameToLayer("Item")) {
						GrabNode hitGrabNode = hitItem.GetComponent<GrabNode>();
						if (hitGrabNode) {
							if (hitGrabNode.referralNode == null) {
								grabOffsetLeft = Quaternion.Euler(hitGrabNode.rotation) * (-hitGrabNode.transform.localPosition + hitGrabNode.offset) + new Vector3(0, handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z);
								grabRotationLeft = Quaternion.Euler(hitGrabNode.rotation);
								grabGrabNodeLeft = hitGrabNode;
							} else {
								grabOffsetLeft = Quaternion.Euler(hitGrabNode.referralNode.rotation) * (-hitGrabNode.referralNode.transform.localPosition + hitGrabNode.referralNode.offset) + new Vector3(0, handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z); ;
								grabRotationLeft = Quaternion.Euler(hitGrabNode.referralNode.rotation);
								grabGrabNodeLeft = hitGrabNode.referralNode;
							}
						} else {
							grabOffsetLeft = Quaternion.Inverse(controllerLeft.transform.rotation) * (grabbedItemLeft.transform.position - controllerLeft.transform.position);
							grabRotationLeft = Quaternion.Inverse(controllerLeft.transform.rotation) * grabbedItemLeft.transform.rotation;
						}
						if (grabbedItemLeft == grabbedItemRight) {      // Is the other hand already holding this item?
							grabDualWieldDirection = (controllerLeft.transform.position - controllerRight.transform.position);
							grabDualWieldDominantHand = "Right";
							grabDualWieldDominantStartRotation = controllerRight.transform.rotation;
						}
					} else if (grabbedItemLeft.transform.gameObject.layer == LayerMask.NameToLayer("Heavy Item")) {
						grabOffsetLeft = Quaternion.Inverse(grabbedItemLeft.transform.rotation) * (controllerLeft.transform.position - grabbedItemLeft.transform.position);
						grabRotationLeft = grabbedItemLeft.transform.rotation;
					}
					return;
				}
			}

			Collider[] climbColliders = Physics.OverlapSphere(originPosition, 0.065f, grabLayerMask);
			foreach (Collider hitClimb in climbColliders) {
				if (hitClimb.transform.gameObject.layer == LayerMask.NameToLayer("Climbable") || (hitClimb.transform.gameObject.layer == LayerMask.NameToLayer("Environment") && (hmd.transform.position.y - rig.transform.position.y < heightCutoffCrouching))) {
					grabOffsetLeft = characterController.transform.position - hitClimb.transform.position;
					grabRotationLeft = hitClimb.transform.rotation;
					grabCCOffsetLeft = controllerLeft.transform.position - characterController.transform.position;
					climbableGrabbedLeft = hitClimb.transform;
					StartCoroutine(TriggerHapticFeedback(controllerDeviceLeft, 0.1f));
					return;
				}
			}

			int rings = 9;
			int slices = 15;
			for (int a = 0; a < rings; a++) {
				for (int b = 0; b < slices; b++) {
					Vector3 currentOrigin = originPosition + (Quaternion.Euler((a - (rings / 2)) * (180 / rings), b * (360 / slices), 0) * new Vector3(0, 0, 0.065f));
					RaycastHit environmentHit;
					if (Physics.Raycast(currentOrigin + new Vector3(0, 0.05f, 0), Vector3.down, out environmentHit, 0.1f, grabLayerMask)) {
						if (Vector3.Angle(environmentHit.normal, Vector3.up) < 10) {
							grabOffsetLeft = characterController.transform.position - environmentHit.transform.position;
							grabRotationLeft = environmentHit.transform.rotation;
							grabCCOffsetLeft = controllerLeft.transform.position - characterController.transform.position;
							climbableGrabbedLeft = environmentHit.transform;
							StartCoroutine(TriggerHapticFeedback(controllerDeviceLeft, 0.1f));
							return;
						}
					}
				}
			}
		} else {
			// Do nothing?
		}
	}

	void ReleaseLeft () {
		grabOffsetLeft = Vector3.zero;
		grabCCOffsetLeft = Vector3.zero;
		if (climbableGrabbedRight == null && climbableGrabbedLeft == true) {
			AttemptClamber();
		}
		if (grabbedItemLeft == true) {
			if (grabbedItemRight != grabbedItemLeft) {
				ThrowItem(grabbedItemLeft, (controllerLeft.transform.position - handPosLastFrameLeft) / Time.deltaTime);
			} else {
				if (grabGrabNodeRight == null) {
					grabOffsetRight = Quaternion.Inverse(controllerRight.transform.rotation) * (grabbedItemRight.transform.position - controllerRight.transform.position);
					grabRotationRight = Quaternion.Inverse(controllerRight.transform.rotation) * grabbedItemRight.transform.rotation;
				}
			}
		}
		climbableGrabbedLeft = null;
		grabbedItemLeft = null;
		grabGrabNodeLeft = null;
	}

	void GrabRight () {
		if (climbableGrabbedRight == null && grabbedItemRight == null) {
			// Try and grab something
			Vector3 originPosition = controllerRight.transform.position + (controllerRight.transform.rotation * new Vector3(-handRigidbodyPositionOffset.x, handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z));
			Collider[] itemColliders = Physics.OverlapSphere(originPosition, 0.2f, grabLayerMask);
			foreach (Collider hitItem in itemColliders) {
				if (hitItem.transform.gameObject.layer == LayerMask.NameToLayer("Item") || hitItem.transform.gameObject.layer == LayerMask.NameToLayer("Heavy Item")) {
					if (hitItem.transform.GetComponent<Rigidbody>()) {
						grabbedItemRight = hitItem.transform.GetComponent<Rigidbody>();
					} else if (hitItem.transform.parent.GetComponent<Rigidbody>()) {
						grabbedItemRight = hitItem.transform.parent.GetComponent<Rigidbody>();
					} else if (hitItem.transform.parent.parent.GetComponent<Rigidbody>()) {
						grabbedItemRight = hitItem.transform.parent.parent.GetComponent<Rigidbody>();
					}

					if (grabbedItemRight.transform.gameObject.layer == LayerMask.NameToLayer("Item")) {
						GrabNode hitGrabNode = hitItem.GetComponent<GrabNode>();
						if (hitGrabNode) {
							if (hitGrabNode.referralNode == null) {
								grabOffsetRight = Quaternion.Euler(hitGrabNode.rotation) * (-hitGrabNode.transform.localPosition + hitGrabNode.offset) + new Vector3(0, handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z);
								grabRotationRight = Quaternion.Euler(hitGrabNode.rotation);
								grabGrabNodeRight = hitGrabNode;
							} else {
								grabOffsetRight = Quaternion.Euler(hitGrabNode.referralNode.rotation) * (-hitGrabNode.referralNode.transform.localPosition + hitGrabNode.referralNode.offset) + new Vector3(0, handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z); ;
								grabRotationRight = Quaternion.Euler(hitGrabNode.referralNode.rotation);
								grabGrabNodeRight = hitGrabNode.referralNode;
							}
						} else {
							grabOffsetRight = Quaternion.Inverse(controllerRight.transform.rotation) * (grabbedItemRight.transform.position - controllerRight.transform.position);
							grabRotationRight = Quaternion.Inverse(controllerRight.transform.rotation) * grabbedItemRight.transform.rotation;
						}
						if (grabbedItemRight == grabbedItemLeft) {      // Is the other hand already holding this item?
							grabDualWieldDirection = (controllerRight.transform.position - controllerLeft.transform.position);
							grabDualWieldDominantHand = "Left";
							grabDualWieldDominantStartRotation = controllerLeft.transform.rotation;
						}
					} else if (grabbedItemRight.transform.gameObject.layer == LayerMask.NameToLayer("Heavy Item")) {
						grabOffsetRight = Quaternion.Inverse(grabbedItemRight.transform.rotation) * controllerRight.transform.position - grabbedItemRight.transform.position;
						grabRotationRight = grabbedItemRight.transform.rotation;
					}
					return;
				}
			}

			Collider[] climbColliders = Physics.OverlapSphere(originPosition, 0.065f, grabLayerMask);
			foreach (Collider hitClimb in climbColliders) {
				if (hitClimb.transform.gameObject.layer == LayerMask.NameToLayer("Climbable") || (hitClimb.transform.gameObject.layer == LayerMask.NameToLayer("Environment") && (hmd.transform.position.y - rig.transform.position.y < heightCutoffCrouching))) {
					grabOffsetRight = characterController.transform.position - hitClimb.transform.position;
					grabRotationRight = hitClimb.transform.rotation;
					grabCCOffsetRight = controllerRight.transform.position - characterController.transform.position;
					climbableGrabbedRight = hitClimb.transform;
					StartCoroutine(TriggerHapticFeedback(controllerDeviceRight, 0.1f));
					return;
				}
			}

			int rings = 9;
			int slices = 15;
			for (int a = 0; a < rings; a++) {
				for (int b = 0; b < slices; b++) {
					Vector3 currentOrigin = originPosition + (Quaternion.Euler((a - (rings / 2)) * (180 / rings), b * (360 / slices), 0) * new Vector3(0, 0, 0.065f));
					RaycastHit environmentHit;
					if (Physics.Raycast(currentOrigin + new Vector3(0, 0.05f, 0), Vector3.down, out environmentHit, 0.1f, grabLayerMask)) {
						if (Vector3.Angle(environmentHit.normal, Vector3.up) < 10) {
							grabOffsetRight = characterController.transform.position - environmentHit.transform.position;
							grabRotationRight = environmentHit.transform.rotation;
							grabCCOffsetRight = controllerRight.transform.position - characterController.transform.position;
							climbableGrabbedRight = environmentHit.transform;
							StartCoroutine(TriggerHapticFeedback(controllerDeviceRight, 0.1f));
							return;
						}
					}
				}
			}
		} else {
			// Do nothing?
		}
	}

	void ReleaseRight () {
		grabOffsetRight = Vector3.zero;
		grabCCOffsetRight = Vector3.zero;
		if (climbableGrabbedLeft == null && climbableGrabbedRight == true) {
			AttemptClamber();
		}
		if (grabbedItemRight == true) {
			if (grabbedItemLeft != grabbedItemRight) {
				ThrowItem(grabbedItemRight, (controllerRight.transform.position - handPosLastFrameRight) / Time.deltaTime);
			} else {
				if (grabGrabNodeLeft == null) {
					grabOffsetLeft = Quaternion.Inverse(controllerLeft.transform.rotation) * (grabbedItemLeft.transform.position - controllerLeft.transform.position);
					grabRotationLeft = Quaternion.Inverse(controllerLeft.transform.rotation) * grabbedItemLeft.transform.rotation;
				}
			}
		}

		climbableGrabbedRight = null;
		grabbedItemRight = null;
		grabGrabNodeRight = null;
	}

	void AttemptClamber () {
		velocityCurrent = Vector3.ClampMagnitude(velocityCurrent, 6);
		RaycastHit hit;
		if (Physics.Raycast(hmd.transform.position, Vector3.down, out hit, (hmd.transform.position.y - rig.transform.position.y), characterControllerLayerMask)) {
			Vector3 rigVerticalChange = new Vector3(0, hit.point.y, 0) - new Vector3(0, rig.transform.position.y, 0);
			rig.transform.position += rigVerticalChange;
			verticalPusher.transform.localPosition -= rigVerticalChange;
			characterController.Move(rigVerticalChange);
		}
	}

	void ThrowItem (Rigidbody item, Vector3 velocity) { 
		item.velocity = Vector3.ClampMagnitude(velocity.magnitude > 5 ? (velocity * 2f) : velocity, 100);
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

		if (climbableGrabbedLeft == null && climbableGrabbedRight == null) {
			SetCharacterControllerHeight((hmd.transform.position.y - (rig.transform.position.y)) - 0.25f);
		} else {
			SetCharacterControllerHeight(0.25f);
		}


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
			if (closestGroundDistance < 0.1f) {
				if (climbableGrabbedLeft == null && climbableGrabbedRight == null) {
					if (grounded == false) { PlayFootstepSound(); }
					grounded = true;
					velocityCurrent.y = -closestGroundDistance;
					if (furthestGrounedDistance < 0.25f) {
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
		headCC.Move(velocityCurrent * Time.deltaTime);
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

	void SetCharacterControllerHeight(float desiredHeight) {
		if (grounded == true) {
			characterController.transform.position = new Vector3(characterController.transform.position.x, (rig.transform.position.y) + (Mathf.Clamp(desiredHeight, characterController.radius * 2f, Mathf.Infinity) / 2), characterController.transform.position.z);
		} else {
			characterController.transform.position = new Vector3(characterController.transform.position.x, (hmd.transform.position.y - 0.25f) - (Mathf.Clamp(desiredHeight, characterController.radius * 2f, Mathf.Infinity) / 2), characterController.transform.position.z);
		}
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
			// Dual Wielding
			Rigidbody grabbedItemDominant = (grabDualWieldDominantHand == "Left") ? grabbedItemLeft : grabbedItemRight;
			SteamVR_TrackedObject controllerDominant = (grabDualWieldDominantHand == "Left") ? controllerLeft : controllerRight;

			Vector3 dualWieldDirectionCurrent = (((grabDualWieldDominantHand == "Left") ? controllerRight.transform.position : controllerLeft.transform.position) - ((grabDualWieldDominantHand == "Left") ? controllerLeft.transform.position : controllerRight.transform.position));

			Quaternion dualWieldDirectionChangeRotation = Quaternion.FromToRotation(grabDualWieldDirection, dualWieldDirectionCurrent);

			Debug.Log(Vector3.Angle(grabDualWieldDirection, dualWieldDirectionCurrent));

			Quaternion rotationDeltaItem = (dualWieldDirectionChangeRotation * grabDualWieldDominantStartRotation * ((grabDualWieldDominantHand == "Left") ? grabRotationLeft : grabRotationRight)) * Quaternion.Inverse(grabbedItemDominant.transform.rotation);

			float angleItem;
			Vector3 axisItem;
			rotationDeltaItem.ToAngleAxis(out angleItem, out axisItem);
			if (angleItem > 180) {
				angleItem -= 360;
			}

			grabbedItemLeft.velocity = Vector3.ClampMagnitude(((controllerDominant.transform.position + (dualWieldDirectionChangeRotation * grabDualWieldDominantStartRotation * ((grabDualWieldDominantHand == "Left") ? grabOffsetLeft : grabOffsetRight))) - grabbedItemDominant.transform.position) / Time.fixedDeltaTime, (grabbedItemDominant.GetComponent<HingeJoint>()) ? 1 : 100);

			if (angleItem != float.NaN) {
				grabbedItemLeft.maxAngularVelocity = Mathf.Infinity;
				grabbedItemLeft.angularVelocity = (angleItem * axisItem);
			}
		} else {
			// Item Physics - Left
			if (grabbedItemLeft != null) {
				if (grabbedItemLeft.gameObject.layer == LayerMask.NameToLayer("Item")) {
					grabbedItemLeft.velocity = Vector3.ClampMagnitude(((controllerLeft.transform.position + (controllerLeft.transform.rotation * grabOffsetLeft)) - grabbedItemLeft.transform.position) / Time.fixedDeltaTime, (grabbedItemLeft.GetComponent<HingeJoint>()) ? 1 : 100);

					if (!grabbedItemLeft.GetComponent<HingeJoint>()) {
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
				} else {
					//Debug.DrawRay((grabbedItemLeft.transform.position + (grabbedItemLeft.transform.rotation * grabOffsetLeft)), Vector3.up, Color.red);
					grabbedItemLeft.AddForceAtPosition((controllerLeft.transform.position - (grabbedItemLeft.transform.position + (grabbedItemLeft.transform.rotation * grabOffsetLeft))) * (grabbedItemLeft.mass * 0.01f) / Time.fixedDeltaTime, (grabbedItemLeft.transform.rotation * grabOffsetLeft));
				}
			}

			// Item Physics - Right
			if (grabbedItemRight != null) {
				if (grabbedItemRight.gameObject.layer == LayerMask.NameToLayer("Item")) {
					grabbedItemRight.velocity = Vector3.ClampMagnitude(((controllerRight.transform.position + (controllerRight.transform.rotation * grabOffsetRight)) - grabbedItemRight.transform.position) / Time.fixedDeltaTime, (grabbedItemRight.GetComponent<HingeJoint>()) ? 1 : 100);

					if (!grabbedItemRight.GetComponent<HingeJoint>()) {
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
				} else {
					grabbedItemRight.AddForceAtPosition((controllerRight.transform.position - (grabbedItemRight.transform.position + (grabbedItemRight.transform.rotation * grabOffsetRight))) * (grabbedItemRight.mass * 0.01f) / Time.fixedDeltaTime, (grabbedItemRight.transform.rotation * grabOffsetRight));
				}
			}
		}
	}

	void UpdateHeadPhysics() {

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

