using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using UnityEngine.PostProcessing;

[RequireComponent(typeof(CharacterController)), RequireComponent(typeof(Entity))]
public class Player : MonoBehaviour {

	[Header("Entity")]
	public Entity entity;

	[Space(10)] [Header("Layer Masks")]
	public LayerMask hmdLayerMask;			// The layerMask for the hmd, defines objects which the player cannot put their head through
	public LayerMask bodyCCLayerMask;		// The layerMask for the bodyCharacterController, defines objects which the player's body cannot pass through
	public LayerMask itemGrabLayerMask;
	public LayerMask envGrabLayerMask;
	public LayerMask attachmentNodeMask;
	public LayerMask pocketMask;

	[Space(10)] [Header("Cameras")]
	public Camera mainCam;

	[Space(10)] [Header("Graphics Settings")]
	public PostProcessingProfile postProcessingProfile;

	[Space(10)] [Header("Player GameObject References")]
	public GameObject head;
	public GameObject rig;
	public GameObject flashlight;
	public GameObject headlight;
	public GameObject torso;

	[Space(10)] [Header("Avatar References")]
	public Transform avatar;
	public Transform avatarTorso;

	[Space(10)] [Header("Audio Sources")]
	public AudioManager audioManager;
	public AudioSource windLoop;
	public AudioSource footstep;
	public Vector3 positionLastFootstepPlayed;

	[Space(10)] [Header("Item Grab Infos")]
	public ItemGrabInformation itemGrabInfoLeft = new ItemGrabInformation();
	public ItemGrabInformation itemGrabInfoRight = new ItemGrabInformation();

	[Space(10)] [Header("Environment Grab Infos")]
	public EnvironmentGrabInformation envGrabInfoLeft = new EnvironmentGrabInformation();
	public EnvironmentGrabInformation envGrabInfoRight = new EnvironmentGrabInformation();

	[Space(10)] [Header("Grabbing Variables")]
	public Vector3 grabDualWieldDirection;             // The vector3 direction from handDominant to handNonDominant (ie: hand right to left direction normalized)
	public string grabDualWieldDominantHand;            // This value determines, when dual wielding an item, which hand is dominant and thus acts as the main pivot point
	public bool isClimbing;                             // Records whether or not the player is climbing

	[Space(10)] [Header("Hand Infos")]
	public HandInformation handInfoLeft = new HandInformation();
	public HandInformation handInfoRight = new HandInformation();

	[Space(10)] [Header("Player Object References")]
	// The player's characterController, used to move the player
	public CharacterController bodyCC;                      // The body's character controller, used for player movement and velocity calculations
	public CharacterController headCC;                      // The player's head's character controller, used for mantling over cover, climbing in small spaces, and for detecting collision in walls, ceilings, and floors.
	public GameObject verticalPusher;                       // Used to push the player's head vertically. This is useful for when a player tries to stand in an inclosed space or duck down into an object. It will keep the player's head from clipping through geometry.
	public SteamVR_TrackedObject hmd;

	[Space(10)] [Header("Constants")]
	float heightCutoffStanding = 1f;                                    // If the player is standing above this height, they are considered standing
	float heightCutoffCrouching = 0.5f;                                 // If the player is standing avove this height but below the standingCutoff, they are crouching. If the player is below the crouchingCutoff then they are laying
	float maxLeanDistance = 0.45f;                                      // The value used to determine how far the player can lean over cover/obstacles. Once the player moves past the leanDistance he/she will be pulled back via rig movement.
	public float leanDistance;                                          // The current distance the player is leaning. See maxLeanDistance.
	Vector3 handRigidbodyPositionOffset = new Vector3(-0.02f, -0.025f, -0.075f);
	float moveSpeedStanding = 4f;
	float moveSpeedCrouching = 2f;
	float moveSpeedLaying = 1f;

	[Space(10)] [Header("Movement Variables")]
	public Vector3 velocityCurrent;     // The current velocity of the player
	public Vector3 velocityDesired;     // The desired velocity of the player
	public float stepHeight;            // The max height the player can step over
	float moveSpeedCurrent;
	public bool grounded = false;
	Vector3 groundNormal;
	float groundedTime;                             // Time the player has been grounded foor
	float timeLastJumped = 0;
	Vector3 platformMovementsAppliedLastFrame = Vector3.zero;
	bool padPressed;

	[Space(10)] [Header("Positional Variables")]
	public float heightCurrent;                     // The current height of the hmd from the rig
	float bodyHeightChangeSpeed = 1;                  // A value which is near 1 when climbing/grounded, but near 0 when airborne; this value changes how quickly the bodyCC's height is changed

	float handOffsetPositionMax = 0.5f;

	[System.Serializable]
	public class ItemGrabInformation {
		public string side;                         // Which side hand is this? "Left" or "Right"
		public Rigidbody grabbedRigidbody;          // The rigidbody of the object being currently grabbed
		public Item grabbedItem;                    // The Item component of the object being currently grabbed
		public GrabNode grabNode;                   // The grabNode of the object currently grabbed (for item objects)
		public Quaternion grabRotation;             // The desired rotation of the item relative to the hand's rotation
		public Vector3 grabOffset;                  // Stores the offset between the hand and the item which when rotated according to rotationOffset is the desired position of the object.
		public Vector3 grabPoint;                   // (For dynamic-grabNode or non-grabNode items) records the exact point at which the item was grabbed
		public float itemVelocityPercentage;        // When items are first picked up their IVP = 0; over time grabbed approaches 1 linearly; this value determines the magnitude of the velocity (and angularVelocity) of the item
		public Transform pocketCandidateLastFrame;     // The pocket candidate found last frame (null if there was none found)
		public Transform grabbableItemLastFrame;

		public ItemGrabInformation() {
			grabbedRigidbody = null;
			grabbedItem = null;
			grabNode = null;
			grabRotation = Quaternion.Euler(0, 0, 0);
			grabOffset = Vector3.zero;
			itemVelocityPercentage = 0;
			side = "null";
		}
	}

	[System.Serializable]
	public class EnvironmentGrabInformation {
		public string side;                         // Which side hand is this? "Left" or "Right"
		public Transform climbableGrabbed;          // The transform of the climbable object being currently grabbed
		public Rigidbody grabbedRigidbody;          // The rigidbody of the object being currently grabbed
		public Item grabbedItem;                    // The Item component of the object being currently grabbed
		public GrabNode grabNode;                   // The grabNode of the object currently grabbed (for item objects)
		public Quaternion grabRotation;             // This value is used differently depending on the case. If grabbing an item, it stores the desired rotation of the item relative to the hand's rotation. If grabbing a climbable object, it stores the rotation of the object when first grabbed.
		public Quaternion grabRotationLastFrame;    // This value is used to record the grabRotation last frame. It is (was) used for an experimental formula which rotates the player along with it's grabbed object's rotation.		// TODO: do we still need this variable?
		public Vector3 grabOffset;                  // This value is used differently depending on the case. If grabbing an item, it stores the offset between the hand and the item which when rotated according to rotationOffset is the desired position of the object. If grabbing a climbable object, it stores the offset between the hand and the grabbed object, to determine the desired position of the player
		public Vector3 grabCCOffset;                // If grabbing a climbable object, this variable stores the offset between the hand and the character controller, to determine where to move the player to when climbing
		public Vector3 grabPoint;                   // (For dynamic-grabNode or non-grabNode items) records the exact point at which the item was grabbed
		public float itemVelocityPercentage;        // When items are first picked up their IVP = 0; over time grabbed approaches 1 linearly; this value determines the magnitude of the velocity (and angularVelocity) of the item
		public Transform pocketCandidateLastFrame;     // The pocket candidate found last frame (null if there was none found)
		public Transform grabbableItemLastFrame;

		public EnvironmentGrabInformation() {
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
		public SteamVR_TrackedObject controller;                // The SteamVR Tracked Object, used to get the position and rotation of the controller
		public SteamVR_Controller.Device controllerDevice;      // The SteamVR Controller Device for controllers; Used to get input
		public GameObject handGameObject;                       // The player's hand GameObject
		public Rigidbody handRigidbody;                         // The rigidbody of the hand

		public Vector3 controllerPosLastFrame;                  // Used to determine the jumping velocity when jumping with the hands
		public Vector3 handPosLastFrame;                        // Used to determine which direction to throw items
		public bool itemReleasingDisabled;                      // Used for picking up non-misc items. Used to disable item releasing when first grabbing an item as to make the grip function as a toggle rather than an on/off grab button
		public bool grabbingDisabled;                           // Is grabbing currently disabled for this hand. Resets every 'grip up' to true

		public Vector3 handOffsetPosition;
		public Vector3 handOffsetRotation;

		public bool jumpLoaded;
		public HandInformation() {

		}
	}

	void Start() {
		bodyCC = GetComponent<CharacterController>();          // Grab the character controller at the start
		avatar = transform.parent.Find("Avatar");
		avatarTorso = avatar.Find("Torso");
		handInfoLeft.handRigidbody = handInfoLeft.handGameObject.GetComponent<Rigidbody>();
		handInfoRight.handRigidbody = handInfoRight.handGameObject.GetComponent<Rigidbody>();
		audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
		entity = GetComponent<Entity>();
		headlight = hmd.transform.parent.Find("Headlight").gameObject;

		// Subscribe Events
		entity.eventTakeDamage += TakeDamage;

		StartCoroutine(UpdateVitals());
	}

	void Update() {
		CheckSetControllers();
		if (handInfoLeft.controllerDevice.index != 0 && handInfoRight.controllerDevice.index != 0) {
			UpdateControllerInput("Left", itemGrabInfoLeft, itemGrabInfoRight, envGrabInfoLeft, envGrabInfoRight, handInfoLeft, handInfoRight);
			UpdateControllerInput("Right", itemGrabInfoRight, itemGrabInfoLeft, envGrabInfoRight, envGrabInfoLeft, handInfoRight, handInfoLeft);
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

	void UpdateControllerInput(string side, ItemGrabInformation itemGrabInfoCurrent, ItemGrabInformation itemGrabInfoOpposite, EnvironmentGrabInformation envGrabInfoCurrent, EnvironmentGrabInformation envGrabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		if (side == "Right") {
			if (handInfoCurrent.controllerDevice.GetPressDown(Valve.VR.EVRButtonId.k_EButton_ApplicationMenu)) {
				flashlight.SetActive(!flashlight.activeSelf);
			}
		}

		// Grip Down and Held functionality
		if (handInfoCurrent.controllerDevice.GetPress(Valve.VR.EVRButtonId.k_EButton_Grip)) {    // Grip Being Held
			if (handInfoCurrent.controllerDevice.GetPressDown(Valve.VR.EVRButtonId.k_EButton_Grip)) {  // Grip Down
				if (itemGrabInfoCurrent.grabbedRigidbody == true && itemGrabInfoCurrent.grabbedItem && (itemGrabInfoCurrent.grabbedItem is Misc) == false) {
					handInfoCurrent.itemReleasingDisabled = false;
				} else {
					handInfoCurrent.itemReleasingDisabled = true;
				}
				Grab(side, itemGrabInfoCurrent, itemGrabInfoOpposite, envGrabInfoCurrent, envGrabInfoOpposite, handInfoCurrent, handInfoOpposite);
				handInfoCurrent.handGameObject.GetComponent<BoxCollider>().enabled = false;

				if (envGrabInfoCurrent.climbableGrabbed == null && itemGrabInfoCurrent.grabbedItem == null) { // If we didn't grab any environments or items
					if (Vector3.Distance(handInfoCurrent.controller.transform.position, hmd.transform.position + hmd.transform.forward * -0.15f) < 0.2125f) {
						headlight.SetActive(!headlight.activeSelf);
					}
				}
			}
			if (handInfoCurrent.grabbingDisabled == false) {
				Grab(side, itemGrabInfoCurrent, itemGrabInfoOpposite, envGrabInfoCurrent, envGrabInfoOpposite, handInfoCurrent, handInfoOpposite);
				handInfoCurrent.handGameObject.GetComponent<BoxCollider>().enabled = false;
			}
		}

		// Grip Up
		if (handInfoCurrent.controllerDevice.GetPressUp(Valve.VR.EVRButtonId.k_EButton_Grip)) {
			handInfoCurrent.grabbingDisabled = false;
			if (envGrabInfoCurrent.climbableGrabbed == true || itemGrabInfoCurrent.grabbedItem == false || ((itemGrabInfoCurrent.grabbedItem && itemGrabInfoCurrent.grabbedItem is Misc) || handInfoCurrent.itemReleasingDisabled == false)) {
				ReleaseAll (side, itemGrabInfoCurrent, itemGrabInfoOpposite, envGrabInfoCurrent, envGrabInfoOpposite, handInfoCurrent, handInfoOpposite);
				handInfoCurrent.handGameObject.GetComponent<BoxCollider>().enabled = true;
			}
		}

		// Trigger functionality
		if (itemGrabInfoCurrent.grabbedItem != null && itemGrabInfoCurrent.grabNode != null) {
			if (handInfoCurrent.controllerDevice.GetHairTriggerDown()) {
				TriggerDown(side, itemGrabInfoCurrent, envGrabInfoCurrent, handInfoCurrent);
			}

			if (handInfoCurrent.controllerDevice.GetHairTriggerUp()) {
				TriggerUp(side, itemGrabInfoCurrent, envGrabInfoCurrent, handInfoCurrent);
			}

			if (handInfoCurrent.controllerDevice.GetHairTrigger()) {
				TriggerHold(side, itemGrabInfoCurrent, envGrabInfoCurrent, handInfoCurrent);
			} else {
				TriggerNull(side, itemGrabInfoCurrent, envGrabInfoCurrent);
			}
		}

		// Interact button functionality
		if (handInfoCurrent.controllerDevice.GetPress(EVRButtonId.k_EButton_ApplicationMenu)) {
			if (handInfoCurrent.controllerDevice.GetPress(EVRButtonId.k_EButton_Grip)) {
				if (itemGrabInfoCurrent.grabbedItem is Attachment) {        // If this Item is an attachment
					if (itemGrabInfoCurrent.grabbedItem.transform.GetComponent<Rigidbody>() == null) {  // If this attachment does not have a Rigidbody Component
						handInfoCurrent.itemReleasingDisabled = true;
						DetachAttachment(itemGrabInfoCurrent, handInfoCurrent);
					}
				}
			}
		}
	}

	AttachmentNodePair GetAttachmentConnection (ItemGrabInformation itemGrabInfoCurrent, HandInformation handInfoCurrent) {
		// This method Checks whether an item 'itemGrabInfoCurrent' can currently be attached to any other items
		// It will then return an ATtachmentNodePair object with the attachmentNode of the held item and the attachmentNode of a item it will attach to

		AttachmentNodePair chosenANP = null;

		Transform attachmentNodesContainer = itemGrabInfoCurrent.grabbedItem.transform.Find("(AttachmentNodes)");   // The attachmentNode container on this item which contains all attachmentNodes this item has
		if (itemGrabInfoCurrent.grabbedItem is Attachment && attachmentNodesContainer != null) {        // 1: Is this item an attachment? 2: Does this item contain attachmentNodes
			List<AttachmentNodePair> possibleAttachmentNodePairs = new List<AttachmentNodePair>();		// List containing all possible pairs of attachmentNodes

			// Find all possible matching attachmentNodes
			foreach (Transform heldNodeTransform in attachmentNodesContainer) {      // For each attachmentNode this item has, test to see if it can connect with any attachmentNodes on other items
				AttachmentNode heldNode = heldNodeTransform.GetComponent<AttachmentNode>();
				if (heldNode.isAttached == false && heldNode.attachmentGender == AttachmentNode.AttachmentGender.Male) {        // Make sure this node isn't already attached to something and only check the male attachmentNodes
					Collider[] hitNodes = Physics.OverlapSphere(heldNode.transform.position, 0.1f, attachmentNodeMask);

					foreach (Collider hitNodeCollider in hitNodes) {
						if (hitNodeCollider.transform.parent.parent != itemGrabInfoCurrent.grabbedItem.transform) {        // Make sure the hit attachmentNode is not a child of this item
							AttachmentNode hitNode = hitNodeCollider.GetComponent<AttachmentNode>();
							if (hitNode != null && hitNode.isAttached == false && hitNode.attachmentType == heldNode.attachmentType && hitNode.attachmentGender != heldNode.attachmentGender) {
								possibleAttachmentNodePairs.Add(new AttachmentNodePair(heldNode, hitNode));
							}
						}
					}
				}
			}

			// Find closest pair of attachmentNodes
			if (possibleAttachmentNodePairs.Count > 0) {
				float shortestDistance = Vector3.Distance(possibleAttachmentNodePairs[0].nodeChild.transform.position, possibleAttachmentNodePairs[0].nodeParent.transform.position);
				chosenANP = possibleAttachmentNodePairs[0];

				// Cycle through all possibleAttachmentNodePairs to find the closest attachmentNodePair
				for (int i = 1; i < possibleAttachmentNodePairs.Count; i++) {
					float thisDistance = Vector3.Distance(possibleAttachmentNodePairs[i].nodeChild.transform.position, possibleAttachmentNodePairs[i].nodeParent.transform.position);
					if (thisDistance < shortestDistance) {
						shortestDistance = thisDistance;
						chosenANP = possibleAttachmentNodePairs[i];
					}
				}
			}
		}
		return chosenANP;
	}

	void InitiateAttachment (AttachmentNodePair attachmentNodePair, ItemGrabInformation itemGrabInfoCurrent) {
		// This method initiates an attachment between to attachmentNodes contained in 'attachmentNodePair'
		if (attachmentNodePair != null) {
			// Set attachmentNode variables
			attachmentNodePair.nodeChild.isAttached = true;
			attachmentNodePair.nodeParent.isAttached = true;
			attachmentNodePair.nodeChild.connectedNode = attachmentNodePair.nodeParent;
			attachmentNodePair.nodeParent.connectedNode = attachmentNodePair.nodeChild;

			// Update attachments list of both items
			attachmentNodePair.nodeParent.item.attachments.Add(attachmentNodePair.nodeChild.item);
			attachmentNodePair.nodeChild.item.attachments.Add(attachmentNodePair.nodeParent.item);
			attachmentNodePair.nodeChild.item.isGrabbed = false;

			// If nodeParent's item is a Weapon, update its combined weaponAttributes
			if (attachmentNodePair.nodeParent.item is Weapon) {
				Debug.Log("attachment triggered");
				(attachmentNodePair.nodeParent.item as Weapon).UpdateCombinedAttributes();
			}

			itemGrabInfoCurrent.grabbedItem.transform.parent = attachmentNodePair.nodeParent.transform.parent.parent.Find("(Attachments)");

			Quaternion nodeChildInvertedRotation = Quaternion.AngleAxis(180, attachmentNodePair.nodeParent.transform.right) * attachmentNodePair.nodeParent.transform.rotation;
			Quaternion rotationDelta = Quaternion.Inverse(itemGrabInfoCurrent.grabbedItem.transform.rotation) * nodeChildInvertedRotation;

			itemGrabInfoCurrent.grabbedItem.transform.rotation *= rotationDelta * Quaternion.Inverse(attachmentNodePair.nodeChild.transform.localRotation);
			itemGrabInfoCurrent.grabbedItem.transform.position = attachmentNodePair.nodeParent.transform.position + (itemGrabInfoCurrent.grabbedItem.transform.rotation * -attachmentNodePair.nodeChild.transform.localPosition);

			itemGrabInfoCurrent.grabbedItem.rigidbodyCopy = new RigidbodyCopy(itemGrabInfoCurrent.grabbedRigidbody);
			Destroy(itemGrabInfoCurrent.grabbedRigidbody);
		}
	}

	void DetachAttachment (ItemGrabInformation itemGrabInfoCurrent, HandInformation handInfoCurrent) {
		// This method is used to fully detach any parent items this item is attached to. It does not detach items that are children of it, it only detaches itself from it's parent
		Transform attachmentNodesContainer = itemGrabInfoCurrent.grabbedItem.transform.Find("(AttachmentNodes)");
		if (attachmentNodesContainer) {
			foreach (Transform itemAttNodeTransform in attachmentNodesContainer) {
				AttachmentNode nodeObject = itemAttNodeTransform.GetComponent<AttachmentNode>();
				if (nodeObject && nodeObject.attachmentGender == AttachmentNode.AttachmentGender.Male && nodeObject.isAttached == true) {
					// Remove attachments in both items' attachment lists
					nodeObject.connectedNode.item.attachments.Remove(nodeObject.item);
					nodeObject.item.attachments.Remove(nodeObject.connectedNode.item);

					// If connectedNode's item is a Weapon, update its combined weaponAttributes
					if (nodeObject.connectedNode.item is Weapon) {
						(nodeObject.connectedNode.item as Weapon).UpdateCombinedAttributes();
					}

					// Detach node
					nodeObject.isAttached = false;
					nodeObject.connectedNode.isAttached = false;
					nodeObject.connectedNode.connectedNode = null;      // Clear this node's connectedNode's connectedNode (yikes)
					nodeObject.connectedNode = null;                      // Clear this node's connectedNode
				}
			}

			// Unparent attachment
			Transform ItemManager = GameObject.Find("ItemManager").transform;
			itemGrabInfoCurrent.grabbedItem.transform.parent = ItemManager;

			// Reapply rigidbody
			itemGrabInfoCurrent.grabbedItem.gameObject.AddComponent<Rigidbody>();
			Rigidbody newRigidbody = itemGrabInfoCurrent.grabbedItem.gameObject.GetComponent<Rigidbody>();

			// Copy RigidbodyCopy's values over to the newly created rigidbody
			RigidbodyCopy.SetRigidbodyValues(newRigidbody, itemGrabInfoCurrent.grabbedItem.rigidbodyCopy);

			itemGrabInfoCurrent.grabbedItem.itemRigidbody = newRigidbody;

			// Reset grab information
			itemGrabInfoCurrent.grabbedRigidbody = newRigidbody;

			if (itemGrabInfoCurrent.grabNode) {
				if (itemGrabInfoCurrent.grabNode.grabType == GrabNode.GrabType.FixedPositionRotation) {
					itemGrabInfoCurrent.grabOffset = Quaternion.Euler(itemGrabInfoCurrent.grabNode.rotation) * ((Quaternion.Inverse(itemGrabInfoCurrent.grabbedItem.transform.rotation) * -(itemGrabInfoCurrent.grabNode.transform.position - itemGrabInfoCurrent.grabbedItem.transform.position)) - itemGrabInfoCurrent.grabNode.offset);
					itemGrabInfoCurrent.grabRotation = Quaternion.Euler(itemGrabInfoCurrent.grabNode.rotation);
				} else if (itemGrabInfoCurrent.grabNode.grabType == GrabNode.GrabType.FixedPosition) {
					itemGrabInfoCurrent.grabOffset = Quaternion.Euler(itemGrabInfoCurrent.grabNode.rotation) * (-itemGrabInfoCurrent.grabNode.transform.localPosition - itemGrabInfoCurrent.grabNode.offset);
					itemGrabInfoCurrent.grabRotation = Quaternion.Inverse(handInfoCurrent.controller.transform.rotation) * itemGrabInfoCurrent.grabbedRigidbody.transform.rotation;
				} else if (itemGrabInfoCurrent.grabNode.grabType == GrabNode.GrabType.Dynamic || itemGrabInfoCurrent.grabNode.grabType == GrabNode.GrabType.Referral) {
					GetDynamicItemGrabInfo(ref itemGrabInfoCurrent, ref handInfoCurrent);
				}
			} else {
				GetDynamicItemGrabInfo(ref itemGrabInfoCurrent, ref handInfoCurrent);
			}

		}
	}

	public class AttachmentNodePair {
		public AttachmentNode nodeChild;
		public AttachmentNode nodeParent;

		public AttachmentNodePair (AttachmentNode _nodeChild, AttachmentNode _nodeParent) {
			nodeChild = _nodeChild;
			nodeParent = _nodeParent;
		}

		public AttachmentNodePair () {
			
		}
	}

	void RotatePlayer(Quaternion rot) {
		Vector3 rigOffset = (rig.transform.position - hmd.transform.position);
		rigOffset.y = 0;
		rig.transform.position = new Vector3(hmd.transform.position.x, rig.transform.position.y, hmd.transform.position.z) + (rot * rigOffset);
		rig.transform.rotation = rig.transform.rotation * rot;
	}

	void Grab(string side, ItemGrabInformation itemGrabInfoCurrent, ItemGrabInformation itemGrabInfoOpposite, EnvironmentGrabInformation envGrabInfoCurrent, EnvironmentGrabInformation envGrabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		if (envGrabInfoCurrent.climbableGrabbed == false && envGrabInfoOpposite.climbableGrabbed == false) {      // Are we currently climbing?
			if (itemGrabInfoCurrent.grabbedRigidbody == null) {      // Are we currently not holding an item
				GrabItem(side, itemGrabInfoCurrent, itemGrabInfoOpposite, handInfoCurrent, handInfoOpposite);
			}
			if (envGrabInfoCurrent.climbableGrabbed == null) {      // Are we currently not climbing something
				GrabEnvironment(side, envGrabInfoCurrent, envGrabInfoOpposite, handInfoCurrent, handInfoOpposite);
			}
		} else {
			if (envGrabInfoCurrent.climbableGrabbed == null) {      // Are we currently not climbing something
				GrabEnvironment(side, envGrabInfoCurrent, envGrabInfoOpposite, handInfoCurrent, handInfoOpposite);
			}
			if (itemGrabInfoCurrent.grabbedRigidbody == null) {      // Are we currently not holding an item
				GrabItem(side, itemGrabInfoCurrent, itemGrabInfoOpposite, handInfoCurrent, handInfoOpposite);
			}
		}
	}

	void GrabItem (string side, ItemGrabInformation itemGrabInfoCurrent, ItemGrabInformation itemGrabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		if (handInfoCurrent.grabbingDisabled == false) {
			// Try and grab something
			Vector3 originPosition = handInfoCurrent.handGameObject.transform.position;
			Collider[] itemColliders = Physics.OverlapSphere(originPosition, 0.2f, itemGrabLayerMask);
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
				foreach (ItemAndNode IAN in itemAndNodes) {
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
					itemGrabInfoCurrent.grabbedItem = chosenIAN.item;
					itemGrabInfoCurrent.grabbedItem.isGrabbed = true;
					itemGrabInfoCurrent.grabbedItem.timeLastGrabbed = Time.timeSinceLevelLoad;
					itemGrabInfoCurrent.grabNode = chosenIAN.node;

					// Set correct rigidbody
					if (itemGrabInfoCurrent.grabbedItem.transform.GetComponent<Rigidbody>() == null) {

						// If this item is an attachment
						if (itemGrabInfoCurrent.grabbedItem.transform.parent.name == "(Attachments)") {
							Transform grabbedItemParentItem = itemGrabInfoCurrent.grabbedItem.transform.parent.parent;
							while (true) {
								// if this new grabbedItemParentItem has a rigidbody, end the loop
								Rigidbody rigidbodySearch = grabbedItemParentItem.GetComponent<Rigidbody>();
								if (rigidbodySearch != null) {
									itemGrabInfoCurrent.grabbedRigidbody = rigidbodySearch;
									break;
								}

								// If theres no item we fucked
								if (grabbedItemParentItem == null) {
									Debug.LogWarning("Aye, where thee fuk is thee item?");
									break;
								}

								grabbedItemParentItem = grabbedItemParentItem.parent.parent;
							}
						}

						Debug.Log(itemGrabInfoCurrent.grabbedRigidbody);
					} else {
						itemGrabInfoCurrent.grabbedRigidbody = chosenIAN.item.transform.GetComponent<Rigidbody>();
					}
					
					if (itemGrabInfoCurrent.grabbedItem.pocketCurrent != null) {
						itemGrabInfoCurrent.grabbedItem.pocketCurrent.ReleaseItem();
						itemGrabInfoCurrent.grabbedItem.pocketCurrent = null;
					}

					itemGrabInfoCurrent.grabbedRigidbody.useGravity = false;
					if (itemGrabInfoCurrent.grabbedRigidbody.transform.GetComponent<Item>() is Weapon) {
						itemGrabInfoCurrent.grabbedRigidbody.transform.GetComponent<Weapon>().UpdateCombinedAttributes();
					}

					if (itemGrabInfoCurrent.grabNode) {
						if (itemGrabInfoCurrent.grabNode.grabType == GrabNode.GrabType.FixedPositionRotation) {
							Vector3 attachmentOffset = Vector3.zero;
							if (itemGrabInfoCurrent.grabbedItem.transform.parent.name == "(Attachments)") {
								attachmentOffset = itemGrabInfoCurrent.grabbedItem.transform.localPosition;
							}
							itemGrabInfoCurrent.grabOffset = Quaternion.Euler(itemGrabInfoCurrent.grabNode.rotation) * -(attachmentOffset + itemGrabInfoCurrent.grabNode.transform.localPosition + itemGrabInfoCurrent.grabNode.offset);
							itemGrabInfoCurrent.grabRotation = Quaternion.Euler(itemGrabInfoCurrent.grabNode.rotation);
						} else if (itemGrabInfoCurrent.grabNode.grabType == GrabNode.GrabType.FixedPosition) {
							itemGrabInfoCurrent.grabOffset = Quaternion.Euler(itemGrabInfoCurrent.grabNode.rotation) * (-itemGrabInfoCurrent.grabNode.transform.localPosition - itemGrabInfoCurrent.grabNode.offset);
							itemGrabInfoCurrent.grabRotation = Quaternion.Inverse(handInfoCurrent.controller.transform.rotation) * itemGrabInfoCurrent.grabbedRigidbody.transform.rotation;
						} else if (itemGrabInfoCurrent.grabNode.grabType == GrabNode.GrabType.Dynamic || itemGrabInfoCurrent.grabNode.grabType == GrabNode.GrabType.Referral) {
							// Here
							GetDynamicItemGrabInfo(ref itemGrabInfoCurrent, ref handInfoCurrent);
						}
					} else {
						GetDynamicItemGrabInfo(ref itemGrabInfoCurrent, ref handInfoCurrent);
					}

					if (itemGrabInfoCurrent.grabbedRigidbody == itemGrabInfoOpposite.grabbedRigidbody) {      // Is the other hand already holding this item?
						itemGrabInfoCurrent.itemVelocityPercentage = 0;
						itemGrabInfoOpposite.itemVelocityPercentage = 0;
						if (itemGrabInfoCurrent.grabNode && itemGrabInfoOpposite.grabNode) {
							if (itemGrabInfoCurrent.grabNode.dominance > itemGrabInfoOpposite.grabNode.dominance) {
								grabDualWieldDominantHand = side;
								grabDualWieldDirection = Quaternion.Euler(itemGrabInfoCurrent.grabNode.rotation) * Quaternion.Inverse(itemGrabInfoCurrent.grabbedRigidbody.transform.rotation) * (GetGrabWorldPosition(itemGrabInfoOpposite) - GetGrabWorldPosition(itemGrabInfoCurrent));
							} else if (itemGrabInfoCurrent.grabNode.dominance <= itemGrabInfoOpposite.grabNode.dominance) {
								grabDualWieldDominantHand = (side == "Right" ? "Left" : "Right");
								if (itemGrabInfoCurrent.grabPoint != Vector3.zero) {
									grabDualWieldDirection = Quaternion.Inverse(handInfoOpposite.handRigidbody.transform.rotation) * (itemGrabInfoCurrent.grabPoint - handInfoOpposite.handRigidbody.transform.position);
								} else {
									grabDualWieldDirection = Quaternion.Inverse(handInfoOpposite.handRigidbody.transform.rotation) * (handInfoCurrent.handRigidbody.transform.position - handInfoOpposite.handRigidbody.transform.position);
								}
							}
						} else {
							grabDualWieldDominantHand = (side == "Right" ? "Left" : "Right");
							if (itemGrabInfoCurrent.grabPoint != Vector3.zero) {
								grabDualWieldDirection = Quaternion.Inverse(handInfoOpposite.handRigidbody.transform.rotation) * (itemGrabInfoCurrent.grabPoint - handInfoOpposite.handRigidbody.transform.position);
							} else {
								grabDualWieldDirection = Quaternion.Inverse(handInfoOpposite.handRigidbody.transform.rotation) * (handInfoCurrent.handRigidbody.transform.position - handInfoOpposite.handRigidbody.transform.position);
							}
						}
					}
				}
				handInfoCurrent.grabbingDisabled = true;
				return;
			}
		}
	}

	Vector3 GetGrabWorldPosition (ItemGrabInformation itemGrabInfoCurrent) {
		Vector3 grabWorldPos = Vector3.zero;

		if (itemGrabInfoCurrent != null) {
			grabWorldPos = itemGrabInfoCurrent.grabbedRigidbody.transform.position + itemGrabInfoCurrent.grabbedRigidbody.transform.rotation * (Quaternion.Inverse(itemGrabInfoCurrent.grabRotation) * -itemGrabInfoCurrent.grabOffset);
		}

		Debug.DrawLine(grabWorldPos + new Vector3(0, -0.125f, 0), grabWorldPos + new Vector3(0, 0.125f, 0), Color.green);
		Debug.DrawLine(grabWorldPos + new Vector3(-0.125f, 0, 0), grabWorldPos + new Vector3(0.125f, 0, 0), Color.green);
		Debug.DrawLine(grabWorldPos + new Vector3(0, 0, -0.125f), grabWorldPos + new Vector3(0, 0, 0.125f), Color.green);

		return grabWorldPos;
	}

	void GrabEnvironment(string side, EnvironmentGrabInformation envGrabInfoCurrent, EnvironmentGrabInformation envGrabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		if (handInfoCurrent.grabbingDisabled == false) {
			Vector3 originPosition = handInfoCurrent.handGameObject.transform.position;
			Collider[] climbColliders = Physics.OverlapSphere(originPosition, 0.065f, envGrabLayerMask);
			foreach (Collider hitClimb in climbColliders) {
				if (hitClimb.transform.gameObject.layer == LayerMask.NameToLayer("Climbable") || (hitClimb.transform.gameObject.layer == LayerMask.NameToLayer("Environment") && (hmd.transform.position.y - rig.transform.position.y < heightCutoffCrouching))) {
					envGrabInfoCurrent.grabOffset = bodyCC.transform.position - hitClimb.transform.position;
					envGrabInfoCurrent.grabRotation = hitClimb.transform.rotation;
					envGrabInfoCurrent.grabCCOffset = handInfoCurrent.controller.transform.position - bodyCC.transform.position;
					envGrabInfoCurrent.climbableGrabbed = hitClimb.transform;
					StartCoroutine(TriggerHapticFeedback(handInfoCurrent.controllerDevice, 3999, 0.1f));
					handInfoCurrent.grabbingDisabled = true;
					return;
				}
			}

			int rings = 9;
			int slices = 15;
			for (int a = 0; a < rings; a++) {
				for (int b = 0; b < slices; b++) {
					Vector3 currentOrigin = originPosition + (Quaternion.Euler((a - (rings / 2)) * (180 / rings), b * (360 / slices), 0) * new Vector3(0, 0, 0.065f));
					RaycastHit environmentHit;
					if (Physics.Raycast(currentOrigin + new Vector3(0, 0.05f, 0), Vector3.down, out environmentHit, 0.1f, envGrabLayerMask)) {
						if (Vector3.Angle(environmentHit.normal, Vector3.up) < 45) {
							envGrabInfoCurrent.grabOffset = bodyCC.transform.position - environmentHit.transform.position;
							envGrabInfoCurrent.grabRotation = environmentHit.transform.rotation;
							envGrabInfoCurrent.grabCCOffset = handInfoCurrent.controller.transform.position - bodyCC.transform.position;
							envGrabInfoCurrent.climbableGrabbed = environmentHit.transform;
							StartCoroutine(TriggerHapticFeedback(handInfoCurrent.controllerDevice, 3999, 0.1f));
							handInfoCurrent.grabbingDisabled = true;
							return;
						}
					}
				}
			}
		}
	}

	void GetDynamicItemGrabInfo (ref ItemGrabInformation itemGrabInfoCurrent, ref HandInformation handInfoCurrent) {
		// The purpose of this method is to make items grabbed by dual weilding and items that do not have grabNodes be grabbed on their surfaces rather than being suspended away from the hands.

		Vector3 closestColliderPoint = Vector3.zero;
		if (itemGrabInfoCurrent.grabNode == true) {
			closestColliderPoint = itemGrabInfoCurrent.grabNode.transform.GetComponent<Collider>().ClosestPoint(handInfoCurrent.handRigidbody.transform.position);
		} else {
			if (itemGrabInfoCurrent.grabbedItem.transform.GetComponent<Collider>() == null) {

			}
			closestColliderPoint = itemGrabInfoCurrent.grabbedItem.transform.GetComponent<Collider>().ClosestPoint(handInfoCurrent.handRigidbody.transform.position);
		}

		itemGrabInfoCurrent.grabPoint = closestColliderPoint;
		itemGrabInfoCurrent.grabOffset = Quaternion.Inverse(handInfoCurrent.handGameObject.transform.rotation) * Quaternion.AngleAxis(0, handInfoCurrent.controller.transform.right) * (itemGrabInfoCurrent.grabbedRigidbody.transform.position - itemGrabInfoCurrent.grabPoint);
		itemGrabInfoCurrent.grabRotation = Quaternion.Inverse(handInfoCurrent.handGameObject.transform.rotation) * Quaternion.AngleAxis(0, handInfoCurrent.controller.transform.right) * itemGrabInfoCurrent.grabbedRigidbody.transform.rotation;

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

	void ReleaseAll(string side, ItemGrabInformation itemGrabInfoCurrent, ItemGrabInformation itemGrabInfoOpposite, EnvironmentGrabInformation envGrabInfoCurrent, EnvironmentGrabInformation envGrabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		if (envGrabInfoCurrent.climbableGrabbed == true) {      // Is the current hand currently grabbing a climbable object?
			// Release: EnvironmentGrab Object
			ReleaseEnvironment(side, envGrabInfoCurrent, envGrabInfoOpposite, handInfoCurrent, handInfoOpposite);
		} else {
			// Release: ItemGrab Object
			ReleaseItem(side, itemGrabInfoCurrent, itemGrabInfoOpposite, handInfoCurrent, handInfoOpposite);
		}
	}

	void ReleaseEnvironment (string side, EnvironmentGrabInformation envGrabInfoCurrent, EnvironmentGrabInformation envGrabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		// Release: EnvironmentGrab Object
		envGrabInfoCurrent.grabOffset = Vector3.zero;
		envGrabInfoCurrent.grabCCOffset = Vector3.zero;

		envGrabInfoCurrent.climbableGrabbed = null;

		if (envGrabInfoCurrent.climbableGrabbed == null && envGrabInfoOpposite.climbableGrabbed == null) {
			velocityCurrent = Vector3.ClampMagnitude(velocityCurrent, 5f);
		}
	}

	void ReleaseItem (string side, ItemGrabInformation itemGrabInfoCurrent, ItemGrabInformation itemGrabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		// Release: ItemGrab Object
		if (itemGrabInfoCurrent.grabbedRigidbody == true) {
			itemGrabInfoCurrent.itemVelocityPercentage = 0.25f;
			itemGrabInfoOpposite.itemVelocityPercentage = 0.25f;

			if (itemGrabInfoOpposite.grabbedRigidbody != itemGrabInfoCurrent.grabbedRigidbody) {
				AttachmentNodePair attachmentNodePairInfo = GetAttachmentConnection(itemGrabInfoCurrent, handInfoCurrent);		// Get possible attachmentNodePair for current item (null if no pairs are available)
				if (attachmentNodePairInfo == null) {        // If there are no possible AttachmentNodePairs for this item
					ThrowItem(handInfoCurrent, itemGrabInfoCurrent, (handInfoCurrent.controller.transform.position - handInfoCurrent.handPosLastFrame) / Time.deltaTime);
				} else {
					InitiateAttachment(attachmentNodePairInfo, itemGrabInfoCurrent);
				}
			} else {
				// For items without grabNodes:
				if (itemGrabInfoOpposite.grabNode == null) {
					if (grabDualWieldDominantHand != itemGrabInfoCurrent.side) {		// If the opposite hand is the dominant hand
						GetDynamicItemGrabInfo(ref itemGrabInfoOpposite, ref handInfoOpposite);
					} else {
						itemGrabInfoOpposite.grabOffset = Quaternion.Inverse(handInfoOpposite.handGameObject.transform.rotation) * (itemGrabInfoOpposite.grabbedRigidbody.transform.position - handInfoOpposite.handGameObject.transform.position);
						itemGrabInfoOpposite.grabRotation = Quaternion.Inverse(handInfoOpposite.handGameObject.transform.rotation) * itemGrabInfoOpposite.grabbedRigidbody.transform.rotation;
					}
				} else {
					if (itemGrabInfoOpposite.grabNode.grabType == GrabNode.GrabType.Dynamic) {
						GetDynamicItemGrabInfo(ref itemGrabInfoOpposite, ref handInfoOpposite);
						//itemGrabInfoOpposite.grabOffset = Quaternion.Inverse(handInfoOpposite.handGameObject.transform.rotation) * (itemGrabInfoOpposite.grabbedRigidbody.transform.position - handInfoOpposite.handGameObject.transform.position);
						//itemGrabInfoOpposite.grabRotation = Quaternion.Inverse(handInfoOpposite.handGameObject.transform.rotation) * itemGrabInfoOpposite.grabbedRigidbody.transform.rotation;
					}
				}
			}

			// If theres a weapon, update it's combined attributes
			itemGrabInfoCurrent.grabbedItem.isGrabbed = false;
			if (itemGrabInfoCurrent.grabbedRigidbody.transform.GetComponent<Item>() is Weapon) {
				itemGrabInfoCurrent.grabbedRigidbody.transform.GetComponent<Weapon>().UpdateCombinedAttributes();
			}

		} else {
			if (handInfoCurrent.jumpLoaded == true) {
				if (grounded == true || timeLastJumped + 0.1f > Time.timeSinceLevelLoad) {
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
		}
		
		// Release item infos
		itemGrabInfoCurrent.grabbedRigidbody = null;
		itemGrabInfoCurrent.grabbedItem = null;
		itemGrabInfoCurrent.grabNode = null;
		itemGrabInfoCurrent.grabPoint = Vector3.zero;
	}

	void TriggerDown(string side, ItemGrabInformation itemGrabInfoCurrent, EnvironmentGrabInformation envGrabInfoCurrent, HandInformation handInfoCurrent) {
		if (itemGrabInfoCurrent.grabbedItem is Weapon) {
			Weapon currentWeapon = itemGrabInfoCurrent.grabbedItem as Weapon;
			currentWeapon.timeLastTriggered = Time.timeSinceLevelLoad;
			currentWeapon.AdjustAmmo(0);
			if (itemGrabInfoCurrent.grabNode.triggerType == GrabNode.TriggerType.Fire) {
				if (currentWeapon.combinedAttributes.chargingEnabled == false) {
					AttemptToFireWeapon(side, itemGrabInfoCurrent.grabbedItem, handInfoCurrent);
				}
			}
		}
	}

	void TriggerHold(string side, ItemGrabInformation itemGrabInfoCurrent, EnvironmentGrabInformation envGrabInfoCurrent, HandInformation handInfoCurrent) {
		if (itemGrabInfoCurrent.grabbedItem is Weapon) {
			Weapon currentWeapon = itemGrabInfoCurrent.grabbedItem as Weapon;
			currentWeapon.triggerHeld = true;
			currentWeapon.timeLastTriggered = Time.timeSinceLevelLoad;
			if (itemGrabInfoCurrent.grabNode.triggerType == GrabNode.TriggerType.Fire) {
				if (currentWeapon.combinedAttributes.automatic == true) {
					AttemptToFireWeapon(side, itemGrabInfoCurrent.grabbedItem, handInfoCurrent);
				}
				if (currentWeapon.combinedAttributes.chargingEnabled == true) {
					currentWeapon.chargeCurrent = Mathf.Clamp01(currentWeapon.chargeCurrent + (currentWeapon.combinedAttributes.chargeIncrement * Time.deltaTime));
				}
			}
		}
	}

	void TriggerUp(string side, ItemGrabInformation itemGrabInfoCurrent, EnvironmentGrabInformation envGrabInfoCurrent, HandInformation handInfoCurrent) {
		if (itemGrabInfoCurrent.grabbedItem is Weapon) {
			Weapon currentWeapon = itemGrabInfoCurrent.grabbedItem as Weapon;
			currentWeapon.triggerHeld = false;
			if (itemGrabInfoCurrent.grabNode.triggerType == GrabNode.TriggerType.Fire) {
				if (currentWeapon.combinedAttributes.chargingEnabled == true) {
					AttemptToFireWeapon(side, itemGrabInfoCurrent.grabbedItem, handInfoCurrent);
				}
			}
		}
	}

	void TriggerNull(string side, ItemGrabInformation itemGrabInfoCurrent, EnvironmentGrabInformation envGrabInfoCurrent) {
		if (itemGrabInfoCurrent.grabbedItem is Weapon) {
			Weapon currentWeapon = itemGrabInfoCurrent.grabbedItem as Weapon;
			currentWeapon.triggerHeld = false;
		}
	}

	void InteractDown () {

	}

	void AttemptToFireWeapon(string side, Item currentItem, HandInformation handInfoCurrent) {
		Weapon currentWeapon = currentItem as Weapon;
		if (currentWeapon.timeLastFired + (1 / currentWeapon.combinedAttributes.firerate) <= Time.timeSinceLevelLoad) {
			if (currentWeapon.combinedAttributes.chargingEnabled == false || (currentWeapon.chargeCurrent >= currentWeapon.combinedAttributes.chargeRequired)) {
				if (currentWeapon.ammoCurrent >= currentWeapon.combinedAttributes.consumption) {       // Does the weapon enough ammo?
					if (currentWeapon.combinedAttributes.consumePerBurst == false) {
						currentWeapon.AdjustAmmo(-currentWeapon.combinedAttributes.consumption);
					}
					StartCoroutine(FireWeapon(side, currentItem, handInfoCurrent));
					currentWeapon.timeLastFired = Time.timeSinceLevelLoad;
				}
			}
		}
	}

	IEnumerator FireWeapon(string hand, Item currentItem, HandInformation handInfoCurrent) {
		Weapon currentWeapon = currentItem as Weapon;

		for (int i = 0; i < Mathf.Clamp(currentWeapon.combinedAttributes.burstCount, 1, 100); i++) {           // For each burst shot in this fire
			if (currentWeapon.combinedAttributes.consumePerBurst == false || currentWeapon.ammoCurrent >= currentWeapon.combinedAttributes.consumption) {
				if (currentWeapon.combinedAttributes.consumePerBurst == true) {
					currentWeapon.AdjustAmmo(-currentWeapon.combinedAttributes.consumption);
				}

				// Step 1: Trigger haptic feedback
				if (itemGrabInfoLeft.grabbedRigidbody == itemGrabInfoRight.grabbedRigidbody) {
					StartCoroutine(TriggerHapticFeedback(handInfoLeft.controllerDevice, 3999, 0.1f));
					StartCoroutine(TriggerHapticFeedback(handInfoRight.controllerDevice, 3999, 0.1f));
				} else {
					StartCoroutine(TriggerHapticFeedback((hand == "Left" ? handInfoLeft.controllerDevice : handInfoRight.controllerDevice), 3999, 0.1f));
				}

				// Step 2: Apply velocity and angular velocity to weapon
				if (itemGrabInfoLeft.grabbedItem == itemGrabInfoRight.grabbedItem) {
					handInfoLeft.handOffsetPosition = Vector3.ClampMagnitude(handInfoLeft.handOffsetPosition + currentItem.transform.rotation * new Vector3(0, 0, -currentWeapon.combinedAttributes.recoilLinear), handOffsetPositionMax);
					handInfoRight.handOffsetPosition = Vector3.ClampMagnitude(handInfoRight.handOffsetPosition + currentItem.transform.rotation * new Vector3(0, 0, -currentWeapon.combinedAttributes.recoilLinear), handOffsetPositionMax);
					handInfoLeft.handOffsetRotation += new Vector3(Random.Range(-currentWeapon.combinedAttributes.recoilAngular, currentWeapon.combinedAttributes.recoilAngular), Random.Range(0, currentWeapon.combinedAttributes.recoilAngular), 0);
					handInfoRight.handOffsetRotation += new Vector3(Random.Range(-currentWeapon.combinedAttributes.recoilAngular, currentWeapon.combinedAttributes.recoilAngular), Random.Range(0, currentWeapon.combinedAttributes.recoilAngular), 0);

					//handInfoLeft.handOffsetRotation *= Quaternion.Euler(Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular));
					//handInfoRight.handOffsetRotation *= Quaternion.Euler(Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular));
				} else {
					handInfoCurrent.handOffsetPosition = Vector3.ClampMagnitude(handInfoCurrent.handOffsetPosition + currentItem.transform.rotation * new Vector3(0, 0, -currentWeapon.combinedAttributes.recoilLinear), handOffsetPositionMax);
					handInfoCurrent.handOffsetRotation += new Vector3(Random.Range(-currentWeapon.combinedAttributes.recoilAngular * 0.25f, currentWeapon.combinedAttributes.recoilAngular * 0.25f), Random.Range(0, currentWeapon.combinedAttributes.recoilAngular), 0);
					//handInfoCurrent.handOffsetRotation *= Quaternion.Euler(Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular), Random.Range(-currentWeapon.recoilAngular, currentWeapon.recoilAngular));
				}

				// Step 3: Adjust weapon accuracy & get random accuracy
				currentWeapon.accuracyCurrent = Mathf.Clamp(currentWeapon.accuracyCurrent - currentWeapon.combinedAttributes.accuracyDecrement, currentWeapon.combinedAttributes.accuracyMin, currentWeapon.combinedAttributes.accuracyMax);
				float angleMax = Mathf.Abs(currentWeapon.accuracyCurrent - 1) * 5f;
				Quaternion randomAccuracy = Quaternion.Euler(Random.Range(-angleMax, angleMax), Random.Range(-angleMax, angleMax), Random.Range(-angleMax, angleMax));

				for (int j = 0; (j < currentWeapon.combinedAttributes.projectileSpreads.Length || (currentWeapon.combinedAttributes.projectileSpreads.Length == 0 && j == 0)); j++) {

					// Step 5: Get random spread deviations
					Quaternion projectileSpreadDeviation = Quaternion.Euler(Random.Range(-currentWeapon.combinedAttributes.projectileSpreadDeviation, currentWeapon.combinedAttributes.projectileSpreadDeviation), Random.Range(-currentWeapon.combinedAttributes.projectileSpreadDeviation, currentWeapon.combinedAttributes.projectileSpreadDeviation), 0);

					// Step 4: Create new projectile
					GameObject newProjectile = (GameObject)Instantiate(currentWeapon.combinedAttributes.projectile, currentWeapon.barrelPoint.position + currentWeapon.barrelPoint.forward * 0.2f, currentItem.transform.rotation * randomAccuracy);
					if (currentWeapon.combinedAttributes.projectileSpreads.Length > 0) {
						if (currentWeapon.combinedAttributes.projectileSpreadType == Weapon.SpreadType.Circular) {
							newProjectile.transform.rotation *= projectileSpreadDeviation * Quaternion.Euler(0, 0, currentWeapon.combinedAttributes.projectileSpreads[j].x) * Quaternion.Euler(currentWeapon.combinedAttributes.projectileSpreads[j].y, 0, 0);
						} else {
							newProjectile.transform.rotation *= Quaternion.Euler(currentWeapon.combinedAttributes.projectileSpreads[j].y, currentWeapon.combinedAttributes.projectileSpreads[j].x, 0);
						}
					}
					Projectile newProjectileClass = newProjectile.GetComponent<Projectile>();
					if (currentWeapon.combinedAttributes.chargingEnabled == true) {
						newProjectileClass.velocity = newProjectile.transform.forward * (currentWeapon.combinedAttributes.projectileVelocity - (currentWeapon.combinedAttributes.projectileVelocity * currentWeapon.combinedAttributes.chargeInfluenceVelocity * Mathf.Abs(currentWeapon.chargeCurrent - 1)));
					} else {
						newProjectileClass.velocity = newProjectile.transform.forward * currentWeapon.combinedAttributes.projectileVelocity;
					}
					newProjectileClass.deceleration = currentWeapon.combinedAttributes.projectileDeceleration;
					newProjectileClass.decelerationType = currentWeapon.combinedAttributes.projectileDecelerationType;
					newProjectileClass.gravity = currentWeapon.combinedAttributes.projectileGravity;
					newProjectileClass.ricochetCount = currentWeapon.combinedAttributes.projectileRicochetCount;
					newProjectileClass.ricochetAngleMax = currentWeapon.combinedAttributes.projectileRicochetAngleMax;
					newProjectileClass.lifespan = currentWeapon.combinedAttributes.projectileLifespan;
					newProjectileClass.sticky = currentWeapon.combinedAttributes.projectileIsSticky;
					audioManager.PlayClipAtPoint(currentWeapon.soundFireNormal, currentWeapon.barrelPoint.position, 2f);

				}
			} else {
				// TODO: Dry shot
			}
			yield return new WaitForSeconds(currentWeapon.combinedAttributes.burstDelay);
		}

		if (currentWeapon.combinedAttributes.chargingEnabled == true) {
			currentWeapon.chargeCurrent = Mathf.Clamp01(currentWeapon.chargeCurrent - currentWeapon.combinedAttributes.chargeDecrementPerShot);
		}
	}

	void ThrowItem(HandInformation handInfoCurrent, ItemGrabInformation itemGrabInfoCurrent, Vector3 velocity) {
		// If we are currently grabbing (throwing) an item that is an attachment on another item, set the current item to that parent item
		Item mainItem = itemGrabInfoCurrent.grabbedItem;
		while (mainItem.transform.parent != null && mainItem.transform.parent.name == "(Attachments)") {     // If this is an attachment that is currently attached (repeat until we are no longer in an attached attachment)
			mainItem = mainItem.transform.parent.parent.GetComponent<Item>();
		}

		itemGrabInfoCurrent.grabbedRigidbody.velocity += velocityCurrent;
		itemGrabInfoCurrent.grabbedRigidbody.useGravity = true;
		itemGrabInfoCurrent.grabbableItemLastFrame = mainItem.transform;

		if (itemGrabInfoCurrent.grabbedRigidbody.transform.GetComponent<Item>() != null) {
			if (itemGrabInfoCurrent.grabbedRigidbody.GetComponent<Item>() is Weapon) {
				(itemGrabInfoCurrent.grabbedRigidbody.transform.GetComponent<Item>() as Weapon).triggerHeld = false;
			}
		}

		// Check for pockets
		Collider[] pockets = Physics.OverlapSphere(handInfoCurrent.handRigidbody.transform.position, 0.2f, pocketMask);
		if (pockets.Length > 0) {
			List<Pocket> availablePockets = new List<Pocket>();

			// Find pockets that are currently available and add them to availablePockets list
			for (int i = 0; i < pockets.Length; i++) {
				if (pockets[i].GetComponent<Pocket>()) {
					Pocket currentPocketObject = pockets[i].GetComponent<Pocket>();
					if (currentPocketObject.GetAvailability() == true && currentPocketObject.pocketSize == mainItem.pocketSize && Vector3.Angle(itemGrabInfoCurrent.grabbedRigidbody.transform.forward, currentPocketObject.transform.forward) <= currentPocketObject.angleRange) {
						availablePockets.Add(currentPocketObject);
					}
				}
			}

			if (availablePockets.Count > 0) {
				// Find closest pocket
				Pocket chosenPocket = availablePockets[0];
				float closestPocketDistance = Vector3.Distance(handInfoCurrent.handRigidbody.transform.position, chosenPocket.transform.position);
				for (int j = 1; j < availablePockets.Count; j++) {
					if (Vector3.Distance(handInfoCurrent.handRigidbody.transform.position, availablePockets[j].transform.position) < closestPocketDistance) {
						chosenPocket = availablePockets[j];
					}
				}

				// Asign Pocket Info
				chosenPocket.PocketItem(mainItem);
			}
		}

	}

	IEnumerator TriggerHapticFeedback(SteamVR_Controller.Device device, ushort strength, float duration) {
		for (float i = 0; i <= duration; i += 0.01f) {
			device.TriggerHapticPulse(strength);
			yield return new WaitForSeconds(0.01f);
		}
	}

	IEnumerator TriggerHapticFeedback(SteamVR_Controller.Device device, ushort strength, float duration, int ticks) {
		for (int t = 0; t < Mathf.Clamp(ticks, 1, 999); t++) {
			for (float i = 0; i <= duration; i += 0.01f) {
				device.TriggerHapticPulse(strength);
				yield return new WaitForSeconds(0.01f);
			}
			yield return new WaitForSeconds(0.025f);
		}
	}

	void UpdatePlayerVelocity() {
		moveSpeedCurrent = Mathf.Lerp(moveSpeedCurrent, (bodyCC.height > heightCutoffStanding ? moveSpeedStanding : (bodyCC.height > heightCutoffCrouching ? moveSpeedCrouching : moveSpeedLaying)), 5 * Time.deltaTime);
		velocityDesired = new Vector3(handInfoLeft.controllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad).x, velocityDesired.y, handInfoLeft.controllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad).y) * moveSpeedCurrent;
		velocityDesired = Quaternion.LookRotation(new Vector3(hmd.transform.forward.x, 0, hmd.transform.forward.z), Vector3.up) * velocityDesired;
		if (envGrabInfoLeft.climbableGrabbed == null && envGrabInfoRight.climbableGrabbed == null) {
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

			if (envGrabInfoLeft.climbableGrabbed == true) {
				climbRotationLeft = Quaternion.Inverse(envGrabInfoLeft.grabRotation) * envGrabInfoLeft.climbableGrabbed.rotation;
			}

			if (envGrabInfoRight.climbableGrabbed == true) {
				climbRotationRight = Quaternion.Inverse(envGrabInfoRight.grabRotation) * envGrabInfoRight.climbableGrabbed.rotation;
			}

			if (envGrabInfoLeft.climbableGrabbed == true) {
				combinedClimbPositions += (envGrabInfoLeft.climbableGrabbed.position + climbRotationLeft * envGrabInfoLeft.grabOffset) + (climbRotationLeft * envGrabInfoLeft.grabCCOffset - (handInfoLeft.controller.transform.position - bodyCC.transform.position));
				climbCount++;
			}

			if (envGrabInfoRight.climbableGrabbed == true) {
				combinedClimbPositions += (envGrabInfoRight.climbableGrabbed.position + climbRotationRight * envGrabInfoRight.grabOffset) + (climbRotationRight * envGrabInfoRight.grabCCOffset - (handInfoRight.controller.transform.position - bodyCC.transform.position));
				climbCount++;
			}

			combinedClimbPositions = combinedClimbPositions / climbCount;

			velocityCurrent = Vector3.Lerp(velocityCurrent, (combinedClimbPositions - bodyCC.transform.position) / Time.deltaTime, Mathf.Clamp01(50 * Time.deltaTime));

			envGrabInfoLeft.grabRotationLastFrame = climbRotationLeft;
			envGrabInfoRight.grabRotationLastFrame = climbRotationRight;
			isClimbing = true;
		}
		handInfoLeft.handPosLastFrame = handInfoLeft.controller.transform.position;
		handInfoRight.handPosLastFrame = handInfoRight.controller.transform.position;

	}

	void UpdateCharacterControllerHeight () {
		// This method updates the bodyCC's height, growing it taller when standing/airborne, and smaller when climbing/jumping.
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
		// This method moves the player's headCC and in turn, bodyCC according to the current frame of HMD movement
		Vector3 bodyPositionBefore = bodyCC.transform.position;

		verticalPusher.transform.localPosition = Vector3.Lerp(verticalPusher.transform.localPosition, Vector3.zero, 7.5f * Time.deltaTime);		// Smooth the vertical pusher into it's default 0,0,0 position

		// Step 1: Move HeadCC to HMD					// Get the difference between the head position and the HMD position and apply that movement to the headCC
		//Vector3 headToHmdDelta = ((hmd.transform.position - ((verticalPusher.transform.localPosition) * Mathf.Clamp01(Time.deltaTime * 5))) - headCC.transform.position);     // Delta position moving from headCC to HMD (smoothing applied through the verticalPusher)
		Vector3 headToHmdDelta = (hmd.transform.position - headCC.transform.position);     // Delta position moving from headCC to HMD (smoothing applied through the verticalPusher)
		headCC.Move(headToHmdDelta); // Attempt to move the headCC			
		if (envGrabInfoLeft.climbableGrabbed == null && envGrabInfoRight.climbableGrabbed == null) { // Are we not climbing?
			verticalPusher.transform.position += new Vector3(0, (headCC.transform.position - hmd.transform.position).y, 0); // Move the vertical pusher to accomodate for HMD moving too far vertically through geometry (ie: down into box, up into desk)
		}

		// Step 2: Move BodyCC to HeadCC (Horizontally Only)
		GetGroundInformation();
		Vector3 neckOffset = new Vector3(hmd.transform.forward.x + hmd.transform.up.x, 0, hmd.transform.forward.z + hmd.transform.up.z).normalized * -0.05f;        // The current neck offset for how far away the bodyCC should be from the center of the headCC
		Vector3 bodyToHeadDeltaXZ = ((headCC.transform.position + neckOffset) - bodyCC.transform.position);
		bodyToHeadDeltaXZ.y = 0;
		CustomCharacterControllerMove(bodyCC, GetSlopeMovement(bodyToHeadDeltaXZ));
		//bodyCC.Move(GetSlopeMovement(bodyToHeadDeltaXZ));

		// Step 3: Move Rig according to BodyDelta (Vertically Only)
		Vector3 bodyDeltaPosition = bodyCC.transform.position - bodyPositionBefore;
		rig.transform.position += bodyDeltaPosition;

		// Step 4: Repeat Step 1 (Vertically Only)
		Vector3 headToHmdDelta2 = (hmd.transform.position - headCC.transform.position);     // Delta position moving from headCC to HMD (smoothing applied through the verticalPusher)
		headToHmdDelta2.x = 0; headToHmdDelta2.z = 0;
		headCC.Move(headToHmdDelta2); // Attempt to move the headCC			
		if (envGrabInfoLeft.climbableGrabbed == null && envGrabInfoRight.climbableGrabbed == null) { // Are we not climbing?
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
		// This method moves the player's bodyCC, and in turn headCC, by applying movement based on the player's velocity
		Vector3 bodyPositionBefore = bodyCC.transform.position;

		Debug.DrawRay(bodyCC.transform.position, velocityCurrent, Color.green);		// Debug ray showing player velocity

		// Step 1: Apply Gravity for this frame
		velocityCurrent += new Vector3(0, -9.8f * Time.deltaTime, 0);

		// Step 2: Move BodyCC with velocityCurrent
		GetGroundInformation();		// First, get ground information to know if we're on a slope/grounded/airborne
		//bodyCC.Move(GetSlopeMovement(velocityCurrent * Time.deltaTime));
		CustomCharacterControllerMove(bodyCC, GetSlopeMovement(velocityCurrent * Time.deltaTime));

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

	void CustomCharacterControllerMove(CharacterController cc, Vector3 deltaMovement) {
		// The purpose of this method is to move a characterController similarly to the CharacterController.Move method
		// This difference here is that this method uses a better stepping calculation

		// Step 1: Test capsule collider to see if cc will hit anything, if not > step 4
		RaycastHit hit;
		if (isClimbing == false && deltaMovement.magnitude > 0.001f && Physics.CapsuleCast(cc.transform.position + new Vector3(0, (-cc.height / 2f) + cc.radius + (cc.skinWidth * 2), 0), cc.transform.position + new Vector3(0, (cc.height / 2f) - cc.radius, 0), cc.radius, deltaMovement.normalized, out hit, deltaMovement.magnitude + cc.skinWidth, bodyCCLayerMask)) {
			// Step 2: check hit.normal. If hit normal is greater than slopeAnlge, then continue, if not > step 4
			RaycastHit hitRay;
			Vector3 hitOffsetPoint = hit.point + (hit.normal * -0.025f);
			Vector3 rayOrigin = cc.transform.position + new Vector3(0, (-cc.height / 2) + 0.0125f, 0);
			if (Physics.Raycast(rayOrigin, (hitOffsetPoint - rayOrigin).normalized, out hitRay, Vector3.Distance(hitOffsetPoint, rayOrigin) + 0.015f, bodyCCLayerMask) && Vector3.Angle(Vector3.up, hitRay.normal) >= (90 - cc.slopeLimit)) {
				// Step 3: Test capsule collider going in same direction increasing starting height every time until we do not hit anything.
				// If we don't hit anything eventually, then step that high vertically. If we always hit something, skip to step 4
				bool canStep = false;
				float stepOffset = 0;

				float incrementHeight = 0.0125f;
				int increments = (int)Mathf.Floor(stepHeight / incrementHeight);

				for (int i = 1; i <= increments; i++) {
					Vector3 currentIncrementOffset = new Vector3(0, incrementHeight * i);		
					if (!Physics.CapsuleCast(cc.transform.position + new Vector3(0, (-cc.height / 2f) + cc.radius + (cc.skinWidth * 2), 0) + currentIncrementOffset, cc.transform.position + new Vector3(0, (cc.height / 2f) - cc.radius, 0) + currentIncrementOffset, cc.radius, deltaMovement.normalized, out hit, deltaMovement.magnitude + cc.skinWidth, bodyCCLayerMask)) {
						canStep = true;
						stepOffset = i * incrementHeight;
						break;
					}
				}

				if (canStep) {
					// Step 4: Check for ceiling
					if (Physics.OverlapCapsule(cc.transform.position + new Vector3(0, (-cc.height / 2f) + cc.radius + cc.skinWidth, 0), cc.transform.position + new Vector3(0, ((cc.height / 2f) - cc.radius) + cc.skinWidth + stepHeight, 0), cc.radius, bodyCCLayerMask).Length == 0) {
						cc.Move(new Vector3(0, stepOffset, 0));
						verticalPusher.transform.position -= new Vector3(0, stepOffset, 0);
					}
				} else {
					
				}
			} else {
				Debug.LogError("Fail secondary raycast");
			}
		}

		// Step 5: move cc with deltaMovement
		cc.Move(deltaMovement);

	}

	void RealignRig() {
		// Realigns the rig to put the hmd in the same position as the headCC by moving the rig (which in turn, moves the hmd into position)
		Vector3 hmdHeadDifference = head.transform.position - hmd.transform.position;
		rig.transform.position += hmdHeadDifference;
	}

	void GetGroundInformation () {
		// This method will get ground infromation:
			// 1. Are we touching the ground?
			// 2. What is the normal of the ground?
		
		RaycastHit hit;

		Vector3 sphereCastOrigin = bodyCC.transform.position + new Vector3(0, (-bodyCC.height / 2) + bodyCC.radius, 0);
		Vector3 sphereCastDirection = Vector3.down;
		float sphereCastRadius = bodyCC.radius;
		float sphereCastLength = 0.025f + (bodyCC.skinWidth * 2);

		if (isClimbing == false && Physics.SphereCast(sphereCastOrigin, sphereCastRadius, sphereCastDirection, out hit, sphereCastLength, bodyCCLayerMask) && hit.point.y < bodyCC.transform.position.y) {
			if (grounded == false) {
				float heightDifference = GetPlayerRealLifeHeight() - bodyCC.height;
				verticalPusher.transform.position += new Vector3(0, -heightDifference, 0);
				rig.transform.position += new Vector3(0, heightDifference, 0);									// TODO: yikes, should a Get method really be messing with stuff like this?
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
		// This method moves items along with the player's movement every frame. This is NOT the method for moving objects along with the player's hands
		if (itemGrabInfoLeft.grabbedRigidbody == itemGrabInfoRight.grabbedRigidbody) {
			if (itemGrabInfoLeft.grabbedRigidbody) {
				itemGrabInfoLeft.grabbedRigidbody.transform.position = (itemGrabInfoLeft.grabbedRigidbody.transform.position + deltaPosition);
			}
		} else {
			if (itemGrabInfoLeft.grabbedRigidbody) {
				itemGrabInfoLeft.grabbedRigidbody.transform.position = (itemGrabInfoLeft.grabbedRigidbody.transform.position + deltaPosition);
			}
			if (itemGrabInfoRight.grabbedRigidbody) {
				itemGrabInfoRight.grabbedRigidbody.transform.position = (itemGrabInfoRight.grabbedRigidbody.transform.position + deltaPosition);
			}

		}
	}

	void UpdateAvatar() {
		avatar.position = bodyCC.transform.position - new Vector3(0, bodyCC.height / 2f, 0);
		avatarTorso.position = new Vector3(bodyCC.transform.position.x, headCC.transform.position.y - 0.3f, bodyCC.transform.position.z);
		Vector3 hmdFlatForward = new Vector3(hmd.transform.forward.x, 0, hmd.transform.forward.z).normalized;
		Vector3 hmdFlatUp = new Vector3(hmd.transform.up.x, 0, hmd.transform.up.z).normalized;

		Vector3 hmdFlatFinal = Vector3.Lerp(hmdFlatForward, (hmd.transform.forward.y > 0) ? -hmdFlatUp : hmdFlatUp, Mathf.Clamp01(Vector3.Angle(hmd.transform.up, Vector3.up) / 90));
		avatar.rotation = Quaternion.LookRotation(hmdFlatFinal, Vector3.up);
	}

	public void MovePlayer(Vector3 deltaPosition) {
		// This method is used as a way for other objects to move the player. (ie: elevator moving the player)
		Vector3 bodyPositionBefore = bodyCC.transform.position;
		bodyCC.Move(deltaPosition);
		Vector3 netCCMovement = (bodyCC.transform.position - bodyPositionBefore);
		rig.transform.position += netCCMovement;
		platformMovementsAppliedLastFrame += netCCMovement;
	}

	void UpdateHandAndItemPhysics() {
		// Left Hand
		UpdateHandPhysics("Left", handInfoLeft, itemGrabInfoLeft);

		// Right Hand
		UpdateHandPhysics("Right", handInfoRight, itemGrabInfoRight);

		Weapon weaponLeft = null;
		Weapon weaponRight = null;

		if (itemGrabInfoLeft.grabbedItem is Weapon) {
			weaponLeft = itemGrabInfoLeft.grabbedItem as Weapon;
			weaponRight = itemGrabInfoRight.grabbedItem as Weapon;
		}

		// Item Physics
		if (itemGrabInfoLeft.grabbedRigidbody != null && itemGrabInfoLeft.grabbedRigidbody == itemGrabInfoRight.grabbedRigidbody) {
			// Physics - Dual Wielding
			Rigidbody grabbedItemDominant = (grabDualWieldDominantHand == "Left") ? itemGrabInfoLeft.grabbedRigidbody : itemGrabInfoRight.grabbedRigidbody;
			Rigidbody handDominant = (grabDualWieldDominantHand == "Left") ? handInfoLeft.handRigidbody : handInfoRight.handRigidbody;
			Vector3 dualWieldDirectionCurrent = (((grabDualWieldDominantHand == "Left") ? handInfoRight.handRigidbody.transform.position : handInfoLeft.handRigidbody.transform.position) - ((grabDualWieldDominantHand == "Left") ? handInfoLeft.handRigidbody.transform.position : handInfoRight.handRigidbody.transform.position));
			Quaternion dualWieldDirectionChangeRotation = Quaternion.FromToRotation(handDominant.transform.rotation * grabDualWieldDirection, dualWieldDirectionCurrent);
			Quaternion rotationDeltaItem = (dualWieldDirectionChangeRotation * handDominant.transform.rotation * ((grabDualWieldDominantHand == "Left") ? itemGrabInfoLeft.grabRotation : itemGrabInfoRight.grabRotation)) * Quaternion.Inverse(itemGrabInfoLeft.grabbedRigidbody.transform.rotation);

			float angleItem;
			Vector3 axisItem;
			rotationDeltaItem.ToAngleAxis(out angleItem, out axisItem);
			if (angleItem > 180) {
				angleItem -= 360;
			}

			itemGrabInfoLeft.grabbedRigidbody.velocity = Vector3.Lerp(itemGrabInfoLeft.grabbedRigidbody.velocity, Vector3.ClampMagnitude(((handDominant.transform.position + (dualWieldDirectionChangeRotation * handDominant.transform.rotation * ((grabDualWieldDominantHand == "Left") ? itemGrabInfoLeft.grabOffset : itemGrabInfoRight.grabOffset))) - grabbedItemDominant.transform.position) / Time.fixedDeltaTime, (grabbedItemDominant.GetComponent<HingeJoint>()) ? 1 : 100) * Mathf.Lerp(itemGrabInfoLeft.itemVelocityPercentage, itemGrabInfoRight.itemVelocityPercentage, 0.5f), Mathf.Clamp01(50 * Time.deltaTime));

			if (angleItem != float.NaN) {
				itemGrabInfoLeft.grabbedRigidbody.maxAngularVelocity = Mathf.Infinity;
				itemGrabInfoLeft.grabbedRigidbody.angularVelocity = Vector3.Lerp(itemGrabInfoLeft.grabbedRigidbody.angularVelocity, (angleItem * axisItem) * Mathf.Lerp(itemGrabInfoLeft.itemVelocityPercentage, itemGrabInfoRight.itemVelocityPercentage, 0.5f) * 0.95f, Mathf.Clamp01(50 * Time.deltaTime));
			}

			// Accuracy - Dual Wield
			if (itemGrabInfoLeft.grabbedItem is Weapon) {
				weaponLeft.accuracyCurrent = Mathf.Clamp(weaponLeft.accuracyCurrent + (weaponLeft.combinedAttributes.accuracyIncrement * Time.deltaTime), weaponLeft.combinedAttributes.accuracyMin, weaponLeft.combinedAttributes.accuracyMax);
			}

		} else {
			// Physics - Left
			if (itemGrabInfoLeft.grabbedItem) {
				UpdateItemPhysics("Left", itemGrabInfoLeft, itemGrabInfoRight, envGrabInfoLeft, envGrabInfoRight, handInfoLeft, handInfoRight);
				if (weaponLeft) {
					weaponLeft.accuracyCurrent = Mathf.Clamp(weaponLeft.accuracyCurrent + (weaponLeft.combinedAttributes.accuracyIncrement * Time.deltaTime), weaponLeft.combinedAttributes.accuracyMin, weaponLeft.combinedAttributes.accuracyMax);
				}
			}

			// Physics - Right
			if (itemGrabInfoRight.grabbedItem) {
				UpdateItemPhysics("Right", itemGrabInfoRight, itemGrabInfoLeft, envGrabInfoLeft, envGrabInfoRight, handInfoRight, handInfoLeft);
				if (weaponRight) {
					weaponRight.accuracyCurrent = Mathf.Clamp(weaponRight.accuracyCurrent + (weaponRight.combinedAttributes.accuracyIncrement * Time.deltaTime), weaponRight.combinedAttributes.accuracyMin, weaponRight.combinedAttributes.accuracyMax);
				}
			}
		}
	}

	void UpdateHandPhysics(string side, HandInformation handInfoCurrent, ItemGrabInformation itemGrabInfoCurrent) {
		itemGrabInfoCurrent.itemVelocityPercentage = Mathf.Clamp01(itemGrabInfoCurrent.itemVelocityPercentage + Time.deltaTime * (itemGrabInfoCurrent.grabbedRigidbody != null ? 2 : -10));

		Vector3 handOffsetDefault = Quaternion.Euler(handInfoCurrent.handOffsetRotation) * handInfoCurrent.controller.transform.rotation * new Vector3(handRigidbodyPositionOffset.x * (side == "Left" ? 1 : -1), handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z);
		Vector3 handOffsetKick = handInfoCurrent.handOffsetPosition;
		handInfoCurrent.handRigidbody.velocity = (((handInfoCurrent.controller.transform.position + handOffsetDefault + handOffsetKick) - handInfoCurrent.handGameObject.transform.position) + verticalPusher.transform.localPosition) / Time.fixedDeltaTime;
		handInfoCurrent.handRigidbody.velocity = (((handInfoCurrent.controller.transform.position + handOffsetDefault + handOffsetKick) - handInfoCurrent.handGameObject.transform.position)) / Time.fixedDeltaTime;


		handInfoCurrent.handOffsetPosition = Vector3.Lerp(handInfoCurrent.handOffsetPosition, Vector3.zero, 5 * Time.deltaTime);
		handInfoCurrent.handOffsetRotation = Vector3.Lerp(handInfoCurrent.handOffsetRotation, new Vector3(0, 0, 0), 10 * Time.deltaTime);

		Quaternion rotationDelta = Quaternion.Euler(0, 0, 0);
		if (itemGrabInfoCurrent.grabbedRigidbody != null && itemGrabInfoCurrent.grabNode != null && (itemGrabInfoCurrent.grabNode.grabType != GrabNode.GrabType.Dynamic && itemGrabInfoCurrent.grabNode.grabType != GrabNode.GrabType.FixedPosition)) {
			rotationDelta = (Quaternion.Euler(handInfoCurrent.handOffsetRotation) * Quaternion.AngleAxis(-30, handInfoCurrent.controller.transform.right) * handInfoCurrent.controller.transform.rotation * Quaternion.Euler(itemGrabInfoCurrent.grabNode.rotation)) * Quaternion.Inverse(handInfoCurrent.handRigidbody.transform.rotation);
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

	void UpdateItemPhysics(string hand, ItemGrabInformation itemGrabInfoCurrent, ItemGrabInformation itemGrabInfoOpposite, EnvironmentGrabInformation envGrabInfoCurrent, EnvironmentGrabInformation envGrabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		if (itemGrabInfoCurrent.grabbedRigidbody.gameObject.layer == LayerMask.NameToLayer("Item")) {
			Vector3 grabOffsetCurrent = (handInfoCurrent.handRigidbody.transform.rotation * itemGrabInfoCurrent.grabOffset);

			UpdateItemPocketingModel(handInfoCurrent, itemGrabInfoCurrent);

			if (Vector3.Distance(itemGrabInfoCurrent.grabbedRigidbody.transform.position, handInfoCurrent.handRigidbody.transform.position + grabOffsetCurrent) > 0.5f) {
				ReleaseAll(hand, itemGrabInfoCurrent, itemGrabInfoOpposite, envGrabInfoCurrent, envGrabInfoOpposite, handInfoCurrent, handInfoOpposite);
			} else {
				itemGrabInfoCurrent.grabbedRigidbody.velocity = Vector3.Lerp(itemGrabInfoCurrent.grabbedRigidbody.velocity, Vector3.ClampMagnitude(((handInfoCurrent.handRigidbody.position + grabOffsetCurrent) - itemGrabInfoCurrent.grabbedRigidbody.transform.position) / Time.fixedDeltaTime, (itemGrabInfoCurrent.grabbedRigidbody.GetComponent<HingeJoint>()) ? 1 : 500) * itemGrabInfoCurrent.itemVelocityPercentage, Mathf.Clamp01(50 * Time.deltaTime));
				//grabInfoCurrent.grabbedRigidbody.velocity = Vector3.Lerp(grabInfoCurrent.grabbedRigidbody.velocity, Vector3.ClampMagnitude(((handInfoCurrent.handRigidbody.transform.position + (dualWieldDirectionChangeRotation * handDominant.transform.rotation * ((grabDualWieldDominantHand == "Left") ? itemGrabInfoLeft.grabOffset : itemGrabInfoRight.grabOffset))) - grabbedItemDominant.transform.position) / Time.fixedDeltaTime, (grabbedItemDominant.GetComponent<HingeJoint>()) ? 1 : 100) * Mathf.Lerp(itemGrabInfoLeft.itemVelocityPercentage, itemGrabInfoRight.itemVelocityPercentage, 0.5f), Mathf.Clamp01(50 * Time.deltaTime));

				if (!itemGrabInfoCurrent.grabbedRigidbody.GetComponent<HingeJoint>()) {
					Quaternion rotationDeltaItem = (handInfoCurrent.handRigidbody.transform.rotation * itemGrabInfoCurrent.grabRotation) * Quaternion.Inverse(itemGrabInfoCurrent.grabbedRigidbody.transform.rotation);
					float angleItem;
					Vector3 axisItem;
					rotationDeltaItem.ToAngleAxis(out angleItem, out axisItem);
					if (angleItem > 180) {
						angleItem -= 360;
					}

					if (angleItem != float.NaN) {
						itemGrabInfoCurrent.grabbedRigidbody.maxAngularVelocity = Mathf.Infinity;
						itemGrabInfoCurrent.grabbedRigidbody.angularVelocity = Vector3.Lerp(itemGrabInfoCurrent.grabbedRigidbody.angularVelocity, (angleItem * axisItem) * itemGrabInfoCurrent.itemVelocityPercentage * 0.95f, Mathf.Clamp01(50 *  Time.deltaTime));
					}
				}
			}
		}
	}

	void UpdateItemPocketingModel(HandInformation handInfoCurrent, ItemGrabInformation itemGrabInfoCurrent) {
		if (handInfoCurrent.itemReleasingDisabled == false || !handInfoCurrent.controllerDevice.GetPress(Valve.VR.EVRButtonId.k_EButton_Grip)) {
			// If this item is an attachment on another item, set the current item to that parent item
			Item mainItem = itemGrabInfoCurrent.grabbedItem;
			while (mainItem.transform.parent != null && mainItem.transform.parent.name == "(Attachments)") {     // If this is an attachment that is currently attached (repeat until we are no longer in an attached attachment)
				mainItem = mainItem.transform.parent.parent.GetComponent<Item>();
			}

			AttachmentNodePair chosenANP = GetAttachmentConnection(itemGrabInfoCurrent, handInfoCurrent);
			if (chosenANP == null) {        // If the attachment item doesn't currently have a possible attachment
				Collider[] pockets = Physics.OverlapSphere(handInfoCurrent.handRigidbody.transform.position, 0.2f, pocketMask);
				if (pockets.Length > 0) {
					List<Pocket> availablePockets = new List<Pocket>();

					// Find pockets that are currently available and add them to availablePockets list
					for (int i = 0; i < pockets.Length; i++) {
						if (pockets[i].GetComponent<Pocket>()) {
							Pocket currentPocketObject = pockets[i].GetComponent<Pocket>();
							if (currentPocketObject.GetAvailability() == true && currentPocketObject.pocketSize == mainItem.pocketSize && Vector3.Angle(itemGrabInfoCurrent.grabbedRigidbody.transform.forward, currentPocketObject.transform.forward) <= currentPocketObject.angleRange) {
								availablePockets.Add(currentPocketObject);
							}
						}
					}

					if (availablePockets.Count > 0) {
						// Find closest pocket
						Pocket chosenPocket = availablePockets[0];
						float closestPocketDistance = Vector3.Distance(handInfoCurrent.handRigidbody.transform.position, chosenPocket.transform.position);
						for (int j = 1; j < availablePockets.Count; j++) {
							if (Vector3.Distance(handInfoCurrent.handRigidbody.transform.position, availablePockets[j].transform.position) < closestPocketDistance) {
								chosenPocket = availablePockets[j];
							}
						}

						if (itemGrabInfoCurrent.pocketCandidateLastFrame != chosenPocket.transform) {       // If the chosenPocket is different from the one found last frame, trigger haptic feedback
							StartCoroutine(TriggerHapticFeedback(handInfoCurrent.controllerDevice, 1000, 0.05f, 2));
							itemGrabInfoCurrent.pocketCandidateLastFrame = chosenPocket.transform;
						}

						// Set item's pocketCandidate information
						mainItem.pocketCandidate = chosenPocket.transform;
						mainItem.pocketCandidateTime = Time.timeSinceLevelLoad;
						mainItem.pocketingModel.parent = chosenPocket.transform;

						if (mainItem.pocketGrabNode) {
							mainItem.pocketingModel.transform.position = mainItem.pocketCandidate.transform.position - (mainItem.pocketingModel.transform.rotation * mainItem.pocketGrabNode.transform.localPosition);
							mainItem.pocketingModel.transform.rotation = mainItem.pocketCandidate.transform.rotation;
						} else {
							mainItem.pocketingModel.transform.position = mainItem.pocketCandidate.transform.position;
							mainItem.pocketingModel.transform.rotation = mainItem.pocketCandidate.transform.rotation;
						}
					} else {
						itemGrabInfoCurrent.pocketCandidateLastFrame = null;
					}
				} else {
					itemGrabInfoCurrent.pocketCandidateLastFrame = null;
				}
			} else {
				// Pocketing onto attachment slots
				if (itemGrabInfoCurrent.pocketCandidateLastFrame != chosenANP.nodeParent.transform) {   // If the chosenPocket is different from the one found last frame, trigger haptic feedback
					StartCoroutine(TriggerHapticFeedback(handInfoCurrent.controllerDevice, 1000, 0.05f, 2));
					itemGrabInfoCurrent.pocketCandidateLastFrame = chosenANP.nodeParent.transform;
				}

				itemGrabInfoCurrent.grabbedItem.pocketCandidate = chosenANP.nodeParent.transform;
				itemGrabInfoCurrent.grabbedItem.pocketCandidateTime = Time.timeSinceLevelLoad;
				itemGrabInfoCurrent.grabbedItem.pocketingModel.parent = chosenANP.nodeParent.transform;

				itemGrabInfoCurrent.grabbedItem.pocketingModel.parent = chosenANP.nodeParent.transform.parent.parent.Find("(Attachments)");

				Quaternion nodeChildInvertedRotation = Quaternion.AngleAxis(180, chosenANP.nodeParent.transform.right) * chosenANP.nodeParent.transform.rotation;
				Quaternion rotationDelta = Quaternion.Inverse(itemGrabInfoCurrent.grabbedItem.pocketingModel.rotation) * nodeChildInvertedRotation;

				itemGrabInfoCurrent.grabbedItem.pocketingModel.rotation *= rotationDelta * Quaternion.Inverse(chosenANP.nodeChild.transform.localRotation);
				itemGrabInfoCurrent.grabbedItem.pocketingModel.position = chosenANP.nodeParent.transform.position + (itemGrabInfoCurrent.grabbedItem.pocketingModel.rotation * -chosenANP.nodeChild.transform.localPosition);
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