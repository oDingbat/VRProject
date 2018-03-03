using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeSetupScript : MonoBehaviour {

	public Transform rope0;
	public BoxCollider ropeCollider;
	public Rigidbody ropeRigidbody;
	public HingeJoint ropeHingeJoint;

	public bool start;

	void OnDrawGizmosSelected() {
		if (start == true && rope0 != null) {
			start = false;

			ropeCollider = rope0.GetComponent<BoxCollider>();
			ropeRigidbody = rope0.GetComponent<Rigidbody>();
			ropeHingeJoint = rope0.GetComponent<HingeJoint>();


			Transform ropeLayerCurrent = rope0.GetChild(0);
			Rigidbody ropeRigidbodyLast = rope0.GetComponent<Rigidbody>();

			while (true) {
				ropeLayerCurrent.parent = rope0.parent;

				BoxCollider newBoxCollider = ropeLayerCurrent.gameObject.AddComponent<BoxCollider>();
				newBoxCollider.size = ropeCollider.size;
				newBoxCollider.center = ropeCollider.center;

				Rigidbody newRigidbody = ropeLayerCurrent.gameObject.AddComponent<Rigidbody>();
				newRigidbody.mass = ropeRigidbody.mass;
				newRigidbody.drag = ropeRigidbody.drag;
				newRigidbody.angularDrag = ropeRigidbody.angularDrag;
				newRigidbody.interpolation = ropeRigidbody.interpolation;
				newRigidbody.collisionDetectionMode = ropeRigidbody.collisionDetectionMode;
				newRigidbody.useGravity = ropeRigidbody.useGravity;

				HingeJoint newHingeJoint = ropeLayerCurrent.gameObject.AddComponent<HingeJoint>();
				newHingeJoint.connectedBody = ropeRigidbodyLast;
				newHingeJoint.anchor = ropeHingeJoint.anchor;
				newHingeJoint.connectedAnchor = ropeHingeJoint.connectedAnchor;

				if (ropeLayerCurrent.childCount > 0) {
					ropeRigidbodyLast = newRigidbody;
					ropeLayerCurrent = ropeLayerCurrent.GetChild(0);
				} else {
					break;
				}
			}

		}
	}

}
