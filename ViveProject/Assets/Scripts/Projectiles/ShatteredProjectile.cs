using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShatteredProjectile : MonoBehaviour {

	void Start () {
		StartCoroutine(SelfDestruct());
	}
	
	IEnumerator SelfDestruct () {
		yield return new WaitForSeconds(2.5f);
		Destroy(gameObject);
	}

}
