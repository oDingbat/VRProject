using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Attachment : Item {

	[Space(10)][Header("Attachment Info")]
	public bool						isAttached;					// Is this attachment currently attached to another item
	public float					timeAttached;
	public Quaternion				desiredRotation;
	public Vector3					desiredPosition;

	[Space(10)][Header("Attachment Attributes")]
	public bool attributesAlwaysPassive;							// Make it so that attributesActive are ignored and only attributesPassive are ever used (ie: barrel attachments, firerateMods etc)
	public Weapon.WeaponAttributes attachmentAttributesPassive;		// attachment attributes which are applied to a weapon passively
	public Weapon.WeaponAttributes attachmentAttributesActive;      // attachment attributes which are applied to a weapon actively (when grabbed; ie: for a grip, this attributes are applied while holding the grip)


	public void Update() {
		if (itemRigidbody != null) {
			if (itemRigidbody.useGravity == false || itemRigidbody.velocity.magnitude > 0) {
				// ?
			}
		} else {
			float lerpCalculation = Mathf.Clamp01((Time.timeSinceLevelLoad - timeAttached) * 200 * Time.deltaTime);
			transform.localPosition = Vector3.Lerp(transform.localPosition, desiredPosition, lerpCalculation);
			transform.localRotation = Quaternion.Lerp(transform.localRotation, desiredRotation, lerpCalculation);
		}
	}

	public override string GetItemType() {
		return "Attachment";
	}

}
