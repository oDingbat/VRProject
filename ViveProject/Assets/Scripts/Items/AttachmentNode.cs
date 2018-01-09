using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttachmentNode : MonoBehaviour {

	[Space(10)][Header("Enums")]
	public AttachmentType attachmentType;
	public enum AttachmentType { Square, Circle, Triangle, Hexagon }
	public AttachmentGender attachmentGender;
	public enum AttachmentGender { Male, Female }

	[Space(10)][Header("Variables")]
	public bool isAttached;
	public AttachmentNode connectedNode;

	void OnDrawGizmosSelected() {
		if (attachmentType == AttachmentType.Square) {
			if (attachmentGender == AttachmentGender.Male) {
				Gizmos.color = Color.red;
				Debug.DrawLine(transform.position, transform.position + (transform.forward * 0.01f), Color.red);
				Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
				Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.02f, 0.02f, 0.00f));
				Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.01f, 0.01f, 0.00f));
			} else {
				Gizmos.color = Color.cyan;
				Debug.DrawLine(transform.position, transform.position + (transform.forward * 0.01f), Color.cyan);
				Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
				Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.02f, 0.02f, 0.00f));
				Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.01f, 0.01f, 0.00f));
			}
		} else if (attachmentType == AttachmentType.Circle) {
			Color currentColor = Color.white;
			Vector3 basePos = Vector3.zero;

			if (attachmentGender == AttachmentGender.Male) {
				currentColor = Color.red;
				basePos = transform.position + transform.forward * 0.01f;
				Debug.DrawLine(transform.position + (transform.forward * 0.01f), transform.position + (transform.forward * 0.035f), Color.red);
			} else {
				currentColor = Color.cyan;
				basePos = transform.position - transform.forward * 0.01f;
				Debug.DrawLine(transform.position + (transform.forward * 0.015f), transform.position + (transform.forward * -0.01f), Color.cyan);
			}
			float circleVerts = 16;
			float degreeIncrement = 360 / circleVerts;
			for (int j = 0; j < 2; j++) {
				for (float a = 0; a < circleVerts; a++) {
					Quaternion rot1 = Quaternion.AngleAxis((degreeIncrement * a), transform.forward);
					Quaternion rot2 = Quaternion.AngleAxis((degreeIncrement * (a + 1)), transform.forward);
					Debug.DrawLine(basePos + (rot1 * transform.up) * (j == 0 ? 0.01f : 0.005f), basePos + (rot2 * transform.up) * (j == 0 ? 0.01f : 0.005f), currentColor);
				}
			}
		} else if (attachmentType == AttachmentType.Triangle) {
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(transform.position, 0.05f);
		}
	}

	void OnTriggerEnter(Collider col) {
		if (col.transform.gameObject.tag == "AttachmentNode") {
			if (col.transform.gameObject.GetComponent<AttachmentNode>()) {

			}
		}
	}

}
