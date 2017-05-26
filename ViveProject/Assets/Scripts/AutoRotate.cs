using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoRotate : MonoBehaviour {

	public float spinSpeed = 1f;

	void Update () {
		GetComponent<Rigidbody>().angularVelocity = new Vector3(0, spinSpeed, 0);
	}

}
