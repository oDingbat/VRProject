using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShatteredProjectilePiece : MonoBehaviour {

	public float velocityMagnitude;
	public Vector3 velocityVector;

	public Rigidbody rigidbody;
	public Collider collider;

	void Start() {
		if (rigidbody != null) {
			rigidbody.velocity += transform.rotation * Quaternion.Euler(velocityVector) * (transform.forward * velocityMagnitude);
			collider.enabled = true;
		}
	}

	void OnDrawGizmosSelected() {
		Debug.DrawRay(transform.position, transform.rotation * Quaternion.Euler(velocityVector) * ((transform.forward * velocityMagnitude) / 10));
	}

}
