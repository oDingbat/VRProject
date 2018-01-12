using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Attachment : Item {

	[Space(10)][Header("Attachment Info")]
	public bool						isAttached;					// Is this attachment currently attached to another item

	[Space(10)][Header("Attachment Attributes")]
	public bool attributesAlwaysPassive;							// Make it so that attributesActive are ignored and only attributesPassive are ever used (ie: barrel attachments, firerateMods etc)
	public Weapon.WeaponAttributes attachmentAttributesPassive;		// attachment attributes which are applied to a weapon passively
	public Weapon.WeaponAttributes attachmentAttributesActive;		// attachment attributes which are applied to a weapon actively (when grabbed; ie: for a grip, this attributes are applied while holding the grip)


	public void Update () {
		if (itemRigidbody != null) {
			if (itemRigidbody.useGravity == false || itemRigidbody.velocity.magnitude > 0) {
				// ?
			}
		}
	}

	public override string GetItemType() {
		return "Attachment";
	}

}
