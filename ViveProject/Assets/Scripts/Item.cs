using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour {

	void OnDrawGizmosSelected() {
		Transform colliders = transform.Find("(Colliders)");
		if (colliders != null) {
			foreach (Transform col in colliders) {
				GrabNode colNode = col.GetComponent<GrabNode>();
				if (colNode) {
					if (colNode.referralNode == null) {
						Gizmos.color = Color.red;
						Gizmos.DrawWireCube(col.position + transform.rotation * colNode.offset, Vector3.one * 0.05f);
						Debug.DrawRay(col.position + transform.rotation * colNode.offset, Quaternion.Euler(colNode.rotation) * transform.forward * 0.075f, Color.cyan, 0);
						Debug.DrawRay(col.position + transform.rotation * colNode.offset, Quaternion.Euler(colNode.rotation) * transform.up * 0.075f, Color.green, 0);
					} else {
						Debug.DrawLine(col.position + transform.rotation * colNode.offset, colNode.referralNode.transform.position + transform.rotation * colNode.referralNode.offset, Color.red, 0);
					}
				}
			}
		}
	}

}
