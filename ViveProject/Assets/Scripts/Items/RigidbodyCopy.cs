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

	public static void SetRigidbodyValues (Rigidbody rigidbody, RigidbodyCopy rigidbodyCopy) {
		rigidbody.mass = rigidbodyCopy.mass;
		rigidbody.drag = rigidbodyCopy.drag;
		rigidbody.angularDrag = rigidbodyCopy.angularDrag;
		rigidbody.useGravity = rigidbodyCopy.useGravity;
		rigidbody.isKinematic = rigidbodyCopy.isKinematic;
		rigidbody.interpolation = rigidbodyCopy.interpolation;
		rigidbody.collisionDetectionMode = rigidbodyCopy.collisionDetectionMode;
		rigidbody.constraints = rigidbodyCopy.constraints;
	}

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
