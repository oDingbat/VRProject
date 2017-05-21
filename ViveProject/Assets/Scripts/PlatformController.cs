using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : MonoBehaviour {

	public Vector3[] movePoints;
	public int pointIndex;

	public GameObject	platformGameObject;
	public Rigidbody platformRigidbody;
	public bool pausing = false;

	void Start () {
		platformRigidbody = platformGameObject.GetComponent<Rigidbody>();
	}

	void Update () {
		if (movePoints.Length > 1) {
			platformGameObject.GetComponent<Rigidbody>();
			platformRigidbody.velocity = Vector3.Lerp(platformRigidbody.velocity, ((movePoints[pointIndex] + transform.position) - platformGameObject.transform.position), 5 * Time.deltaTime);
			platformRigidbody.velocity = platformRigidbody.velocity.normalized * Mathf.Clamp(platformRigidbody.velocity.magnitude, 0, 5);
			if (pausing == false && Vector3.Distance(platformGameObject.transform.position, movePoints[pointIndex] + transform.position) < 0.5f) {
				StartCoroutine(PauseThenMove(3));
			}
		}
	}

	IEnumerator PauseThenMove (float delayTime) {
		pausing = true;
		yield return new WaitForSeconds(delayTime);
		pointIndex = (pointIndex == movePoints.Length - 1 ? 0 : pointIndex + 1);
		pausing = false;
	}

	void OnDrawGizmosSelected() {
		Gizmos.color = Color.red;
		for (int i = 0; i < movePoints.Length; i++) {
			Gizmos.DrawWireMesh(platformGameObject.GetComponent<MeshFilter>().mesh, transform.position + movePoints[i], platformGameObject.transform.rotation, platformGameObject.transform.localScale);
		}
	}

}
