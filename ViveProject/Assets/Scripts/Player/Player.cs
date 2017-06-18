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
	public AudioManager				audioManager;
	public AudioSource				windLoop;
	public AudioSource				footstep;
	public Vector3					positionLastFootstepPlayed;

	// States for the hands
	public Transform				climbableGrabbedLeft;				// The transform of the climbable object being currently grabbed by the left hand
	public Transform				climbableGrabbedRight;				// The transform of the climbable object being currently grabbed by the right hand
	public Rigidbody				grabbedRigidbodyLeft;				// The rigidbody of the object being currently grabbed by the left hand
	public Rigidbody				grabbedRigidbodyRight;              // The rigidbody of the object being currently grabbed by the right hand
	public Item						grabbedItemLeft;                    // The Item component of the object being currently grabbed by the left hand			// TODO: Make all of these variables not public
	public Item						grabbedItemRight;                   // The Item component of the object being currently grabbed by the right hand
	public GrabNode					grabNodeLeft;						// The grabNode of the object currently grabbed by the left hand (for item objects)
	public GrabNode					grabNodeRight;                      // The grabNode of the object currently grabbed by the right hand (for item objects)
	public Quaternion				grabRotationLeft;                   // This value is used differently depending on the case. If grabbing an item, it stores the desired rotation of the item relative to the hand's rotation. If grabbing a climbable object, it stores the rotation of the object when first grabbed.
	public Quaternion				grabRotationRight;					// ^ ^
	public Quaternion				grabRotationLastFrameLeft;			// This value is used to record the grabRotation last frame. It is (was) used for an experimental formula which rotates the player along with it's grabbed object's rotation.		// TODO: do we still need these variables?
	public Quaternion				grabRotationLastFrameRight;			// ^ ^
	public bool						wasClimbingLastFrame;               // Records whether or not the player was climbing last frame; used for experimental formula which rotates the player along with it's grabbed object's rotation.		// TODO: do we still need this variable?
	public Vector3					grabOffsetLeft;                     // Used differently depending on the case. If grabbing an item, it stores the offset between the hand and the item which when rotated according to rotationOffset is the desired position of the object. If grabbing a climbable object, it stores the offset between the hand and the grabbed object, to determine the desired position of the player
	public Vector3					grabOffsetRight;					// ^ ^
	public Vector3					grabCCOffsetLeft;                   // If grabbing a climbable object, this variable stores the offset between the hand and the character controller, to determine where to move the player to when climbing
	public Vector3					grabCCOffsetRight;					// ^ ^
	public Vector3					grabDualWieldDirection;             // The vector3 direction from handDominant to handNonDominant (ie: hand right to left direction normalized)
	public string					grabDualWieldDominantHand;			// This value determines, when dual wielding an item, which hand is dominant and thus acts as the main pivot point
	Vector3							handPosLastFrameLeft;				// Used to determine which direction to throw items
	Vector3							handPosLastFrameRight;              // Used to determine which direction to throw items
	public Vector3					controllerPosLastFrameLeft;			// Used to determine the jumping velocity when jumping with the left hand
	public Vector3					controllerPosLastFrameRight;        // Used to determine the jumping velocity when jumping with the right hand
	bool							itemReleasingDisabledLeft;			// Used for picking up non-prop items. Used to disable item releasing when first grabbing an item as to make the grip function as a toggle rather than an on/off grab button
	bool							itemReleasingDisabledRight;         // Used for picking up non-prop items. Used to disable item releasing when first grabbing an item as to make the grip function as a toggle rather than an on/off grab button
	public float					itemVelocityPercentageLeft;			// When items are first picked up their IVP = 0; over time grabbed approaches 1 linearly; this value determines the magnitude of the velocity (and angularVelocity) of the item
	public float					itemVelocityPercentageRight;		// When items are first picked up their IVP = 0; over time grabbed approaches 1 linearly; this value determines the magnitude of the velocity (and angularVelocity) of the item

	// The player's characterController, used to move the player
	public CharacterController		characterController;				// The main character controller, used for player movement and velocity calculations
	public CharacterController		headCC;								// The player's head's character controller, used for mantling over cover, climbing in small spaces, and for detecting collision in walls, ceilings, and floors.
	public GameObject				verticalPusher;						// Used to push the player's head vertically. This is useful for when a player tries to stand in an inclosed space or duck down into an object. It will keep the player's head from clipping through geometry.
	public float					leanDistance;						// The current distance the player is leaning. See maxLeanDistance.
	float							maxLeanDistance = 0.375f;           // The value used to determine how far the player can lean over cover/obstacles. Once the player moves past the leanDistance he/she will be pulled back via rig movement.
	public float					heightCurrent;						// The current height of the hmd from the rig
	float							heightCutoffStanding = 1f;			// If the player is standing above this height, they are considered standing
	float							heightCutoffCrouching = 0.5f;		// If the player is standing avove this height but below the standingCutoff, they are crouching
																		// If the player is below the crouchingCutoff then they are laying

	public Vector3					velocityCurrent;		// The current velocity of the player
	public Vector3					velocityDesired;        // The desired velocity of the player
	Vector3							ccPositionLastFrame;
	Vector3							platformMovementsAppliedLastFrame = Vector3.zero;
	float							moveSpeedRunning = 6f;
	float							moveSpeedStanding = 4f;
	float							moveSpeedCrouching = 2.5f;
	float							moveSpeedLaying = 1.25f;
	float							moveSpeedCurrent;
	float							slopeHighest;
	float							slopeLowest;
	public bool						grounded = false;
	bool							jumpLoadedLeft = false;
	bool							jumpLoadedRight = false;
	bool							padPressed = false;
	float							timeLastJumped = 0;

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
	 */

	void Start () {
		characterController = GetComponent<CharacterController>();          // Grab the character controller at the start
		handRigidbodyLeft = handLeft.GetComponent<Rigidbody>();
		handRigidbodyRight = handRight.GetComponent<Rigidbody>();
		audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
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

		controllerPosLastFrameLeft = controllerLeft.transform.position;
		controllerPosLastFrameRight = controllerRight.transform.position;
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

			// Controller Left: Grip Down
			if (controllerDeviceLeft.GetPressDown(Valve.VR.EVRButtonId.k_EButton_Grip)) {
				if (grabbedRigidbodyLeft == true && grabbedItemLeft && grabbedItemLeft.itemType != Item.ItemType.Prop) {
					itemReleasingDisabledLeft = false;
				} else {
					itemReleasingDisabledLeft = true;
				}
				GrabLeft();
				handLeft.GetComponent<BoxCollider>().enabled = false;
			}

			// Controller Left: Grip Being Held
			if (controllerDeviceLeft.GetPress(Valve.VR.EVRButtonId.k_EButton_Grip)) {
				GrabLeft();
			}

			// Controller Left: Grip Up
			if (controllerDeviceLeft.GetPressUp(Valve.VR.EVRButtonId.k_EButton_Grip)) {
				if (grabbedRigidbodyLeft == false || ((grabbedItemLeft && grabbedItemLeft.itemType == Item.ItemType.Prop) || itemReleasingDisabledLeft == false)) {
					ReleaseLeft();
				}
				handLeft.GetComponent<BoxCollider>().enabled = true;
			}

			// Controller Right: Grip Down
			if (controllerDeviceRight.GetPressDown(Valve.VR.EVRButtonId.k_EButton_Grip)) {
				if (grabbedRigidbodyRight == true && grabbedItemRight && grabbedItemRight.itemType != Item.ItemType.Prop) {
					itemReleasingDisabledRight = false;
				} else {
					itemReleasingDisabledRight = true;
				}
				GrabRight();
				handRight.GetComponent<BoxCollider>().enabled = false;
			}

			// Controller Right: Grip Being Held
			if (controllerDeviceRight.GetPress(Valve.VR.EVRButtonId.k_EButton_Grip)) {
				GrabRight();
			}

			// Controller Right: Grip Up
			if (controllerDeviceRight.GetPressUp(Valve.VR.EVRButtonId.k_EButton_Grip)) {
				if (grabbedRigidbodyRight == false || ((grabbedItemRight && grabbedItemRight.itemType == Item.ItemType.Prop) || itemReleasingDisabledRight == false)) {
					ReleaseRight();
				}
				handRight.GetComponent<BoxCollider>().enabled = true;
			}

			if (grabbedItemLeft != null && grabNodeLeft != null) {
				if (controllerDeviceLeft.GetHairTriggerDown()) {
					InteractDown("Left", grabbedItemLeft);
				}

				if (controllerDeviceLeft.GetHairTriggerUp()) {
					InteractUp("Left", grabbedItemLeft);
				}

				if (controllerDeviceLeft.GetHairTrigger()) {
					InteractHold("Left", grabbedItemLeft);
				} else {
					InteractNull("Left", grabbedItemLeft);
				}
			}

			if (grabbedItemRight != null && grabNodeRight != null) {
				if (controllerDeviceRight.GetHairTriggerDown()) {
					InteractDown("Right", grabbedItemRight);
				}

				if (controllerDeviceRight.GetHairTriggerUp()) {
					InteractUp("Right", grabbedItemRight);
				}

				if (controllerDeviceRight.GetHairTrigger()) {
					InteractHold("Right", grabbedItemRight);
				} else {
					InteractNull("Right", grabbedItemRight);
				}
			}

			if (padPressed == true) {
				if (!controllerDeviceLeft.GetPress(SteamVR_Controller.ButtonMask.Touchpad) && controllerDeviceLeft.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad) == Vector2.zero) {
					//padPressed = false;
				}
			} else {
				if (controllerDeviceLeft.GetPress(SteamVR_Controller.ButtonMask.Touchpad)) {
					//padPressed = true;
				}
			}

			float slopeSpeed = ((-slopeHighest + 45) / (characterController.slopeLimit * 2)) + 0.5f;
			moveSpeedCurrent = Mathf.Lerp(moveSpeedCurrent, (heightCurrent > heightCutoffStanding ? (padPressed ? moveSpeedRunning : moveSpeedStanding) : (heightCurrent > heightCutoffCrouching ? moveSpeedCrouching : moveSpeedLaying)) * slopeSpeed, 5 * Time.deltaTime);
			velocityDesired = new Vector3(controllerDeviceLeft.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad).x, velocityDesired.y, controllerDeviceLeft.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad).y) * moveSpeedCurrent;
			velocityDesired = Quaternion.LookRotation(new Vector3(hmd.transform.forward.x, 0, hmd.transform.forward.z), Vector3.up) * velocityDesired;
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
		if (climbableGrabbedLeft == null && grabbedRigidbodyLeft == null) {
			// Try and grab something
			Vector3 originPosition = controllerLeft.transform.position + (controllerLeft.transform.rotation * new Vector3(handRigidbodyPositionOffset.x, handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z));
			Collider[] itemColliders = Physics.OverlapSphere(originPosition, 0.2f, grabLayerMask);
			foreach (Collider hitItem in itemColliders) {
				if (hitItem.transform.gameObject.layer == LayerMask.NameToLayer("Item") || hitItem.transform.gameObject.layer == LayerMask.NameToLayer("Heavy Item")) {
					StartCoroutine(TriggerHapticFeedback(controllerDeviceLeft, 0.1f));

					if (hitItem.transform.GetComponent<Rigidbody>()) {
						grabbedRigidbodyLeft = hitItem.transform.GetComponent<Rigidbody>();
					} else if (hitItem.transform.parent.GetComponent<Rigidbody>()) {
						grabbedRigidbodyLeft = hitItem.transform.parent.GetComponent<Rigidbody>();
					} else if (hitItem.transform.parent.parent.GetComponent<Rigidbody>()) {
						grabbedRigidbodyLeft = hitItem.transform.parent.parent.GetComponent<Rigidbody>();
					}

					if (hitItem.transform.GetComponent<Item>()) {
						grabbedItemLeft = hitItem.transform.GetComponent<Item>();
					} else if (hitItem.transform.parent.GetComponent<Item>()) {
						grabbedItemLeft = hitItem.transform.parent.GetComponent<Item>();
					} else if (hitItem.transform.parent.parent.GetComponent<Item>()) {
						grabbedItemLeft = hitItem.transform.parent.parent.GetComponent<Item>();
					}

					grabbedRigidbodyLeft.useGravity = false;

					if (grabbedRigidbodyLeft.transform.gameObject.layer == LayerMask.NameToLayer("Item")) {
						GrabNode hitGrabNode = hitItem.GetComponent<GrabNode>();
						if (hitGrabNode) {
							if (hitGrabNode.referralNode == null) {
								grabNodeLeft = hitGrabNode;
							} else {
								grabNodeLeft = hitGrabNode.referralNode;
							}

							if (grabNodeLeft.grabType == GrabNode.GrabType.FixedPositionRotation) {
								grabOffsetLeft = Quaternion.Euler(grabNodeLeft.rotation) * (-grabNodeLeft.transform.localPosition - grabNodeLeft.offset) + new Vector3(0, handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z);
								grabRotationLeft = Quaternion.Euler(grabNodeLeft.rotation);
							} else if (grabNodeLeft.grabType == GrabNode.GrabType.FixedPosition) {
								grabOffsetLeft = Quaternion.Euler(grabNodeLeft.rotation) * (-grabNodeLeft.transform.localPosition - grabNodeLeft.offset) + new Vector3(0, handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z);
								grabRotationLeft = Quaternion.Inverse(controllerLeft.transform.rotation) * grabbedRigidbodyLeft.transform.rotation;
							} else if (grabNodeLeft.grabType == GrabNode.GrabType.Dynamic || grabNodeLeft.grabType == GrabNode.GrabType.Referral) {
								grabOffsetLeft = Quaternion.Inverse(controllerLeft.transform.rotation) * (grabbedRigidbodyLeft.transform.position - controllerLeft.transform.position);
								grabRotationLeft = Quaternion.Inverse(controllerLeft.transform.rotation) * grabbedRigidbodyLeft.transform.rotation;
							}
						} else {
							grabOffsetLeft = Quaternion.Inverse(controllerLeft.transform.rotation) * (grabbedRigidbodyLeft.transform.position - controllerLeft.transform.position);
							grabRotationLeft = Quaternion.Inverse(controllerLeft.transform.rotation) * grabbedRigidbodyLeft.transform.rotation;
						}
						if (grabbedRigidbodyLeft == grabbedRigidbodyRight) {      // Is the other hand already holding this item?
							itemVelocityPercentageLeft = 0;
							itemVelocityPercentageRight = 0;
							if (grabNodeLeft.dominance > grabNodeRight.dominance) {
								grabDualWieldDominantHand = "Left";
								grabDualWieldDirection = Quaternion.Euler(grabNodeLeft.rotation) * Quaternion.Inverse(grabbedRigidbodyLeft.transform.rotation) * (controllerRight.transform.position - (grabNodeLeft.transform.position + grabbedRigidbodyLeft.transform.rotation * -grabNodeLeft.offset));
							} else if (grabNodeLeft.dominance < grabNodeRight.dominance) {
								grabDualWieldDominantHand = "Right";
								grabDualWieldDirection = Quaternion.Inverse(controllerRight.transform.rotation) * (controllerLeft.transform.position - controllerRight.transform.position);
							} else {
								grabDualWieldDominantHand = "Right";
								grabDualWieldDirection = Quaternion.Inverse(controllerRight.transform.rotation) * (controllerLeft.transform.position - controllerRight.transform.position);
							}
						}
					} else if (grabbedRigidbodyLeft.transform.gameObject.layer == LayerMask.NameToLayer("Heavy Item")) {
						grabOffsetLeft = Quaternion.Inverse(grabbedRigidbodyLeft.transform.rotation) * (controllerLeft.transform.position - grabbedRigidbodyLeft.transform.position);
						grabRotationLeft = grabbedRigidbodyLeft.transform.rotation;
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
						if (Vector3.Angle(environmentHit.normal, Vector3.up) < 45) {
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
		if (grabbedRigidbodyLeft == true) {
			if (grabbedRigidbodyRight != grabbedRigidbodyLeft) {
				ThrowItem(grabbedRigidbodyLeft, (controllerLeft.transform.position - handPosLastFrameLeft) / Time.deltaTime);
			} else {
				if (grabNodeRight == null) {
					itemVelocityPercentageLeft = 0;
					itemVelocityPercentageRight = 0;
					grabOffsetRight = Quaternion.Inverse(controllerRight.transform.rotation) * (grabbedRigidbodyRight.transform.position - controllerRight.transform.position);
					grabRotationRight = Quaternion.Inverse(controllerRight.transform.rotation) * grabbedRigidbodyRight.transform.rotation;
				}
			}
		}
		if (jumpLoadedLeft == true) {
			if ((grounded == true || timeLastJumped + 0.1f > Time.timeSinceLevelLoad) && climbableGrabbedLeft == false && grabbedRigidbodyLeft == false) {
				velocityCurrent += Vector3.ClampMagnitude((controllerPosLastFrameLeft - controllerLeft.transform.position) * 500, (timeLastJumped + 0.1f > Time.timeSinceLevelLoad) ? 1f : 5f);
				jumpLoadedLeft = false;
				if (grounded == true) { timeLastJumped = Time.timeSinceLevelLoad; }
			}
		}
		climbableGrabbedLeft = null;
		grabbedRigidbodyLeft = null;
		grabbedItemLeft = null;
		grabNodeLeft = null;
	}

	void GrabRight () {
		if (climbableGrabbedRight == null && grabbedRigidbodyRight == null) {
			// Try and grab something
			Vector3 originPosition = controllerRight.transform.position + (controllerRight.transform.rotation * new Vector3(-handRigidbodyPositionOffset.x, handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z));
			Collider[] itemColliders = Physics.OverlapSphere(originPosition, 0.2f, grabLayerMask);
			if (itemColliders.Length > 0) {
				Collider hitItemClosest = itemColliders[0];
				float closestDistance = Mathf.Infinity;
				foreach (Collider hitItem in itemColliders) {
					GrabNode hitItemClass = null;
					if (hitItem.GetComponent<GrabNode>()) {
						hitItemClass = hitItem.GetComponent<GrabNode>();
						if (Vector3.Distance(hitItem.transform.position + (hitItem.transform.parent.parent.rotation * hitItemClass.offset), controllerRight.transform.position) < closestDistance) {
							hitItemClosest = hitItem;
							closestDistance = Vector3.Distance(hitItem.transform.position + (hitItem.transform.parent.parent.rotation * hitItemClass.offset), controllerRight.transform.position);
						}
					} else {
						if (Vector3.Distance(hitItem.transform.position, controllerRight.transform.position) < closestDistance) {
							hitItemClosest = hitItem;
							closestDistance = Vector3.Distance(hitItem.transform.position, controllerRight.transform.position);
						}
					}
				}

				if (hitItemClosest.transform.gameObject.layer == LayerMask.NameToLayer("Item") || hitItemClosest.transform.gameObject.layer == LayerMask.NameToLayer("Heavy Item")) {
					StartCoroutine(TriggerHapticFeedback(controllerDeviceRight, 0.1f));

					if (hitItemClosest.transform.GetComponent<Rigidbody>()) {
						grabbedRigidbodyRight = hitItemClosest.transform.GetComponent<Rigidbody>();
					} else if (hitItemClosest.transform.parent.GetComponent<Rigidbody>()) {
						grabbedRigidbodyRight = hitItemClosest.transform.parent.GetComponent<Rigidbody>();
					} else if (hitItemClosest.transform.parent.parent.GetComponent<Rigidbody>()) {
						grabbedRigidbodyRight = hitItemClosest.transform.parent.parent.GetComponent<Rigidbody>();
					}
					
					if (hitItemClosest.transform.GetComponent<Item>()) {
						grabbedItemRight = hitItemClosest.transform.GetComponent<Item>();
					} else if (hitItemClosest.transform.parent.GetComponent<Item>()) {
						grabbedItemRight = hitItemClosest.transform.parent.GetComponent<Item>();
					} else if (hitItemClosest.transform.parent.parent.GetComponent<Item>()) {
						grabbedItemRight = hitItemClosest.transform.parent.parent.GetComponent<Item>();
					}

					grabbedRigidbodyRight.useGravity = false;

					if (grabbedRigidbodyRight.transform.gameObject.layer == LayerMask.NameToLayer("Item")) {
						GrabNode hitGrabNode = hitItemClosest.GetComponent<GrabNode>();
						if (hitGrabNode) {
							if (hitGrabNode.referralNode == null) {
								grabNodeRight = hitGrabNode;
							} else {
								grabNodeRight = hitGrabNode.referralNode;
							}

							if (grabNodeRight.grabType == GrabNode.GrabType.FixedPositionRotation) {
								grabOffsetRight = Quaternion.Euler(grabNodeRight.rotation) * (-grabNodeRight.transform.localPosition - grabNodeRight.offset) + new Vector3(0, handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z);
								grabRotationRight = Quaternion.Euler(grabNodeRight.rotation);
							} else if (grabNodeRight.grabType == GrabNode.GrabType.FixedPosition) {
								grabOffsetRight = Quaternion.Euler(grabNodeRight.rotation) * (-grabNodeRight.transform.localPosition - grabNodeRight.offset) + new Vector3(0, handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z);
								grabRotationRight = Quaternion.Inverse(controllerRight.transform.rotation) * grabbedRigidbodyRight.transform.rotation;
							} else if (grabNodeRight.grabType == GrabNode.GrabType.Dynamic || grabNodeRight.grabType == GrabNode.GrabType.Referral) {
								grabOffsetRight = Quaternion.Inverse(controllerRight.transform.rotation) * (grabbedRigidbodyRight.transform.position - controllerRight.transform.position);
								grabRotationRight = Quaternion.Inverse(controllerRight.transform.rotation) * grabbedRigidbodyRight.transform.rotation;
							}
						} else {
							grabOffsetRight = Quaternion.Inverse(controllerRight.transform.rotation) * (grabbedRigidbodyRight.transform.position - controllerRight.transform.position);
							grabRotationRight = Quaternion.Inverse(controllerRight.transform.rotation) * grabbedRigidbodyRight.transform.rotation;
						}
						if (grabbedRigidbodyRight == grabbedRigidbodyLeft) {      // Is the other hand already holding this item?
							itemVelocityPercentageLeft = 0;
							itemVelocityPercentageRight = 0;
							if (grabNodeRight.dominance > grabNodeLeft.dominance) {
								grabDualWieldDominantHand = "Right";
								grabDualWieldDirection = Quaternion.Euler(grabNodeRight.rotation) * Quaternion.Inverse(grabbedRigidbodyRight.transform.rotation) * (controllerLeft.transform.position - (grabNodeRight.transform.position + grabbedRigidbodyRight.transform.rotation * -grabNodeRight.offset));
							} else if (grabNodeRight.dominance < grabNodeLeft.dominance) {
								grabDualWieldDominantHand = "Left";
								grabDualWieldDirection = Quaternion.Inverse(controllerLeft.transform.rotation) * (controllerRight.transform.position - controllerLeft.transform.position);
							} else {
								grabDualWieldDominantHand = "Left";
								grabDualWieldDirection = Quaternion.Inverse(controllerLeft.transform.rotation) * (controllerRight.transform.position - controllerLeft.transform.position);
							}
						}
					} else if (grabbedRigidbodyRight.transform.gameObject.layer == LayerMask.NameToLayer("Heavy Item")) {
						grabOffsetRight = Quaternion.Inverse(grabbedRigidbodyRight.transform.rotation) * controllerRight.transform.position - grabbedRigidbodyRight.transform.position;
						grabRotationRight = grabbedRigidbodyRight.transform.rotation;
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
						if (Vector3.Angle(environmentHit.normal, Vector3.up) < 45) {
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
		if (grabbedRigidbodyRight == true) {
			if (grabbedRigidbodyLeft != grabbedRigidbodyRight) {
				ThrowItem(grabbedRigidbodyRight, (controllerRight.transform.position - handPosLastFrameRight) / Time.deltaTime);
			} else {
				if (grabNodeLeft == null) {
					itemVelocityPercentageLeft = 0;
					itemVelocityPercentageRight = 0;
					grabOffsetLeft = Quaternion.Inverse(controllerLeft.transform.rotation) * (grabbedRigidbodyLeft.transform.position - controllerLeft.transform.position);
					grabRotationLeft = Quaternion.Inverse(controllerLeft.transform.rotation) * grabbedRigidbodyLeft.transform.rotation;
				}
			}
		}
		if (jumpLoadedRight == true) {
			if ((grounded == true || timeLastJumped + 0.1f > Time.timeSinceLevelLoad) && climbableGrabbedRight == false && grabbedRigidbodyRight == false) {
				velocityCurrent += Vector3.ClampMagnitude((controllerPosLastFrameRight - controllerRight.transform.position) * 500, (timeLastJumped + 0.1f > Time.timeSinceLevelLoad) ? 1f : 5f);
				jumpLoadedRight = false;
				if (grounded == true) { timeLastJumped = Time.timeSinceLevelLoad; }
			}
		}

		climbableGrabbedRight = null;
		grabbedRigidbodyRight = null;
		grabbedItemRight = null;
		grabNodeRight = null;
	}

	void InteractDown (string hand, Item currentItem) {
		Weapon currentWeapon = currentItem.weapon;
		if ((hand == "Left" ? grabNodeLeft : grabNodeRight).interactionType == GrabNode.InteractionType.Trigger) {
			if (currentWeapon.chargingEnabled == false) {
				AttemptToFireWeapon(hand, currentItem);
			}
		}
	}

	void InteractHold (string hand, Item currentItem) {
		Weapon currentWeapon = currentItem.weapon;
		currentWeapon.triggerHeld = true;
		if ((hand == "Left" ? grabNodeLeft : grabNodeRight).interactionType == GrabNode.InteractionType.Trigger) {
			if (currentWeapon.automatic == true) {
				AttemptToFireWeapon(hand, currentItem);
			}
			if (currentWeapon.chargingEnabled == true) {
				currentWeapon.chargeCurrent = Mathf.Clamp01(currentWeapon.chargeCurrent + (currentWeapon.chargeIncrement * Time.deltaTime));
			}
		}
	}

	void InteractUp(string hand, Item currentItem) {
		Weapon currentWeapon = currentItem.weapon;
		currentWeapon.triggerHeld = false;
		if ((hand == "Left" ? grabNodeLeft : grabNodeRight).interactionType == GrabNode.InteractionType.Trigger) {
			if (currentWeapon.chargingEnabled == true) {
				AttemptToFireWeapon(hand, currentItem);
			}
		}
	}

	void InteractNull (string hand, Item currentItem) {
		Weapon currentWeapon = currentItem.weapon;
		currentWeapon.triggerHeld = false;
	}

	void AttemptToFireWeapon (string hand, Item currentItem) {
		Weapon currentWeapon = currentItem.weapon;
		if (currentWeapon.timeLastFired + (1 / currentWeapon.firerate) <= Time.timeSinceLevelLoad) {
			if (currentWeapon.chargingEnabled == false || (currentWeapon.chargeCurrent >= currentWeapon.chargeRequired)) {
				StartCoroutine(FireWeapon(hand, currentItem));
				currentWeapon.timeLastFired = Time.timeSinceLevelLoad;
			}
		}
	}

	IEnumerator FireWeapon(string hand, Item currentItem) {
		Weapon currentWeapon = currentItem.weapon;
		Rigidbody currentRigidbody = currentItem.transform.GetComponent<Rigidbody>();
		Transform barrel = currentItem.transform.Find("(Barrel Point)");

		for (int i = 0; i < Mathf.Clamp(currentWeapon.burstCount, 1, 100); i++) {           // For each burst shot in this fire
			
			// Step 1: Trigger haptic feedback
			if (grabbedRigidbodyLeft == grabbedRigidbodyRight) {
				StartCoroutine(TriggerHapticFeedback(controllerDeviceLeft, 0.1f));
				StartCoroutine(TriggerHapticFeedback(controllerDeviceRight, 0.1f));
			} else {
				StartCoroutine(TriggerHapticFeedback((hand == "Left" ? controllerDeviceLeft : controllerDeviceRight), 0.1f));
			}

			// Step 2: Apply velocity and angular velocity to weapon
			if (grabbedItemLeft == grabbedItemRight) {
				itemVelocityPercentageLeft = 0f;
				itemVelocityPercentageRight = 0f;
				currentRigidbody.velocity += (currentRigidbody.transform.forward * -currentWeapon.recoilLinear * 0.5f);
				currentRigidbody.angularVelocity += new Vector3(Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular)) * 0.5f;
			} else {
				currentRigidbody.velocity += (currentRigidbody.transform.forward * -currentWeapon.recoilLinear);
				currentRigidbody.angularVelocity += new Vector3(Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular));
				if (hand == "Left") {
					itemVelocityPercentageLeft = 0;
				} else if (hand == "Right") {
					itemVelocityPercentageRight = 0;
				}
			}

			// Step 3: Adjust weapon accuracy & get random accuracy
			currentWeapon.accuracyCurrent = Mathf.Clamp(currentWeapon.accuracyCurrent - currentWeapon.accuracyDecrement, currentWeapon.accuracyMin, currentWeapon.accuracyMax);
			float angleMax = Mathf.Abs(currentWeapon.accuracyCurrent - 1) * 5f;
			Quaternion randomAccuracy = Quaternion.Euler(Random.Range(-angleMax, angleMax), Random.Range(-angleMax, angleMax), Random.Range(-angleMax, angleMax));

			for (int j = 0; (j < currentWeapon.projectileSpreads.Length || (currentWeapon.projectileSpreads.Length == 0 && j == 0)); j++) {

				// Step 5: Get random spread deviations
				Quaternion projectileSpreadDeviation = Quaternion.Euler(Random.Range(-currentWeapon.projectileSpreadDeviation, currentWeapon.projectileSpreadDeviation), Random.Range(-currentWeapon.projectileSpreadDeviation, currentWeapon.projectileSpreadDeviation), 0);

				// Step 4: Create new projectile
				GameObject newProjectile = (GameObject)Instantiate(currentWeapon.projectile, barrel.position + barrel.forward * 0.2f, currentItem.transform.rotation * randomAccuracy);
				if (currentWeapon.projectileSpreads.Length > 0) {
					if (currentWeapon.projectileSpreadType == Weapon.SpreadType.Circular) {
						newProjectile.transform.rotation *= projectileSpreadDeviation * Quaternion.Euler(0, 0, currentWeapon.projectileSpreads[j].x) * Quaternion.Euler(currentWeapon.projectileSpreads[j].y, 0, 0);
					} else {
						newProjectile.transform.rotation *= Quaternion.Euler(currentWeapon.projectileSpreads[j].y, currentWeapon.projectileSpreads[j].x, 0);
					}
				}
				Projectile newProjectileClass = newProjectile.GetComponent<Projectile>();
				if (currentWeapon.chargingEnabled == true) {
					newProjectileClass.velocity = newProjectile.transform.forward * (currentWeapon.projectileVelocity - (currentWeapon.projectileVelocity * currentWeapon.chargeInfluenceVelocity * Mathf.Abs(currentWeapon.chargeCurrent - 1)));
				} else {
					newProjectileClass.velocity = newProjectile.transform.forward * currentWeapon.projectileVelocity;
				}
				newProjectileClass.deceleration = currentWeapon.projectileDeceleration;
				newProjectileClass.decelerationType = currentWeapon.projectileDecelerationType;
				newProjectileClass.gravity = currentWeapon.projectileGravity;
				newProjectileClass.ricochetCount = currentWeapon.projectileRicochetCount;
				newProjectileClass.ricochetAngleMax = currentWeapon.projectileRicochetAngleMax;
				audioManager.PlayClipAtPoint(currentWeapon.soundFireNormal, barrel.position, 2f);

			}
			yield return new WaitForSeconds(currentWeapon.burstDelay);
		}

		if (currentWeapon.chargingEnabled == true) {
			currentWeapon.chargeCurrent = Mathf.Clamp01(currentWeapon.chargeCurrent - currentWeapon.chargeDecrementPerShot);
		}
	}

	void AttemptClamber () {
		velocityCurrent = Vector3.ClampMagnitude(velocityCurrent, 4.5f);
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
		item.useGravity = true;
		if (item.transform.GetComponent<Item>().weapon != null) {
			item.transform.GetComponent<Item>().weapon.triggerHeld = false;
		}
	}

	IEnumerator TriggerHapticFeedback(SteamVR_Controller.Device device, float duration) {
		for (float i = 0; i <= duration; i += 0.01f) {
			device.TriggerHapticPulse(3999);
			yield return new WaitForSeconds(0.01f);
		}
	}

	void UpdatePlayerMovement() {
		if (climbableGrabbedLeft == null && climbableGrabbedRight == null) {
			SetCharacterControllerHeight((hmd.transform.position.y - (rig.transform.position.y)) - 0.25f);
		} else {
			SetCharacterControllerHeight(0.25f);
		}

		ccPositionLastFrame = characterController.transform.position;

		//velocityDesired += new Vector3(0, -9 * Time.deltaTime, 0);

		RaycastHit hit;

		// Step 1: HMD movement

		Vector3 hmdPosDelta = ((hmd.transform.position - ((verticalPusher.transform.localPosition) * Mathf.Clamp01(Time.deltaTime * 10))) - headCC.transform.position);
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
		Vector3 verticalDelta = new Vector3(0, (characterController.transform.position - ccPositionLastFrame).y, 0);
		rig.transform.position += verticalDelta;

		// Lean Distance Debug
		Debug.DrawLine(hmd.transform.position, new Vector3(characterController.transform.position.x, hmd.transform.position.y, characterController.transform.position.z), Color.red, 0, false);

		// Step 4: If HMD leanDistance is too far (greater than maxLeanDistance) then pull back the HMD (by moving the camera rig)
		leanDistance = Vector3.Distance(new Vector3(hmd.transform.position.x, 0, hmd.transform.position.z) + neckOffset, new Vector3(characterController.transform.position.x, 0, characterController.transform.position.z));

		if (leanDistance > maxLeanDistance) {
			Vector3 leanPullBack = (characterController.transform.position - hmd.transform.position); // The direction to pull the hmd back
			leanPullBack = new Vector3(leanPullBack.x, 0, leanPullBack.z).normalized;
			rig.transform.position += leanPullBack * (leanDistance - maxLeanDistance);
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
			if (closestGroundDistance < 0.05f) {
				if (climbableGrabbedLeft == null && climbableGrabbedRight == null && velocityCurrent.y < 0) {
					if (grounded == false) { PlayFootstepSound(); }
					grounded = true;
					jumpLoadedLeft = true;
					jumpLoadedRight = true;
					velocityCurrent.y = -closestGroundDistance;
					if (furthestGrounedDistance < 0.2f) {
						Vector3 ccStart = characterController.transform.position;
						characterController.Move(new Vector3(0, -closestGroundDistance + characterController.skinWidth, 0));
						Vector3 ccDelta = (characterController.transform.position - ccStart);
						rig.transform.position += ccDelta;
					}
				}
			} else if (-closestGroundDistance > velocityCurrent.y * Time.deltaTime) {
				if (grounded == false) { PlayFootstepSound(); }
				grounded = true;
				jumpLoadedLeft = true;
				jumpLoadedRight = true;
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

		MoveItems(characterController.transform.position - ccPositionLastFrame);

		//if (characterController.transform.position.y - ccPositionBeforePad.y > 0.01f) {
		if (grounded == true) {
			verticalPusher.transform.localPosition -= new Vector3(0, (characterController.transform.position.y - ccPositionBeforePad.y), 0);
		}

	}

	void MoveItems (Vector3 deltaPosition) {
		if (grabbedRigidbodyLeft == grabbedRigidbodyRight) {
			if (grabbedRigidbodyLeft) {
				grabbedRigidbodyLeft.MovePosition(grabbedRigidbodyLeft.transform.position + deltaPosition);
			}
		} else {
			if (grabbedRigidbodyLeft) {
				grabbedRigidbodyLeft.MovePosition(grabbedRigidbodyLeft.transform.position + deltaPosition);
			}
			if (grabbedRigidbodyRight) {
				grabbedRigidbodyRight.MovePosition(grabbedRigidbodyRight.transform.position + deltaPosition);
			}

		}
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
		characterController.stepOffset = characterController.height / 3.5f;
	}

	void UpdateHandPhysics () {
		// Left Hand
		if (grabbedRigidbodyLeft != null) {
			itemVelocityPercentageLeft = Mathf.Clamp01(itemVelocityPercentageLeft + Time.deltaTime * 2f);
		} else {
			itemVelocityPercentageLeft = Mathf.Clamp01(itemVelocityPercentageLeft - Time.deltaTime * 10);
		}

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
		if (grabbedRigidbodyRight != null) {
			itemVelocityPercentageRight = Mathf.Clamp01(itemVelocityPercentageRight + Time.deltaTime * 2f);
		} else {
			itemVelocityPercentageRight = Mathf.Clamp01(itemVelocityPercentageRight - Time.deltaTime * 10);
		}

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
		if (grabbedRigidbodyLeft != null && grabbedRigidbodyLeft == grabbedRigidbodyRight) {
			// Physics - Dual Wielding
			Rigidbody grabbedItemDominant = (grabDualWieldDominantHand == "Left") ? grabbedRigidbodyLeft : grabbedRigidbodyRight;
			SteamVR_TrackedObject controllerDominant = (grabDualWieldDominantHand == "Left") ? controllerLeft : controllerRight;

			Vector3 dualWieldDirectionCurrent = (((grabDualWieldDominantHand == "Left") ? controllerRight.transform.position : controllerLeft.transform.position) - ((grabDualWieldDominantHand == "Left") ? controllerLeft.transform.position : controllerRight.transform.position));

			Quaternion dualWieldDirectionChangeRotation = Quaternion.FromToRotation(controllerDominant.transform.rotation * grabDualWieldDirection, dualWieldDirectionCurrent);

			Quaternion rotationDeltaItem = (dualWieldDirectionChangeRotation * controllerDominant.transform.rotation * ((grabDualWieldDominantHand == "Left") ? grabRotationLeft : grabRotationRight)) * Quaternion.Inverse(grabbedRigidbodyLeft.transform.rotation);

			float angleItem;
			Vector3 axisItem;
			rotationDeltaItem.ToAngleAxis(out angleItem, out axisItem);
			if (angleItem > 180) {
				angleItem -= 360;
			}

			grabbedRigidbodyLeft.velocity = Vector3.Lerp(grabbedRigidbodyLeft.velocity, Vector3.ClampMagnitude(((controllerDominant.transform.position + (dualWieldDirectionChangeRotation * controllerDominant.transform.rotation * ((grabDualWieldDominantHand == "Left") ? grabOffsetLeft : grabOffsetRight))) - grabbedItemDominant.transform.position) / Time.fixedDeltaTime, (grabbedItemDominant.GetComponent<HingeJoint>()) ? 1 : 100) * Mathf.Lerp(itemVelocityPercentageLeft, itemVelocityPercentageRight, 0.5f), Mathf.Clamp01(50 * Time.deltaTime));

			if (angleItem != float.NaN) {
				grabbedRigidbodyLeft.maxAngularVelocity = Mathf.Infinity;
				grabbedRigidbodyLeft.angularVelocity = Vector3.Lerp(grabbedRigidbodyLeft.angularVelocity, (angleItem * axisItem) * Mathf.Lerp(itemVelocityPercentageLeft, itemVelocityPercentageRight, 0.5f) * 0.95f, Mathf.Clamp01(50 * Time.deltaTime));
			}

			// Accuracy - Dual Wield
			grabbedItemLeft.weapon.accuracyCurrent = Mathf.Clamp(grabbedItemLeft.weapon.accuracyCurrent + (grabbedItemLeft.weapon.accuracyIncrement * Time.deltaTime), grabbedItemLeft.weapon.accuracyMin, grabbedItemLeft.weapon.accuracyMax);

		} else {
			// Physics - Left
			if (grabbedRigidbodyLeft != null) {
				if (grabbedRigidbodyLeft.gameObject.layer == LayerMask.NameToLayer("Item")) {
					grabbedRigidbodyLeft.velocity = Vector3.Lerp(grabbedRigidbodyLeft.velocity, Vector3.ClampMagnitude(((controllerLeft.transform.position + (controllerLeft.transform.rotation * grabOffsetLeft)) - grabbedRigidbodyLeft.transform.position) / Time.fixedDeltaTime, (grabbedRigidbodyLeft.GetComponent<HingeJoint>()) ? 1 : 100) * itemVelocityPercentageLeft, Mathf.Clamp01(50 * Time.deltaTime));

					if (!grabbedRigidbodyLeft.GetComponent<HingeJoint>()) {
						Quaternion rotationDeltaItemLeft = (controllerLeft.transform.rotation * grabRotationLeft) * Quaternion.Inverse(grabbedRigidbodyLeft.transform.rotation);
						float angleItemLeft;
						Vector3 axisItemLeft;
						rotationDeltaItemLeft.ToAngleAxis(out angleItemLeft, out axisItemLeft);
						if (angleItemLeft > 180) {
							angleItemLeft -= 360;
						}

						if (angleItemLeft != float.NaN) {
							grabbedRigidbodyLeft.maxAngularVelocity = Mathf.Infinity;
							grabbedRigidbodyLeft.angularVelocity = Vector3.Lerp(grabbedRigidbodyLeft.angularVelocity, (angleItemLeft * axisItemLeft) * itemVelocityPercentageLeft * 0.95f, Mathf.Clamp01(50 * Time.deltaTime));
						}
					}
				} else {
					//Debug.DrawRay((grabbedRigidbodyLeft.transform.position + (grabbedRigidbodyLeft.transform.rotation * grabOffsetLeft)), Vector3.up, Color.red);
					grabbedRigidbodyLeft.AddForceAtPosition((controllerLeft.transform.position - (grabbedRigidbodyLeft.transform.position + (grabbedRigidbodyLeft.transform.rotation * grabOffsetLeft))) * (grabbedRigidbodyLeft.mass * 0.01f) / Time.fixedDeltaTime, (grabbedRigidbodyLeft.transform.rotation * grabOffsetLeft));
				}
			}

			// Accuracy - Left
			if (grabbedItemLeft) {
				grabbedItemLeft.weapon.accuracyCurrent = Mathf.Clamp(grabbedItemLeft.weapon.accuracyCurrent + (grabbedItemLeft.weapon.accuracyIncrement * Time.deltaTime), grabbedItemLeft.weapon.accuracyMin, grabbedItemLeft.weapon.accuracyMax);
			}
			
			// Physics - Right
			if (grabbedRigidbodyRight != null) {
				if (grabbedRigidbodyRight.gameObject.layer == LayerMask.NameToLayer("Item")) {
					//grabbedRigidbodyRight.velocity = Vector3.Lerp(grabbedRigidbodyRight.velocity, Vector3.ClampMagnitude(((controllerRight.transform.position + (controllerRight.transform.rotation * grabOffsetRight)) - grabbedRigidbodyRight.transform.position) / Time.fixedDeltaTime, (grabbedRigidbodyRight.GetComponent<HingeJoint>()) ? 1 : 100), itemVelocityPercentageRight);
					grabbedRigidbodyRight.velocity = Vector3.Lerp(grabbedRigidbodyRight.velocity, Vector3.ClampMagnitude(((controllerRight.transform.position + (controllerRight.transform.rotation * grabOffsetRight)) - grabbedRigidbodyRight.transform.position) / Time.fixedDeltaTime, (grabbedRigidbodyRight.GetComponent<HingeJoint>()) ? 1 : 100) * itemVelocityPercentageRight, Mathf.Clamp01(50 * Time.deltaTime));

					if (!grabbedRigidbodyRight.GetComponent<HingeJoint>()) {
						Quaternion rotationDeltaItemRight = (controllerRight.transform.rotation * grabRotationRight) * Quaternion.Inverse(grabbedRigidbodyRight.transform.rotation);
						float angleItemRight;
						Vector3 axisItemRight;
						rotationDeltaItemRight.ToAngleAxis(out angleItemRight, out axisItemRight);
						if (angleItemRight > 180) {
							angleItemRight -= 360;
						}

						if (angleItemRight != float.NaN) {
							grabbedRigidbodyRight.maxAngularVelocity = Mathf.Infinity;
							grabbedRigidbodyRight.angularVelocity = Vector3.Lerp(grabbedRigidbodyRight.angularVelocity, (angleItemRight * axisItemRight) * itemVelocityPercentageRight * 0.95f, Mathf.Clamp01(50 * Time.deltaTime));
						}
					}
				} else {
					grabbedRigidbodyRight.AddForceAtPosition((controllerRight.transform.position - (grabbedRigidbodyRight.transform.position + (grabbedRigidbodyRight.transform.rotation * grabOffsetRight))) * (grabbedRigidbodyRight.mass * 0.01f) / Time.fixedDeltaTime, (grabbedRigidbodyRight.transform.rotation * grabOffsetRight));
				}
			}

			// Accuracy - Right
			if (grabbedItemRight) {
				grabbedItemRight.weapon.accuracyCurrent = Mathf.Clamp(grabbedItemRight.weapon.accuracyCurrent + (grabbedItemRight.weapon.accuracyIncrement * Time.deltaTime), grabbedItemRight.weapon.accuracyMin, grabbedItemRight.weapon.accuracyMax);
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

