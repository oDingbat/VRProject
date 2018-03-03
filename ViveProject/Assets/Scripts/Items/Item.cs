using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class Item : MonoBehaviour {

	[Space(10)][Header("Information")]
	public string				itemName;
	public bool					nonDualWieldable;
	public bool					isGrabbed;								// Is this item currently being grabbed?
	public float				timeLastGrabbed;						// The time at which the item was last grabbed
	public Vector3				initialCenterOfMass;

	[Space(10)][Header("Pocketing Info")]
	public GrabNode				pocketGrabNode;
	public Pocket				pocketCurrent;
	public Pocket.PocketSize	pocketSize = Pocket.PocketSize.Small;
	public Transform			pocketingModel;
	public Transform			pocketCandidate;
	public float				pocketCandidateTime;
	public Material				pocketingModelMaterialPrivate;
	public Quaternion			pocketingModelRotationOffset;

	[Space(10)][Header("Rigidbody Information")]
	public Rigidbody			itemRigidbody;
	public RigidbodyCopy		rigidbodyCopy;


	[Space(10)][Header("Attachments")]
	public List<Item>			attachments = new List<Item>();

	AudioSource audioSourceHit;
	AudioSource audioSourceMove;

	public List<AttachmentNode> attachmentNodes;

	void Start () {
		itemRigidbody = GetComponent<Rigidbody>();
		rigidbodyCopy = new RigidbodyCopy(itemRigidbody);
		initialCenterOfMass = itemRigidbody.centerOfMass;

		// Create list of attachmentNodes
		if (transform.Find("(AttachmentNodes")) {
			foreach (Transform childAttachmentNode in transform.Find("(AttachmentNodes)")) {
				if (childAttachmentNode.GetComponent<AttachmentNode>()) {
					attachmentNodes.Add(childAttachmentNode.GetComponent<AttachmentNode>());
				}
			}
		}

		if (transform.Find("(PocketingModel)")) {
			pocketingModel = transform.Find("(PocketingModel)");

			if (pocketingModel) {
				Transform[] modelChildren = pocketingModel.gameObject.GetComponentsInChildren<Transform>();
				foreach (Transform model in modelChildren) {
					if (model.GetComponent<Renderer>() != null) {
						if (pocketingModelMaterialPrivate == null) {
							pocketingModelMaterialPrivate = new Material(model.GetComponent<Renderer>().material);
						}
						model.GetComponent<Renderer>().material = pocketingModelMaterialPrivate;
					}
				}
			}
		} else {
			Debug.LogWarning("No pocketingModel found!");
		}
	}

	void OnCollisionEnter (Collision collision) {
		if (audioSourceHit) {
			if (collision.relativeVelocity.magnitude > 2) {
				audioSourceHit.volume = collision.relativeVelocity.magnitude * 0.1f;
				audioSourceHit.Play();
			}
		}
	}

	void FixedUpdate () {
		UpdatePocketingModel();
	}

	void Update () {
		OnItemUpdate();
	}

	void UpdatePocketingModel () {
		if (pocketingModel) {		// Does this item currently have a pocketingModel?
			if (pocketCandidate) {	// Does this item currently have a candidate for a pocketing position
				// Set Pocketing Model Color
				if (Time.timeSinceLevelLoad - pocketCandidateTime < 0.1f) {
					pocketingModelMaterialPrivate.color = Color.Lerp(pocketingModelMaterialPrivate.color, new Color(pocketingModelMaterialPrivate.color.r, pocketingModelMaterialPrivate.color.g, pocketingModelMaterialPrivate.color.b, 0.2f), Mathf.Clamp01(Time.deltaTime * 25));
				} else {
					pocketingModelMaterialPrivate.color = Color.Lerp(pocketingModelMaterialPrivate.color, new Color(pocketingModelMaterialPrivate.color.r, pocketingModelMaterialPrivate.color.g, pocketingModelMaterialPrivate.color.b, 0.0f), Mathf.Clamp01(Time.deltaTime * 25));
				}
			} else {
				if (pocketingModelMaterialPrivate) {
					pocketingModelMaterialPrivate.color = Color.Lerp(pocketingModelMaterialPrivate.color, new Color(pocketingModelMaterialPrivate.color.r, pocketingModelMaterialPrivate.color.g, pocketingModelMaterialPrivate.color.b, 0.0f), Mathf.Clamp01(Time.deltaTime * 25));
				}
			}
		}
	}

	protected virtual void OnItemUpdate () { }

	public abstract string GetItemType();

}
