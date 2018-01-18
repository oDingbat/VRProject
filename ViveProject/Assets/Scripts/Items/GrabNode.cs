using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GrabNode : MonoBehaviour {

	[Space(10)][Header("Item Reference")]
	public Item						item;						// The item this grabNode is associated with
	public List<GrabNode>			grabNodeChildren;			// The grabNodes attached to this grabNode
	public GrabNode					grabNodeParent;

	[Space(10)][Header("Settings")]
	public Vector3			rotation;
	public Vector3			offset;
	public GrabNode			referralNode;
	public int				dominance;
	public bool				grabThisNodeFirst;		// Should the player grab this grabNode rather than it's attached grabNodes (ie: yes for the trigger handle, so we grab the firing mechanism; no for body parts of weapons with grips attached)
	
	[Space(10)][Header("Variables")]
	public bool interactionOn;

	[Space(10)][Header("Enums")]
	public GrabType			grabType = GrabType.Dynamic;
	public enum				GrabType { FixedPositionRotation, FixedPosition, Dynamic, Referral, PocketOnly }
	public TriggerType		triggerType;
	public enum				TriggerType { None, Fire }
	public InteractionType	interactionType;
	public enum				InteractionType { Toggle, Hold }

	// Events
	public event Action<bool> eventTriggerInteraction;

	void Start () {
		item = transform.parent.parent.GetComponent<Item>();
		TriggerInteraction(interactionOn);
	}

	void OnDrawGizmosSelected() {
		if (referralNode == null) {
			GrabNode colNode = GetComponent<GrabNode>();
			if (grabType == GrabType.FixedPositionRotation) {
				Gizmos.color = Color.magenta;
				Gizmos.DrawWireCube(transform.position + transform.parent.rotation * offset, Vector3.one * 0.05f);
				Debug.DrawRay(transform.position + transform.parent.rotation * offset, Quaternion.Euler(colNode.rotation) * transform.parent.forward * 0.075f, Color.cyan, 0);
				Debug.DrawRay(transform.position + transform.parent.rotation * offset, Quaternion.Euler(colNode.rotation) * transform.parent.up * 0.075f, Color.green, 0);
			} else if (grabType == GrabType.FixedPosition) {
				Gizmos.color = Color.magenta;
				Gizmos.DrawWireSphere(transform.position + transform.parent.rotation * offset, 0.05f);
				Debug.DrawRay(transform.position + offset, Quaternion.Euler(colNode.rotation) * transform.parent.forward * 0.075f, Color.cyan, 0);
				Debug.DrawRay(transform.position + offset, Quaternion.Euler(colNode.rotation) * transform.parent.up * 0.075f, Color.green, 0);
			} else if (grabType == GrabType.Dynamic) {
				Gizmos.color = Color.cyan;
				Gizmos.DrawWireSphere(transform.position + transform.parent.rotation * offset, 0.05f);
			}
		} else {
			Debug.DrawLine(transform.position + transform.parent.rotation * offset, referralNode.transform.position + transform.parent.rotation * referralNode.offset, Color.red, 0);
		}
	}

	public void TriggerInteraction (bool interactionState) {
		interactionOn = interactionState;
		
		if (eventTriggerInteraction != null) {
			eventTriggerInteraction.Invoke(interactionState);
		}
	}

}
