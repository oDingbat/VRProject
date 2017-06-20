using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using Valve.VR;

public class HolographicSight : MonoBehaviour {

	public LayerMask hitMask;
	public float scale = 1;

	public Transform dotLeft;
	public Transform dotRight;

	void Update () {
		if (transform.parent.Find("(Barrel Point)") != null) {
			Transform barrel = transform.parent.Find("(Barrel Point)");

			Vector3 left = Quaternion.Inverse(InputTracking.GetLocalRotation(VRNode.LeftEye)) * InputTracking.GetLocalPosition(VRNode.LeftEye);
			Vector3 right = Quaternion.Inverse(InputTracking.GetLocalRotation(VRNode.RightEye)) * InputTracking.GetLocalPosition(VRNode.RightEye);
			Vector3 leftWorld, rightWorld;
			Vector3 offset = (left - right) * 0.5f;

			Matrix4x4 m = Camera.main.cameraToWorldMatrix;
			leftWorld = m.MultiplyPoint(-offset);
			rightWorld = m.MultiplyPoint(offset);


			RaycastHit hitLeft;
			if (Physics.Raycast(leftWorld, (barrel.transform.position + barrel.transform.forward * 1000) - leftWorld, out hitLeft, Mathf.Infinity, hitMask)) {
				dotLeft.transform.position = hitLeft.point;
				dotLeft.gameObject.SetActive(true);
			} else {
				dotLeft.gameObject.SetActive(false);
			}

			dotLeft.localScale = Vector3.one * Mathf.Sqrt(Vector3.Distance(leftWorld, dotLeft.position)) * 0.1f * scale;
			dotLeft.rotation = transform.rotation;

			RaycastHit hitRight;
			if (Physics.Raycast(rightWorld, (barrel.transform.position + barrel.transform.forward * 1000) - rightWorld, out hitRight, Mathf.Infinity, hitMask)) {
				dotRight.transform.position = hitRight.point;
				dotRight.gameObject.SetActive(true);
			} else {
				dotRight.gameObject.SetActive(false);
			}

			dotRight.localScale = Vector3.one * Mathf.Sqrt(Vector3.Distance(rightWorld, dotRight.position)) * 0.1f * scale;
			dotRight.rotation = transform.rotation;
		}
	}

}
