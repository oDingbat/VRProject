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
	public LayerMask bodyCCLayerMask;       // The layerMask for the bodyCharacterController, defines objects which the player's body cannot pass through
	public LayerMask physicsGrabLayerMask;
	public LayerMask itemGrabLayerMask;
	public LayerMask envGrabLayerMask;
	public LayerMask envItemGrabLayerMask;
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
	public GrabInformation itemGrabInfoLeft = new GrabInformation();
	public GrabInformation itemGrabInfoRight = new GrabInformation();
	public GrabInformation potentialItemGrabInfoLeft = new GrabInformation();
	public GrabInformation potentialItemGrabInfoRight = new GrabInformation();

	[Space(10)] [Header("Environment Grab Infos")]
	public GrabInformation envGrabInfoLeft = new GrabInformation();
	public GrabInformation envGrabInfoRight = new GrabInformation();
	public GrabInformation potentialEnvGrabInfoLeft = new GrabInformation();
	public GrabInformation potentialEnvGrabInfoRight = new GrabInformation();

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
	float maxEnvironmentItemGrabDistance = 0.5f;

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

	public Material handMaterial;
	public GameObject debugBall1;
	public GameObject debugBall2;

	public enum GrabType { Null, Climbable, ClimbablePhysics, EnvironmnentItem, Item, Ragdoll }

	// Before redux: 1739 lines

	public class GrabInformation {
		// This class is used for both envGrabInfos and itemGrabInfos
		// Each	variable uses the class in several different ways, as described in the comments
		//		Both:	Indicates both envGrabInfos and itemGrabInfos use that variable in an identical way
		//		Item:	Indicates how the itemGrabInfos use the variable
		//		Env:		Indicates how the envGrabInfos use the variable
		//		Only:	Indicates only that respective variable uses said commented variable

		public string side;									// [Both: Which side hand is this? "Left" or "Right"]
		public GrabType grabType;                           // [Both: The type of object currently grabbed (Null if none is grabbed)]
		public Transform grabbedTransform;                  // [Both: The transform of the grabbed object]
		public Rigidbody grabbedRigidbody;                  // [Both: The rigidbody of the object being currently grabbed]
		public MonoBehaviour grabbedScript;                 // [Both: The grabbedScript component of the object being currently grabbed]
		public GrabNode itemGrabNode;                       // [Item/Ragdoll Only: The itemGrabNode of the object currently grabbed (for item objects)]
		public Quaternion offsetRotation;                   // [Item: The desired rotation of the item relative to the hand's rotation] / [Env: The rotation of the object when first grabbed]
		public Vector3 offsetPosition;                      // [Item: Stores the offset between the hand and the item which when rotated according to rotationOffset is the desired position of the object.] / [Env: Stores offset between hand and environmentOrigin]
		public Vector3 offsetBodyHand;                      // The offset between the player's grabbing hand and body character controller, used to move the player while climbing
		public Vector3 grabPoint;                           // [Item Only: The localPosition on the object where it was grabbed (Takes into account position and rotation of item)]
		public Vector3 grabWorldPos;						// [Item Only: The world position of where the item was grabbed
		public float rigidbodyVelocityPercentage;			// [Item Only: When items are first picked up their IVP = 0; over time grabbed approaches 1 linearly; this value determines the magnitude of the velocity (and angularVelocity) of the item]
		public MonoBehaviour potentialGrabbedScript;        // [Both: The grabbedScript component attached to the object potentially grabbed if the player were to grab]

		public GrabInformation() {
			side = "null";
			grabType = GrabType.Null;
			grabbedTransform = null;
			grabbedRigidbody = null;
			grabbedScript = null;
			itemGrabNode = null;
			offsetRotation = Quaternion.identity;
			offsetPosition = Vector3.zero;
			offsetBodyHand = Vector3.zero;
			grabPoint = Vector3.zero;
			grabWorldPos = Vector3.zero;
			rigidbodyVelocityPercentage = 0;
			potentialGrabbedScript = null;
		}
	}
	
	[System.Serializable]
	public class HandInformation {
		public SteamVR_TrackedObject controller;                // The SteamVR Tracked Object, used to get the position and rotation of the controller
		public SteamVR_Controller.Device controllerDevice;      // The SteamVR Controller Device for controllers; Used to get input
		public GameObject handGameObject;                       // The player's hand GameObject
		public Rigidbody handRigidbody;                         // The rigidbody of the hand
		public Collider handCollider;							// The collider of the hand

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
		QualitySettings.vSyncCount = 0;
		bodyCC = GetComponent<CharacterController>();          // Grab the character controller at the start
		avatar = transform.parent.Find("Avatar");
		avatarTorso = avatar.Find("Torso");
		handInfoLeft.handRigidbody = handInfoLeft.handGameObject.GetComponent<Rigidbody>();
		handInfoRight.handRigidbody = handInfoRight.handGameObject.GetComponent<Rigidbody>();
		audioManager = GameObject.Find("AudioManager").GetComponent<AudioManager>();
		entity = GetComponent<Entity>();
		//headlight = hmd.transform.parent.Find("Headlight").gameObject;

		handInfoLeft.handCollider = handInfoLeft.handGameObject.GetComponent<Collider>();
		handInfoRight.handCollider = handInfoRight.handGameObject.GetComponent<Collider>();

		// Subscribe Events
		entity.eventTakeDamage += TakeDamage;

		StartCoroutine(UpdateVitals());
	}

	void Update() {
		Time.fixedDeltaTime = Time.deltaTime;           // IMPORTANT (Fixes physics/sec hiccup) TODO: Yikes maybe move this? IDK

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

		UpdateHandAndItemPhysics();
		
	}

	void FixedUpdate() {
		//UpdateHandAndItemPhysics();
		
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

	void UpdateControllerInput(string side, GrabInformation itemGrabInfoCurrent, GrabInformation itemGrabInfoOpposite, GrabInformation envGrabInfoCurrent, GrabInformation envGrabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		if (side == "Right") {
			if (handInfoCurrent.controllerDevice.GetPressDown(Valve.VR.EVRButtonId.k_EButton_ApplicationMenu)) {
				//flashlight.SetActive(!flashlight.activeSelf);
			}
		}

		// Grip Down and Held functionality
		if (handInfoCurrent.controllerDevice.GetPress(Valve.VR.EVRButtonId.k_EButton_Grip)) {    // Grip Being Held
			if (handInfoCurrent.controllerDevice.GetPressDown(Valve.VR.EVRButtonId.k_EButton_Grip)) {  // Grip Down
				if (itemGrabInfoCurrent.grabbedRigidbody == true && itemGrabInfoCurrent.grabbedScript && (itemGrabInfoCurrent.grabbedScript is Misc) == false) {
					handInfoCurrent.itemReleasingDisabled = false;
				} else {
					handInfoCurrent.itemReleasingDisabled = true;
				}
				Grab(side, itemGrabInfoCurrent, itemGrabInfoOpposite, envGrabInfoCurrent, envGrabInfoOpposite, handInfoCurrent, handInfoOpposite);

				if (envGrabInfoCurrent.grabbedTransform == null && itemGrabInfoCurrent.grabbedScript == null) { // If we didn't grab any environments or items
					if (Vector3.Distance(handInfoCurrent.controller.transform.position, hmd.transform.position + hmd.transform.forward * -0.15f) < 0.2125f) {
						headlight.SetActive(!headlight.activeSelf);
					}
				}
			}
			if (handInfoCurrent.grabbingDisabled == false) {
				Grab(side, itemGrabInfoCurrent, itemGrabInfoOpposite, envGrabInfoCurrent, envGrabInfoOpposite, handInfoCurrent, handInfoOpposite);
			}
		}

		// Grip Up
		if (handInfoCurrent.controllerDevice.GetPressUp(Valve.VR.EVRButtonId.k_EButton_Grip)) {
			handInfoCurrent.grabbingDisabled = false;
			if (envGrabInfoCurrent.grabbedTransform == true || itemGrabInfoCurrent.grabbedScript == false || ((itemGrabInfoCurrent.grabbedScript && itemGrabInfoCurrent.grabbedScript is Misc) || handInfoCurrent.itemReleasingDisabled == false)) {
				ReleaseAll(side, itemGrabInfoCurrent, itemGrabInfoOpposite, envGrabInfoCurrent, envGrabInfoOpposite, handInfoCurrent, handInfoOpposite);
			}
		}

		// Trigger functionality
		if (itemGrabInfoCurrent.grabbedScript != null && itemGrabInfoCurrent.itemGrabNode != null) {
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
				if (itemGrabInfoCurrent.grabbedScript is Attachment) {        // If this Item is an attachment
					if (itemGrabInfoCurrent.grabbedScript.transform.GetComponent<Rigidbody>() == null) {  // If this attachment does not have a Rigidbody Component
						handInfoCurrent.itemReleasingDisabled = true;
						DetachAttachment(itemGrabInfoCurrent, handInfoCurrent);
					}
				}
			}
		}

		// Handling itemGrabNode interaction (ie: flashlights, lasersights, etc)
		if (itemGrabInfoCurrent.itemGrabNode) {
			if (itemGrabInfoCurrent.itemGrabNode.interactionType == GrabNode.InteractionType.Toggle) {
				if (handInfoCurrent.controllerDevice.GetPressDown(EVRButtonId.k_EButton_ApplicationMenu)) {
					itemGrabInfoCurrent.itemGrabNode.TriggerInteraction(!itemGrabInfoCurrent.itemGrabNode.interactionOn);
					
				}
			} else {
				if (handInfoCurrent.controllerDevice.GetPress(EVRButtonId.k_EButton_ApplicationMenu)) {
					itemGrabInfoCurrent.itemGrabNode.TriggerInteraction(true);
				} else {
					itemGrabInfoCurrent.itemGrabNode.TriggerInteraction(false);
				}
			}
		}

		if (itemGrabInfoCurrent.grabbedScript == true || envGrabInfoCurrent.grabbedRigidbody == true || envGrabInfoCurrent.grabbedTransform == true) {
			handInfoCurrent.handCollider.enabled = false;
		} else {
			Collider[] handHitColliders = Physics.OverlapSphere(handInfoCurrent.handGameObject.transform.position, 0.065f, physicsGrabLayerMask);
			if (handHitColliders.Length == 0) {
				handInfoCurrent.handCollider.enabled = true;
			}
		}
	}

	AttachmentNodePair GetAttachmentConnection (GrabInformation itemGrabInfoCurrent, HandInformation handInfoCurrent) {
		// This method Checks whether an item 'itemGrabInfoCurrent' can currently be attached to any other items
		// It will then return an ATtachmentNodePair object with the attachmentNode of the held item and the attachmentNode of a item it will attach to

		AttachmentNodePair chosenANP = null;

		Transform attachmentNodesContainer = itemGrabInfoCurrent.grabbedScript.transform.Find("(AttachmentNodes)");   // The attachmentNode container on this item which contains all attachmentNodes this item has
		if (itemGrabInfoCurrent.grabbedScript is Attachment && attachmentNodesContainer != null) {        // 1: Is this item an attachment? 2: Does this item contain attachmentNodes
			List<AttachmentNodePair> possibleAttachmentNodePairs = new List<AttachmentNodePair>();		// List containing all possible pairs of attachmentNodes

			// Find all possible matching attachmentNodes
			foreach (Transform heldNodeTransform in attachmentNodesContainer) {      // For each attachmentNode this item has, test to see if it can connect with any attachmentNodes on other items
				AttachmentNode heldNode = heldNodeTransform.GetComponent<AttachmentNode>();
				if (heldNode.isAttached == false && heldNode.attachmentGender == AttachmentNode.AttachmentGender.Male) {        // Make sure this node isn't already attached to something and only check the male attachmentNodes
					Collider[] hitNodes = Physics.OverlapSphere(heldNode.transform.position, 0.1f, attachmentNodeMask);

					foreach (Collider hitNodeCollider in hitNodes) {
						if (hitNodeCollider.transform.parent.parent != itemGrabInfoCurrent.grabbedScript.transform) {        // Make sure the hit attachmentNode is not a child of this item
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

	void InitiateAttachment (AttachmentNodePair attachmentNodePair, GrabInformation itemGrabInfoCurrent) {
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

			// Update itemGrabNode information
			if (attachmentNodePair.nodeParent.associatedGrabNode && attachmentNodePair.nodeChild.associatedGrabNode) {
				attachmentNodePair.nodeParent.associatedGrabNode.grabNodeChildren.Add(attachmentNodePair.nodeChild.associatedGrabNode);
				attachmentNodePair.nodeChild.associatedGrabNode.grabNodeParent = attachmentNodePair.nodeParent.associatedGrabNode;
			}

			// If nodeParent's item is a Weapon, update its combined weaponAttributes
			if (attachmentNodePair.nodeParent.item is Weapon) {
				(attachmentNodePair.nodeParent.item as Weapon).UpdateCombinedAttributes();
			}

			if (attachmentNodePair.nodeChild.item is Attachment) {
				(attachmentNodePair.nodeChild.item as Attachment).timeAttached = Time.timeSinceLevelLoad;
			}

			itemGrabInfoCurrent.grabbedScript.transform.parent = attachmentNodePair.nodeParent.transform.parent.parent.Find("(Attachments)");

			Quaternion nodeChildInvertedRotation = Quaternion.AngleAxis(180, attachmentNodePair.nodeParent.transform.right) * attachmentNodePair.nodeParent.transform.rotation;
			Quaternion rotationDelta = Quaternion.Inverse(itemGrabInfoCurrent.grabbedScript.transform.rotation) * nodeChildInvertedRotation;

			//itemGrabInfoCurrent.grabbedScript.transform.rotation *= rotationDelta * Quaternion.Inverse(attachmentNodePair.nodeChild.transform.localRotation);
			//itemGrabInfoCurrent.grabbedScript.transform.position = attachmentNodePair.nodeParent.transform.position + (itemGrabInfoCurrent.grabbedScript.transform.rotation * rotationDelta * Quaternion.Inverse(attachmentNodePair.nodeChild.transform.localRotation) * -attachmentNodePair.nodeChild.transform.localPosition);
			Attachment grabbedAttachment = itemGrabInfoCurrent.grabbedScript as Attachment;
			grabbedAttachment.desiredRotation = Quaternion.Inverse(itemGrabInfoCurrent.grabbedScript.transform.parent.rotation) * itemGrabInfoCurrent.grabbedScript.transform.rotation * rotationDelta * Quaternion.Inverse(attachmentNodePair.nodeChild.transform.localRotation);
			grabbedAttachment.desiredPosition = Quaternion.Inverse(itemGrabInfoCurrent.grabbedScript.transform.parent.rotation) * ((attachmentNodePair.nodeParent.transform.position + (itemGrabInfoCurrent.grabbedScript.transform.rotation * rotationDelta * Quaternion.Inverse(attachmentNodePair.nodeChild.transform.localRotation) * -attachmentNodePair.nodeChild.transform.localPosition)) - itemGrabInfoCurrent.grabbedScript.transform.parent.position); // TODO: clean this ugly ass calculation

			(itemGrabInfoCurrent.grabbedScript as Item).rigidbodyCopy = new RigidbodyCopy(itemGrabInfoCurrent.grabbedRigidbody);	// TODO: Sloppy 'as' use
			Destroy(itemGrabInfoCurrent.grabbedRigidbody);
		}
	}

	void DetachAttachment (GrabInformation itemGrabInfoCurrent, HandInformation handInfoCurrent) {
		// This method is used to fully detach any parent items this item is attached to. It does not detach items that are children of it, it only detaches itself from it's parent
		Transform attachmentNodesContainer = itemGrabInfoCurrent.grabbedScript.transform.Find("(AttachmentNodes)");
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

					// Update itemGrabNode information
					if (nodeObject.associatedGrabNode && nodeObject.associatedGrabNode.grabNodeParent) {
						nodeObject.associatedGrabNode.grabNodeParent.grabNodeChildren.Remove(nodeObject.associatedGrabNode);
						nodeObject.associatedGrabNode.grabNodeParent = null;
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
			itemGrabInfoCurrent.grabbedScript.transform.parent = ItemManager;

			// Reapply rigidbody
			itemGrabInfoCurrent.grabbedScript.gameObject.AddComponent<Rigidbody>();
			Rigidbody newRigidbody = itemGrabInfoCurrent.grabbedScript.gameObject.GetComponent<Rigidbody>();

			// Copy RigidbodyCopy's values over to the newly created rigidbody
			RigidbodyCopy.SetRigidbodyValues(newRigidbody, (itemGrabInfoCurrent.grabbedScript as Item).rigidbodyCopy);

			(itemGrabInfoCurrent.grabbedScript as Item).itemRigidbody = newRigidbody;

			// Reset grab information
			itemGrabInfoCurrent.grabbedRigidbody = newRigidbody;

			if (itemGrabInfoCurrent.itemGrabNode) {
				if (itemGrabInfoCurrent.itemGrabNode.grabType == GrabNode.GrabType.FixedPositionRotation) {
					itemGrabInfoCurrent.offsetPosition = Quaternion.Euler(itemGrabInfoCurrent.itemGrabNode.rotation) * ((Quaternion.Inverse(itemGrabInfoCurrent.grabbedScript.transform.rotation) * -(itemGrabInfoCurrent.itemGrabNode.transform.position - itemGrabInfoCurrent.grabbedScript.transform.position)) - itemGrabInfoCurrent.itemGrabNode.offset);
					itemGrabInfoCurrent.offsetRotation = Quaternion.Euler(itemGrabInfoCurrent.itemGrabNode.rotation);
					itemGrabInfoCurrent.grabPoint = itemGrabInfoCurrent.itemGrabNode.transform.position + (itemGrabInfoCurrent.grabbedRigidbody.transform.rotation * itemGrabInfoCurrent.itemGrabNode.offset);
				} else if (itemGrabInfoCurrent.itemGrabNode.grabType == GrabNode.GrabType.FixedPosition) {
					itemGrabInfoCurrent.offsetPosition = Quaternion.Euler(itemGrabInfoCurrent.itemGrabNode.rotation) * (-itemGrabInfoCurrent.itemGrabNode.transform.localPosition - itemGrabInfoCurrent.itemGrabNode.offset);
					itemGrabInfoCurrent.offsetRotation = Quaternion.Inverse(handInfoCurrent.controller.transform.rotation) * itemGrabInfoCurrent.grabbedRigidbody.transform.rotation;
					itemGrabInfoCurrent.grabPoint = itemGrabInfoCurrent.itemGrabNode.transform.position + (itemGrabInfoCurrent.grabbedRigidbody.transform.rotation * itemGrabInfoCurrent.itemGrabNode.offset);
				} else if (itemGrabInfoCurrent.itemGrabNode.grabType == GrabNode.GrabType.Dynamic || itemGrabInfoCurrent.itemGrabNode.grabType == GrabNode.GrabType.Referral) {
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

	void Grab(string side, GrabInformation itemGrabInfoCurrent, GrabInformation itemGrabInfoOpposite, GrabInformation envGrabInfoCurrent, GrabInformation envGrabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		// This method handles deciding which type of grab to do, either an itemGrab or an environmentGrab
		if (envGrabInfoCurrent.grabbedTransform == false && envGrabInfoOpposite.grabbedTransform == false) {      // Are we currently climbing?
			if (itemGrabInfoCurrent.grabbedRigidbody == null) {      // Are we currently not holding an item
				GrabItem(side, itemGrabInfoCurrent, itemGrabInfoOpposite, handInfoCurrent, handInfoOpposite);
			}
			if (envGrabInfoCurrent.grabbedTransform == null) {      // Are we currently not climbing something
				GrabEnvironment(side, envGrabInfoCurrent, envGrabInfoOpposite, handInfoCurrent, handInfoOpposite);
			}
		} else {
			if (envGrabInfoCurrent.grabbedTransform == null) {      // Are we currently not climbing something
				GrabEnvironment(side, envGrabInfoCurrent, envGrabInfoOpposite, handInfoCurrent, handInfoOpposite);
			}
			if (itemGrabInfoCurrent.grabbedRigidbody == null) {      // Are we currently not holding an item
				GrabItem(side, itemGrabInfoCurrent, itemGrabInfoOpposite, handInfoCurrent, handInfoOpposite);
			}
		}
	}

	void GrabItem (string side, GrabInformation itemGrabInfoCurrent, GrabInformation itemGrabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		// This method handles grabbing items and ragdolls

		if (handInfoCurrent.grabbingDisabled == false) {	// Is grabbing currently disabled on this hand?

			Vector3 originPosition = handInfoCurrent.handGameObject.transform.position;						// Origin of the current hand
			Collider[] itemColliders = Physics.OverlapSphere(originPosition, 0.2f, itemGrabLayerMask);		// Colliders found in a spherecast around the current hand
			List<ItemAndNode> itemAndNodes = new List<ItemAndNode>();

			// Search through all hit colliders to find possible items & nodes
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
						GrabNode nodeCurrent = hitItem.GetComponent<GrabNode>();        // nodeCurrent will be set to the itemGrabNode that was hit, unless there is no node, which will set it to null
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
					// Check to see if nodeCurrent is attached to another itemGrabNode with higher dominance
					if (chosenIAN.node) {
						if (chosenIAN.node.grabThisNodeFirst == false) {
							if (chosenIAN.node.grabNodeParent != null && chosenIAN.node.grabNodeParent.grabThisNodeFirst == true) { // If this node is attached to a parent, and it has nodeFirst, make that node the currentNode
								chosenIAN.node = chosenIAN.node.grabNodeParent;
								chosenIAN.item = chosenIAN.node.transform.parent.parent.GetComponent<Item>();
							} else {
								if (chosenIAN.node.grabNodeChildren.Count > 0) {
									GrabNode childNodeCurrent = chosenIAN.node;
									float closestChildNodeDistance = Mathf.Infinity;
									for (int i = 0; i < chosenIAN.node.grabNodeChildren.Count; i++) {
										float currentChildNodeDistance = Vector3.Distance(originPosition, chosenIAN.node.grabNodeChildren[i].transform.position);
										if (currentChildNodeDistance < 0.2f && currentChildNodeDistance < closestChildNodeDistance) {
											childNodeCurrent = chosenIAN.node.grabNodeChildren[i];
											closestChildNodeDistance = currentChildNodeDistance;
										}
									}
									chosenIAN.node = childNodeCurrent;
									chosenIAN.item = childNodeCurrent.transform.parent.parent.GetComponent<Item>();
								}
							}
						}
					}

					itemGrabInfoCurrent.grabbedScript = chosenIAN.item;
					(itemGrabInfoCurrent.grabbedScript as Item).isGrabbed = true;
					(itemGrabInfoCurrent.grabbedScript as Item).timeLastGrabbed = Time.timeSinceLevelLoad;
					itemGrabInfoCurrent.itemGrabNode = chosenIAN.node;

					// Set correct rigidbody
					if (itemGrabInfoCurrent.grabbedScript.transform.GetComponent<Rigidbody>() == null) {

						// If this item is an attachment
						if (itemGrabInfoCurrent.grabbedScript.transform.parent.name == "(Attachments)") {
							Transform grabbedItemParentItem = itemGrabInfoCurrent.grabbedScript.transform.parent.parent;
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
						
					} else {
						itemGrabInfoCurrent.grabbedRigidbody = chosenIAN.item.transform.GetComponent<Rigidbody>();
					}
					
					if ((itemGrabInfoCurrent.grabbedScript as Item).pocketCurrent != null) {
						(itemGrabInfoCurrent.grabbedScript as Item).pocketCurrent.ReleaseItem();
						(itemGrabInfoCurrent.grabbedScript as Item).pocketCurrent = null;
					}

					itemGrabInfoCurrent.grabbedRigidbody.useGravity = false;
					if (itemGrabInfoCurrent.grabbedRigidbody.transform.GetComponent<Item>() is Weapon) {
						itemGrabInfoCurrent.grabbedRigidbody.transform.GetComponent<Weapon>().UpdateCombinedAttributes();
					}

					if (itemGrabInfoCurrent.itemGrabNode) {
						if (itemGrabInfoCurrent.itemGrabNode.grabType == GrabNode.GrabType.FixedPositionRotation) {
							Vector3 attachmentOffset = Vector3.zero;
							if (itemGrabInfoCurrent.grabbedScript.transform.parent && itemGrabInfoCurrent.grabbedScript.transform.parent.name == "(Attachments)") {
								attachmentOffset = itemGrabInfoCurrent.grabbedScript.transform.localPosition;
							}
							itemGrabInfoCurrent.offsetPosition = Quaternion.Euler(itemGrabInfoCurrent.itemGrabNode.rotation) * -(attachmentOffset + itemGrabInfoCurrent.itemGrabNode.transform.localPosition + itemGrabInfoCurrent.itemGrabNode.offset);
							itemGrabInfoCurrent.offsetRotation = Quaternion.Euler(itemGrabInfoCurrent.itemGrabNode.rotation);
							itemGrabInfoCurrent.grabPoint = itemGrabInfoCurrent.itemGrabNode.transform.localPosition + itemGrabInfoCurrent.itemGrabNode.offset;
						} else if (itemGrabInfoCurrent.itemGrabNode.grabType == GrabNode.GrabType.FixedPosition) {
							itemGrabInfoCurrent.offsetPosition = Quaternion.Euler(itemGrabInfoCurrent.itemGrabNode.rotation) * (-itemGrabInfoCurrent.itemGrabNode.transform.localPosition - itemGrabInfoCurrent.itemGrabNode.offset);
							itemGrabInfoCurrent.offsetRotation = Quaternion.Inverse(handInfoCurrent.controller.transform.rotation) * itemGrabInfoCurrent.grabbedRigidbody.transform.rotation;
							itemGrabInfoCurrent.grabPoint = itemGrabInfoCurrent.itemGrabNode.transform.localPosition + itemGrabInfoCurrent.itemGrabNode.offset;
						} else if (itemGrabInfoCurrent.itemGrabNode.grabType == GrabNode.GrabType.Dynamic || itemGrabInfoCurrent.itemGrabNode.grabType == GrabNode.GrabType.Referral) {
							// Here
							GetDynamicItemGrabInfo(ref itemGrabInfoCurrent, ref handInfoCurrent);
						}
					} else {
						GetDynamicItemGrabInfo(ref itemGrabInfoCurrent, ref handInfoCurrent);
					}

					// Set grabbed Item centerOfMass
					if (itemGrabInfoCurrent.grabbedScript) {
						if (itemGrabInfoCurrent.grabbedScript != itemGrabInfoOpposite.grabbedScript) {
							itemGrabInfoCurrent.grabbedRigidbody.centerOfMass = itemGrabInfoCurrent.grabPoint;
						} else {
							itemGrabInfoCurrent.grabbedRigidbody.centerOfMass = (itemGrabInfoCurrent.grabPoint + itemGrabInfoOpposite.grabPoint) / 2;
						}
					}

					if (itemGrabInfoCurrent.grabbedRigidbody == itemGrabInfoOpposite.grabbedRigidbody) {      // Is the other hand already holding this item?
						itemGrabInfoCurrent.rigidbodyVelocityPercentage = 0;
						itemGrabInfoOpposite.rigidbodyVelocityPercentage = 0;
						if (itemGrabInfoCurrent.itemGrabNode && itemGrabInfoOpposite.itemGrabNode) {
							if (itemGrabInfoCurrent.itemGrabNode.dominance > itemGrabInfoOpposite.itemGrabNode.dominance) {
								grabDualWieldDominantHand = side;
								grabDualWieldDirection = Quaternion.Euler(itemGrabInfoCurrent.itemGrabNode.rotation) * Quaternion.Inverse(itemGrabInfoCurrent.grabbedRigidbody.transform.rotation) * (GetGrabWorldPosition(itemGrabInfoOpposite) - GetGrabWorldPosition(itemGrabInfoCurrent));
							} else if (itemGrabInfoCurrent.itemGrabNode.dominance <= itemGrabInfoOpposite.itemGrabNode.dominance) {
								grabDualWieldDominantHand = (side == "Right" ? "Left" : "Right");
								if (itemGrabInfoCurrent.grabWorldPos != Vector3.zero) {
									grabDualWieldDirection = Quaternion.Inverse(handInfoOpposite.handRigidbody.transform.rotation) * (itemGrabInfoCurrent.grabWorldPos - handInfoOpposite.handRigidbody.transform.position);
								} else {
									grabDualWieldDirection = Quaternion.Inverse(handInfoOpposite.handRigidbody.transform.rotation) * (handInfoCurrent.handRigidbody.transform.position - handInfoOpposite.handRigidbody.transform.position);
								}
							}
						} else {
							grabDualWieldDominantHand = (side == "Right" ? "Left" : "Right");
							if (itemGrabInfoCurrent.grabWorldPos != Vector3.zero) {
								grabDualWieldDirection = Quaternion.Inverse(handInfoOpposite.handRigidbody.transform.rotation) * (itemGrabInfoCurrent.grabWorldPos - handInfoOpposite.handRigidbody.transform.position);
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

	Vector3 GetGrabWorldPosition (GrabInformation itemGrabInfoCurrent) {
		Vector3 grabWorldPos = Vector3.zero;

		if (itemGrabInfoCurrent != null) {
			grabWorldPos = itemGrabInfoCurrent.grabbedRigidbody.transform.position + itemGrabInfoCurrent.grabbedRigidbody.transform.rotation * (Quaternion.Inverse(itemGrabInfoCurrent.offsetRotation) * -itemGrabInfoCurrent.offsetPosition);
		}
		return grabWorldPos;
	}

	void GrabEnvironment(string side, GrabInformation envGrabInfoCurrent, GrabInformation envGrabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		if (handInfoCurrent.grabbingDisabled == false) {
			if (envGrabInfoCurrent.grabbedRigidbody == false && envGrabInfoCurrent.grabbedTransform == false) {
				Vector3 originPosition = handInfoCurrent.handGameObject.transform.position;

				// For grabbing environmentItems
				Collider[] envItemColliders = Physics.OverlapSphere(originPosition, 0.1f, envItemGrabLayerMask);
				foreach (Collider hitItem in envItemColliders) {
					if (hitItem.transform.gameObject.layer == LayerMask.NameToLayer("EnvironmentItem")) {
						if (hitItem.transform.GetComponent<Rigidbody>()) {
							envGrabInfoCurrent.offsetPosition = Quaternion.Inverse(hitItem.transform.rotation) * (originPosition - hitItem.transform.position);
							envGrabInfoCurrent.grabbedRigidbody = hitItem.transform.GetComponent<Rigidbody>();
							if (hitItem.transform.GetComponent<EnvironmentItem>()) {
								envGrabInfoCurrent.grabbedScript = hitItem.transform.GetComponent<EnvironmentItem>();
								(envGrabInfoCurrent.grabbedScript as EnvironmentItem).OnGrab(this, side);
							}
							return;
						}
					} else if (hitItem.transform.gameObject.layer == LayerMask.NameToLayer("ProjectileSticky")) {
						if (hitItem.transform.parent && hitItem.transform.parent.parent.parent.name == "[StickyProjectiles]") {
							Debug.Log("yes2");
							Rigidbody hitItemRigidbody = hitItem.attachedRigidbody;
							if (hitItemRigidbody) {
								Debug.Log("yes3");
								envGrabInfoCurrent.offsetPosition = Quaternion.Inverse(hitItemRigidbody.transform.rotation) * (originPosition - hitItemRigidbody.transform.position);
								envGrabInfoCurrent.grabbedRigidbody = hitItemRigidbody;
								if (hitItemRigidbody.transform.GetComponent<EnvironmentItem>()) {
									envGrabInfoCurrent.grabbedScript = hitItemRigidbody.transform.GetComponent<EnvironmentItem>();
									(envGrabInfoCurrent.grabbedScript as EnvironmentItem).OnGrab(this, side);
								}
								return;
							}
						}
					}
				}

				// For grabbing Climbables
				Collider[] climbColliders = Physics.OverlapSphere(originPosition, 0.065f, envGrabLayerMask);
				foreach (Collider hitClimb in climbColliders) {
					if (hitClimb.transform.gameObject.layer == LayerMask.NameToLayer("Climbable") || hitClimb.transform.gameObject.layer == LayerMask.NameToLayer("ClimbablePhysics") || hitClimb.transform.gameObject.layer == LayerMask.NameToLayer("VehicleClimbable") || (hitClimb.transform.gameObject.layer == LayerMask.NameToLayer("Environment") && (hmd.transform.position.y - rig.transform.position.y < heightCutoffCrouching))) {
						envGrabInfoCurrent.offsetPosition = bodyCC.transform.position - hitClimb.transform.position;
						envGrabInfoCurrent.offsetRotation = hitClimb.transform.rotation;
						envGrabInfoCurrent.offsetBodyHand = handInfoCurrent.controller.transform.position - bodyCC.transform.position;
						envGrabInfoCurrent.grabbedTransform = hitClimb.transform;
						envGrabInfoCurrent.grabPoint = Quaternion.Inverse(envGrabInfoCurrent.grabbedTransform.rotation) * (hitClimb.ClosestPoint(originPosition) - envGrabInfoCurrent.grabbedTransform.position);
						StartCoroutine(TriggerHapticFeedback(handInfoCurrent.controllerDevice, 3999, 0.1f));
						handInfoCurrent.grabbingDisabled = true;

						Rigidbody hitClimbRigidbody = hitClimb.GetComponent<Rigidbody>();
						if (hitClimbRigidbody != null) {
							envGrabInfoCurrent.grabType = GrabType.ClimbablePhysics;
							envGrabInfoCurrent.grabbedRigidbody = hitClimbRigidbody;
							envGrabInfoCurrent.grabbedRigidbody.mass *= 10f;
							if (envGrabInfoOpposite.grabbedTransform == false || (envGrabInfoOpposite.grabbedTransform == true && envGrabInfoCurrent.grabbedTransform.parent != envGrabInfoOpposite.grabbedTransform.parent)) {
								envGrabInfoCurrent.grabbedRigidbody.AddForceAtPosition(velocityCurrent * 2500, hitClimb.ClosestPoint(originPosition));
							}
						}

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
								envGrabInfoCurrent.offsetPosition = bodyCC.transform.position - environmentHit.transform.position;
								envGrabInfoCurrent.offsetRotation = environmentHit.transform.rotation;
								envGrabInfoCurrent.offsetBodyHand = handInfoCurrent.controller.transform.position - bodyCC.transform.position;
								envGrabInfoCurrent.grabbedTransform = environmentHit.transform;
								StartCoroutine(TriggerHapticFeedback(handInfoCurrent.controllerDevice, 3999, 0.1f));
								handInfoCurrent.grabbingDisabled = true;
								return;
							}
						}
					}
				}
			}
		}
	}

	void GetDynamicItemGrabInfo (ref GrabInformation itemGrabInfoCurrent, ref HandInformation handInfoCurrent) {
		// The purpose of this method is to make items grabbed by dual weilding and items that do not have grabNodes be grabbed on their surfaces rather than being suspended away from the hands.

		Vector3 closestColliderPoint = Vector3.zero;
		if (itemGrabInfoCurrent.itemGrabNode == true) {
			closestColliderPoint = itemGrabInfoCurrent.itemGrabNode.transform.GetComponent<Collider>().ClosestPoint(handInfoCurrent.handRigidbody.transform.position);
		} else {
			if (itemGrabInfoCurrent.grabbedScript.transform.GetComponent<Collider>() == null) {

			}
			closestColliderPoint = itemGrabInfoCurrent.grabbedScript.transform.GetComponent<Collider>().ClosestPoint(handInfoCurrent.handRigidbody.transform.position);
		}

		itemGrabInfoCurrent.grabWorldPos = closestColliderPoint;
		itemGrabInfoCurrent.offsetPosition = Quaternion.Inverse(handInfoCurrent.handGameObject.transform.rotation) * Quaternion.AngleAxis(0, handInfoCurrent.controller.transform.right) * (itemGrabInfoCurrent.grabbedRigidbody.transform.position - itemGrabInfoCurrent.grabWorldPos);
		itemGrabInfoCurrent.offsetRotation = Quaternion.Inverse(handInfoCurrent.handGameObject.transform.rotation) * Quaternion.AngleAxis(0, handInfoCurrent.controller.transform.right) * itemGrabInfoCurrent.grabbedRigidbody.transform.rotation;
		itemGrabInfoCurrent.grabPoint = Quaternion.Inverse(itemGrabInfoCurrent.grabbedRigidbody.transform.rotation) * (itemGrabInfoCurrent.grabWorldPos - itemGrabInfoCurrent.grabbedRigidbody.transform.position);

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

	void ReleaseAll(string side, GrabInformation itemGrabInfoCurrent, GrabInformation itemGrabInfoOpposite, GrabInformation envGrabInfoCurrent, GrabInformation envGrabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		if (envGrabInfoCurrent.grabbedTransform == true || envGrabInfoCurrent.grabbedRigidbody == true) {      // Is the current hand currently grabbing a climbable object?
			// Release: EnvironmentGrab Object
			ReleaseEnvironment(side, envGrabInfoCurrent, envGrabInfoOpposite, handInfoCurrent, handInfoOpposite);
		} else {
			// Release: ItemGrab Object
			ReleaseItem(side, itemGrabInfoCurrent, itemGrabInfoOpposite, handInfoCurrent, handInfoOpposite);
		}
	}

	void ReleaseEnvironment (string side, GrabInformation envGrabInfoCurrent, GrabInformation envGrabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		// Release: EnvironmentGrab Object
		
		if (envGrabInfoCurrent.grabbedScript != null && envGrabInfoCurrent.grabbedScript is HingeEnvironmentItem) {
			(envGrabInfoCurrent.grabbedScript as HingeEnvironmentItem).OnRelease();				// TODO: Sloppy
		}

		// Apply velocity
		if (envGrabInfoOpposite.grabbedTransform == null) {
			velocityCurrent = Vector3.ClampMagnitude(velocityCurrent, (envGrabInfoCurrent.grabbedRigidbody ? Mathf.Clamp(envGrabInfoCurrent.grabbedRigidbody.velocity.magnitude + 5f, 5, 100) : 5f));
		}

		if (envGrabInfoCurrent.grabbedRigidbody == true) {
			envGrabInfoCurrent.grabbedRigidbody.mass *= 0.1f;
			envGrabInfoCurrent.grabbedRigidbody.ResetCenterOfMass();
			if (envGrabInfoOpposite.grabbedTransform == false || (envGrabInfoOpposite.grabbedTransform == true && envGrabInfoCurrent.grabbedTransform.parent != envGrabInfoOpposite.grabbedTransform.parent)) {
				envGrabInfoCurrent.grabbedRigidbody.AddForceAtPosition(-velocityCurrent * 2500, envGrabInfoCurrent.grabbedTransform.position + (envGrabInfoCurrent.grabbedTransform.rotation * envGrabInfoCurrent.grabPoint));
			}
		}

		envGrabInfoCurrent.offsetPosition = Vector3.zero;
		envGrabInfoCurrent.offsetBodyHand = Vector3.zero;
		envGrabInfoCurrent.grabPoint = Vector3.zero;
		envGrabInfoCurrent.grabbedTransform = null;
		envGrabInfoCurrent.grabbedRigidbody = null;
		envGrabInfoCurrent.grabbedScript = null;
		envGrabInfoCurrent.grabType = GrabType.Null;
		
		
	}

	void ReleaseItem (string side, GrabInformation itemGrabInfoCurrent, GrabInformation itemGrabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		// Release: ItemGrab Object
		if (itemGrabInfoCurrent.grabbedRigidbody == true) {
			itemGrabInfoCurrent.rigidbodyVelocityPercentage = 0.25f;
			itemGrabInfoOpposite.rigidbodyVelocityPercentage = 0.25f;

			if (itemGrabInfoOpposite.grabbedRigidbody != itemGrabInfoCurrent.grabbedRigidbody) {
				AttachmentNodePair attachmentNodePairInfo = GetAttachmentConnection(itemGrabInfoCurrent, handInfoCurrent);		// Get possible attachmentNodePair for current item (null if no pairs are available)
				if (attachmentNodePairInfo == null) {        // If there are no possible AttachmentNodePairs for this item
					ThrowItem(handInfoCurrent, itemGrabInfoCurrent, (handInfoCurrent.controller.transform.position - handInfoCurrent.handPosLastFrame) / Time.deltaTime);
				} else {
					InitiateAttachment(attachmentNodePairInfo, itemGrabInfoCurrent);
				}
			} else {
				// For items without grabNodes:
				if (itemGrabInfoOpposite.itemGrabNode == null) {
					if (grabDualWieldDominantHand != itemGrabInfoCurrent.side) {		// If the opposite hand is the dominant hand
						GetDynamicItemGrabInfo(ref itemGrabInfoOpposite, ref handInfoOpposite);
					} else {
						itemGrabInfoOpposite.offsetPosition = Quaternion.Inverse(handInfoOpposite.handGameObject.transform.rotation) * (itemGrabInfoOpposite.grabbedRigidbody.transform.position - handInfoOpposite.handGameObject.transform.position);
						itemGrabInfoOpposite.offsetRotation = Quaternion.Inverse(handInfoOpposite.handGameObject.transform.rotation) * itemGrabInfoOpposite.grabbedRigidbody.transform.rotation;
					}
				} else {
					if (itemGrabInfoOpposite.itemGrabNode.grabType == GrabNode.GrabType.Dynamic) {
						GetDynamicItemGrabInfo(ref itemGrabInfoOpposite, ref handInfoOpposite);
						//itemGrabInfoOpposite.offsetPosition = Quaternion.Inverse(handInfoOpposite.handGameObject.transform.rotation) * (itemGrabInfoOpposite.grabbedRigidbody.transform.position - handInfoOpposite.handGameObject.transform.position);
						//itemGrabInfoOpposite.offsetRotation = Quaternion.Inverse(handInfoOpposite.handGameObject.transform.rotation) * itemGrabInfoOpposite.grabbedRigidbody.transform.rotation;
					}
				}
			}

			// If theres a weapon, update it's combined attributes
			(itemGrabInfoCurrent.grabbedScript as Item).isGrabbed = false;
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
		itemGrabInfoCurrent.grabbedScript = null;
		itemGrabInfoCurrent.itemGrabNode = null;
		itemGrabInfoCurrent.grabPoint = Vector3.zero;
	}

	void TriggerDown(string side, GrabInformation itemGrabInfoCurrent, GrabInformation envGrabInfoCurrent, HandInformation handInfoCurrent) {
		if (itemGrabInfoCurrent.grabbedScript is Weapon) {
			Weapon currentWeapon = itemGrabInfoCurrent.grabbedScript as Weapon;
			currentWeapon.timeLastTriggered = Time.timeSinceLevelLoad;
			currentWeapon.AdjustAmmo(0);
			if (itemGrabInfoCurrent.itemGrabNode.triggerType == GrabNode.TriggerType.Fire) {
				if (currentWeapon.combinedAttributes.chargingEnabled == false) {
					AttemptToFireWeapon(side, (itemGrabInfoCurrent.grabbedScript as Weapon), handInfoCurrent);
				}
			}
		}
	}

	void TriggerHold(string side, GrabInformation itemGrabInfoCurrent, GrabInformation envGrabInfoCurrent, HandInformation handInfoCurrent) {
		if (itemGrabInfoCurrent.grabbedScript is Weapon) {
			Weapon currentWeapon = itemGrabInfoCurrent.grabbedScript as Weapon;
			currentWeapon.triggerHeld = true;
			currentWeapon.timeLastTriggered = Time.timeSinceLevelLoad;
			if (itemGrabInfoCurrent.itemGrabNode.triggerType == GrabNode.TriggerType.Fire) {
				if (currentWeapon.combinedAttributes.automatic == true) {
					AttemptToFireWeapon(side, currentWeapon, handInfoCurrent);
				}
				if (currentWeapon.combinedAttributes.chargingEnabled == true) {
					currentWeapon.chargeCurrent = Mathf.Clamp01(currentWeapon.chargeCurrent + (currentWeapon.combinedAttributes.chargeIncrement * Time.deltaTime));
				}
			}
		}
	}

	void TriggerUp(string side, GrabInformation itemGrabInfoCurrent, GrabInformation envGrabInfoCurrent, HandInformation handInfoCurrent) {
		if (itemGrabInfoCurrent.grabbedScript is Weapon) {
			Weapon currentWeapon = itemGrabInfoCurrent.grabbedScript as Weapon;
			currentWeapon.triggerHeld = false;
			if (itemGrabInfoCurrent.itemGrabNode.triggerType == GrabNode.TriggerType.Fire) {
				if (currentWeapon.combinedAttributes.chargingEnabled == true) {
					AttemptToFireWeapon(side, (itemGrabInfoCurrent.grabbedScript as Weapon), handInfoCurrent);
				}
			}
		}
	}

	void TriggerNull(string side, GrabInformation itemGrabInfoCurrent, GrabInformation envGrabInfoCurrent) {
		if (itemGrabInfoCurrent.grabbedScript is Weapon) {
			Weapon currentWeapon = itemGrabInfoCurrent.grabbedScript as Weapon;
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
					StartCoroutine(TriggerHapticFeedback(handInfoLeft.controllerDevice, 3999, 0.02f));
					StartCoroutine(TriggerHapticFeedback(handInfoRight.controllerDevice, 3999, 0.02f));
				} else {
					StartCoroutine(TriggerHapticFeedback((hand == "Left" ? handInfoLeft.controllerDevice : handInfoRight.controllerDevice), 3999, 0.02f));
				}

				// Step 2: Apply velocity and angular velocity to weapon
				if (itemGrabInfoLeft.grabbedScript == itemGrabInfoRight.grabbedScript) {
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

				for (int j = 0; (j < currentWeapon.combinedAttributes.projectileSpreadAttributes.spreads.Length || (currentWeapon.combinedAttributes.projectileSpreadAttributes.spreads.Length == 0 && j == 0)); j++) {
					currentWeapon.muzzleFlash.gameObject.SetActive(true);

					// Step 5: Get random spread deviations
					Quaternion projectileSpreadDeviation = Quaternion.Euler(Random.Range(-currentWeapon.combinedAttributes.projectileSpreadAttributes.spreadDeviation, currentWeapon.combinedAttributes.projectileSpreadAttributes.spreadDeviation), Random.Range(-currentWeapon.combinedAttributes.projectileSpreadAttributes.spreadDeviation, currentWeapon.combinedAttributes.projectileSpreadAttributes.spreadDeviation), 0);

					// Step 4: Create new projectile
					GameObject newProjectile = (GameObject)Instantiate(currentWeapon.combinedAttributes.projectileAttributes.prefabProjectile, currentWeapon.barrelPoint.position + currentWeapon.barrelPoint.forward * 0.01f, currentItem.transform.rotation * randomAccuracy);
					if (currentWeapon.combinedAttributes.projectileSpreadAttributes.spreads.Length > 0) {
						if (currentWeapon.combinedAttributes.projectileSpreadAttributes.spreadType == Projectile.ProjectileSpreadAttributes.SpreadType.Circular) {
							newProjectile.transform.rotation *= projectileSpreadDeviation * Quaternion.Euler(0, 0, currentWeapon.combinedAttributes.projectileSpreadAttributes.spreads[j].x) * Quaternion.Euler(currentWeapon.combinedAttributes.projectileSpreadAttributes.spreads[j].y, 0, 0);
						} else {
							newProjectile.transform.rotation *= Quaternion.Euler(currentWeapon.combinedAttributes.projectileSpreadAttributes.spreads[j].y, currentWeapon.combinedAttributes.projectileSpreadAttributes.spreads[j].x, 0);
						}
					}
					Projectile newProjectileClass = newProjectile.GetComponent<Projectile>();
					// Apply velocity
					if (currentWeapon.combinedAttributes.chargingEnabled == true) {
						newProjectileClass.velocityCurrent = newProjectile.transform.forward * (currentWeapon.combinedAttributes.projectileAttributes.velocityInitial - (currentWeapon.combinedAttributes.projectileAttributes.velocityInitial * currentWeapon.combinedAttributes.chargeInfluenceVelocity * Mathf.Abs(currentWeapon.chargeCurrent - 1)));
					} else {
						newProjectileClass.velocityCurrent = newProjectile.transform.forward * currentWeapon.combinedAttributes.projectileAttributes.velocityInitial;
					}
					newProjectileClass.projectileAttributes.deceleration = currentWeapon.combinedAttributes.projectileAttributes.deceleration;
					newProjectileClass.projectileAttributes.decelerationType = currentWeapon.combinedAttributes.projectileAttributes.decelerationType;
					newProjectileClass.projectileAttributes.gravity = currentWeapon.combinedAttributes.projectileAttributes.gravity;
					newProjectileClass.ricochetCount = currentWeapon.combinedAttributes.projectileAttributes.ricochetCountInitial;
					newProjectileClass.projectileAttributes.ricochetAngleMax = currentWeapon.combinedAttributes.projectileAttributes.ricochetAngleMax;
					newProjectileClass.projectileAttributes.damage = currentWeapon.combinedAttributes.projectileAttributes.damage;
					newProjectileClass.projectileAttributes.lifespan = currentWeapon.combinedAttributes.projectileAttributes.lifespan;
					newProjectileClass.projectileAttributes.isSticky = currentWeapon.combinedAttributes.projectileAttributes.isSticky;
					audioManager.PlayClipAtPoint(currentWeapon.soundFireNormal, currentWeapon.barrelPoint.position, 2f);

				}
			} else {
				// TODO: Dry shot
			}
			yield return new WaitForSeconds(0.01f);

			currentWeapon.muzzleFlash.gameObject.SetActive(false);

			yield return new WaitForSeconds(currentWeapon.combinedAttributes.burstDelay - 0.01f);
		}

		currentWeapon.muzzleFlash.gameObject.SetActive(false);

		if (currentWeapon.combinedAttributes.chargingEnabled == true) {
			currentWeapon.chargeCurrent = Mathf.Clamp01(currentWeapon.chargeCurrent - currentWeapon.combinedAttributes.chargeDecrementPerShot);
		}
	}

	void ThrowItem(HandInformation handInfoCurrent, GrabInformation itemGrabInfoCurrent, Vector3 velocity) {
		Item grabbedItem = itemGrabInfoCurrent.grabbedScript as Item;       // Cast the script as an Item (null if not an item)

		if (grabbedItem != null) {				// Is the grabbedScript an Item?

			itemGrabInfoCurrent.grabbedRigidbody.velocity += velocityCurrent;
			itemGrabInfoCurrent.grabbedRigidbody.useGravity = true;
			itemGrabInfoCurrent.grabbedRigidbody.ResetCenterOfMass();       // Reset the grabbedItem's center of mass

			Weapon grabbedWeapon = grabbedItem as Weapon;
			if (grabbedWeapon != null) {        // Is the grabbedItem a Weapon?
				grabbedWeapon.triggerHeld = false;	// Unhold the trigger
			}

			//		TODO: redo pocketing to act more like a cube around the item?

			// Check for pockets
			Collider[] pockets = Physics.OverlapSphere(handInfoCurrent.handRigidbody.transform.position, 0.2f, pocketMask);
			if (pockets.Length > 0) {
				List<Pocket> availablePockets = new List<Pocket>();

				// Find pockets that are currently available and add them to availablePockets list
				for (int i = 0; i < pockets.Length; i++) {
					if (pockets[i].GetComponent<Pocket>()) {
						Pocket currentPocketObject = pockets[i].GetComponent<Pocket>();
						if (currentPocketObject.GetAvailability() == true && currentPocketObject.pocketSize == grabbedItem.pocketSize && Vector3.Angle(itemGrabInfoCurrent.grabbedRigidbody.transform.forward, currentPocketObject.transform.forward) <= currentPocketObject.angleRange) {
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
					chosenPocket.PocketItem(grabbedItem);
				}
			}
		}
	}

	public IEnumerator TriggerHapticFeedback(SteamVR_Controller.Device device, ushort strength, float duration) {
		for (float i = 0; i <= duration; i += 0.01f) {
			device.TriggerHapticPulse(strength);
			yield return new WaitForSeconds(0.01f);
		}
	}

	public IEnumerator TriggerHapticFeedback(SteamVR_Controller.Device device, ushort strength, float duration, int ticks) {
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
		if (envGrabInfoLeft.grabbedTransform == null && envGrabInfoRight.grabbedTransform == null) {
			isClimbing = false;
			if (grounded == true) {
				velocityCurrent = Vector3.Lerp(velocityCurrent, new Vector3(velocityDesired.x, velocityCurrent.y, velocityDesired.z), 12.5f * Time.deltaTime * Mathf.Clamp01(groundedTime));
			} else {
				if (handInfoLeft.controllerDevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad) != Vector2.zero) {
					Vector3 normalizedVelocityDesired = new Vector3(velocityDesired.x, 0, velocityDesired.z).normalized * (Mathf.Clamp01(new Vector3(velocityDesired.x, 0, velocityDesired.z).magnitude) * Mathf.Clamp(new Vector3(velocityCurrent.x, 0, velocityCurrent.z).magnitude, moveSpeedCurrent, Mathf.Infinity));
					velocityCurrent = Vector3.Lerp(velocityCurrent, new Vector3(normalizedVelocityDesired.x, velocityCurrent.y, normalizedVelocityDesired.z), 1f * Time.deltaTime);
				}
			}
		} else {
			Vector3 combinedClimbPositions = Vector3.zero;
			Quaternion climbRotationLeft = Quaternion.Euler(0, 0, 0);
			Quaternion climbRotationRight = Quaternion.Euler(0, 0, 0);
			int climbCount = 0;

			if (envGrabInfoLeft.grabbedTransform == true) {
				climbRotationLeft = Quaternion.Inverse(envGrabInfoLeft.offsetRotation) * envGrabInfoLeft.grabbedTransform.rotation;
			}

			if (envGrabInfoRight.grabbedTransform == true) {
				climbRotationRight = Quaternion.Inverse(envGrabInfoRight.offsetRotation) * envGrabInfoRight.grabbedTransform.rotation;
			}

			if (envGrabInfoLeft.grabbedTransform == true) {
				combinedClimbPositions += (envGrabInfoLeft.grabbedTransform.position + climbRotationLeft * envGrabInfoLeft.offsetPosition) + (climbRotationLeft * envGrabInfoLeft.offsetBodyHand - (handInfoLeft.controller.transform.position - bodyCC.transform.position));
				climbCount++;
			}

			if (envGrabInfoRight.grabbedTransform == true) {
				combinedClimbPositions += (envGrabInfoRight.grabbedTransform.position + climbRotationRight * envGrabInfoRight.offsetPosition) + (climbRotationRight * envGrabInfoRight.offsetBodyHand - (handInfoRight.controller.transform.position - bodyCC.transform.position));
				climbCount++;
			}
			
			combinedClimbPositions = combinedClimbPositions / climbCount;

			velocityCurrent = Vector3.Lerp(velocityCurrent, (combinedClimbPositions - bodyCC.transform.position) / Time.deltaTime, Mathf.Clamp01(50 * Time.deltaTime));

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
		if (envGrabInfoLeft.grabbedTransform == null && envGrabInfoRight.grabbedTransform == null) { // Are we not climbing?
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
		if (envGrabInfoLeft.grabbedTransform == null && envGrabInfoRight.grabbedTransform == null) { // Are we not climbing?
			verticalPusher.transform.position += new Vector3(0, (headCC.transform.position - hmd.transform.position).y, 0); // Move the vertical pusher to accomodate for HMD moving too far vertically through geometry (ie: down into box, up into desk)
		}

		// Step 5: Move HeadCC if leaning too far
		//Debug.DrawLine(hmd.transform.position, new Vector3(bodyCC.transform.position.x, hmd.transform.position.y, bodyCC.transform.position.z), Color.red, 0, false);   // Shows a line representing lean distance drawing from the bodyCC to the headCC
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

		//Debug.DrawRay(bodyCC.transform.position, velocityCurrent, Color.green);		// Debug ray showing player velocity

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
		//Debug.DrawLine(hmd.transform.position, new Vector3(bodyCC.transform.position.x, hmd.transform.position.y, bodyCC.transform.position.z), Color.red, 0, false);   // Shows a line representing lean distance drawing from the bodyCC to the headCC
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
		// This method will get ground infromation:
			// 1. Are we touching the ground?
			// 2. What is the normal of the ground?
		
		RaycastHit hit;

		Vector3 sphereCastOrigin = bodyCC.transform.position + new Vector3(0, (-bodyCC.height / 2) + bodyCC.radius, 0);
		Vector3 sphereCastDirection = Vector3.down;
		float sphereCastRadius = bodyCC.radius - bodyCC.skinWidth;
		float sphereCastLength = 0.025f + (bodyCC.skinWidth * 2);
		
		if (isClimbing == false && Physics.SphereCast(sphereCastOrigin, sphereCastRadius, sphereCastDirection, out hit, sphereCastLength, bodyCCLayerMask) && hit.point.y < bodyCC.transform.position.y) {
			if (grounded == false) {
				float heightDifference = GetPlayerRealLifeHeight() - bodyCC.height;
				verticalPusher.transform.position += new Vector3(0, -heightDifference, 0);
				rig.transform.position += new Vector3(0, heightDifference, 0);									// TODO: yikes, should a Get method really be messing with stuff like this? and this ^
			}
			handInfoLeft.jumpLoaded = true;
			handInfoRight.jumpLoaded = true;
			velocityCurrent.y = 0;
			grounded = true;
			groundNormal = hit.normal;
		} else {
			grounded = false;
			groundNormal = Vector3.up;
		}
		
		//debugBall.transform.position = sphereCastOrigin + (sphereCastDirection * sphereCastLength);
		//debugBall1.transform.localScale = Vector3.one * sphereCastRadius * 2;

		if (grounded == true) {
			handMaterial.color = Color.green;
		} else {
			handMaterial.color = Color.red;
		}

		SetBodyHeight(bodyCC.height);
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
	
	void MoveItems (Vector3 deltaPosition) {
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
		//avatar.position = bodyCC.transform.position - new Vector3(0, bodyCC.height / 2f, 0);
		avatar.position = bodyCC.transform.position;
		avatarTorso.position = new Vector3(bodyCC.transform.position.x, headCC.transform.position.y - 0.3f, bodyCC.transform.position.z);
		Vector3 hmdFlatForward = new Vector3(hmd.transform.forward.x, 0, hmd.transform.forward.z).normalized;
		Vector3 hmdFlatUp = new Vector3(hmd.transform.up.x, 0, hmd.transform.up.z).normalized;

		Vector3 hmdFlatFinal = Vector3.Lerp(hmdFlatForward, (hmd.transform.forward.y > 0) ? -hmdFlatUp : hmdFlatUp, Mathf.Clamp01(Vector3.Angle(hmd.transform.up, Vector3.up) / 90));
		avatar.rotation = Quaternion.LookRotation(hmdFlatFinal, Vector3.up);
	}

	public void MovePlayer (Vector3 deltaPosition) {
		// This method is used as a way for other objects to move the player. (ie: elevator moving the player)
		Vector3 bodyPositionBefore = bodyCC.transform.position;
		bodyCC.Move(deltaPosition);
		Vector3 netCCMovement = (bodyCC.transform.position - bodyPositionBefore);
		rig.transform.position += netCCMovement;
		platformMovementsAppliedLastFrame += netCCMovement;
	}

	public void MovePlayerWithoutCollision (Vector3 deltaPosition) {
		bodyCC.transform.position += deltaPosition;
		headCC.transform.position += deltaPosition;
		rig.transform.position += deltaPosition;
	}

	void UpdateHandAndItemPhysics() {
		// Left Hand
		UpdateHandPhysics("Left", handInfoLeft, itemGrabInfoLeft);

		// Right Hand
		UpdateHandPhysics("Right", handInfoRight, itemGrabInfoRight);

		UpdateEnvironmentItemPhysics("Right", itemGrabInfoRight, itemGrabInfoLeft, envGrabInfoRight, envGrabInfoLeft, handInfoRight, handInfoLeft);
		UpdateEnvironmentItemPhysics("Left", itemGrabInfoLeft, itemGrabInfoRight, envGrabInfoLeft, envGrabInfoRight, handInfoLeft, handInfoRight);

		Weapon weaponLeft = null;
		Weapon weaponRight = null;

		if (itemGrabInfoLeft.grabbedScript is Weapon) {
			weaponLeft = itemGrabInfoLeft.grabbedScript as Weapon;
			weaponRight = itemGrabInfoRight.grabbedScript as Weapon;
		}

		// Item Physics
		if (itemGrabInfoLeft.grabbedRigidbody != null && itemGrabInfoLeft.grabbedRigidbody == itemGrabInfoRight.grabbedRigidbody) {
			// Physics - Dual Wielding
			Rigidbody grabbedItemDominant = (grabDualWieldDominantHand == "Left") ? itemGrabInfoLeft.grabbedRigidbody : itemGrabInfoRight.grabbedRigidbody;
			Rigidbody handDominant = (grabDualWieldDominantHand == "Left") ? handInfoLeft.handRigidbody : handInfoRight.handRigidbody;
			Vector3 dualWieldDirectionCurrent = (((grabDualWieldDominantHand == "Left") ? handInfoRight.handRigidbody.transform.position : handInfoLeft.handRigidbody.transform.position) - ((grabDualWieldDominantHand == "Left") ? handInfoLeft.handRigidbody.transform.position : handInfoRight.handRigidbody.transform.position));
			Quaternion dualWieldDirectionChangeRotation = Quaternion.FromToRotation(handDominant.transform.rotation * grabDualWieldDirection, dualWieldDirectionCurrent);
			Quaternion rotationDeltaItem = (dualWieldDirectionChangeRotation * handDominant.transform.rotation * ((grabDualWieldDominantHand == "Left") ? itemGrabInfoLeft.offsetRotation : itemGrabInfoRight.offsetRotation)) * Quaternion.Inverse(itemGrabInfoLeft.grabbedRigidbody.transform.rotation);

			float angleItem;
			Vector3 axisItem;
			rotationDeltaItem.ToAngleAxis(out angleItem, out axisItem);
			if (angleItem > 180) {
				angleItem -= 360;
			}

			itemGrabInfoLeft.grabbedRigidbody.velocity = Vector3.Lerp(itemGrabInfoLeft.grabbedRigidbody.velocity, Vector3.ClampMagnitude(((handDominant.transform.position + (dualWieldDirectionChangeRotation * handDominant.transform.rotation * ((grabDualWieldDominantHand == "Left") ? itemGrabInfoLeft.offsetPosition : itemGrabInfoRight.offsetPosition))) - grabbedItemDominant.transform.position) / Time.fixedDeltaTime, (grabbedItemDominant.GetComponent<HingeJoint>()) ? 1 : 100) * Mathf.Lerp(itemGrabInfoLeft.rigidbodyVelocityPercentage, itemGrabInfoRight.rigidbodyVelocityPercentage, 0.5f), Mathf.Clamp01(50 * Time.deltaTime));

			if (angleItem != float.NaN) {
				itemGrabInfoLeft.grabbedRigidbody.maxAngularVelocity = 100f;
				itemGrabInfoLeft.grabbedRigidbody.angularVelocity = Vector3.Lerp(itemGrabInfoLeft.grabbedRigidbody.angularVelocity, (angleItem * axisItem) * Mathf.Lerp(itemGrabInfoLeft.rigidbodyVelocityPercentage, itemGrabInfoRight.rigidbodyVelocityPercentage, 0.5f) * 1f, Mathf.Clamp01(50 * Time.deltaTime));
			}

			// Accuracy - Dual Wield
			if (itemGrabInfoLeft.grabbedScript is Weapon) {
				weaponLeft.accuracyCurrent = Mathf.Clamp(weaponLeft.accuracyCurrent + (weaponLeft.combinedAttributes.accuracyIncrement * Time.deltaTime), weaponLeft.combinedAttributes.accuracyMin, weaponLeft.combinedAttributes.accuracyMax);
			}

		} else {
			// Physics - Left
			if (itemGrabInfoLeft.grabbedScript) {
				UpdateItemPhysics("Left", itemGrabInfoLeft, itemGrabInfoRight, envGrabInfoLeft, envGrabInfoRight, handInfoLeft, handInfoRight);
				if (weaponLeft) {
					weaponLeft.accuracyCurrent = Mathf.Clamp(weaponLeft.accuracyCurrent + (weaponLeft.combinedAttributes.accuracyIncrement * Time.deltaTime), weaponLeft.combinedAttributes.accuracyMin, weaponLeft.combinedAttributes.accuracyMax);
				}
			}

			
			// Physics - Right
			if (itemGrabInfoRight.grabbedScript) {
				UpdateItemPhysics("Right", itemGrabInfoRight, itemGrabInfoLeft, envGrabInfoRight, envGrabInfoLeft, handInfoRight, handInfoLeft);
				if (weaponRight) {
					weaponRight.accuracyCurrent = Mathf.Clamp(weaponRight.accuracyCurrent + (weaponRight.combinedAttributes.accuracyIncrement * Time.deltaTime), weaponRight.combinedAttributes.accuracyMin, weaponRight.combinedAttributes.accuracyMax);
				}
			}
		}
	}

	void UpdateHandPhysics(string side, HandInformation handInfoCurrent, GrabInformation itemGrabInfoCurrent) {
		itemGrabInfoCurrent.rigidbodyVelocityPercentage = Mathf.Clamp01(itemGrabInfoCurrent.rigidbodyVelocityPercentage + Time.deltaTime * (itemGrabInfoCurrent.grabbedRigidbody != null ? 2 : -10));

		Vector3 handOffsetDefault = Quaternion.Euler(handInfoCurrent.handOffsetRotation) * handInfoCurrent.controller.transform.rotation * new Vector3(handRigidbodyPositionOffset.x * (side == "Left" ? 1 : -1), handRigidbodyPositionOffset.y, handRigidbodyPositionOffset.z);
		Vector3 handOffsetKick = handInfoCurrent.handOffsetPosition;
		handInfoCurrent.handRigidbody.velocity = (((handInfoCurrent.controller.transform.position + handOffsetDefault + handOffsetKick) - handInfoCurrent.handGameObject.transform.position) + verticalPusher.transform.localPosition) / Time.fixedDeltaTime;
		handInfoCurrent.handRigidbody.velocity = (((handInfoCurrent.controller.transform.position + handOffsetDefault + handOffsetKick) - handInfoCurrent.handGameObject.transform.position)) / Time.fixedDeltaTime;


		handInfoCurrent.handOffsetPosition = Vector3.Lerp(handInfoCurrent.handOffsetPosition, Vector3.zero, 5 * Time.deltaTime);
		handInfoCurrent.handOffsetRotation = Vector3.Lerp(handInfoCurrent.handOffsetRotation, new Vector3(0, 0, 0), 10 * Time.deltaTime);

		Quaternion rotationDelta = Quaternion.Euler(0, 0, 0);
		if (itemGrabInfoCurrent.grabbedRigidbody != null && itemGrabInfoCurrent.itemGrabNode != null && (itemGrabInfoCurrent.itemGrabNode.grabType != GrabNode.GrabType.Dynamic && itemGrabInfoCurrent.itemGrabNode.grabType != GrabNode.GrabType.FixedPosition)) {
			rotationDelta = (Quaternion.Euler(handInfoCurrent.handOffsetRotation) * Quaternion.AngleAxis(-30, handInfoCurrent.controller.transform.right) * handInfoCurrent.controller.transform.rotation * Quaternion.Euler(itemGrabInfoCurrent.itemGrabNode.rotation)) * Quaternion.Inverse(handInfoCurrent.handRigidbody.transform.rotation);
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

	void UpdateItemPhysics(string hand, GrabInformation itemGrabInfoCurrent, GrabInformation itemGrabInfoOpposite, GrabInformation envGrabInfoCurrent, GrabInformation envGrabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		if (itemGrabInfoCurrent.grabbedRigidbody.gameObject.layer == LayerMask.NameToLayer("Item")) {
			Vector3 grabOffsetCurrent = (handInfoCurrent.handRigidbody.transform.rotation * itemGrabInfoCurrent.offsetPosition);
			
			//debugBall1.transform.position = itemGrabInfoCurrent.grabbedRigidbody.transform.position + (itemGrabInfoCurrent.grabbedRigidbody.transform.rotation * itemGrabInfoCurrent.grabPoint);

			Vector3 grabWorldPosition = itemGrabInfoCurrent.grabbedRigidbody.transform.position + (itemGrabInfoCurrent.grabbedRigidbody.transform.rotation * itemGrabInfoCurrent.grabPoint);
			if (Vector3.Distance(grabWorldPosition, handInfoCurrent.handRigidbody.transform.position + grabOffsetCurrent) > 0.5f) {
				ReleaseAll(hand, itemGrabInfoCurrent, itemGrabInfoOpposite, envGrabInfoCurrent, envGrabInfoOpposite, handInfoCurrent, handInfoOpposite);		// TODO : hey this is why you fall when dropping a weapon due to distance while also climbing
			} else {
				itemGrabInfoCurrent.grabbedRigidbody.velocity = Vector3.Lerp(itemGrabInfoCurrent.grabbedRigidbody.velocity, Vector3.ClampMagnitude(((handInfoCurrent.handRigidbody.position + grabOffsetCurrent) - itemGrabInfoCurrent.grabbedRigidbody.transform.position) / Time.fixedDeltaTime, 500) * itemGrabInfoCurrent.rigidbodyVelocityPercentage, 1);
				//grabInfoCurrent.grabbedRigidbody.velocity = Vector3.Lerp(grabInfoCurrent.grabbedRigidbody.velocity, Vector3.ClampMagnitude(((handInfoCurrent.handRigidbody.transform.position + (dualWieldDirectionChangeRotation * handDominant.transform.rotation * ((grabDualWieldDominantHand == "Left") ? itemGrabInfoLeft.offsetPosition : itemGrabInfoRight.offsetPosition))) - grabbedItemDominant.transform.position) / Time.fixedDeltaTime, (grabbedItemDominant.GetComponent<HingeJoint>()) ? 1 : 100) * Mathf.Lerp(itemGrabInfoLeft.rigidbodyVelocityPercentage, itemGrabInfoRight.rigidbodyVelocityPercentage, 0.5f), Mathf.Clamp01(50 * Time.deltaTime));
				
				if (!itemGrabInfoCurrent.grabbedRigidbody.GetComponent<HingeJoint>()) {		// TODO: Do we still need this?
					Quaternion rotationDeltaItem = (handInfoCurrent.handRigidbody.transform.rotation * itemGrabInfoCurrent.offsetRotation) * Quaternion.Inverse(itemGrabInfoCurrent.grabbedRigidbody.transform.rotation);
					float angleItem;
					Vector3 axisItem;
					rotationDeltaItem.ToAngleAxis(out angleItem, out axisItem);
					if (angleItem > 180) {
						angleItem -= 360;
					}

					if (angleItem != float.NaN) {
						itemGrabInfoCurrent.grabbedRigidbody.maxAngularVelocity = Mathf.Infinity;
						itemGrabInfoCurrent.grabbedRigidbody.angularVelocity = Vector3.Lerp(itemGrabInfoCurrent.grabbedRigidbody.angularVelocity, (angleItem * axisItem) * itemGrabInfoCurrent.rigidbodyVelocityPercentage, 1);
					}
				}
			}
		}
	}

	void UpdateEnvironmentItemPhysics (string hand, GrabInformation itemGrabInfoCurrent, GrabInformation itemGrabInfoOpposite, GrabInformation envGrabInfoCurrent, GrabInformation envGrabInfoOpposite, HandInformation handInfoCurrent, HandInformation handInfoOpposite) {
		// Handles physics updates for environment items being grabbed (ie: doors, levers, etc)
		
		if (envGrabInfoCurrent.grabbedRigidbody != null) {

			Vector3 grabWorldPos = envGrabInfoCurrent.grabbedTransform.transform.position + (envGrabInfoCurrent.grabbedRigidbody.transform.rotation * envGrabInfoCurrent.offsetPosition);
			Vector3 handPos = handInfoCurrent.controller.transform.position;

			if (envGrabInfoCurrent.grabbedScript is SimpleEnvironmentItem) {       // Is the grabbedScript a simpleEnvironmentItem?
				SimpleEnvironmentItem currentHingeItem = (envGrabInfoCurrent.grabbedScript as SimpleEnvironmentItem);
				
				Vector3 velocityDesired = (handPos - grabWorldPos);
				velocityDesired = Vector3.ClampMagnitude(velocityDesired * 5000, velocityDesired.magnitude * 25000);
				velocityDesired *= Mathf.Sqrt(envGrabInfoCurrent.grabbedRigidbody.mass);

				envGrabInfoCurrent.grabbedRigidbody.AddForceAtPosition(velocityDesired, grabWorldPos);
			} else if (envGrabInfoCurrent.grabbedScript is HingeEnvironmentItem) {		// Is the grabbedScript a hingeEnvironmentItem?
				HingeEnvironmentItem currentHingeItem = (envGrabInfoCurrent.grabbedScript as HingeEnvironmentItem);

				Vector3 anchorPos = (envGrabInfoCurrent.grabbedRigidbody.transform.position + (envGrabInfoCurrent.grabbedRigidbody.transform.rotation * currentHingeItem.hingeJoint.anchor));
				Vector3 handPosOffset = handInfoCurrent.controller.transform.position - anchorPos;
				Vector3 grabWorldOffsetPos = grabWorldPos - anchorPos;
				Vector3 desiredPos = anchorPos + (Vector3.ProjectOnPlane(handPosOffset, envGrabInfoCurrent.grabbedRigidbody.transform.rotation * currentHingeItem.hingeJoint.axis).normalized * currentHingeItem.hingeMovementNormalizeDistance) + Vector3.Project(grabWorldOffsetPos, envGrabInfoCurrent.grabbedRigidbody.transform.rotation * currentHingeItem.hingeJoint.axis);

				Vector3 velocityDirection = desiredPos - grabWorldPos;
				velocityDirection *= Mathf.Sqrt(velocityDirection.magnitude);

				//debugBall1.transform.position = grabWorldPos;
				//debugBall2.transform.position = desiredPos;

				Debug.DrawRay(grabWorldPos, velocityDirection, Color.red);
			
				envGrabInfoCurrent.grabbedRigidbody.AddForceAtPosition(envGrabInfoCurrent.grabbedRigidbody.mass * velocityDirection * 200, grabWorldPos);
			} else {
				envGrabInfoCurrent.grabbedRigidbody.centerOfMass = ((envGrabInfoCurrent.grabbedRigidbody.transform.position + new Vector3(bodyCC.transform.position.x, envGrabInfoCurrent.grabbedRigidbody.transform.position.y, bodyCC.transform.position.z)) / 2) - envGrabInfoCurrent.grabbedTransform.position;
			}

			// If the distance between the grabbed object and the grabbing hand is greater than maxEnvironmentItemGrabDistance, release that item
			if (envGrabInfoCurrent.grabType != GrabType.ClimbablePhysics && Vector3.Distance(grabWorldPos, handPos) > maxEnvironmentItemGrabDistance) {
				ReleaseEnvironment(hand, envGrabInfoCurrent, envGrabInfoOpposite, handInfoCurrent, handInfoOpposite);
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