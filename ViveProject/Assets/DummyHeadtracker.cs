using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Entity))]
public class DummyHeadtracker : MonoBehaviour {

	public Entity entity;

	public GameObject head;
	public GameObject forwardDirector;

	Rigidbody headRigidbody;
	Quaternion headInitialRotation;
	
	public float headTurnAngleMax;

	float headTurnStrength = 1f;
	
	void Start () {
		entity = GetComponent<Entity>();
		headRigidbody = head.GetComponent<Rigidbody>();
		headInitialRotation = head.transform.localRotation;
	}

	void Update () {
		if (entity.focusedMonobehaviour != null && entity.vitals.stunCurrent < entity.vitals.stunThreshold) {
			HeadTrack();
			
		}
	}

	void HeadTrack() {
		Vector3 focusPosition = Vector3.zero;

		if (entity.focusedMonobehaviour is Entity) {
			focusPosition = (entity.focusedMonobehaviour as Entity).head.transform.position;
		} else if (entity.focusedMonobehaviour is Item) {
			focusPosition = entity.focusedMonobehaviour.transform.position;
		}

		

		Vector3 headToTrackingDirection = (focusPosition - head.transform.position).normalized;
		Quaternion rotationDesired = Quaternion.LookRotation(headToTrackingDirection, Vector3.up) * headInitialRotation;

		float rotationDistance = Vector3.Angle(forwardDirector.transform.forward, headToTrackingDirection);
		rotationDistance = Mathf.Abs((rotationDistance / 180) - 1);

		headTurnStrength = Mathf.Lerp(headTurnStrength, rotationDistance, 5f * Time.deltaTime);

		if (rotationDistance >= headTurnAngleMax) {
			Debug.Log("Yup");
			Quaternion rotationDeltaItem = rotationDesired * Quaternion.Inverse(head.transform.rotation);
			float angleItem;
			Vector3 axisItem;
			rotationDeltaItem.ToAngleAxis(out angleItem, out axisItem);
			if (angleItem > 180) {
				angleItem -= 360;
			}

			if (angleItem != float.NaN) {
				//headRigidbody.maxAngularVelocity = Mathf.Infinity;
				headRigidbody.angularVelocity = Vector3.Lerp(headRigidbody.angularVelocity, (angleItem * axisItem) * headTurnStrength * 0.5f, Mathf.Clamp01(50 * Time.deltaTime));
			}
		}
	}

}
