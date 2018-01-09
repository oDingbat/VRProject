using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RigidbodyCopy {

	public float					mass;
	public float					drag;
	public float					angularDrag;
	public bool						useGravity;
	public bool						isKinematic;
	public RigidbodyInterpolation	interpolation;
	public CollisionDetectionMode	collisionDetectionMode;
	public RigidbodyConstraints		constraints;

	public RigidbodyCopy (Rigidbody copiedRigidbody) {
		mass = copiedRigidbody.mass;
		drag = copiedRigidbody.drag;
		angularDrag = copiedRigidbody.angularDrag;
		useGravity = copiedRigidbody.useGravity;
		isKinematic = copiedRigidbody.isKinematic;
		interpolation = copiedRigidbody.interpolation;
		collisionDetectionMode = copiedRigidbody.collisionDetectionMode;
		constraints = copiedRigidbody.constraints;
	}

	public RigidbodyCopy () {

	}

}
