using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using UnityEngine.PostProcessing;

[RequireComponent(typeof(CharacterController)), RequireComponent(typeof(Entity))]
public class Player : MonoBehaviour {

	[Header("Entity")]
	public Entity entity;

	[Space(10)][Header("Layer Masks")]
	public LayerMask hmdLayerMask;  // The layerMask for the hmd, defines objects which the player cannot put their head through
	public LayerMask bodyCCLayerMask;
	public LayerMask grabLayerMask;
	public LayerMask pocketMask;

	[Space(10)][Header("Cameras")]
	public Camera mainCam;

	[Space(10)][Header("Graphics Settings")]
	public PostProcessingProfile postProcessingProfile;

	[Space(10)][Header("Player GameObject References")]
	public GameObject head;
	public GameObject rig;
	public GameObject flashlight;
	public GameObject headlight;
	public GameObject torso;

	[Space(10)][Header("Avatar References")]
	public Transform avatar;
	public Transform avatarTorso;

	[Space(10)][Header("Audio Sources")]
	public AudioManager audioManager;
	public AudioSource windLoop;
	public AudioSource footstep;
	public Vector3 positionLastFootstepPlayed;

	[Space(10)][Header("Grab Infos")]
	public GrabInformation grabInfoLeft = new GrabInformation();
	public GrabInformation grabInfoRight = new GrabInformation();

	[Space(10)]
	[Header("Grabbing Variables")]
	public Vector3 grabDualWieldDirection;             // The vector3 direction from handDominant to handNonDominant (ie: hand right to left direction normalized)
	public string grabDualWieldDominantHand;            // This value determines, when dual wielding an item, which hand is dominant and thus acts as the main pivot point
	public bool isClimbing;								// Records whether or not the player is climbing


	[Space(10)][Header("Hand Infos")]
	public HandInformation handInfoLeft = new HandInformation();
	public HandInformation handInfoRight = new HandInformation();

	[Space(10)][Header("Player Object References")]
	// The player's characterController, used to move the player
	public CharacterController bodyCC;						// The body's character controller, used for player movement and velocity calculations
	public CharacterController headCC;                      // The player's head's character controller, used for mantling over cover, climbing in small spaces, and for detecting collision in walls, ceilings, and floors.
	public GameObject verticalPusher;						// Used to push the player's head vertically. This is useful for when a player tries to stand in an inclosed space or duck down into an object. It will keep the player's head from clipping through geometry.
	public SteamVR_TrackedObject hmd;

	[Space(10)][Header("Constants")]
	float heightCutoffStanding = 1f;                                    // If the player is standing above this height, they are considered standing
	float heightCutoffCrouching = 0.5f;                                 // If the player is standing avove this height but below the standingCutoff, they are crouching. If the player is below the crouchingCutoff then they are laying
	float maxLeanDistance = 0.45f;										// The value used to determine how far the player can lean over cover/obstacles. Once the player moves past the leanDistance he/she will be pulled back via rig movement.
	public float leanDistance;                                          // The current distance the player is leaning. See maxLeanDistance.
	Vector3 handRigidbodyPositionOffset = new Vector3(-0.02f, -0.025f, -0.075f);
	float moveSpeedStanding = 4f;
	float moveSpeedCrouching = 2f;
	float moveSpeedLaying = 1f;

	[Space(10)][Header("Movement Variables")]
	public Vector3 velocityCurrent;     // The current velocity of the player
	public Vector3 velocityDesired;        // The desired velocity of the player
	float moveSpeedCurrent;
	public bool grounded = false;
	Vector3 groundNormal;
	float groundedTime;                             // Time the player has been grounded foor
	float timeLastJumped = 0;
	Vector3 platformMovementsAppliedLastFrame = Vector3.zero;
	bool justStepped;
	bool padPressed;

	[Space(10)][Header("Positional Variables")]
	public float heightCurrent;                     // The current height of the hmd from the rig
	float bodyHeightChangeSpeed = 1;                  // A value which is near 1 when climbing/grounded, but near 0 when airborne; this value changes how quickly the bodyCC's height is changed

	float handOffsetPositionMax = 0.5f;

	[System.Serializable]
	public class GrabInformation {
		public Transform climbableGrabbed;			// The transform of the climbable object being currently grabbed
		public Rigidbody grabbedRigidbody;			// The rigidbody of the object being currently grabbed
		public Item grabbedItem;					// The Item component of the object being currently grabbed
		public GrabNode grabNode;					// The grabNode of the object currently grabbed (for item objects)
		public Quaternion grabRotation;				// This value is used differently depending on the case. If grabbing an item, it stores the desired rotation of the item relative to the hand's rotation. If grabbing a climbable object, it stores the rotation of the object when first grabbed.
		public Quaternion grabRotationLastFrame;	// This value is used to record the grabRotation last frame. It is (was) used for an experimental formula which rotates the player along with it's grabbed object's rotation.		// TODO: do we still need this variable?
		public Vector3 grabOffset;					// This value is used differently depending on the case. If grabbing an item, it stores the offset between the hand and the item which when rotated according to rotationOffset is the desired position of the object. If grabbing a climbable object, it stores the offset between the hand and the grabbed object, to determine the desired position of the player
		public Vector3 grabCCOffset;                // If grabbing a climbable object, this variable stores the offset between the hand and the character controller, to determine where to move the player to when climbing
		public Vector3 grabPoint;					// (For dynamic-grabNode or non-grabNode items) records the exact point at which the item was grabbed
		public float itemVelocityPercentage;        // When items are first picked up their IVP = 0; over time grabbed approaches 1 linearly; this value determines the magnitude of the velocity (and angularVelocity) of the item
		public string side;							// Which side hand is this? "Left" or "Right"

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
			side = "null";
		}

	}

	[System.Serializable]
	public class HandInformation {
		public SteamVR_TrackedObject controller;				// The SteamVR Tracked Object, used to get the position and rotation of the controller
		public SteamVR_Controller.Device controllerDevice;		// The SteamVR Controller Device for controllers; Used to get input
		public GameObject handGameObject;						// The player's hand GameObject
		public Rigidbody handRigidbody;							// The rigidbody of the hand

		public Vector3 controllerPosLastFrame;					// Used to determine the jumping velocity when jumping with the hands
		public Vector3 handPosLastFrame;						// Used to determine which direction to throw items
		public bool itemReleasingDisabled;                      // Used for picking up non-misc items. Used to disable item releasing when first grabbing an item as to make the grip function as a toggle rather than an on/off grab button

		public Vector3 handOffsetPosition;
		public Vector3 handOffsetRotation;

		public bool jumpLoaded;
		public HandInformation() {

		}
	}

	/*	Concept for step by step player movement process:
	 *	
	 *	OLD:
	 *	
	 * step 1: Get change in hmd move position
	 * step 2: Adjust the rig position to account for hmd collision
	 * step 3: Attempt to move the bodyCC to that position horizontally
	 *	(ignore vertical positionChange; hmd move does not move camera rig)
	 * step 4: If hmd leanDistance is too far from the character controller and we cannot move the character controller there, then pull back the hmd
	 * (unless there is a large enough gap for the player to move, so teleport the bodyCC through the gap)
	 * step 5: Record hmd leanDistance as horizontal distance from center of character controller
	 * step 6: Get secondary controller trackpad input as character controller velocity
	 * step 7: Move character controller to velocity
	 * step 8: Check again if we can compensate for leanDistance
	 * step 9: Move camera rig to compensate
	 * 
	 * NEW: (11/30/17)
	 * 
	 */

	void Start() {
		bodyCC = GetComponent<CharacterController>();          // Grab the character controller at the start
		avatar = transform.parent.Find("Avatar");
		avatarTorso = avatar.Find("Torso");
		handInfoLeft.handRigidbody = handInfoLeft.handGameObject.GetComponent<Rigidbody>();
		handInfoRight.handRigidbody = handInfoRight.handGameObject.GetComponent<Rigidbody>();
		audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
		entity = GetComponent<Entity>();
		headlight = hmd.transform.parent.Find("Headlight").gameObject;
		Debug.Log("Vitals");

		// Subscribe Events
		entity.eventTakeDamage += TakeDamage;
		
		StartCoroutine(UpdateVitals());
	}

	void Update() {
		CheckSetControllers();
		if (handInfoLeft.controllerDevice.index != 0 && handInfoRight.controllerDevice.index != 0) {
			UpdateControllerInput("Left", grabInfoLeft, grabInfoRight, handInfoLeft, handInfoRight);
			UpdateControllerInput("Right", grabInfoRight, grabInfoLeft, handInfoRight, handInfoLeft);
		}
		UpdatePlayerVelocity();
		UpdateCharacterControllerHeight();
		UpdatePlayerMovement();
		UpdateAvatar();

		platformMovementsAppliedLastFrame = Vector3.zero;

		if (grounded == true && Vector3.Distance(new Vector3(bodyCC.transform.position.x, 0, bodyCC.transform.position.z), positionLastFootstepPlayed) > 2f) {
			PlayFootstepSound();
		}

		handInfoLeft.controllerPosLastFrame = handInfoLeft.controller.transform.position;
		handInfoRight.controllerPosLastFrame = handInfoRight.controller.transform.position;
	}

	void FixedUpdate() {
		UpdateHandAndItemPhysics();
	}

	void CheckSetControllers() {
		if (handInfoLeft.controllerDevice == null) {
			handInfoLeft.controllerDevice = SteamVR_Controller.Input((int)handInfoLeft.controller.index);
		}

		if (handInfoRight.controllerDevice == null) {
			handInfoRight.controllerDevice = SteamVR_Controller.Input((int)handInfoRight.controller.index);
		}
	}

	void PlayFootstepSound() {
		footstep.volume = Mathf.Clamp01(velocityCurrent.magnitude / 40f);
		footstep.Play();
		positionLastFootstepPlayed = new Vector3(bodyCC.transform.position.x, 0, bodyCC.transform.position.z);
	}

	void UpdateControllerInput(string side, GrabInformation grabInfoCurrent, GrabInformation grabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		if (side == "Right") {
			if (handInfoCurrent.controllerDevice.GetPressDown(Valve.VR.EVRButtonId.k_EButton_ApplicationMenu)) {
				flashlight.SetActive(!flashlight.activeSelf);
			}
		}

		CheckGrab(grabInfoCurrent, grabInfoOpposite, handInfoCurrent);

		if (handInfoCurrent.controllerDevice.GetPress(Valve.VR.EVRButtonId.k_EButton_Grip)) {    // Grip Being Held
			if (handInfoCurrent.controllerDevice.GetPressDown(Valve.VR.EVRButtonId.k_EButton_Grip)) {  // Grip Down
				if (grabInfoCurrent.grabbedRigidbody == true && grabInfoCurrent.grabbedItem && (grabInfoCurrent.grabbedItem is Misc) == false) {
					handInfoCurrent.itemReleasingDisabled = false;
				} else {
					handInfoCurrent.itemReleasingDisabled = true;
				}
				Grab(side, grabInfoCurrent, grabInfoOpposite, handInfoCurrent, handInfoOpposite);
				handInfoCurrent.handGameObject.GetComponent<BoxCollider>().enabled = false;

				if (grabInfoCurrent.climbableGrabbed == null && grabInfoCurrent.grabbedRigidbody == null) { // If we didn't grab anything
					if (Vector3.Distance(handInfoCurrent.controller.transform.position, hmd.transform.position + hmd.transform.forward * -0.15f) < 0.2125f) {
						headlight.SetActive(!headlight.activeSelf);
					}
				}
			} else if (grabInfoCurrent.grabbedItem == null && grabInfoCurrent.climbableGrabbed == null) {
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
				InteractDown(side, grabInfoCurrent, handInfoCurrent);
			}

			if (handInfoCurrent.controllerDevice.GetHairTriggerUp()) {
				InteractUp(side, grabInfoCurrent, handInfoCurrent);
			}

			if (handInfoCurrent.controllerDevice.GetHairTrigger()) {
				InteractHold(side, grabInfoCurrent, handInfoCurrent);
			} else {
				InteractNull(side, grabInfoCurrent);
			}
		}
	}

	void RotatePlayer(Quaternion rot) {
		Vector3 rigOffset = (rig.transform.position - hmd.transform.position);
		rigOffset.y = 0;
		rig.transform.position = new Vector3(hmd.transform.position.x, rig.transform.position.y, hmd.transform.position.z) + (rot * rigOffset);
		rig.transform.rotation = rig.transform.rotation * rot;
	}

	void CheckGrab (GrabInformation grabInfoCurrent, GrabInformation grabInfoOpposite, HandInformation handInfoCurrent) {
		// This function checks to see if the hand can grab an item. If it can, it will glow
		if (grabInfoCurrent.climbableGrabbed == null && grabInfoCurrent.grabbedRigidbody == null) {
			Vector3 originPosition = handInfoCurrent.controller.transform.position + (handInfoCurrent.controller.transform.rotation * new Vector3(handRigidbodyPositionOffset.x, handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z));
			Collider[] itemColliders = Physics.OverlapSphere(originPosition, 0.2f, grabLayerMask);
			foreach (Collider hitItem in itemColliders) {
				if (hitItem.transform.gameObject.layer == LayerMask.NameToLayer("Item") || hitItem.transform.gameObject.layer == LayerMask.NameToLayer("Heavy Item")) {
					Item item = null;
					if (hitItem.transform.GetComponent<Item>()) {
						item = hitItem.transform.GetComponent<Item>();
					} else if (hitItem.transform.parent.GetComponent<Item>()) {
						item = hitItem.transform.parent.GetComponent<Item>();
					} else if (hitItem.transform.parent.parent.GetComponent<Item>()) {
						item = hitItem.transform.parent.parent.GetComponent<Item>();
					}

					GrabNode hitGrabNode = hitItem.GetComponent<GrabNode>();
					if (hitGrabNode) {
						if (hitGrabNode.referralNode != null) {
							hitGrabNode = hitGrabNode.referralNode;
						}

						if (hitGrabNode == grabInfoOpposite.grabNode) {
							return;
						}
					} else {
						if (item == grabInfoOpposite.grabbedItem) {
							return;
						}
					}

					if (item != null && item.GetComponent<GlowObjectCmd>()) {
						item.GetComponent<GlowObjectCmd>().handNear = true;
					}
					return;
				}
			}
		}
	}

	void Grab(string side, GrabInformation grabInfoCurrent, GrabInformation grabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		if (grabInfoCurrent.climbableGrabbed == null && grabInfoCurrent.grabbedRigidbody == null) {
			// Try and grab something
			//Vector3 originPosition = handInfoCurrent.controller.transform.position + (handInfoCurrent.controller.transform.rotation * new Vector3(handRigidbodyPositionOffset.x, handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z));
			Vector3 originPosition = handInfoCurrent.handGameObject.transform.position;
			Collider[] itemColliders = Physics.OverlapSphere(originPosition, 0.2f, grabLayerMask);

			List<ItemAndNode> itemAndNodes = new List<ItemAndNode>();

			foreach (Collider hitItem in itemColliders) {
				if (hitItem.transform.gameObject.layer == LayerMask.NameToLayer("Item")) {
					Item itemCurrent = null;
					if (hitItem.transform.GetComponent<Item>()) {
						itemCurrent = hitItem.transform.GetComponent<Item>();
					} else if (hitItem.transform.parent.GetComponent<Item>()) {
						itemCurrent = hitItem.transform.parent.GetComponent<Item>();
					} else if (hitItem.transform.parent.parent.GetComponent<Item>()) {
						itemCurrent = hitItem.transform.parent.parent.GetComponent<Item>();
					}

					if (itemCurrent != null) {
						GrabNode nodeCurrent = hitItem.GetComponent<GrabNode>();        // nodeCurrent will be set to the grabNode that was hit, unless there is no node, which will set it to null
						if (nodeCurrent && nodeCurrent.referralNode != null) {
							nodeCurrent = nodeCurrent.referralNode;
						}
						itemAndNodes.Add(new ItemAndNode(itemCurrent, nodeCurrent));
					}
				}
			}

			if (itemAndNodes.Count > 0) {
				ItemAndNode chosenIAN = null;
				float closestDistance = Mathf.Infinity;
				foreach(ItemAndNode IAN in itemAndNodes) {
					float thisDistance = closestDistance + 1;
					if (IAN.node != null) {
						thisDistance = Vector3.Distance(handInfoCurrent.handGameObject.transform.position, IAN.item.transform.position + (IAN.item.transform.rotation * (IAN.node.transform.localPosition + IAN.node.offset)));
					} else {
						thisDistance = Vector3.Distance(handInfoCurrent.handGameObject.transform.position, IAN.item.transform.position);
					}

					if (thisDistance < closestDistance) {
						chosenIAN = IAN;
						closestDistance = thisDistance;
					}
				}

				if (chosenIAN != null) {
					grabInfoCurrent.grabbedItem = chosenIAN.item;
					grabInfoCurrent.grabNode = chosenIAN.node;
					grabInfoCurrent.grabbedRigidbody = chosenIAN.item.transform.GetComponent<Rigidbody>();

					grabInfoCurrent.grabbedItem.timeLastGrabbed = Time.timeSinceLevelLoad;      // TODO: move?
					grabInfoCurrent.grabbedRigidbody.useGravity = false;

					if (grabInfoCurrent.grabbedItem.pocketCurrent != null) {
						grabInfoCurrent.grabbedItem.pocketCurrent.pocketedItem = null;
						grabInfoCurrent.grabbedItem.pocketCurrent = null;
					}

					if (grabInfoCurrent.grabNode) {
						if (grabInfoCurrent.grabNode.grabType == GrabNode.GrabType.FixedPositionRotation) {
							grabInfoCurrent.grabOffset = Quaternion.Euler(grabInfoCurrent.grabNode.rotation) * (-grabInfoCurrent.grabNode.transform.localPosition - grabInfoCurrent.grabNode.offset);
							grabInfoCurrent.grabRotation = Quaternion.Euler(grabInfoCurrent.grabNode.rotation);
						} else if (grabInfoCurrent.grabNode.grabType == GrabNode.GrabType.FixedPosition) {
							grabInfoCurrent.grabOffset = Quaternion.Euler(grabInfoCurrent.grabNode.rotation) * (-grabInfoCurrent.grabNode.transform.localPosition - grabInfoCurrent.grabNode.offset);
							grabInfoCurrent.grabRotation = Quaternion.Inverse(handInfoCurrent.controller.transform.rotation) * grabInfoCurrent.grabbedRigidbody.transform.rotation;
						} else if (grabInfoCurrent.grabNode.grabType == GrabNode.GrabType.Dynamic || grabInfoCurrent.grabNode.grabType == GrabNode.GrabType.Referral) {
							// Here
							GetDynamicItemGrabInfo(ref grabInfoCurrent, ref handInfoCurrent);
						}
					} else {
						GetDynamicItemGrabInfo(ref grabInfoCurrent, ref handInfoCurrent);
					}

					if (grabInfoCurrent.grabbedRigidbody == grabInfoOpposite.grabbedRigidbody) {      // Is the other hand already holding this item?
						grabInfoCurrent.itemVelocityPercentage = 0;
						grabInfoOpposite.itemVelocityPercentage = 0;
						if (grabInfoCurrent.grabNode && grabInfoOpposite.grabNode) {
							if (grabInfoCurrent.grabNode.dominance > grabInfoOpposite.grabNode.dominance) {
								grabDualWieldDominantHand = side;
								grabDualWieldDirection = Quaternion.Euler(grabInfoCurrent.grabNode.rotation) * Quaternion.Inverse(grabInfoCurrent.grabbedRigidbody.transform.rotation) * (handInfoOpposite.handRigidbody.transform.position - (grabInfoCurrent.grabNode.transform.position + grabInfoCurrent.grabbedRigidbody.transform.rotation * -grabInfoCurrent.grabNode.offset));
							} else if (grabInfoCurrent.grabNode.dominance <= grabInfoOpposite.grabNode.dominance) {
								grabDualWieldDominantHand = (side == "Right" ? "Left" : "Right");
								if (grabInfoCurrent.grabPoint != Vector3.zero) {
									grabDualWieldDirection = Quaternion.Inverse(handInfoOpposite.handRigidbody.transform.rotation) * (grabInfoCurrent.grabPoint - handInfoOpposite.handRigidbody.transform.position);
								} else {
									grabDualWieldDirection = Quaternion.Inverse(handInfoOpposite.handRigidbody.transform.rotation) * (handInfoCurrent.handRigidbody.transform.position - handInfoOpposite.handRigidbody.transform.position);
								}
							}
						} else {
							grabDualWieldDominantHand = (side == "Right" ? "Left" : "Right");
							if (grabInfoCurrent.grabPoint != Vector3.zero) {
								grabDualWieldDirection = Quaternion.Inverse(handInfoOpposite.handRigidbody.transform.rotation) * (grabInfoCurrent.grabPoint - handInfoOpposite.handRigidbody.transform.position);
							} else {
								grabDualWieldDirection = Quaternion.Inverse(handInfoOpposite.handRigidbody.transform.rotation) * (handInfoCurrent.handRigidbody.transform.position - handInfoOpposite.handRigidbody.transform.position);
							}
						}
					}
				}
				return;
			}

			Collider[] climbColliders = Physics.OverlapSphere(originPosition, 0.065f, grabLayerMask);
			foreach (Collider hitClimb in climbColliders) {
				if (hitClimb.transform.gameObject.layer == LayerMask.NameToLayer("Climbable") || (hitClimb.transform.gameObject.layer == LayerMask.NameToLayer("Environment") && (hmd.transform.position.y - rig.transform.position.y < heightCutoffCrouching))) {
					grabInfoCurrent.grabOffset = bodyCC.transform.position - hitClimb.transform.position;
					grabInfoCurrent.grabRotation = hitClimb.transform.rotation;
					grabInfoCurrent.grabCCOffset = handInfoCurrent.controller.transform.position - bodyCC.transform.position;
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
							grabInfoCurrent.grabOffset = bodyCC.transform.position - environmentHit.transform.position;
							grabInfoCurrent.grabRotation = environmentHit.transform.rotation;
							grabInfoCurrent.grabCCOffset = handInfoCurrent.controller.transform.position - bodyCC.transform.position;
							grabInfoCurrent.climbableGrabbed = environmentHit.transform;
							StartCoroutine(TriggerHapticFeedback(handInfoCurrent.controllerDevice, 0.1f));
							return;
						}
					}
				}
			}
		}
	}

	void GetDynamicItemGrabInfo (ref GrabInformation grabInfoCurrent, ref HandInformation handInfoCurrent) {
		// The purpose of this method is to make items grabbed by dual weilding and items that do not have grabNodes be grabbed on their surfaces rather than being suspended away from the hands.

		Vector3 closestColliderPoint = Vector3.zero;
		if (grabInfoCurrent.grabNode == true) {
			closestColliderPoint = grabInfoCurrent.grabNode.transform.GetComponent<Collider>().ClosestPoint(handInfoCurrent.handRigidbody.transform.position);
		} else {
			closestColliderPoint = grabInfoCurrent.grabbedItem.transform.GetComponent<Collider>().ClosestPoint(handInfoCurrent.handRigidbody.transform.position);
		}

		grabInfoCurrent.grabPoint = (closestColliderPoint);
		grabInfoCurrent.grabOffset = Quaternion.Inverse(handInfoCurrent.handGameObject.transform.rotation) * Quaternion.AngleAxis(0, handInfoCurrent.controller.transform.right) * (grabInfoCurrent.grabbedRigidbody.transform.position - grabInfoCurrent.grabPoint);
		grabInfoCurrent.grabRotation = Quaternion.Inverse(handInfoCurrent.handGameObject.transform.rotation) * Quaternion.AngleAxis(0, handInfoCurrent.controller.transform.right) * grabInfoCurrent.grabbedRigidbody.transform.rotation;

	}

	public class ItemAndNode {
		public Item item = null;
		public GrabNode node = null;

		public ItemAndNode(Item _item, GrabNode _node) {
			item = _item;
			node = _node;
		}

		public ItemAndNode() {
			item = null;
			node = null;
		}
	}

	void Release(string side, GrabInformation grabInfoCurrent, GrabInformation grabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		grabInfoCurrent.grabOffset = Vector3.zero;
		grabInfoCurrent.grabCCOffset = Vector3.zero;
		if (grabInfoOpposite.climbableGrabbed == null && grabInfoCurrent.climbableGrabbed == true) {
			velocityCurrent = Vector3.ClampMagnitude(velocityCurrent, 5f);
		}
		if (grabInfoCurrent.grabbedRigidbody == true) {
			if (grabInfoOpposite.grabbedRigidbody != grabInfoCurrent.grabbedRigidbody) {
				ThrowItem(handInfoCurrent, grabInfoCurrent, (handInfoCurrent.controller.transform.position - handInfoCurrent.handPosLastFrame) / Time.deltaTime);
			} else {
				if (grabInfoOpposite.grabNode == null) {
					grabInfoCurrent.itemVelocityPercentage = 0;
					grabInfoOpposite.itemVelocityPercentage = 0;
					if (grabDualWieldDominantHand != grabInfoCurrent.side) {
						GetDynamicItemGrabInfo(ref grabInfoOpposite, ref handInfoOpposite);
					} else {
						grabInfoOpposite.grabOffset = Quaternion.Inverse(handInfoOpposite.handGameObject.transform.rotation) * (grabInfoOpposite.grabbedRigidbody.transform.position - handInfoOpposite.handGameObject.transform.position);
						grabInfoOpposite.grabRotation = Quaternion.Inverse(handInfoOpposite.handGameObject.transform.rotation) * grabInfoOpposite.grabbedRigidbody.transform.rotation;
					}
				}
			}
		}
		if (handInfoCurrent.jumpLoaded == true) {
			if ((grounded == true || timeLastJumped + 0.1f > Time.timeSinceLevelLoad) && grabInfoCurrent.climbableGrabbed == false && grabInfoCurrent.grabbedRigidbody == false) {
				Vector3 addedVelocity = Vector3.ClampMagnitude((handInfoCurrent.controllerPosLastFrame - handInfoCurrent.controller.transform.position) * 500, (timeLastJumped + 0.1f > Time.timeSinceLevelLoad) ? 1.75f : 5f);
				addedVelocity = new Vector3(addedVelocity.x * 0.8f, Mathf.Clamp(addedVelocity.y, 0, Mathf.Infinity), addedVelocity.z * 0.8f);
				velocityCurrent = Vector3.ClampMagnitude(velocityCurrent + addedVelocity, Mathf.Clamp(velocityCurrent.magnitude * 1.05f, 4.5f, Mathf.Infinity));
				handInfoCurrent.jumpLoaded = false;
				if (timeLastJumped + 0.1f < Time.timeSinceLevelLoad) {
					float velocityHeightMagnifier = Mathf.Clamp((new Vector3(velocityCurrent.x, 0, velocityCurrent.z).magnitude + velocityCurrent.y - 1f) * 0.5f, 1, 2.25f);
					grounded = false;
					SetBodyHeight((hmd.transform.position.y - (rig.transform.position.y)) / velocityHeightMagnifier);
				}

				groundedTime = 0;

				

				if (grounded == true) { timeLastJumped = Time.timeSinceLevelLoad; }
			}
		}
		grabInfoCurrent.climbableGrabbed = null;
		grabInfoCurrent.grabbedRigidbody = null;
		grabInfoCurrent.grabbedItem = null;
		grabInfoCurrent.grabNode = null;
		grabInfoCurrent.grabPoint = Vector3.zero;
	}

	void InteractDown(string side, GrabInformation grabInfoCurrent, HandInformation handInfoCurrent) {
		Debug.Log("Down");
		if (grabInfoCurrent.grabbedItem is Weapon) {
			Weapon currentWeapon = grabInfoCurrent.grabbedItem as Weapon;
			currentWeapon.timeLastTriggered = Time.timeSinceLevelLoad;
			currentWeapon.AdjustAmmo(0);
			if (grabInfoCurrent.grabNode.interactionType == GrabNode.InteractionType.Trigger) {
				if (currentWeapon.chargingEnabled == false) {
					AttemptToFireWeapon(side, grabInfoCurrent.grabbedItem, handInfoCurrent);
				}
			}
		}
	}

	void InteractHold(string side, GrabInformation grabInfoCurrent, HandInformation handInfoCurrent) {
		if (grabInfoCurrent.grabbedItem is Weapon) {
			Weapon currentWeapon = grabInfoCurrent.grabbedItem as Weapon;
			currentWeapon.triggerHeld = true;
			currentWeapon.timeLastTriggered = Time.timeSinceLevelLoad;
			if (grabInfoCurrent.grabNode.interactionType == GrabNode.InteractionType.Trigger) {
				if (currentWeapon.automatic == true) {
					AttemptToFireWeapon(side, grabInfoCurrent.grabbedItem, handInfoCurrent);
				}
				if (currentWeapon.chargingEnabled == true) {
					currentWeapon.chargeCurrent = Mathf.Clamp01(currentWeapon.chargeCurrent + (currentWeapon.chargeIncrement * Time.deltaTime));
				}
			}
		}
	}

	void InteractUp(string side, GrabInformation grabInfoCurrent, HandInformation handInfoCurrent) {
		if (grabInfoCurrent.grabbedItem is Weapon) {
			Weapon currentWeapon = grabInfoCurrent.grabbedItem as Weapon;
			currentWeapon.triggerHeld = false;
			if (grabInfoCurrent.grabNode.interactionType == GrabNode.InteractionType.Trigger) {
				if (currentWeapon.chargingEnabled == true) {
					AttemptToFireWeapon(side, grabInfoCurrent.grabbedItem, handInfoCurrent);
				}
			}
		}
	}

	void InteractNull(string side, GrabInformation grabInfoCurrent) {
		if (grabInfoCurrent.grabbedItem is Weapon) {
			Weapon currentWeapon = grabInfoCurrent.grabbedItem as Weapon;
			currentWeapon.triggerHeld = false;
		}
	}

	void AttemptToFireWeapon(string side, Item currentItem, HandInformation handInfoCurrent) {
		Weapon currentWeapon = currentItem as Weapon;
		if (currentWeapon.timeLastFired + (1 / currentWeapon.firerate) <= Time.timeSinceLevelLoad) {
			if (currentWeapon.chargingEnabled == false || (currentWeapon.chargeCurrent >= currentWeapon.chargeRequired)) {
				if (currentWeapon.ammoCurrent >= currentWeapon.consumption) {       // Does the weapon enough ammo?
					if (currentWeapon.consumePerBurst == false) {
						currentWeapon.AdjustAmmo(-currentWeapon.consumption);
					}
					StartCoroutine(FireWeapon(side, currentItem, handInfoCurrent));
					currentWeapon.timeLastFired = Time.timeSinceLevelLoad;
				}
			}
		}
	}

	IEnumerator FireWeapon(string hand, Item currentItem, HandInformation handInfoCurrent) {
		Weapon currentWeapon = currentItem as Weapon;
		Transform barrel = currentItem.transform.Find("(Barrel Point)");

		for (int i = 0; i < Mathf.Clamp(currentWeapon.burstCount, 1, 100); i++) {           // For each burst shot in this fire
			if (currentWeapon.consumePerBurst == false || currentWeapon.ammoCurrent >= currentWeapon.consumption) {
				if (currentWeapon.consumePerBurst == true) {
					currentWeapon.AdjustAmmo(-currentWeapon.consumption);
				}

				// Step 1: Trigger haptic feedback
				if (grabInfoLeft.grabbedRigidbody == grabInfoRight.grabbedRigidbody) {
					StartCoroutine(TriggerHapticFeedback(handInfoLeft.controllerDevice, 0.1f));
					StartCoroutine(TriggerHapticFeedback(handInfoRight.controllerDevice, 0.1f));
				} else {
					StartCoroutine(TriggerHapticFeedback((hand == "Left" ? handInfoLeft.controllerDevice : handInfoRight.controllerDevice), 0.1f));
				}

				// Step 2: Apply velocity and angular velocity to weapon
				if (grabInfoLeft.grabbedItem == grabInfoRight.grabbedItem) {
					handInfoLeft.handOffsetPosition = Vector3.ClampMagnitude(handInfoLeft.handOffsetPosition + currentItem.transform.rotation * new Vector3(0, 0, -currentWeapon.recoilLinear), handOffsetPositionMax);
					handInfoRight.handOffsetPosition = Vector3.ClampMagnitude(handInfoRight.handOffsetPosition + currentItem.transform.rotation * new Vector3(0, 0, -currentWeapon.recoilLinear), handOffsetPositionMax);
					handInfoLeft.handOffsetRotation += new Vector3(Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(0, currentWeapon.recoilAngular), 0);
					handInfoRight.handOffsetRotation += new Vector3(Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(0, currentWeapon.recoilAngular), 0);

					//handInfoLeft.handOffsetRotation *= Quaternion.Euler(Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular));
					//handInfoRight.handOffsetRotation *= Quaternion.Euler(Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular));
				} else {
					handInfoCurrent.handOffsetPosition = Vector3.ClampMagnitude(handInfoCurrent.handOffsetPosition + currentItem.transform.rotation * new Vector3(0, 0, -currentWeapon.recoilLinear), handOffsetPositionMax);
					handInfoCurrent.handOffsetRotation += new Vector3(Random.Range(-currentWeapon.recoilAngular * 0.25f, currentWeapon.recoilAngular * 0.25f), Random.Range(0, currentWeapon.recoilAngular), 0);
					//handInfoCurrent.handOffsetRotation *= Quaternion.Euler(Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular));
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
			} else {
				// TODO: Dry shot
			}
			yield return new WaitForSeconds(currentWeapon.burstDelay);
		}

		if (currentWeapon.chargingEnabled == true) {
			currentWeapon.chargeCurrent = Mathf.Clamp01(currentWeapon.chargeCurrent - currentWeapon.chargeDecrementPerShot);
		}
	}

	void ThrowItem(HandInformation handInfoCurrent, GrabInformation grabInfoCurrent, Vector3 velocity) {
		grabInfoCurrent.grabbedRigidbody.velocity = Vector3.ClampMagnitude(velocity.magnitude > 5 ? (velocity * 2f) : velocity, 100);
		grabInfoCurrent.grabbedRigidbody.useGravity = true;
		if (grabInfoCurrent.grabbedRigidbody.transform.GetComponent<Item>() != null) {
			if (grabInfoCurrent.grabbedRigidbody.GetComponent<Item>() is Weapon) {
				(grabInfoCurrent.grabbedRigidbody.transform.GetComponent<Item>() as Weapon).triggerHeld = false;
			}
		}

		// Check for pockets
		Collider[] pockets = Physics.OverlapSphere(handInfoCurrent.handRigidbody.transform.position, 0.15f, pocketMask);
		if (pockets.Length > 0) {
			Collider choosenPocket = pockets[0];

			// Find closest Pocket
			if (pockets.Length > 1) {
				float closestPocketDistance = Vector3.Distance(choosenPocket.transform.position, handInfoCurrent.handRigidbody.transform.position);
				for (int i = 1; i < pockets.Length; i++) {
					float thisPocketDistance = Vector3.Distance(pockets[i].transform.position, handInfoCurrent.handRigidbody.transform.position);
					if (thisPocketDistance < closestPocketDistance) {
						closestPocketDistance = thisPocketDistance;
						choosenPocket = pockets[i];
					}
				}
			}

			// Asign Pocket Info
			if (choosenPocket.GetComponent<Pocket>()) {
				Pocket choosenPocketObject = choosenPocket.transform.GetComponent<Pocket>();
				if (choosenPocketObject.pocketedItem == null && choosenPocketObject.pocketSize >= grabInfoCurrent.grabbedItem.pocketSize) {
					choosenPocketObject.pocketedItem = grabInfoCurrent.grabbedItem;        // Set Item
					choosenPocketObject.timePocketed = Time.timeSinceLevelLoad;
					grabInfoCurrent.grabbedItem.pocketCurrent = choosenPocketObject;       // Set Pocket
				}
			}
		}

	}

	IEnumerator TriggerHapticFeedback(SteamVR_Controller.Device device, float duration) {
		for (float i = 0; i <= duration; i += 0.01f) {
			device.TriggerHapticPulse(3999);
			yield return new WaitForSeconds(0.01f);
		}
	}

	void UpdatePlayerVelocity() {
		moveSpeedCurrent = Mathf.Lerp(moveSpeedCurrent, (bodyCC.height > heightCutoffStanding ? moveSpeedStanding : (bodyCC.height > heightCutoffCrouching ? moveSpeedCrouching : moveSpeedLaying)), 5 * Time.deltaTime);
		velocityDesired = new Vector3(handInfoLeft.controllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad).x, velocityDesired.y, handInfoLeft.controllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad).y) * moveSpeedCurrent;
		velocityDesired = Quaternion.LookRotation(new Vector3(hmd.transform.forward.x, 0, hmd.transform.forward.z), Vector3.up) * velocityDesired;
		if (grabInfoLeft.climbableGrabbed == null && grabInfoRight.climbableGrabbed == null) {
			isClimbing = false;
			if (grounded == true) {
				velocityCurrent = Vector3.Lerp(velocityCurrent, new Vector3(velocityDesired.x, velocityCurrent.y, velocityDesired.z), 25 * Time.deltaTime * Mathf.Clamp01(groundedTime));
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
				combinedClimbPositions += (grabInfoLeft.climbableGrabbed.position + climbRotationLeft * grabInfoLeft.grabOffset) + (climbRotationLeft * grabInfoLeft.grabCCOffset - (handInfoLeft.controller.transform.position - bodyCC.transform.position));
				climbCount++;
			}

			if (grabInfoRight.climbableGrabbed == true) {
				combinedClimbPositions += (grabInfoRight.climbableGrabbed.position + climbRotationRight * grabInfoRight.grabOffset) + (climbRotationRight * grabInfoRight.grabCCOffset - (handInfoRight.controller.transform.position - bodyCC.transform.position));
				climbCount++;
			}

			combinedClimbPositions = combinedClimbPositions / climbCount;

			velocityCurrent = Vector3.Lerp(velocityCurrent, (combinedClimbPositions - bodyCC.transform.position) / Time.deltaTime, 25 * Time.deltaTime);

			grabInfoLeft.grabRotationLastFrame = climbRotationLeft;
			grabInfoRight.grabRotationLastFrame = climbRotationRight;
			isClimbing = true;
		}
		handInfoLeft.handPosLastFrame = handInfoLeft.controller.transform.position;
		handInfoRight.handPosLastFrame = handInfoRight.controller.transform.position;

	}

	void UpdateCharacterControllerHeight () {
		// This function updates the bodyCC's height, growing it taller when standing/airborne, and smaller when climbing/jumping.
		// This makes things like clambering and jumping over obstacles far easier.

		if (isClimbing == true) {      // Are we currently climbing?
			bodyHeightChangeSpeed = Mathf.Clamp01(Mathf.Lerp(bodyHeightChangeSpeed, 0.75f, 50 * Time.deltaTime));
			SetBodyHeight(Mathf.Lerp(bodyCC.height, Mathf.Clamp(GetPlayerRealLifeHeight(), 0, 0.5f), bodyHeightChangeSpeed * 10 * Time.deltaTime));       // If Climbing: set height to player's real height clamped at 0.25f
		} else {
			if (grounded == true) {
				bodyHeightChangeSpeed = Mathf.Clamp01(Mathf.Lerp(bodyHeightChangeSpeed, 0.9f, 50 * Time.deltaTime));
			} else {
				bodyHeightChangeSpeed = Mathf.Clamp01(Mathf.Lerp(bodyHeightChangeSpeed, 0.4f, 50 * Time.deltaTime));
			}
			SetBodyHeight(Mathf.Lerp(bodyCC.height, GetPlayerRealLifeHeight(), bodyHeightChangeSpeed * 10 * Time.deltaTime));                               // If Not Climbing: set height to player's real height
		}
	}

	void SetBodyHeight(float desiredHeight) {
		Vector3 handPositionBeforeLeft = handInfoLeft.handGameObject.transform.position;
		Vector3 handPositionBeforeRight = handInfoRight.handGameObject.transform.position;

		if (grounded == true && isClimbing == false) {
			bodyCC.height = Mathf.Clamp(desiredHeight, bodyCC.radius * 2f, 2.25f);          // Set the body height to the desired body height, clamping it between the diameter of the body minimum, and 2.25 meters maximum
			bodyCC.transform.position = new Vector3(bodyCC.transform.position.x, (hmd.transform.position.y - (GetPlayerRealLifeHeight() - desiredHeight)) - (desiredHeight / 2), bodyCC.transform.position.z);
		} else {
			bodyCC.height = Mathf.Clamp(desiredHeight, bodyCC.radius * 2f, 2.25f);          // Set the body height to the desired body height, clamping it between the diameter of the body minimum, and 2.25 meters maximum
			bodyCC.transform.position = new Vector3(bodyCC.transform.position.x, (hmd.transform.position.y - (desiredHeight / 2)), bodyCC.transform.position.z);
		}

		handInfoLeft.handGameObject.transform.position = handPositionBeforeLeft;
		handInfoRight.handGameObject.transform.position = handPositionBeforeRight;
	}

	float GetPlayerRealLifeHeight() {
		// Returns the player's real life height (not accounting for verticalPusher offset)		// TODO: should we be?
		return (hmd.transform.position.y - rig.transform.position.y);
	}

	void UpdatePlayerMovement() {
		Vector3 bodyPositionBeforeMovement = bodyCC.transform.position;

		// Move the player through the HMD movement
		MovePlayerViaHMD();

		// Move the player through applying velocity
		MovePlayerViaVelocity();

		// Move held items
		Vector3 deltaPosition = (bodyCC.transform.position - bodyPositionBeforeMovement);
		MoveItems(deltaPosition);
		handInfoLeft.handRigidbody.transform.position += (deltaPosition);
		handInfoRight.handRigidbody.transform.position += (deltaPosition);

		// Change volume of windLoop depending on how fast the player is moving
		windLoop.volume = Mathf.Lerp(windLoop.volume, Mathf.Clamp01(((Vector3.Distance(bodyPositionBeforeMovement, bodyCC.transform.position) + platformMovementsAppliedLastFrame.magnitude) / Time.deltaTime) / 75) - 0.15f, 50 * Time.deltaTime);
	}

	void MovePlayerViaHMD() {
		// This function moves the player's headCC and in turn, bodyCC according to the current frame of HMD movement
		Vector3 bodyPositionBefore = bodyCC.transform.position;

		verticalPusher.transform.localPosition = Vector3.Lerp(verticalPusher.transform.localPosition, Vector3.zero, 7.5f * Time.deltaTime);		// Smooth the vertical pusher into it's default 0,0,0 position

		// Step 1: Move HeadCC to HMD					// Get the difference between the head position and the HMD position and apply that movement to the headCC
		//Vector3 headToHmdDelta = ((hmd.transform.position - ((verticalPusher.transform.localPosition) * Mathf.Clamp01(Time.deltaTime * 5))) - headCC.transform.position);     // Delta position moving from headCC to HMD (smoothing applied through the verticalPusher)
		Vector3 headToHmdDelta = (hmd.transform.position - headCC.transform.position);     // Delta position moving from headCC to HMD (smoothing applied through the verticalPusher)
		headCC.Move(headToHmdDelta); // Attempt to move the headCC			
		if (grabInfoLeft.climbableGrabbed == null && grabInfoRight.climbableGrabbed == null) { // Are we not climbing?
			verticalPusher.transform.position += new Vector3(0, (headCC.transform.position - hmd.transform.position).y, 0); // Move the vertical pusher to accomodate for HMD moving too far vertically through geometry (ie: down into box, up into desk)
		}

		// Step 2: Move BodyCC to HeadCC (Horizontally Only)
		GetGroundInformation();
		Vector3 neckOffset = new Vector3(hmd.transform.forward.x + hmd.transform.up.x, 0, hmd.transform.forward.z + hmd.transform.up.z).normalized * -0.05f;        // The current neck offset for how far away the bodyCC should be from the center of the headCC
		Vector3 bodyToHeadDeltaXZ = ((headCC.transform.position + neckOffset) - bodyCC.transform.position);
		bodyToHeadDeltaXZ.y = 0;
		bodyCC.Move(GetSlopeMovement(bodyToHeadDeltaXZ));

		// Step 3: Move Rig according to BodyDelta (Vertically Only)
		Vector3 bodyDeltaPosition = bodyCC.transform.position - bodyPositionBefore;
		rig.transform.position += bodyDeltaPosition;

		// Step 4: Repeat Step 1 (Vertically Only)
		Vector3 headToHmdDelta2 = (hmd.transform.position - headCC.transform.position);     // Delta position moving from headCC to HMD (smoothing applied through the verticalPusher)
		headToHmdDelta2.x = 0; headToHmdDelta2.z = 0;
		headCC.Move(headToHmdDelta2); // Attempt to move the headCC			
		if (grabInfoLeft.climbableGrabbed == null && grabInfoRight.climbableGrabbed == null) { // Are we not climbing?
			verticalPusher.transform.position += new Vector3(0, (headCC.transform.position - hmd.transform.position).y, 0); // Move the vertical pusher to accomodate for HMD moving too far vertically through geometry (ie: down into box, up into desk)
		}

		// Step 5: Move HeadCC if leaning too far
		Debug.DrawLine(hmd.transform.position, new Vector3(bodyCC.transform.position.x, hmd.transform.position.y, bodyCC.transform.position.z), Color.red, 0, false);   // Shows a line representing lean distance drawing from the bodyCC to the headCC
		leanDistance = Vector3.Distance(new Vector3(headCC.transform.position.x, 0, headCC.transform.position.z) + neckOffset, new Vector3(bodyCC.transform.position.x, 0, bodyCC.transform.position.z));       // Current lean distance
		if (leanDistance > maxLeanDistance) {       // Is the player currently leaning further than the max lean distance allows?
			Vector3 leanPullBack = (bodyCC.transform.position - headCC.transform.position); // The direction to pull the hmd back
			leanPullBack = new Vector3(leanPullBack.x, 0, leanPullBack.z).normalized * (leanDistance - maxLeanDistance);
			headCC.Move(leanPullBack);
		}

		// Step 6: Realign Rig
		RealignRig();
		
	}

	void MovePlayerViaVelocity() {
		// This function moves the player's bodyCC, and in turn headCC, by applying movement based on the player's velocity
		Vector3 bodyPositionBefore = bodyCC.transform.position;

		Debug.DrawRay(bodyCC.transform.position, velocityCurrent, Color.green);		// Debug ray showing player velocity

		// Step 1: Apply Gravity for this frame
		velocityCurrent += new Vector3(0, -9.8f * Time.deltaTime, 0);

		// Step 2: Move BodyCC with velocityCurrent
		GetGroundInformation();		// First, get ground information to know if we're on a slope/grounded/airborne
		bodyCC.Move(GetSlopeMovement(velocityCurrent * Time.deltaTime));

		// Step 3: Move Rig according to BodyDelta
		Vector3 rigToBodyDelta = bodyCC.transform.position - bodyPositionBefore;
		rig.transform.position += rigToBodyDelta;

		// Step 4: Move HeadCC to HMD
		Vector3 headToHmdDelta = (hmd.transform.position - headCC.transform.position);     // Delta position moving from headCC to HMD (smoothing applied through the verticalPusher)
		headCC.Move(headToHmdDelta); // Attempt to move the headCC			
		if (isClimbing == false) { // Are we not climbing?
			verticalPusher.transform.position += new Vector3(0, (headCC.transform.position - hmd.transform.position).y, 0); // Move the vertical pusher to accomodate for HMD moving too far vertically through geometry (ie: down into box, up into desk)
		}

		// Step 5: Move HeadCC if leaning too far
		Vector3 neckOffset = new Vector3(hmd.transform.forward.x + hmd.transform.up.x, 0, hmd.transform.forward.z + hmd.transform.up.z).normalized * -0.15f;        // The current neck offset for how far away the bodyCC should be from the center of the headCC
		Debug.DrawLine(hmd.transform.position, new Vector3(bodyCC.transform.position.x, hmd.transform.position.y, bodyCC.transform.position.z), Color.red, 0, false);   // Shows a line representing lean distance drawing from the bodyCC to the headCC
		leanDistance = Vector3.Distance(new Vector3(headCC.transform.position.x, 0, headCC.transform.position.z) + neckOffset, new Vector3(bodyCC.transform.position.x, 0, bodyCC.transform.position.z));       // Current lean distance
		if (leanDistance > maxLeanDistance) {       // Is the player currently leaning further than the max lean distance allows?
			Vector3 leanPullBack = (bodyCC.transform.position - headCC.transform.position); // The direction to pull the hmd back
			leanPullBack = new Vector3(leanPullBack.x, 0, leanPullBack.z).normalized * (leanDistance - maxLeanDistance);
			headCC.Move(leanPullBack);
		}

		// Step 6: Check for ceiling collision
		if (velocityCurrent.y > 0) {
			RaycastHit hit;
			Vector3 sphereCastOrigin = headCC.transform.position + new Vector3(0, -headCC.skinWidth / 2, 0);
			Vector3 sphereCastDirection = Vector3.up;
			float sphereCastRadius = headCC.radius - (headCC.skinWidth / 2);
			float sphereCastLength = 0.01f + (headCC.skinWidth * 2);
			if (isClimbing == false && Physics.SphereCast(sphereCastOrigin, sphereCastRadius, sphereCastDirection, out hit, sphereCastLength, bodyCCLayerMask)) {
				velocityCurrent.y = 0;
			}
		}

		// Step 7: Realign Rig
		RealignRig();
		
		if (grounded == true) {
			groundedTime += Time.deltaTime;
		} else {
			groundedTime = 0;
		}
	}

	void RealignRig() {
		// Realigns the rig to put the hmd in the same position as the headCC by moving the rig (which in turn, moves the hmd into position)
		Vector3 hmdHeadDifference = head.transform.position - hmd.transform.position;
		rig.transform.position += hmdHeadDifference;
	}

	void GetGroundInformation () {
		// This function will get ground infromation:
			// 1. Are we touching the ground?
			// 2. What is the normal of the ground?
		
		RaycastHit hit;

		Vector3 sphereCastOrigin = bodyCC.transform.position + new Vector3(0, (-bodyCC.height / 2) + bodyCC.radius, 0);
		Vector3 sphereCastDirection = Vector3.down;
		float sphereCastRadius = bodyCC.radius - (bodyCC.skinWidth / 8);
		float sphereCastLength = 0.025f + (bodyCC.skinWidth * 2);

		if (isClimbing == false && Physics.SphereCast(sphereCastOrigin, sphereCastRadius, sphereCastDirection, out hit, sphereCastLength, bodyCCLayerMask)) {
			if (grounded == false) {
				float heightDifference = GetPlayerRealLifeHeight() - bodyCC.height;
				verticalPusher.transform.position += new Vector3(0, -heightDifference, 0);
				rig.transform.position += new Vector3(0, heightDifference, 0);									// TODO: yikes, should a Get function really be messing with stuff like this?
			}
			handInfoLeft.jumpLoaded = true;
			handInfoRight.jumpLoaded = true;
			velocityCurrent.y = 0;
			grounded = true;
			groundNormal = Vector3.Lerp(groundNormal, hit.normal, 25 * Time.deltaTime);
		} else {
			grounded = false;
			groundNormal = Vector3.Lerp(groundNormal, Vector3.up, 25 * Time.deltaTime); ;
		}
	}

	Vector3 GetSlopeMovement (Vector3 movementInput) {
		Vector3 movementOutput = Vector3.zero;

		if (grounded && Vector3.Angle(Vector3.up, groundNormal) < 50) {     // Is the player currently grounded && the normal angle is less than 50 degrees?
			if (isClimbing == true || timeLastJumped + 0.1f > Time.timeSinceLevelLoad) {
				movementOutput = movementInput;
			} else {
				movementOutput = Vector3.ProjectOnPlane(movementInput, groundNormal);
				movementOutput *= Mathf.Clamp(Vector3.Angle(movementOutput, Vector3.up) / 105f, 0.75f, 1.25f);
			}
		} else {
			movementOutput = movementInput;
		}
		
		return movementOutput;
	}
	
	void MoveItems(Vector3 deltaPosition) {
		// This function moves items along with the player's movement every frame. This is NOT the function for moving objects along with the player's hands
		if (grabInfoLeft.grabbedRigidbody == grabInfoRight.grabbedRigidbody) {
			if (grabInfoLeft.grabbedRigidbody) {
				grabInfoLeft.grabbedRigidbody.transform.position = (grabInfoLeft.grabbedRigidbody.transform.position + deltaPosition);
			}
		} else {
			if (grabInfoLeft.grabbedRigidbody) {
				grabInfoLeft.grabbedRigidbody.transform.position = (grabInfoLeft.grabbedRigidbody.transform.position + deltaPosition);
			}
			if (grabInfoRight.grabbedRigidbody) {
				grabInfoRight.grabbedRigidbody.transform.position = (grabInfoRight.grabbedRigidbody.transform.position + deltaPosition);
			}

		}
	}

	void UpdateAvatar() {
		avatar.position = bodyCC.transform.position - new Vector3(0, bodyCC.height / 2f, 0);
		avatarTorso.position = new Vector3(bodyCC.transform.position.x, headCC.transform.position.y - 0.3f, bodyCC.transform.position.z);
		Vector3 hmdFlatForward = new Vector3(hmd.transform.forward.x, 0, hmd.transform.forward.z).normalized;
		Vector3 hmdFlatUp = new Vector3(hmd.transform.up.x, 0, hmd.transform.up.z).normalized;

		Vector3 hmdFlatFinal = Vector3.Lerp(hmdFlatForward, hmdFlatUp, Mathf.Clamp01(Vector3.Angle(hmd.transform.up, Vector3.up) / 90));
		avatar.rotation = Quaternion.LookRotation(hmdFlatFinal, Vector3.up);
	}

	public void MovePlayer(Vector3 deltaPosition) {
		Debug.Log("Move Player");
		Vector3 bodyPositionBefore = bodyCC.transform.position;
		bodyCC.Move(deltaPosition);
		Vector3 netCCMovement = (bodyCC.transform.position - bodyPositionBefore);
		rig.transform.position += netCCMovement;
		platformMovementsAppliedLastFrame += netCCMovement;
	}

	void UpdateHandAndItemPhysics() {
		// Left Hand
		grabInfoLeft.itemVelocityPercentage = Mathf.Clamp01(grabInfoLeft.itemVelocityPercentage + Time.deltaTime * (grabInfoLeft.grabbedRigidbody != null ? 2 : -10));
		UpdateHandPhysics("Left", handInfoLeft, grabInfoLeft);

		// Right Hand
		grabInfoRight.itemVelocityPercentage = Mathf.Clamp01(grabInfoRight.itemVelocityPercentage + Time.deltaTime * (grabInfoRight.grabbedRigidbody != null ? 2 : -10));
		UpdateHandPhysics("Right", handInfoRight, grabInfoRight);

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
			Rigidbody handDominant = (grabDualWieldDominantHand == "Left") ? handInfoLeft.handRigidbody : handInfoRight.handRigidbody;
			Vector3 dualWieldDirectionCurrent = (((grabDualWieldDominantHand == "Left") ? handInfoRight.handRigidbody.transform.position : handInfoLeft.handRigidbody.transform.position) - ((grabDualWieldDominantHand == "Left") ? handInfoLeft.handRigidbody.transform.position : handInfoRight.handRigidbody.transform.position));
			Quaternion dualWieldDirectionChangeRotation = Quaternion.FromToRotation(handDominant.transform.rotation * grabDualWieldDirection, dualWieldDirectionCurrent);
			Quaternion rotationDeltaItem = (dualWieldDirectionChangeRotation * handDominant.transform.rotation * ((grabDualWieldDominantHand == "Left") ? grabInfoLeft.grabRotation : grabInfoRight.grabRotation)) * Quaternion.Inverse(grabInfoLeft.grabbedRigidbody.transform.rotation);

			float angleItem;
			Vector3 axisItem;
			rotationDeltaItem.ToAngleAxis(out angleItem, out axisItem);
			if (angleItem > 180) {
				angleItem -= 360;
			}

			if (Vector3.Distance(grabInfoLeft.grabbedItem.transform.position, handDominant.transform.position + (dualWieldDirectionChangeRotation * handDominant.transform.rotation * ((grabDualWieldDominantHand == "Left") ? grabInfoLeft.grabOffset : grabInfoRight.grabOffset))) > 1f) {		// TODO: Bad Code
				Release("Left", grabInfoLeft, grabInfoRight, handInfoLeft, handInfoRight);
				Release("Right", grabInfoRight, grabInfoLeft, handInfoRight, handInfoLeft);
			} else {
				grabInfoLeft.grabbedRigidbody.velocity = Vector3.Lerp(grabInfoLeft.grabbedRigidbody.velocity, Vector3.ClampMagnitude(((handDominant.transform.position + (dualWieldDirectionChangeRotation * handDominant.transform.rotation * ((grabDualWieldDominantHand == "Left") ? grabInfoLeft.grabOffset : grabInfoRight.grabOffset))) - grabbedItemDominant.transform.position) / Time.fixedDeltaTime, (grabbedItemDominant.GetComponent<HingeJoint>()) ? 1 : 100) * Mathf.Lerp(grabInfoLeft.itemVelocityPercentage, grabInfoRight.itemVelocityPercentage, 0.5f), Mathf.Clamp01(50 * Time.deltaTime));

				if (angleItem != float.NaN) {
					grabInfoLeft.grabbedRigidbody.maxAngularVelocity = Mathf.Infinity;
					grabInfoLeft.grabbedRigidbody.angularVelocity = Vector3.Lerp(grabInfoLeft.grabbedRigidbody.angularVelocity, (angleItem * axisItem) * Mathf.Lerp(grabInfoLeft.itemVelocityPercentage, grabInfoRight.itemVelocityPercentage, 0.5f) * 0.95f, Mathf.Clamp01(50 * Time.deltaTime));
				}

				// Accuracy - Dual Wield
				if (grabInfoLeft.grabbedItem is Weapon) {
					weaponLeft.accuracyCurrent = Mathf.Clamp(weaponLeft.accuracyCurrent + (weaponLeft.accuracyIncrement * Time.deltaTime), weaponLeft.accuracyMin, weaponLeft.accuracyMax);
				}
			}

		} else {
			// Physics - Left
			if (grabInfoLeft.grabbedItem) {
				UpdateItemPhysics("Left", grabInfoLeft, grabInfoRight, handInfoLeft, handInfoRight);
				if (weaponLeft) {
					weaponLeft.accuracyCurrent = Mathf.Clamp(weaponLeft.accuracyCurrent + (weaponLeft.accuracyIncrement * Time.deltaTime), weaponLeft.accuracyMin, weaponLeft.accuracyMax);
				}
			}

			// Physics - Right
			if (grabInfoRight.grabbedItem) {
				UpdateItemPhysics("Right", grabInfoRight, grabInfoLeft, handInfoRight, handInfoLeft);
				if (weaponLeft) {
					weaponRight.accuracyCurrent = Mathf.Clamp(weaponRight.accuracyCurrent + (weaponRight.accuracyIncrement * Time.deltaTime), weaponRight.accuracyMin, weaponRight.accuracyMax);
				}
			}
		}
	}

	void UpdateHandPhysics(string side, HandInformation handInfoCurrent, GrabInformation grabInfoCurrent) {
		Vector3 handOffsetDefault = Quaternion.Euler(handInfoCurrent.handOffsetRotation) * handInfoCurrent.controller.transform.rotation * new Vector3(handRigidbodyPositionOffset.x * (side == "Left" ? 1 : -1), handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z);
		Vector3 handOffsetKick = handInfoCurrent.handOffsetPosition;
		//handInfoCurrent.handRigidbody.velocity = (((handInfoCurrent.controller.transform.position + handOffsetDefault + handOffsetKick) - handInfoCurrent.handGameObject.transform.position) + verticalPusher.transform.localPosition) / Time.fixedDeltaTime;
		handInfoCurrent.handRigidbody.velocity = (((handInfoCurrent.controller.transform.position + handOffsetDefault + handOffsetKick) - handInfoCurrent.handGameObject.transform.position)) / Time.fixedDeltaTime;


		handInfoCurrent.handOffsetPosition = Vector3.Lerp(handInfoCurrent.handOffsetPosition, Vector3.zero, 5 * Time.deltaTime);
		handInfoCurrent.handOffsetRotation = Vector3.Lerp(handInfoCurrent.handOffsetRotation, new Vector3(0, 0, 0), 10 * Time.deltaTime);

		Quaternion rotationDelta = Quaternion.Euler(0, 0, 0);
		if (grabInfoCurrent.grabbedRigidbody != null && grabInfoCurrent.grabNode != null && (grabInfoCurrent.grabNode.grabType != GrabNode.GrabType.Dynamic && grabInfoCurrent.grabNode.grabType != GrabNode.GrabType.FixedPosition)) {
			rotationDelta = (Quaternion.Euler(handInfoCurrent.handOffsetRotation) * Quaternion.AngleAxis(-30, handInfoCurrent.controller.transform.right) * handInfoCurrent.controller.transform.rotation * Quaternion.Euler(grabInfoCurrent.grabNode.rotation)) * Quaternion.Inverse(handInfoCurrent.handRigidbody.transform.rotation);
		} else {
			rotationDelta = (Quaternion.Euler(handInfoCurrent.handOffsetRotation) * Quaternion.AngleAxis(30, handInfoCurrent.controller.transform.right) * handInfoCurrent.controller.transform.rotation) * Quaternion.Inverse(handInfoCurrent.handRigidbody.transform.rotation);
		}

		float angle;
		Vector3 axis;
		rotationDelta.ToAngleAxis(out angle, out axis);
		if (angle > 180) {
			angle -= 360;
		}

		if (axis.x != float.NaN) {
			handInfoCurrent.handRigidbody.maxAngularVelocity = Mathf.Infinity;
			handInfoCurrent.handRigidbody.angularVelocity = (angle * axis);
		}
	}

	void UpdateItemPhysics(string hand, GrabInformation grabInfoCurrent, GrabInformation grabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		if (justStepped == false) {
			if (grabInfoCurrent.grabbedRigidbody.gameObject.layer == LayerMask.NameToLayer("Item")) {
				Vector3 grabOffsetCurrent = (handInfoCurrent.handRigidbody.transform.rotation * grabInfoCurrent.grabOffset);

				if (Vector3.Distance(grabInfoCurrent.grabbedRigidbody.transform.position, handInfoCurrent.handRigidbody.transform.position + grabOffsetCurrent) > 1f) {
					Release(hand, grabInfoCurrent, grabInfoOpposite, handInfoCurrent, handInfoOpposite);
				} else {
					  grabInfoCurrent.grabbedRigidbody.velocity = Vector3.Lerp(grabInfoCurrent.grabbedRigidbody.velocity, Vector3.ClampMagnitude(((handInfoCurrent.handRigidbody.position + grabOffsetCurrent) - grabInfoCurrent.grabbedRigidbody.transform.position) / Time.fixedDeltaTime, (grabInfoCurrent.grabbedRigidbody.GetComponent<HingeJoint>()) ? 1 : 500) * grabInfoCurrent.itemVelocityPercentage, Mathf.Clamp01(50 * Time.deltaTime));
					//grabInfoCurrent.grabbedRigidbody.velocity = Vector3.Lerp(grabInfoCurrent.grabbedRigidbody.velocity, Vector3.ClampMagnitude(((handInfoCurrent.handRigidbody.transform.position + (dualWieldDirectionChangeRotation * handDominant.transform.rotation * ((grabDualWieldDominantHand == "Left") ? grabInfoLeft.grabOffset : grabInfoRight.grabOffset))) - grabbedItemDominant.transform.position) / Time.fixedDeltaTime, (grabbedItemDominant.GetComponent<HingeJoint>()) ? 1 : 100) * Mathf.Lerp(grabInfoLeft.itemVelocityPercentage, grabInfoRight.itemVelocityPercentage, 0.5f), Mathf.Clamp01(50 * Time.deltaTime));


					if (!grabInfoCurrent.grabbedRigidbody.GetComponent<HingeJoint>()) {
						Quaternion rotationDeltaItem = (handInfoCurrent.handRigidbody.transform.rotation * grabInfoCurrent.grabRotation) * Quaternion.Inverse(grabInfoCurrent.grabbedRigidbody.transform.rotation);
						float angleItem;
						Vector3 axisItem;
						rotationDeltaItem.ToAngleAxis(out angleItem, out axisItem);
						if (angleItem > 180) {
							angleItem -= 360;
						}

						if (angleItem != float.NaN) {
							grabInfoCurrent.grabbedRigidbody.maxAngularVelocity = Mathf.Infinity;
							grabInfoCurrent.grabbedRigidbody.angularVelocity = Vector3.Lerp(grabInfoCurrent.grabbedRigidbody.angularVelocity, (angleItem * axisItem) * grabInfoCurrent.itemVelocityPercentage * 0.95f, Mathf.Clamp01(50 *  Time.deltaTime));
						}
					}
				}
			}
		}
	}

	public void TakeDamage() {
		float desiredIntensity = Mathf.Abs(Mathf.Clamp((float)entity.vitals.healthCurrent * (100 / 75), 0, 100) - 100) / 100;
		UpdateDamageVignette(desiredIntensity);
	}

	IEnumerator UpdateVitals() {
		while (true) {
			entity.vitals.healthCurrent = Mathf.Clamp(entity.vitals.healthCurrent + 1, 0, entity.vitals.healthMax);
			float desiredIntensity = Mathf.Abs(Mathf.Clamp((float)entity.vitals.healthCurrent * (100 / 75), 0, 100) - 100) / 100;
			UpdateDamageVignette(desiredIntensity);
			yield return new WaitForSeconds(0.2f);
		}
	}

	void UpdateDamageVignette(float intensity) {
		VignetteModel.Settings newVignetteSettings = postProcessingProfile.vignette.settings;
		newVignetteSettings.intensity = intensity;
		postProcessingProfile.vignette.settings = newVignetteSettings;
	}

}