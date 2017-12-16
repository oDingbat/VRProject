using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : MonoBehaviour {

	public LayerMask		playerLayerMask;

	public Vector3[]		movePoints;
	public int				pointIndex;

	public Vector3			playerCollisionBox;

	public GameObject		platformGameObject;
	public Rigidbody		platformRigidbody;
	public bool				pausing = false;
	public float			speed = 5;
	bool					playerHitLastFrame;

	void Start () {
		platformRigidbody = platformGameObject.GetComponent<Rigidbody>();
	}

	void FixedUpdate () {
		if (movePoints.Length > 1) {
			platformGameObject.GetComponent<Rigidbody>();
			platformRigidbody.velocity = Vector3.Lerp(platformRigidbody.velocity, ((movePoints[pointIndex] + transform.position) - platformGameObject.transform.position), 5 * Time.fixedDeltaTime);
			platformRigidbody.velocity = platformRigidbody.velocity.normalized * Mathf.Clamp(platformRigidbody.velocity.magnitude, 0, speed);
			if (pausing == false && Vector3.Distance(platformGameObject.transform.position, movePoints[pointIndex] + transform.position) < 0.5f) {
				StartCoroutine(PauseThenMove(3));
			}

			Collider[] playerColliders = Physics.OverlapBox(platformGameObject.transform.position, playerCollisionBox * 0.5f, platformGameObject.transform.rotation, playerLayerMask);

			if (playerColliders.Length == 0) {
				if (playerHitLastFrame == true) {
					playerHitLastFrame = false;
					GameObject.Find("Player Body").GetComponent<Player>().velocityCurrent += platformRigidbody.velocity;
				}
			} else {
				foreach (Collider hit in playerColliders) {
					GameObject.Find("Player Body").GetComponent<Player>().MovePlayer(platformRigidbody.velocity * Time.fixedDeltaTime);
					playerHitLastFrame = true;
					break;
				}
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
			Gizmos.DrawWireMesh(platformGameObject.GetComponent<MeshFilter>().mesh, platformGameObject.transform.position + movePoints[i], platformGameObject.transform.rotation, platformGameObject.transform.localScale);
		}

		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube(platformGameObject.transform.position, playerCollisionBox);
	}

}
