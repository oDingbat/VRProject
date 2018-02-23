using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleEnvironmentItem : EnvironmentItem {

	void Start () {
		rigidbody = GetComponent<Rigidbody>();
	}

	public override void OnGrab(Player player, string handSide) {
		isGrabbed = true;
		playerGrabbing = player;
		handGrabbingSide = handSide;
	}

	public override void OnRelease() {
		isGrabbed = false;
		playerGrabbing = null;
		handGrabbingSide = "";
	}

}
