using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class InteractionSubscriber : MonoBehaviour {

	public GrabNode connectedGrabNode;

	protected void BaseStart () {
		if (connectedGrabNode) {
			connectedGrabNode.eventTriggerInteraction += UpdateInteraction;
		}
	}

	protected virtual void UpdateInteraction (bool interactionState) { }


}
