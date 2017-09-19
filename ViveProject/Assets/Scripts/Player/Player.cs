using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using UnityEngine.PostProcessing;

[RequireComponent (typeof (CharacterController)), RequireComponent(typeof(Entity))]
public class Player : MonoBehaviour {

	[Space(10)]
	[Header("Entity")]
	public Entity entity;

	[Header("Layer Masks")]
	public LayerMask				hmdLayerMask;	// The layerMask for the hmd, defines objects which the player cannot put their head through
	public LayerMask				characterControllerLayerMask;
	public LayerMask				grabLayerMask;

	[Space(10)]
	[Header("Cameras")]
	public Camera					mainCam;

	[Space(10)]
	[Header("Graphics Settings")]
	public PostProcessingProfile	postProcessingProfile;

	[Space(10)]
	[Header("Player GameObject References")]
	public GameObject				head;
	public GameObject				rig;
	public GameObject				flashlight;
	
	[Space(10)]
	[Header("Audio Sources")]
	public AudioManager				audioManager;
	public AudioSource				windLoop;
	public AudioSource				footstep;
	public Vector3					positionLastFootstepPlayed;

	[Space(10)]
	[Header("Grab Infos")]
	public GrabInformation			grabInfoLeft = new GrabInformation();					
	public GrabInformation			grabInfoRight = new GrabInformation();

	[Space(10)]
	[Header("Grabbing Variables")]
	public Vector3 grabDualWieldDirection;             // The vector3 direction from handDominant to handNonDominant (ie: hand right to left direction normalized)
	public string grabDualWieldDominantHand;            // This value determines, when dual wielding an item, which hand is dominant and thus acts as the main pivot point
	public bool wasClimbingLastFrame;               // Records whether or not the player was climbing last frame; used for experimental formula which rotates the player along with it's grabbed object's rotation.		// TODO: do we still need this variable?


	[Space(10)]
	[Header("Hand Infos")]
	public HandInformation			handInfoLeft = new HandInformation();
	public HandInformation			handInfoRight = new HandInformation();

	[Space(10)]
	[Header("Player Object References")]
	// The player's characterController, used to move the player
	public CharacterController		characterController;				// The main character controller, used for player movement and velocity calculations
	public CharacterController		headCC;								// The player's head's character controller, used for mantling over cover, climbing in small spaces, and for detecting collision in walls, ceilings, and floors.
	public GameObject				verticalPusher;                     // Used to push the player's head vertically. This is useful for when a player tries to stand in an inclosed space or duck down into an object. It will keep the player's head from clipping through geometry.
	public SteamVR_TrackedObject	hmd;

	[Space(10)]
	[Header("Constants")]
	float							heightCutoffStanding = 1f;									// If the player is standing above this height, they are considered standing
	float							heightCutoffCrouching = 0.5f;									// If the player is standing avove this height but below the standingCutoff, they are crouching. If the player is below the crouchingCutoff then they are laying
	float							maxLeanDistance = 0.375f;										// The value used to determine how far the player can lean over cover/obstacles. Once the player moves past the leanDistance he/she will be pulled back via rig movement.
	public float					leanDistance;											// The current distance the player is leaning. See maxLeanDistance.
	Vector3							handRigidbodyPositionOffset = new Vector3(-0.02f, -0.025f, -0.075f);
	float							moveSpeedRunning = 6f;
	float							moveSpeedStanding = 4.5f;
	float							moveSpeedCrouching = 2.5f;
	float							moveSpeedLaying = 1.25f;

	[Space(10)]
	[Header("Movement Variables")]
	public Vector3					velocityCurrent;		// The current velocity of the player
	public Vector3					velocityDesired;        // The desired velocity of the player
	float							moveSpeedCurrent;
	float							slopeHighest;
	float							slopeLowest;
	public bool						grounded = false;
	float							timeLastJumped = 0;
	Vector3							platformMovementsAppliedLastFrame = Vector3.zero;
	bool							justStepped;
	bool							padPressed;

	[Space(10)]
	[Header("Positional Variables")]
	public float					heightCurrent;                     // The current height of the hmd from the rig
	Vector3							ccPositionLastFrame;
	
	[System.Serializable]
	public class GrabInformation {
		public Transform	climbableGrabbed;       // The transform of the climbable object being currently grabbed
		public Rigidbody	grabbedRigidbody;       // The rigidbody of the object being currently grabbed
		public Item			grabbedItem;            // The Item component of the object being currently grabbed
		public GrabNode		grabNode;               // The grabNode of the object currently grabbed (for item objects)
		public Quaternion	grabRotation;           // This value is used differently depending on the case. If grabbing an item, it stores the desired rotation of the item relative to the hand's rotation. If grabbing a climbable object, it stores the rotation of the object when first grabbed.
		public Quaternion	grabRotationLastFrame;	// This value is used to record the grabRotation last frame. It is (was) used for an experimental formula which rotates the player along with it's grabbed object's rotation.		// TODO: do we still need this variable?
		public Vector3		grabOffset;             // This value is used differently depending on the case. If grabbing an item, it stores the offset between the hand and the item which when rotated according to rotationOffset is the desired position of the object. If grabbing a climbable object, it stores the offset between the hand and the grabbed object, to determine the desired position of the player
		public Vector3		grabCCOffset;           // If grabbing a climbable object, this variable stores the offset between the hand and the character controller, to determine where to move the player to when climbing
		public float		itemVelocityPercentage; // When items are first picked up their IVP = 0; over time grabbed approaches 1 linearly; this value determines the magnitude of the velocity (and angularVelocity) of the item


		public GrabInformation() {
			climbableGrabbed = null;
			grabbedRigidbody = null;
			grabbedItem = null;
			grabNode = null;
			grabRotation = Quaternion.Euler(0, 0, 0);
			grabRotationLastFrame = Quaternion.Euler(0, 0, 0);
			grabOffset = Vector3.zero;
			grabCCOffset = Vector3.zero;
			itemVelocityPercentage = 0;
		}

	}

	[System.Serializable]
	public class HandInformation {
		public SteamVR_TrackedObject			controller;						// The SteamVR Tracked Object, used to get the position and rotation of the controller
		public SteamVR_Controller.Device		controllerDevice;				// The SteamVR Controller Device for controllers; Used to get input
		public GameObject						handGameObject;					// The player's hand GameObject
		public Rigidbody						handRigidbody;					// The rigidbody of the hand
	
		public Vector3							controllerPosLastFrame;			// Used to determine the jumping velocity when jumping with the hands
		public Vector3							handPosLastFrame;               // Used to determine which direction to throw items
		public bool								itemReleasingDisabled;			// Used for picking up non-misc items. Used to disable item releasing when first grabbing an item as to make the grip function as a toggle rather than an on/off grab button

		public bool								jumpLoaded;
		public HandInformation () {

		}
	}

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
		handInfoLeft.handRigidbody = handInfoLeft.handGameObject.GetComponent<Rigidbody>();
		handInfoRight.handRigidbody = handInfoRight.handGameObject.GetComponent<Rigidbody>();
		audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
		entity = GetComponent<Entity>();
		Debug.Log("Vitals");

		// Subscribe Events
		entity.eventTakeDamage += TakeDamage;

		StartCoroutine(UpdateVitals());
	}

	void Update () {
		CheckSetControllers();
		if (handInfoLeft.controllerDevice.index != 0 && handInfoRight.controllerDevice.index != 0) {
			UpdateControllerInput("Left", grabInfoLeft, grabInfoRight, handInfoLeft, handInfoRight);
			UpdateControllerInput("Right", grabInfoRight, grabInfoLeft, handInfoRight, handInfoLeft);
		}
		UpdatePlayerVelocity();
		UpdatePlayerMovement();

		windLoop.volume = Mathf.Lerp(windLoop.volume, Mathf.Clamp01(((Vector3.Distance(ccPositionLastFrame, characterController.transform.position) + platformMovementsAppliedLastFrame.magnitude) / Time.deltaTime) / 75) - 0.15f, 50 * Time.deltaTime);
		ccPositionLastFrame = characterController.transform.position;
		platformMovementsAppliedLastFrame = Vector3.zero;

		if (grounded == true && Vector3.Distance(new Vector3(characterController.transform.position.x, 0, characterController.transform.position.z), positionLastFootstepPlayed) > 2f) {
			PlayFootstepSound();
		}

		handInfoLeft.controllerPosLastFrame = handInfoLeft.controller.transform.position;
		handInfoRight.controllerPosLastFrame = handInfoRight.controller.transform.position;
	}

	void FixedUpdate () {
		UpdateHandAndItemPhysics();
	}

	void CheckSetControllers () {
		if (handInfoLeft.controllerDevice == null) {
			handInfoLeft.controllerDevice = SteamVR_Controller.Input((int)handInfoLeft.controller.index);
		}

		if (handInfoRight.controllerDevice == null) {
			handInfoRight.controllerDevice = SteamVR_Controller.Input((int)handInfoRight.controller.index);
		}
	}

	void PlayFootstepSound () {
		footstep.volume = Mathf.Clamp01(velocityCurrent.magnitude / 40f);
		footstep.Play();
		positionLastFootstepPlayed = new Vector3(characterController.transform.position.x, 0, characterController.transform.position.z);
	}

	void UpdateControllerInput (string side, GrabInformation grabInfoCurrent, GrabInformation grabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		if (side == "Right") {
			if (handInfoCurrent.controllerDevice.GetPressDown(Valve.VR.EVRButtonId.k_EButton_ApplicationMenu)) {
				flashlight.SetActive(!flashlight.activeSelf);
			}
		}

		if (handInfoCurrent.controllerDevice.GetPress(Valve.VR.EVRButtonId.k_EButton_Grip)) {    // Grip Being Held
			if (handInfoCurrent.controllerDevice.GetPressDown(Valve.VR.EVRButtonId.k_EButton_Grip)) {  // Grip Down
				if (grabInfoCurrent.grabbedRigidbody == true && grabInfoCurrent.grabbedItem && (grabInfoCurrent.grabbedItem is Misc) == false) {
					handInfoCurrent.itemReleasingDisabled = false;
				} else {
					handInfoCurrent.itemReleasingDisabled = true;
				}
				Grab(side, grabInfoCurrent, grabInfoOpposite, handInfoCurrent, handInfoOpposite);
				handInfoCurrent.handGameObject.GetComponent<BoxCollider>().enabled = false;
			}
		}

		if (handInfoCurrent.controllerDevice.GetPressUp(Valve.VR.EVRButtonId.k_EButton_Grip)) {
			if (grabInfoCurrent.grabbedRigidbody == false || ((grabInfoCurrent.grabbedItem && grabInfoCurrent.grabbedItem is Misc) || handInfoCurrent.itemReleasingDisabled == false)) {
				Release(side, grabInfoCurrent, grabInfoOpposite, handInfoCurrent, handInfoOpposite);
				handInfoCurrent.handGameObject.GetComponent<BoxCollider>().enabled = true;
			}
		}

		if (grabInfoCurrent.grabbedItem != null && grabInfoCurrent.grabNode != null) {
			if (handInfoCurrent.controllerDevice.GetHairTriggerDown()) {
				InteractDown(side, grabInfoCurrent);
			}

			if (handInfoLeft.controllerDevice.GetHairTriggerUp()) {
				InteractUp(side, grabInfoCurrent);
			}

			if (handInfoLeft.controllerDevice.GetHairTrigger()) {
				InteractHold(side, grabInfoCurrent);
			} else {
				InteractNull(side, grabInfoCurrent);
			}
		}
	}

	void RotatePlayer (Quaternion rot) {
		Vector3 rigOffset = (rig.transform.position - hmd.transform.position);
		rigOffset.y = 0;
		rig.transform.position = new Vector3(hmd.transform.position.x, rig.transform.position.y, hmd.transform.position.z) + (rot * rigOffset);
		rig.transform.rotation = rig.transform.rotation * rot;
	}

	void Grab (string side, GrabInformation grabInfoCurrent, GrabInformation grabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		if (grabInfoCurrent.climbableGrabbed == null && grabInfoCurrent.grabbedRigidbody == null) {
			// Try and grab something
			Vector3 originPosition = handInfoCurrent.controller.transform.position + (handInfoCurrent.controller.transform.rotation * new Vector3(handRigidbodyPositionOffset.x, handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z));
			Collider[] itemColliders = Physics.OverlapSphere(originPosition, 0.2f, grabLayerMask);
			foreach (Collider hitItem in itemColliders) {
				if (hitItem.transform.gameObject.layer == LayerMask.NameToLayer("Item") || hitItem.transform.gameObject.layer == LayerMask.NameToLayer("Heavy Item")) {
					StartCoroutine(TriggerHapticFeedback(handInfoCurrent.controllerDevice, 0.1f));

					if (hitItem.transform.GetComponent<Rigidbody>()) {
						grabInfoCurrent.grabbedRigidbody = hitItem.transform.GetComponent<Rigidbody>();
					} else if (hitItem.transform.parent.GetComponent<Rigidbody>()) {
						grabInfoCurrent.grabbedRigidbody = hitItem.transform.parent.GetComponent<Rigidbody>();
					} else if (hitItem.transform.parent.parent.GetComponent<Rigidbody>()) {
						grabInfoCurrent.grabbedRigidbody = hitItem.transform.parent.parent.GetComponent<Rigidbody>();
					}

					if (hitItem.transform.GetComponent<Item>()) {
						grabInfoCurrent.grabbedItem = hitItem.transform.GetComponent<Item>();
					} else if (hitItem.transform.parent.GetComponent<Item>()) {
						grabInfoCurrent.grabbedItem = hitItem.transform.parent.GetComponent<Item>();
					} else if (hitItem.transform.parent.parent.GetComponent<Item>()) {
						grabInfoCurrent.grabbedItem = hitItem.transform.parent.parent.GetComponent<Item>();
					}

					grabInfoCurrent.grabbedRigidbody.useGravity = false;

					if (grabInfoCurrent.grabbedRigidbody.transform.gameObject.layer == LayerMask.NameToLayer("Item")) {
						GrabNode hitGrabNode = hitItem.GetComponent<GrabNode>();
						if (hitGrabNode) {
							if (hitGrabNode.referralNode == null) {
								grabInfoCurrent.grabNode = hitGrabNode;
							} else {
								grabInfoCurrent.grabNode = hitGrabNode.referralNode;
							}

							if (grabInfoCurrent.grabNode.grabType == GrabNode.GrabType.FixedPositionRotation) {
								grabInfoCurrent.grabOffset = Quaternion.Euler(grabInfoCurrent.grabNode.rotation) * (-grabInfoCurrent.grabNode.transform.localPosition - grabInfoCurrent.grabNode.offset) + new Vector3(0, handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z);
								grabInfoCurrent.grabRotation = Quaternion.Euler(grabInfoCurrent.grabNode.rotation);
							} else if (grabInfoCurrent.grabNode.grabType == GrabNode.GrabType.FixedPosition) {
								grabInfoCurrent.grabOffset = Quaternion.Euler(grabInfoCurrent.grabNode.rotation) * (-grabInfoCurrent.grabNode.transform.localPosition - grabInfoCurrent.grabNode.offset) + new Vector3(0, handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z);
								grabInfoCurrent.grabRotation = Quaternion.Inverse(handInfoCurrent.controller.transform.rotation) * grabInfoCurrent.grabbedRigidbody.transform.rotation;
							} else if (grabInfoCurrent.grabNode.grabType == GrabNode.GrabType.Dynamic || grabInfoCurrent.grabNode.grabType == GrabNode.GrabType.Referral) {
								grabInfoCurrent.grabOffset = Quaternion.Inverse(handInfoCurrent.controller.transform.rotation) * (grabInfoCurrent.grabbedRigidbody.transform.position - handInfoCurrent.controller.transform.position);
								grabInfoCurrent.grabRotation = Quaternion.Inverse(handInfoCurrent.controller.transform.rotation) * grabInfoCurrent.grabbedRigidbody.transform.rotation;
							}
						} else {
							grabInfoCurrent.grabOffset = Quaternion.Inverse(handInfoCurrent.controller.transform.rotation) * (grabInfoCurrent.grabbedRigidbody.transform.position - handInfoCurrent.controller.transform.position);
							grabInfoCurrent.grabRotation = Quaternion.Inverse(handInfoCurrent.controller.transform.rotation) * grabInfoCurrent.grabbedRigidbody.transform.rotation;
						}
						if (grabInfoCurrent.grabbedRigidbody == grabInfoOpposite.grabbedRigidbody) {      // Is the other hand already holding this item?
							grabInfoCurrent.itemVelocityPercentage = 0;
							grabInfoOpposite.itemVelocityPercentage = 0;
							if (grabInfoCurrent.grabNode && grabInfoOpposite.grabNode) {
								if (grabInfoCurrent.grabNode.dominance > grabInfoOpposite.grabNode.dominance) {
									grabDualWieldDominantHand = side;
									grabDualWieldDirection = Quaternion.Euler(grabInfoCurrent.grabNode.rotation) * Quaternion.Inverse(grabInfoCurrent.grabbedRigidbody.transform.rotation) * (handInfoOpposite.controller.transform.position - (grabInfoCurrent.grabNode.transform.position + grabInfoCurrent.grabbedRigidbody.transform.rotation * -grabInfoCurrent.grabNode.offset));
								} else if (grabInfoCurrent.grabNode.dominance < grabInfoOpposite.grabNode.dominance) {
									grabDualWieldDominantHand = side;
									grabDualWieldDirection = Quaternion.Inverse(handInfoOpposite.controller.transform.rotation) * (handInfoCurrent.controller.transform.position - handInfoOpposite.controller.transform.position);
								}
							} else {
								grabDualWieldDominantHand = (side == "Right" ? "Left" : "Right");
								grabDualWieldDirection = Quaternion.Inverse(handInfoOpposite.controller.transform.rotation) * (handInfoCurrent.controller.transform.position - handInfoOpposite.controller.transform.position);
							}
						}
					} else if (grabInfoCurrent.grabbedRigidbody.transform.gameObject.layer == LayerMask.NameToLayer("Heavy Item")) {
						grabInfoCurrent.grabOffset = Quaternion.Inverse(grabInfoCurrent.grabbedRigidbody.transform.rotation) * (handInfoCurrent.controller.transform.position - grabInfoCurrent.grabbedRigidbody.transform.position);
						grabInfoCurrent.grabRotation = grabInfoCurrent.grabbedRigidbody.transform.rotation;
					}
					return;
				}
			}

			Collider[] climbColliders = Physics.OverlapSphere(originPosition, 0.065f, grabLayerMask);
			foreach (Collider hitClimb in climbColliders) {
				if (hitClimb.transform.gameObject.layer == LayerMask.NameToLayer("Climbable") || (hitClimb.transform.gameObject.layer == LayerMask.NameToLayer("Environment") && (hmd.transform.position.y - rig.transform.position.y < heightCutoffCrouching))) {
					grabInfoCurrent.grabOffset = characterController.transform.position - hitClimb.transform.position;
					grabInfoCurrent.grabRotation = hitClimb.transform.rotation;
					grabInfoCurrent.grabCCOffset = handInfoCurrent.controller.transform.position - characterController.transform.position;
					grabInfoCurrent.climbableGrabbed = hitClimb.transform;
					StartCoroutine(TriggerHapticFeedback(handInfoCurrent.controllerDevice, 0.1f));
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
							grabInfoCurrent.grabOffset = characterController.transform.position - environmentHit.transform.position;
							grabInfoCurrent.grabRotation = environmentHit.transform.rotation;
							grabInfoCurrent.grabCCOffset = handInfoCurrent.controller.transform.position - characterController.transform.position;
							grabInfoCurrent.climbableGrabbed = environmentHit.transform;
							StartCoroutine(TriggerHapticFeedback(handInfoCurrent.controllerDevice, 0.1f));
							return;
						}
					}
				}
			}
		}
	}

	void Release(string side, GrabInformation grabInfoCurrent, GrabInformation grabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		grabInfoCurrent.grabOffset = Vector3.zero;
		grabInfoCurrent.grabCCOffset = Vector3.zero;
		if (grabInfoOpposite.climbableGrabbed == null && grabInfoCurrent.climbableGrabbed == true) {
			AttemptClamber();
		}
		if (grabInfoCurrent.grabbedRigidbody == true) {
			if (grabInfoOpposite.grabbedRigidbody != grabInfoCurrent.grabbedRigidbody) {
				ThrowItem(grabInfoCurrent.grabbedRigidbody, (handInfoCurrent.controller.transform.position - handInfoCurrent.handPosLastFrame) / Time.deltaTime);
			} else {
				if (grabInfoOpposite.grabNode == null) {
					grabInfoCurrent.itemVelocityPercentage = 0;
					grabInfoOpposite.itemVelocityPercentage = 0;
					grabInfoOpposite.grabOffset = Quaternion.Inverse(handInfoOpposite.controller.transform.rotation) * (grabInfoOpposite.grabbedRigidbody.transform.position - handInfoOpposite.controller.transform.position);
					grabInfoOpposite.grabRotation = Quaternion.Inverse(handInfoOpposite.controller.transform.rotation) * grabInfoOpposite.grabbedRigidbody.transform.rotation;
				}
			}
		}
		if (handInfoCurrent.jumpLoaded == true) {
			if ((grounded == true || timeLastJumped + 0.1f > Time.timeSinceLevelLoad) && grabInfoCurrent.climbableGrabbed == false && grabInfoCurrent.grabbedRigidbody == false) {
				velocityCurrent += Vector3.ClampMagnitude((handInfoCurrent.controllerPosLastFrame - handInfoCurrent.controller.transform.position) * 500, (timeLastJumped + 0.1f > Time.timeSinceLevelLoad) ? 1f : 5f);
				handInfoCurrent.jumpLoaded = false;
				if (grounded == true) { timeLastJumped = Time.timeSinceLevelLoad; }
			}
		}
		grabInfoCurrent.climbableGrabbed = null;
		grabInfoCurrent.grabbedRigidbody = null;
		grabInfoCurrent.grabbedItem = null;
		grabInfoCurrent.grabNode = null;
	}

	void InteractDown (string side, GrabInformation grabInfoCurrent) {
		if (grabInfoCurrent.grabbedItem is Weapon) {
			Weapon currentWeapon = grabInfoCurrent.grabbedItem as Weapon;
			if (grabInfoCurrent.grabNode.interactionType == GrabNode.InteractionType.Trigger) {
				if (currentWeapon.chargingEnabled == false) {
					AttemptToFireWeapon(side, grabInfoCurrent.grabbedItem);
				}
			}
		}
	}

	void InteractHold (string side, GrabInformation grabInfoCurrent) {
		if (grabInfoCurrent.grabbedItem is Weapon) {
			Weapon currentWeapon = grabInfoCurrent.grabbedItem as Weapon;
			currentWeapon.triggerHeld = true;
			if (grabInfoCurrent.grabNode.interactionType == GrabNode.InteractionType.Trigger) {
				if (currentWeapon.automatic == true) {
					AttemptToFireWeapon(side, grabInfoCurrent.grabbedItem);
				}
				if (currentWeapon.chargingEnabled == true) {
					currentWeapon.chargeCurrent = Mathf.Clamp01(currentWeapon.chargeCurrent + (currentWeapon.chargeIncrement * Time.deltaTime));
				}
			}
		}
	}

	void InteractUp(string side, GrabInformation grabInfoCurrent) {
		if (grabInfoCurrent.grabbedItem is Weapon) {
			Weapon currentWeapon = grabInfoCurrent.grabbedItem as Weapon;
			currentWeapon.triggerHeld = false;
			if (grabInfoCurrent.grabNode.interactionType == GrabNode.InteractionType.Trigger) {
				if (currentWeapon.chargingEnabled == true) {
					AttemptToFireWeapon(side, grabInfoCurrent.grabbedItem);
				}
			}
		}
	}

	void InteractNull (string side, GrabInformation grabInfoCurrent) {
		if (grabInfoCurrent.grabbedItem is Weapon) {
			Weapon currentWeapon = grabInfoCurrent.grabbedItem as Weapon;
			currentWeapon.triggerHeld = false;
		}
	}

	void AttemptToFireWeapon (string side, Item currentItem) {
		Weapon currentWeapon = currentItem as Weapon;
		if (currentWeapon.timeLastFired + (1 / currentWeapon.firerate) <= Time.timeSinceLevelLoad) {
			if (currentWeapon.chargingEnabled == false || (currentWeapon.chargeCurrent >= currentWeapon.chargeRequired)) {
				StartCoroutine(FireWeapon(side, currentItem));
				currentWeapon.timeLastFired = Time.timeSinceLevelLoad;
			}
		}
	}

	IEnumerator FireWeapon(string hand, Item currentItem) {
		Weapon currentWeapon = currentItem as Weapon;
		Rigidbody currentRigidbody = currentItem.transform.GetComponent<Rigidbody>();
		Transform barrel = currentItem.transform.Find("(Barrel Point)");

		for (int i = 0; i < Mathf.Clamp(currentWeapon.burstCount, 1, 100); i++) {           // For each burst shot in this fire
			
			// Step 1: Trigger haptic feedback
			if (grabInfoLeft.grabbedRigidbody == grabInfoRight.grabbedRigidbody) {
				StartCoroutine(TriggerHapticFeedback(handInfoLeft.controllerDevice, 0.1f));
				StartCoroutine(TriggerHapticFeedback(handInfoRight.controllerDevice, 0.1f));
			} else {
				StartCoroutine(TriggerHapticFeedback((hand == "Left" ? handInfoLeft.controllerDevice : handInfoRight.controllerDevice), 0.1f));
			}

			// Step 2: Apply velocity and angular velocity to weapon
			if (grabInfoLeft.grabbedItem == grabInfoRight.grabbedItem) {
				grabInfoLeft.itemVelocityPercentage = 0f;
				grabInfoRight.itemVelocityPercentage = 0f;
				currentRigidbody.velocity += (currentRigidbody.transform.forward * -currentWeapon.recoilLinear * 0.5f);
				currentRigidbody.angularVelocity += new Vector3(Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular)) * 0.5f;
			} else {
				currentRigidbody.velocity += (currentRigidbody.transform.forward * -currentWeapon.recoilLinear);
				currentRigidbody.angularVelocity += new Vector3(Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular));
				if (hand == "Left") {
					grabInfoLeft.itemVelocityPercentage = 0;
				} else if (hand == "Right") {
					grabInfoRight.itemVelocityPercentage = 0;
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
				newProjectileClass.lifespan = currentWeapon.projectileLifespan;
				newProjectileClass.sticky = currentWeapon.projectileIsSticky;
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
		if (item.transform.GetComponent<Item>() != null) {
			if (item.GetComponent<Item>() is Weapon) {
				(item.transform.GetComponent<Item>() as Weapon).triggerHeld = false;
			}
		}
	}

	IEnumerator TriggerHapticFeedback(SteamVR_Controller.Device device, float duration) {
		for (float i = 0; i <= duration; i += 0.01f) {
			device.TriggerHapticPulse(3999);
			yield return new WaitForSeconds(0.01f);
		}
	}

	void UpdatePlayerVelocity () {
		float slopeSpeed = ((-slopeHighest + 45) / (characterController.slopeLimit * 2)) + 0.5f;
		moveSpeedCurrent = Mathf.Lerp(moveSpeedCurrent, (heightCurrent > heightCutoffStanding ? (padPressed ? moveSpeedRunning : moveSpeedStanding) : (heightCurrent > heightCutoffCrouching ? moveSpeedCrouching : moveSpeedLaying)) * slopeSpeed, 5 * Time.deltaTime);
		velocityDesired = new Vector3(handInfoLeft.controllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad).x, velocityDesired.y, handInfoLeft.controllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad).y) * moveSpeedCurrent;
		velocityDesired = Quaternion.LookRotation(new Vector3(hmd.transform.forward.x, 0, hmd.transform.forward.z), Vector3.up) * velocityDesired;
		if (grabInfoLeft.climbableGrabbed == null && grabInfoRight.climbableGrabbed == null) {
			wasClimbingLastFrame = false;
			if (grounded == true) {
				velocityCurrent = Vector3.Lerp(velocityCurrent, new Vector3(velocityDesired.x, velocityCurrent.y, velocityDesired.z), 25 * Time.deltaTime);
			} else {
				if (handInfoLeft.controllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad) != Vector2.zero) {
					Vector3 normalizedVelocityDesired = new Vector3(velocityDesired.x, 0, velocityDesired.z).normalized * (Mathf.Clamp01(new Vector3(velocityDesired.x, 0, velocityDesired.z).magnitude) * Mathf.Clamp(new Vector3(velocityCurrent.x, 0, velocityCurrent.z).magnitude, moveSpeedCurrent, Mathf.Infinity));
					velocityCurrent = Vector3.Lerp(velocityCurrent, new Vector3(normalizedVelocityDesired.x, velocityCurrent.y, normalizedVelocityDesired.z), 2.5f * Time.deltaTime);
				}
			}
		} else {
			Vector3 combinedClimbPositions = Vector3.zero;
			Quaternion climbRotationLeft = Quaternion.Euler(0, 0, 0);
			Quaternion climbRotationRight = Quaternion.Euler(0, 0, 0);
			int climbCount = 0;

			if (grabInfoLeft.climbableGrabbed == true) {
				climbRotationLeft = Quaternion.Inverse(grabInfoLeft.grabRotation) * grabInfoLeft.climbableGrabbed.rotation;
			}

			if (grabInfoRight.climbableGrabbed == true) {
				climbRotationRight = Quaternion.Inverse(grabInfoRight.grabRotation) * grabInfoRight.climbableGrabbed.rotation;
			}

			if (grabInfoLeft.climbableGrabbed == true) {
				combinedClimbPositions += (grabInfoLeft.climbableGrabbed.position + climbRotationLeft * grabInfoLeft.grabOffset) + (climbRotationLeft * grabInfoLeft.grabCCOffset - (handInfoLeft.controller.transform.position - characterController.transform.position));
				climbCount++;
			}

			if (grabInfoRight.climbableGrabbed == true) {
				combinedClimbPositions += (grabInfoRight.climbableGrabbed.position + climbRotationRight * grabInfoRight.grabOffset) + (climbRotationRight * grabInfoRight.grabCCOffset - (handInfoRight.controller.transform.position - characterController.transform.position));
				climbCount++;
			}

			combinedClimbPositions = combinedClimbPositions / climbCount;

			velocityCurrent = Vector3.Lerp(velocityCurrent, (combinedClimbPositions - characterController.transform.position) / Time.deltaTime, 25 * Time.deltaTime);

			if (wasClimbingLastFrame == true) {
				if (grabInfoLeft.climbableGrabbed) {
					if (grabInfoRight.climbableGrabbed) {
						//RotatePlayer(Quaternion.Slerp(Quaternion.Inverse(grabInfoLeft.grabRotationLastFrame) * climbRotationLeft, Quaternion.Inverse(grabInfoRight.grabRotationLastFrame) * climbRotationRight, 0.5f));
					} else {
						//RotatePlayer(Quaternion.Inverse(grabInfoLeft.grabRotationLastFrame) * climbRotationLeft);
					}
				} else {
					if (grabInfoRight.climbableGrabbed) {
						//RotatePlayer(Quaternion.Inverse(grabInfoRight.grabRotationLastFrame) * climbRotationRight);
					} else {
						Debug.LogWarning("Why are we here?");
					}
				}
			}

			grabInfoLeft.grabRotationLastFrame = climbRotationLeft;
			grabInfoRight.grabRotationLastFrame = climbRotationRight;
			wasClimbingLastFrame = true;
		}
		handInfoLeft.handPosLastFrame = handInfoLeft.controller.transform.position;
		handInfoRight.handPosLastFrame = handInfoRight.controller.transform.position;

	}

	void UpdatePlayerMovement () {
		if (grabInfoLeft.climbableGrabbed == null && grabInfoRight.climbableGrabbed == null) {
			SetCharacterControllerHeight((hmd.transform.position.y - (rig.transform.position.y)) + 0.25f);
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
		if (grabInfoLeft.climbableGrabbed == null && grabInfoRight.climbableGrabbed == null) {
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
				if (grabInfoLeft.climbableGrabbed == null && grabInfoRight.climbableGrabbed == null && velocityCurrent.y < 0) {
					if (grounded == false) { PlayFootstepSound(); }
					grounded = true;
					handInfoLeft.jumpLoaded = true;
					handInfoRight.jumpLoaded = true;
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
				handInfoLeft.jumpLoaded = true;
				handInfoRight.jumpLoaded = true;
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
			Vector3 difference = new Vector3(0, (characterController.transform.position.y - ccPositionBeforePad.y), 0);
			verticalPusher.transform.localPosition -= difference;
			if (Mathf.Abs(difference.y) > 0.1f) {
				justStepped = true;
			} else {
				justStepped = false;
			}
		}

	}

	void MoveItems (Vector3 deltaPosition) {
		if (grabInfoLeft.grabbedRigidbody == grabInfoRight.grabbedRigidbody) {
			if (grabInfoLeft.grabbedRigidbody) {
				grabInfoLeft.grabbedRigidbody.MovePosition(grabInfoLeft.grabbedRigidbody.transform.position + deltaPosition);
			}
		} else {
			if (grabInfoLeft.grabbedRigidbody) {
				grabInfoLeft.grabbedRigidbody.MovePosition(grabInfoLeft.grabbedRigidbody.transform.position + deltaPosition);
			}
			if (grabInfoRight.grabbedRigidbody) {
				grabInfoRight.grabbedRigidbody.MovePosition(grabInfoRight.grabbedRigidbody.transform.position + deltaPosition);
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
		Vector3 handPositionBeforeLeft = handInfoLeft.handGameObject.transform.position;
		Vector3 handPositionBeforeRight = handInfoRight.handGameObject.transform.position;

		characterController.transform.position = new Vector3(characterController.transform.position.x, (hmd.transform.position.y + rig.transform.position.y) / 2, characterController.transform.position.z);
		characterController.height = Mathf.Clamp(desiredHeight, characterController.radius * 2f, Mathf.Infinity);
		characterController.stepOffset = characterController.height / 3.5f;

		handInfoLeft.handGameObject.transform.position = handPositionBeforeLeft;
		handInfoRight.handGameObject.transform.position = handPositionBeforeRight;
	}

	void UpdateHandAndItemPhysics () {
		// Left Hand
		grabInfoLeft.itemVelocityPercentage = Mathf.Clamp01(grabInfoLeft.itemVelocityPercentage + Time.deltaTime * (grabInfoLeft.grabbedRigidbody != null ? 2 : -10));
		UpdateHandPhysics("Left", handInfoLeft);

		// Right Hand
		grabInfoRight.itemVelocityPercentage = Mathf.Clamp01(grabInfoRight.itemVelocityPercentage + Time.deltaTime * (grabInfoRight.grabbedRigidbody != null ? 2 : -10));
		UpdateHandPhysics("Right", handInfoRight);

		Weapon weaponLeft = null;
		Weapon weaponRight = null;

		if (grabInfoLeft.grabbedItem is Weapon) {
			weaponLeft = grabInfoLeft.grabbedItem as Weapon;
			weaponRight = grabInfoRight.grabbedItem as Weapon;
		}

		// Item Physics
		if (grabInfoLeft.grabbedRigidbody != null && grabInfoLeft.grabbedRigidbody == grabInfoRight.grabbedRigidbody) {
			// Physics - Dual Wielding
			Rigidbody grabbedItemDominant = (grabDualWieldDominantHand == "Left") ? grabInfoLeft.grabbedRigidbody : grabInfoRight.grabbedRigidbody;
			SteamVR_TrackedObject controllerDominant = (grabDualWieldDominantHand == "Left") ? handInfoLeft.controller : handInfoRight.controller;
			Vector3 dualWieldDirectionCurrent = (((grabDualWieldDominantHand == "Left") ? handInfoRight.controller.transform.position : handInfoLeft.controller.transform.position) - ((grabDualWieldDominantHand == "Left") ? handInfoLeft.controller.transform.position : handInfoRight.controller.transform.position));
			Quaternion dualWieldDirectionChangeRotation = Quaternion.FromToRotation(controllerDominant.transform.rotation * grabDualWieldDirection, dualWieldDirectionCurrent);
			Quaternion rotationDeltaItem = (dualWieldDirectionChangeRotation * controllerDominant.transform.rotation * ((grabDualWieldDominantHand == "Left") ? grabInfoLeft.grabRotation : grabInfoRight.grabRotation)) * Quaternion.Inverse(grabInfoLeft.grabbedRigidbody.transform.rotation);

			float angleItem;
			Vector3 axisItem;
			rotationDeltaItem.ToAngleAxis(out angleItem, out axisItem);
			if (angleItem > 180) {
				angleItem -= 360;
			}

			grabInfoLeft.grabbedRigidbody.velocity = Vector3.Lerp(grabInfoLeft.grabbedRigidbody.velocity, Vector3.ClampMagnitude(((controllerDominant.transform.position + (dualWieldDirectionChangeRotation * controllerDominant.transform.rotation * ((grabDualWieldDominantHand == "Left") ? grabInfoLeft.grabOffset : grabInfoRight.grabOffset))) - grabbedItemDominant.transform.position) / Time.fixedDeltaTime, (grabbedItemDominant.GetComponent<HingeJoint>()) ? 1 : 100) * Mathf.Lerp(grabInfoLeft.itemVelocityPercentage, grabInfoRight.itemVelocityPercentage, 0.5f), Mathf.Clamp01(50 * Time.deltaTime));

			if (angleItem != float.NaN) {
				grabInfoLeft.grabbedRigidbody.maxAngularVelocity = Mathf.Infinity;
				grabInfoLeft.grabbedRigidbody.angularVelocity = Vector3.Lerp(grabInfoLeft.grabbedRigidbody.angularVelocity, (angleItem * axisItem) * Mathf.Lerp(grabInfoLeft.itemVelocityPercentage, grabInfoRight.itemVelocityPercentage, 0.5f) * 0.95f, Mathf.Clamp01(50 * Time.deltaTime));
			}

			// Accuracy - Dual Wield
			if (grabInfoLeft.grabbedItem is Weapon) {
				weaponLeft.accuracyCurrent = Mathf.Clamp(weaponLeft.accuracyCurrent + (weaponLeft.accuracyIncrement * Time.deltaTime), weaponLeft.accuracyMin, weaponLeft.accuracyMax);
			}
			
		} else {
			// Physics - Left
			if (grabInfoLeft.grabbedItem) {
				UpdateItemPhysics("Left", grabInfoLeft.grabbedRigidbody, handInfoLeft.controller);
				if (weaponLeft) {
					weaponLeft.accuracyCurrent = Mathf.Clamp(weaponLeft.accuracyCurrent + (weaponLeft.accuracyIncrement * Time.deltaTime), weaponLeft.accuracyMin, weaponLeft.accuracyMax);
				}
			}

			// Physics - Right
			if (grabInfoRight.grabbedItem) {
				UpdateItemPhysics("Right", grabInfoRight.grabbedRigidbody, handInfoRight.controller);
				if (weaponLeft) {
					weaponRight.accuracyCurrent = Mathf.Clamp(weaponRight.accuracyCurrent + (weaponRight.accuracyIncrement * Time.deltaTime), weaponRight.accuracyMin, weaponRight.accuracyMax);
				}
			}
		}
	}

	void UpdateHandPhysics(string side, HandInformation handInfoCurrent) {
		handInfoCurrent.handRigidbody.velocity = (((handInfoCurrent.controller.transform.position + handInfoCurrent.controller.transform.rotation * new Vector3(handRigidbodyPositionOffset.x * (side == "Left" ? 1 : -1), handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z)) - handInfoCurrent.handGameObject.transform.position) + verticalPusher.transform.localPosition) / Time.fixedDeltaTime;
		Quaternion rotationDelta = (Quaternion.AngleAxis(30, handInfoCurrent.controller.transform.right) * handInfoCurrent.controller.transform.rotation) * Quaternion.Inverse(handInfoCurrent.handRigidbody.transform.rotation);
		float angle;
		Vector3 axis;
		rotationDelta.ToAngleAxis(out angle, out axis);
		if (angle > 180) {
			angle -= 360;
		}

		if (angle != float.NaN) {
			handInfoCurrent.handRigidbody.maxAngularVelocity = Mathf.Infinity;
			handInfoCurrent.handRigidbody.angularVelocity = (angle * axis);
		}
	}

	void UpdateItemPhysics (string hand, Rigidbody itemRigidbodyCurrent, SteamVR_TrackedObject controllerCurrent) {
		if (justStepped == false) {
			if (itemRigidbodyCurrent.gameObject.layer == LayerMask.NameToLayer("Item")) {
				itemRigidbodyCurrent.velocity = Vector3.Lerp(itemRigidbodyCurrent.velocity, Vector3.ClampMagnitude(((controllerCurrent.transform.position + (controllerCurrent.transform.rotation * (hand == "Left" ? grabInfoLeft.grabOffset : grabInfoRight.grabOffset))) - itemRigidbodyCurrent.transform.position) / Time.fixedDeltaTime, (itemRigidbodyCurrent.GetComponent<HingeJoint>()) ? 1 : 100) * (hand == "Left" ? grabInfoLeft.itemVelocityPercentage : grabInfoRight.itemVelocityPercentage), Mathf.Clamp01(50 * Time.deltaTime));

				if (!itemRigidbodyCurrent.GetComponent<HingeJoint>()) {
					Quaternion rotationDeltaItem = (controllerCurrent.transform.rotation * (hand == "Left" ? grabInfoLeft.grabRotation : grabInfoRight.grabRotation)) * Quaternion.Inverse(itemRigidbodyCurrent.transform.rotation);
					float angleItem;
					Vector3 axisItem;
					rotationDeltaItem.ToAngleAxis(out angleItem, out axisItem);
					if (angleItem > 180) {
						angleItem -= 360;
					}

					if (angleItem != float.NaN) {
						itemRigidbodyCurrent.maxAngularVelocity = Mathf.Infinity;
						itemRigidbodyCurrent.angularVelocity = Vector3.Lerp(itemRigidbodyCurrent.angularVelocity, (angleItem * axisItem) * (hand == "Left" ? grabInfoLeft.itemVelocityPercentage : grabInfoRight.itemVelocityPercentage) * 0.95f, Mathf.Clamp01(50 * Time.deltaTime));
					}
				}
			}
		}
	}

	public void TakeDamage () {
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
