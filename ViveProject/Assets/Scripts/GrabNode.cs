using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabNode : MonoBehaviour {

	public Vector3 rotation;
	public Vector3 offset;
	public GrabNode referralNode;



	void OnDrawGizmosSelected() {
		if (referralNode == null) {
			GrabNode colNode = GetComponent<GrabNode>();
			Gizmos.color = Color.red;
			Gizmos.DrawWireCube(transform.position + transform.parent.rotation * offset, Vector3.one * 0.05f);
			Debug.DrawRay(transform.position + transform.parent.rotation * offset, Quaternion.Euler(colNode.rotation) * transform.parent.forward * 0.075f, Color.cyan, 0);
			Debug.DrawRay(transform.position + transform.parent.rotation * offset, Quaternion.Euler(colNode.rotation) * transform.parent.up * 0.075f, Color.green, 0);
		} else {
			Debug.DrawLine(transform.position + transform.parent.rotation * offset, referralNode.transform.position + transform.parent.rotation * referralNode.offset, Color.red, 0);
		}
	}

}
