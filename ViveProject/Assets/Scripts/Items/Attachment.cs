using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Attachment : Item {



	public void Update () {
		if (itemRigidbody.useGravity == false || itemRigidbody.velocity.magnitude > 0) {

		}
	}

	public override string GetItemType() {
		return "Attachment";
	}

}
