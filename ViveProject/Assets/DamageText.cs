using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageText : MonoBehaviour {

	Transform playerHead;
	public Vector3 velocity;

	void Start () {
		playerHead = GameObject.Find("Camera (both) (eye)").transform;
		StartCoroutine(SelfDestruct());
	}

	IEnumerator SelfDestruct () {
		yield return new WaitForSeconds(0.625f);
		Destroy(gameObject);
	}


	
	void Update () {
		transform.rotation = Quaternion.LookRotation(transform.position - playerHead.transform.position, Vector3.up);
		transform.position += velocity * Time.deltaTime;
		velocity += (Vector3.down * 9.81f * Time.deltaTime);
	}
}
