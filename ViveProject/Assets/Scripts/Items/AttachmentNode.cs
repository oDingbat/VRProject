using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttachmentNode : MonoBehaviour {

	[Space(10)][Header("Item Reference")]
	public Item						item;						// The item this attachmentNode is associated with

	[Space(10)][Header("Enums")]
	public AttachmentType			attachmentType;
	public enum AttachmentType		{ Barrel, Square, Circle, Triangle }
	public AttachmentGender			attachmentGender;
	public enum AttachmentGender	{ Male, Female }

	[Space(10)][Header("Variables")]
	public bool						isAttached;
	public AttachmentNode			connectedNode;

	void Start() {
		item = transform.parent.parent.GetComponent<Item>();
	}

	void OnDrawGizmosSelected() {
		// The purpose of this method is to show a visual representation of the attachmentNode's values in the editor
		Color baseColor = Color.white;

		// Set Male/Female specific values
		if (attachmentGender == AttachmentGender.Male) {
			baseColor = Color.red;
			Debug.DrawLine(transform.position, transform.position + (transform.forward * 0.01f), baseColor);
		} else {
			baseColor = Color.cyan;
			Debug.DrawLine(transform.position, transform.position + (transform.forward * -0.01f), baseColor);
		}

		// Set attachment specific values
		switch (attachmentType) {
			case AttachmentType.Barrel: {
				Create2DPolygon(transform.position, 6, baseColor);
			} break; 
			case AttachmentType.Square: {
				Create2DPolygon(transform.position, 4, baseColor, 45);
			} break;
			case AttachmentType.Circle: {
				Create2DPolygon(transform.position, 16, baseColor);
			} break;
			case AttachmentType.Triangle: {
				Create2DPolygon(transform.position, 3, baseColor);
			} break;
		}
	}

	void Create2DPolygon (Vector3 basePosition, int sides, Color baseColor) {
		float degreeIncrement = 360 / (float)sides;
		for (int j = 0; j < 2; j++) {
			for (float a = 0; a < sides; a++) {
				Quaternion rot1 = Quaternion.AngleAxis((degreeIncrement * a), transform.forward);
				Quaternion rot2 = Quaternion.AngleAxis((degreeIncrement * (a + 1)), transform.forward);
				Debug.DrawLine(basePosition + (rot1 * transform.up) * (j == 0 ? 0.01f : 0.005f), basePosition + (rot2 * transform.up) * (j == 0 ? 0.01f : 0.005f), baseColor);
			}
		}
	}

	void Create2DPolygon(Vector3 basePosition, int sides, Color baseColor, float rotationOffset) {
		float degreeIncrement = 360 / (float)sides;
		for (int j = 0; j < 2; j++) {
			for (float a = 0; a < sides; a++) {
				Quaternion rot1 = Quaternion.AngleAxis((degreeIncrement * a) + rotationOffset, transform.forward);
				Quaternion rot2 = Quaternion.AngleAxis((degreeIncrement * (a + 1)) + rotationOffset, transform.forward);
				Debug.DrawLine(basePosition + (rot1 * transform.up) * (j == 0 ? 0.01f : 0.005f), basePosition + (rot2 * transform.up) * (j == 0 ? 0.01f : 0.005f), baseColor);
			}
		}
	}

	void OnTriggerEnter(Collider col) {
		if (col.transform.gameObject.tag == "AttachmentNode") {
			if (col.transform.gameObject.GetComponent<AttachmentNode>()) {

			}
		}
	}

}
