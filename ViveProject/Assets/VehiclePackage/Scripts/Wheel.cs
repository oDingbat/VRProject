using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(WheelCollider))]
public class Wheel : MonoBehaviour {

	public WheelCollider wheelCollider;

	public Transform suspension;
	public Transform hinge;
	public Transform wheel;

	public Quaternion initialRotHinge;
	public Quaternion initialRotWheel;

	void Start () {
		wheelCollider = GetComponent<WheelCollider>();

		Transform[] allChildren = GetComponentsInChildren<Transform>();
		foreach (Transform child in allChildren) {
			if (child.name.Contains("Suspension")) {
				suspension = child;
			} else if (child.name.Contains("Hinge")) {
				hinge = child;
				initialRotHinge = hinge.rotation;
			} else if (child.name.Contains("Wheel")) {
				wheel = child;
				initialRotWheel = wheel.rotation;
			}
		}
	}

	void Update () {
		if (suspension) {
			Vector3 pos;
			Quaternion rot;
			wheelCollider.GetWorldPose(out pos, out rot);

			suspension.transform.parent.position = pos;

			if (hinge) {
				hinge.rotation = initialRotHinge * Quaternion.Euler(0, wheelCollider.steerAngle, 0);
			}

			if (wheel) {
				wheel.rotation = rot * initialRotWheel;
			}
		}
	}

}
