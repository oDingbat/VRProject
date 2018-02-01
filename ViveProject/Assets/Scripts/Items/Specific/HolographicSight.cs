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
		if (transform.parent && transform.parent.parent && transform.parent.parent && transform.parent.parent.parent && transform.parent.parent.parent.GetComponent<Weapon>() != null) {
			Transform barrel = transform.parent.parent.parent.GetComponent<Weapon>().barrelPoint;

			Vector3 left = Quaternion.Inverse(UnityEngine.XR.InputTracking.GetLocalRotation(UnityEngine.XR.XRNode.LeftEye)) * UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.LeftEye);
			Vector3 right = Quaternion.Inverse(UnityEngine.XR.InputTracking.GetLocalRotation(UnityEngine.XR.XRNode.RightEye)) * UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.RightEye);
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

			Debug.DrawLine(leftWorld, (barrel.transform.position + barrel.transform.forward * 1000), Color.red);

			dotLeft.localScale = Vector3.one * Mathf.Sqrt(Vector3.Distance(leftWorld, dotLeft.position)) * 0.1f * scale;
			dotLeft.rotation = transform.rotation;

			RaycastHit hitRight;
			if (Physics.Raycast(rightWorld, (barrel.transform.position + barrel.transform.forward * 1000) - rightWorld, out hitRight, Mathf.Infinity, hitMask)) {
				dotRight.transform.position = hitRight.point;
				dotRight.gameObject.SetActive(true);
			} else {
				dotRight.gameObject.SetActive(false);
			}

			Debug.DrawLine(rightWorld, (barrel.transform.position + barrel.transform.forward * 1000), Color.blue);

			dotRight.localScale = Vector3.one * Mathf.Sqrt(Vector3.Distance(rightWorld, dotRight.position)) * 0.1f * scale;
			dotRight.rotation = transform.rotation;
		}
	}

}
