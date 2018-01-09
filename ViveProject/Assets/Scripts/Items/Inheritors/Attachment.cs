using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Attachment : Item {

	[Space(10)][Header("Accuracy Info")]
	public bool isAttached;			// Is this attachment currently attached to another item

	public void Update () {
		if (itemRigidbody != null) {
			if (itemRigidbody.useGravity == false || itemRigidbody.velocity.magnitude > 0) {

			}
		}
	}

	public override string GetItemType() {
		return "Attachment";
	}

}
