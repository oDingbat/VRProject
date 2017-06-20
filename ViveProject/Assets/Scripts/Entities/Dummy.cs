using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent (typeof (Entity))]
public class Dummy : MonoBehaviour {

	Rigidbody thisRigidbody;
	public NavMeshAgent navMeshAgent;
	Entity entity;

	float speed = 3.5f;
	bool isAirbourne;

	public Material deadMaterial;

	void Start () {
		entity = GetComponent<Entity>();
		thisRigidbody = GetComponent<Rigidbody>();
		entity.eventDie += Die;
	}

	void Update () {
		if (entity.vitals.isDead == false) {
			if (isAirbourne == false) {
				Debug.Log(navMeshAgent.path.corners.Length);
				Vector3 desiredVelocity = (navMeshAgent.path.corners[1] - transform.position).normalized * speed;
				desiredVelocity.y = thisRigidbody.velocity.y;

				thisRigidbody.velocity = Vector3.Lerp(thisRigidbody.velocity, desiredVelocity, 5f * Time.deltaTime);
			}
		}
	}

	void OnCollisionStay (Collision col) {
		isAirbourne = false;
	}

	void OnCollisionExit (Collision col) {
		isAirbourne = true;
	}

	void Die () {
		GetComponent<MeshRenderer>().material = deadMaterial;
	}
	
}
