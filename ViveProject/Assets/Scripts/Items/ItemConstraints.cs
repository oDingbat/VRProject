using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemConstraints : MonoBehaviour {

	public Collider colliderContainer;
	public GameObject constrainedRotation;

	void Update () {
		if (colliderContainer != null) {
			//Vector3 closestPointInContainer = colliderContainer.ClosestPoint(transform.position);
			Vector3 closestPointInContainer = colliderContainer.ClosestPoint(transform.position);
			if (Vector3.Distance(closestPointInContainer, transform.position) > 0.001f) {
				transform.GetComponent<Rigidbody>().velocity = Vector3.zero;
			}
			transform.position = closestPointInContainer;
		}

		if (constrainedRotation != null) {
			transform.rotation = constrainedRotation.transform.rotation;
		}
	}
}
