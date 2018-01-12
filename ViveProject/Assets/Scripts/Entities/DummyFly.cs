using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent (typeof (Entity))]
public class DummyFly : MonoBehaviour {

	Rigidbody thisRigidbody;
	public NavMeshAgent navMeshAgent;
	Entity entity;

	public LayerMask visionMask;

	float speed = 4f;

	public Material matDead;
	public Material matIdle;
	public Material matAgro;

	void Start () {
		entity = GetComponent<Entity>();
		thisRigidbody = GetComponent<Rigidbody>();
		entity.eventDie += Die;
	}

	void Update () {
		if (entity.vitals.isDead == false) {
			Vector3 desiredPos = navMeshAgent.path.corners[1];

			RaycastHit hitTarget;
			if (Physics.Raycast(transform.position, entity.target.position - transform.position, out hitTarget, Mathf.Infinity, visionMask)) {
				if (hitTarget.transform.gameObject.layer == LayerMask.NameToLayer("Player")) {
					desiredPos = hitTarget.point;
				}
			}

			if (hitTarget.transform != null || hitTarget.transform.gameObject.layer != LayerMask.NameToLayer("Player")) {
				RaycastHit hitCeiling;
				if (Physics.Raycast(desiredPos, Vector3.up, out hitCeiling, Mathf.Infinity, visionMask)) {
					if (hitCeiling.transform != null) {
						desiredPos = (desiredPos + hitCeiling.point + new Vector3(0, -1, 0)) / 2;
					}
				}
			}

			Vector3 desiredVelocity = (desiredPos - transform.position).normalized * speed;
			desiredVelocity.y = thisRigidbody.velocity.y;
		
			thisRigidbody.velocity = Vector3.Lerp(thisRigidbody.velocity, desiredVelocity, 2.5f * Time.deltaTime);
		}
	}

	void OnCollisionStay (Collision col) {
		
	}

	void OnCollisionExit (Collision col) {
		
	}

	void Die () {
		GetComponent<MeshRenderer>().material = matDead;
	}
	
}
